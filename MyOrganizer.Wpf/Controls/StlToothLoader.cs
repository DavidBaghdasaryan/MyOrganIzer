using System.Globalization;
using System.IO;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

internal sealed class StlMeshStats
{
    public string Header = "";
    public int TriangleCount;
    public int VertexCount;
    public double Dx, Dy, Dz;
    public double XyAspect;
    public double CrownRadius;
    public double RootRadius;
    public double OcclusalRelief;
    public string CrownAxis = "";
    public bool LoadFailed;
    public string Error = "";
    public string Format = "";
    public string SourcePath = "";
    public bool Mirrored;
    public bool FlippedX;
    public double YawDeg;
    public int RootClusters;
    public string Palatal = "";
    public string Mb = "";
    public string Db = "";
    public string SplitSource = "";
    public int CrownTriangles;
    public int RootTriangles;
    public int PolypaintColors;
    public int OcclusalRootLeakFixed;
    public double CrownMeanZ;
    public double RootMeanZ;
}

internal sealed class ToothMeshParts
{
    public MeshGeometry3D Crown { get; init; } = new();
    public MeshGeometry3D Root { get; init; } = new();
}

internal sealed class MeshLoadOptions
{
    public bool MirrorX { get; init; }
    public bool OrientFdi16 { get; init; } = true;
}

internal static class StlToothLoader
{
    public static ToothMeshParts LoadAlignedParts(Stream stream, out StlMeshStats stats, MeshLoadOptions? options = null)
    {
        options ??= new MeshLoadOptions();
        stats = new StlMeshStats();
        var raw = Read(stream);
        stats.Header = raw.Header.Trim('\0', ' ', '\t');
        stats.Format = raw.Format;
        stats.PolypaintColors = raw.PolypaintColors;
        stats.SplitSource = raw.SplitSource;
        stats.TriangleCount = raw.Indices.Count / 3;

        var welded = Weld(raw.Positions, raw.Indices);
        stats.VertexCount = welded.Positions.Count;

        AlignCrownUp(welded, stats);
        if (options.OrientFdi16)
            ToothMeshOrient.AlignFdi16(welded, stats);
        if (options.MirrorX && stats.RootClusters < 3)
        {
            MirrorX(welded);
            stats.Mirrored = true;
        }
        UniformScale(welded, stats, 2.2);

        var triMat = raw.TriMat;
        if (triMat.Count != stats.TriangleCount)
            triMat = Enumerable.Repeat((byte)255, stats.TriangleCount).ToList();
        if (stats.SplitSource != "zbrush-mrgb")
        {
            ClassifyByCervix(welded, triMat);
            stats.SplitSource = "spatial-cej";
        }
        stats.OcclusalRootLeakFixed = 0;

        var parts = SplitByMaterial(welded, triMat);
        stats.CrownTriangles = parts.Crown.TriangleIndices.Count / 3;
        stats.RootTriangles = parts.Root.TriangleIndices.Count / 3;
        stats.TriangleCount = stats.CrownTriangles + stats.RootTriangles;
        stats.CrownMeanZ = MeanZ(parts.Crown);
        stats.RootMeanZ = MeanZ(parts.Root);
        return parts;
    }

    private sealed class RawMesh
    {
        public List<Point3D> Positions = new();
        public List<int> Indices = new();
        public List<byte> TriMat = new();
        public string Header = "";
        public string Format = "";
        public int PolypaintColors;
        public string SplitSource = "none";
    }

    private static RawMesh Read(Stream stream)
    {
        if (!stream.CanSeek)
        {
            var copy = new MemoryStream();
            stream.CopyTo(copy);
            copy.Position = 0;
            stream = copy;
        }

        var headerBytes = new byte[88];
        var n = stream.Read(headerBytes, 0, headerBytes.Length);
        stream.Position = 0;
        var header = System.Text.Encoding.ASCII.GetString(headerBytes, 0, n);

        if (n >= 2 && headerBytes[0] == 0x50 && headerBytes[1] == 0x4B)
            return ReadZip(stream);
        if (LooksObj(header))
            return ReadObj(stream);

        using var reader = new BinaryReader(stream);
        var stlHeader = reader.ReadBytes(80);
        var stlHeaderText = System.Text.Encoding.ASCII.GetString(stlHeader);
        if (stlHeaderText.StartsWith("solid", StringComparison.OrdinalIgnoreCase) &&
            !LooksBinary(stream.Length, stlHeader))
        {
            stream.Position = 0;
            return ReadAscii(stream);
        }

        var count = reader.ReadUInt32();
        var raw = new RawMesh { Header = stlHeaderText, Format = "stl-binary", SplitSource = "spatial-cej" };
        for (var i = 0; i < count; i++)
        {
            reader.ReadSingle(); reader.ReadSingle(); reader.ReadSingle();
            for (var v = 0; v < 3; v++)
            {
                raw.Indices.Add(raw.Positions.Count);
                raw.Positions.Add(new Point3D(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            }
            reader.ReadUInt16();
            raw.TriMat.Add(255);
        }
        return raw;
    }

    private static bool LooksBinary(long length, byte[] header)
    {
        if (length < 84) return false;
        var n = BitConverter.ToUInt32(header, 0);
        // triangle count is after the header; size check uses file length
        return (length - 84) % 50 == 0;
    }

    private static RawMesh ReadAscii(Stream stream)
    {
        using var text = new StreamReader(stream);
        var body = text.ReadToEnd();
        var raw = new RawMesh { Header = "solid ascii", Format = "stl-ascii", SplitSource = "spatial-cej" };
        foreach (var line in body.Split('\n'))
        {
            var t = line.Trim();
            if (!t.StartsWith("vertex", StringComparison.OrdinalIgnoreCase))
                continue;
            var parts = t.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) continue;
            raw.Indices.Add(raw.Positions.Count);
            raw.Positions.Add(new Point3D(Num(parts[1]), Num(parts[2]), Num(parts[3])));
            if (raw.Indices.Count % 3 == 0)
                raw.TriMat.Add(255);
        }
        return raw;
    }

    private static bool LooksObj(string header)
    {
        var t = header.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        return t.StartsWith('#') ||
               t.StartsWith("v ", StringComparison.Ordinal) ||
               t.StartsWith("vn ", StringComparison.Ordinal) ||
               t.StartsWith("vt ", StringComparison.Ordinal) ||
               t.StartsWith("o ", StringComparison.Ordinal) ||
               t.StartsWith("g ", StringComparison.Ordinal) ||
               t.StartsWith("mtllib", StringComparison.OrdinalIgnoreCase);
    }

    private static RawMesh ReadZip(Stream stream)
    {
        using var zip = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true);
        var entry = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                    ?? zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".stl", StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            var nested = zip.Entries.FirstOrDefault(e =>
                e.FullName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                e.FullName.Contains("source", StringComparison.OrdinalIgnoreCase))
                ?? zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
            if (nested is null)
                throw new InvalidDataException("zip-has-no-obj-or-stl");
            using var nestedStream = nested.Open();
            var nestedMs = new MemoryStream();
            nestedStream.CopyTo(nestedMs);
            nestedMs.Position = 0;
            return Read(nestedMs);
        }
        using var inner = entry.Open();
        var ms = new MemoryStream();
        inner.CopyTo(ms);
        ms.Position = 0;
        var result = Read(ms);
        result.Header = entry.FullName;
        result.Format += "+zip";
        return result;
    }

    private static RawMesh ReadObj(Stream stream)
    {
        using var text = new StreamReader(stream);
        var verts = new List<Point3D>();
        var colors = new List<(byte R, byte G, byte B)>();
        var faces = new List<int[]>();
        string? line;
        while ((line = text.ReadLine()) is not null)
        {
            if (line.StartsWith("#MRGB", StringComparison.Ordinal))
            {
                var hex = line.Length > 6 ? line[6..].Trim() : "";
                for (var i = 0; i + 8 <= hex.Length; i += 8)
                {
                    var tok = hex.AsSpan(i, 8);
                    colors.Add((
                        ParseHex(tok[2], tok[3]),
                        ParseHex(tok[4], tok[5]),
                        ParseHex(tok[6], tok[7])));
                }
                continue;
            }
            if (line.Length < 2) continue;
            if (line[0] == 'v' && (line[1] == ' ' || line[1] == '\t'))
            {
                var parts = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4) continue;
                verts.Add(new Point3D(Num(parts[1]), Num(parts[2]), Num(parts[3])));
                continue;
            }
            if (line[0] != 'f' || (line[1] != ' ' && line[1] != '\t'))
                continue;
            var face = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (face.Length < 4) continue;
            var corners = new int[face.Length - 1];
            for (var i = 1; i < face.Length; i++)
            {
                var token = face[i];
                var slash = token.IndexOf('/');
                var idxToken = slash < 0 ? token : token[..slash];
                if (!int.TryParse(idxToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var vi))
                    continue;
                if (vi < 0) vi = verts.Count + vi + 1;
                corners[i - 1] = vi - 1;
            }
            faces.Add(corners);
        }

        var painted = colors.Count == verts.Count && verts.Count > 0;
        var raw = new RawMesh
        {
            Header = "obj " + verts.Count + " verts",
            Format = "obj",
            PolypaintColors = colors.Count,
            SplitSource = painted ? "zbrush-mrgb" : "spatial-cej"
        };
        foreach (var corners in faces)
        {
            for (var i = 1; i + 1 < corners.Length; i++)
            {
                var a = corners[0];
                var b = corners[i];
                var c = corners[i + 1];
                if (a < 0 || b < 0 || c < 0 || a >= verts.Count || b >= verts.Count || c >= verts.Count)
                    continue;
                raw.Indices.Add(raw.Positions.Count); raw.Positions.Add(verts[a]);
                raw.Indices.Add(raw.Positions.Count); raw.Positions.Add(verts[b]);
                raw.Indices.Add(raw.Positions.Count); raw.Positions.Add(verts[c]);
                raw.TriMat.Add(painted ? ClassifyPaint(colors[a], colors[b], colors[c]) : (byte)255);
            }
        }
        return raw;
    }

    private static byte ClassifyPaint((byte R, byte G, byte B) a, (byte R, byte G, byte B) b, (byte R, byte G, byte B) c)
    {
        var warm = (a.R - a.B) + (b.R - b.B) + (c.R - c.B);
        return warm >= 90 ? (byte)1 : (byte)0;
    }

    private static byte ParseHex(char hi, char lo)
    {
        static int N(char ch) =>
            ch is >= '0' and <= '9' ? ch - '0' :
            ch is >= 'a' and <= 'f' ? ch - 'a' + 10 :
            ch is >= 'A' and <= 'F' ? ch - 'A' + 10 : 0;
        return (byte)((N(hi) << 4) | N(lo));
    }

    private static void MirrorX(MeshGeometry3D mesh)
    {
        var pts = mesh.Positions;
        for (var i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            pts[i] = new Point3D(-p.X, p.Y, p.Z);
        }
        var idx = mesh.TriangleIndices;
        for (var i = 0; i + 2 < idx.Count; i += 3)
            (idx[i + 1], idx[i + 2]) = (idx[i + 2], idx[i + 1]);
    }

    private static void UniformScale(MeshGeometry3D mesh, StlMeshStats stats, double targetSpan)
    {
        var span = Math.Max(stats.Dz, Math.Max(stats.Dx, stats.Dy));
        if (span < 1e-9) return;
        var s = targetSpan / span;
        var pts = mesh.Positions;
        for (var i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            pts[i] = new Point3D(p.X * s, p.Y * s, p.Z * s);
        }
        stats.Dx *= s;
        stats.Dy *= s;
        stats.Dz *= s;
        stats.CrownRadius *= s;
        stats.RootRadius *= s;
    }

    private static MeshGeometry3D Weld(List<Point3D> positions, List<int> indices)
    {
        var map = new Dictionary<(int, int, int), int>();
        var mesh = new MeshGeometry3D();
        var remap = new int[positions.Count];
        const double q = 1e5;
        for (var i = 0; i < positions.Count; i++)
        {
            var p = positions[i];
            var key = ((int)Math.Round(p.X * q), (int)Math.Round(p.Y * q), (int)Math.Round(p.Z * q));
            if (!map.TryGetValue(key, out var idx))
            {
                idx = mesh.Positions.Count;
                map[key] = idx;
                mesh.Positions.Add(p);
            }
            remap[i] = idx;
        }
        foreach (var i in indices)
            mesh.TriangleIndices.Add(remap[i]);
        return mesh;
    }

    private static void AlignCrownUp(MeshGeometry3D mesh, StlMeshStats stats)
    {
        var pts = mesh.Positions;
        var n = pts.Count;
        if (n == 0) return;

        Bounds(pts, out var min, out var max, out var c);
        stats.Dx = max.X - min.X;
        stats.Dy = max.Y - min.Y;
        stats.Dz = max.Z - min.Z;
        var axis = LongestAxis(stats.Dx, stats.Dy, stats.Dz);
        stats.CrownAxis = axis;

        for (var i = 0; i < n; i++)
            pts[i] = MapLongAxisToZ(pts[i], c, axis);

        Bounds(pts, out min, out max, out c);
        var zSpan = Math.Max(1e-9, max.Z - min.Z);
        var hi = min.Z + 0.80 * zSpan;
        var lo = min.Z + 0.20 * zSpan;
        stats.CrownRadius = MeanRadius(pts, hi, true);
        stats.RootRadius = MeanRadius(pts, lo, false);
        var flip = stats.RootRadius > stats.CrownRadius;
        if (flip)
        {
            (stats.CrownRadius, stats.RootRadius) = (stats.RootRadius, stats.CrownRadius);
            for (var i = 0; i < n; i++)
            {
                var p = pts[i];
                pts[i] = new Point3D(p.X, p.Y, -p.Z);
            }
        }

        Bounds(pts, out min, out max, out c);
        for (var i = 0; i < n; i++)
        {
            var p = pts[i];
            pts[i] = new Point3D(p.X - c.X, p.Y - c.Y, p.Z - c.Z);
        }
        Bounds(pts, out min, out max, out _);
        stats.Dx = max.X - min.X;
        stats.Dy = max.Y - min.Y;
        stats.Dz = max.Z - min.Z;
        stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;

        var zCut = max.Z - stats.Dz * 0.12;
        var occlusalZ = new List<double>();
        foreach (var p in pts)
        {
            if (p.Z >= zCut)
                occlusalZ.Add(p.Z);
        }
        if (occlusalZ.Count > 8)
        {
            var mean = occlusalZ.Average();
            var variance = occlusalZ.Average(z => (z - mean) * (z - mean));
            stats.OcclusalRelief = Math.Sqrt(variance) / Math.Max(1e-9, stats.Dz);
        }
    }

    private static void Bounds(Point3DCollection pts, out Point3D min, out Point3D max, out Point3D centroid)
    {
        var minX = double.PositiveInfinity;
        var minY = minX;
        var minZ = minX;
        var maxX = double.NegativeInfinity;
        var maxY = maxX;
        var maxZ = maxX;
        var sx = 0d;
        var sy = 0d;
        var sz = 0d;
        foreach (var p in pts)
        {
            minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
            minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
            minZ = Math.Min(minZ, p.Z); maxZ = Math.Max(maxZ, p.Z);
            sx += p.X; sy += p.Y; sz += p.Z;
        }
        var n = Math.Max(1, pts.Count);
        min = new Point3D(minX, minY, minZ);
        max = new Point3D(maxX, maxY, maxZ);
        centroid = new Point3D(sx / n, sy / n, sz / n);
    }

    private static Point3D MapLongAxisToZ(Point3D p, Point3D c, string axis) => axis switch
    {
        "X" => new Point3D(p.Y - c.Y, p.Z - c.Z, p.X - c.X),
        "Y" => new Point3D(p.Z - c.Z, p.X - c.X, p.Y - c.Y),
        _ => new Point3D(p.X - c.X, p.Y - c.Y, p.Z - c.Z)
    };

    private static string LongestAxis(double dx, double dy, double dz)
    {
        if (dx >= dy && dx >= dz) return "X";
        if (dy >= dx && dy >= dz) return "Y";
        return "Z";
    }

    private static double MeanRadius(Point3DCollection pts, double cut, bool upper)
    {
        var sum = 0d;
        var c = 0;
        foreach (var p in pts)
        {
            var ok = upper ? p.Z >= cut : p.Z <= cut;
            if (!ok) continue;
            sum += Math.Sqrt(p.X * p.X + p.Y * p.Y);
            c++;
        }
        return c == 0 ? 0 : sum / c;
    }

    private static void ClassifyByCervix(MeshGeometry3D mesh, List<byte> triMat)
    {
        Bounds(mesh.Positions, out var min, out var max, out _);
        var cut = min.Z + 0.62 * Math.Max(1e-9, max.Z - min.Z);
        var idx = mesh.TriangleIndices;
        for (var t = 0; t < triMat.Count; t++)
        {
            var i = t * 3;
            if (i + 2 >= idx.Count) break;
            var z = (mesh.Positions[idx[i]].Z + mesh.Positions[idx[i + 1]].Z + mesh.Positions[idx[i + 2]].Z) / 3.0;
            triMat[t] = z >= cut ? (byte)0 : (byte)1;
        }
    }

    private static ToothMeshParts SplitByMaterial(MeshGeometry3D src, List<byte> triMat)
    {
        var crownIdx = new List<int>();
        var rootIdx = new List<int>();
        var idx = src.TriangleIndices;
        for (var t = 0; t < triMat.Count; t++)
        {
            var i = t * 3;
            if (i + 2 >= idx.Count) break;
            var dest = triMat[t] == 1 ? rootIdx : crownIdx;
            dest.Add(idx[i]);
            dest.Add(idx[i + 1]);
            dest.Add(idx[i + 2]);
        }
        return new ToothMeshParts
        {
            Crown = Extract(src, crownIdx),
            Root = Extract(src, rootIdx)
        };
    }

    private static MeshGeometry3D Extract(MeshGeometry3D src, List<int> triIndices)
    {
        var mesh = new MeshGeometry3D();
        if (triIndices.Count == 0)
        {
            mesh.Freeze();
            return mesh;
        }
        var map = new Dictionary<int, int>();
        foreach (var old in triIndices)
        {
            if (!map.TryGetValue(old, out var neu))
            {
                neu = mesh.Positions.Count;
                map[old] = neu;
                mesh.Positions.Add(src.Positions[old]);
            }
            mesh.TriangleIndices.Add(neu);
        }
        ComputeNormals(mesh);
        mesh.Freeze();
        return mesh;
    }

    private static double MeanZ(MeshGeometry3D mesh)
    {
        if (mesh.Positions.Count == 0) return 0;
        var s = 0d;
        foreach (var p in mesh.Positions)
            s += p.Z;
        return s / mesh.Positions.Count;
    }

    private static void ComputeNormals(MeshGeometry3D mesh)
    {
        var n = mesh.Positions.Count;
        var acc = new Vector3D[n];
        var idx = mesh.TriangleIndices;
        for (var i = 0; i + 2 < idx.Count; i += 3)
        {
            var a = mesh.Positions[idx[i]];
            var b = mesh.Positions[idx[i + 1]];
            var c = mesh.Positions[idx[i + 2]];
            var nrm = Vector3D.CrossProduct(b - a, c - a);
            if (nrm.LengthSquared < 1e-18) continue;
            nrm.Normalize();
            acc[idx[i]] += nrm;
            acc[idx[i + 1]] += nrm;
            acc[idx[i + 2]] += nrm;
        }
        mesh.Normals.Clear();
        foreach (var v in acc)
        {
            var nrm = v;
            if (nrm.LengthSquared < 1e-18)
                nrm = new Vector3D(0, 0, 1);
            else
                nrm.Normalize();
            mesh.Normals.Add(nrm);
        }
    }

    private static double Num(string s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : 0;
}
