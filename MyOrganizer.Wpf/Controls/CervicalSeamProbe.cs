using System.Globalization;
using System.IO;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Read-only probe of the crown CEJ rim vs the red (Occlusal) cervical band.
/// Does not mutate meshes or SurfaceMaps.
/// </summary>
internal static class CervicalSeamProbe
{
    public static void DiagnoseFrozen36And46()
    {
        Probe("36", "FDI36_High.obj", "FDI36SurfaceMap.json", ToothSide.Left);
        Probe("46", "FDI46_High.obj", "FDI46SurfaceMap.json", ToothSide.Right);
    }

    private static void Probe(string fdi, string objName, string jsonName, ToothSide laterality)
    {
        var obj = Find(Path.Combine("Assets", "Teeth", "Source", objName))
                  ?? throw new FileNotFoundException(objName);
        var json = Find(Path.Combine("Assets", "Teeth", jsonName))
                   ?? throw new FileNotFoundException(jsonName);
        using var fs = File.OpenRead(obj);
        var parts = StlToothLoader.LoadAlignedParts(fs, out _, MandibularFirstMolarTemplate.LoadOptions(laterality));
        var nTri = parts.Crown.TriangleIndices.Count / 3;
        var labels = ReadLabels(json, nTri);
        var red = new List<int>();
        for (var t = 0; t < labels.Length; t++)
        {
            if (labels[t] == ClinicalSurface.Occlusal)
                red.Add(t);
        }
        CrownSurfaceClassifier.OverlayMesh(parts.Crown, red, 0.0009);
        Console.WriteLine(fdi + " " + Json(fdi, parts.Crown, labels));
    }

    public static string Json(string fdi, MeshGeometry3D crown, ClinicalSurface[] labels)
    {
        var idx = crown.TriangleIndices;
        var nTri = labels.Length;
        var border = OpenBoundary(crown, nTri);
        var nBorder = 0;
        var nBorderRed = 0;
        var nBorderNotRed = 0;
        var notRedHist = new int[5];
        var minZ = double.PositiveInfinity;
        var maxZ = double.NegativeInfinity;
        var z = new double[nTri];
        var nrmOut = 0;
        var nrmIn = 0;
        for (var t = 0; t < nTri; t++)
        {
            var a = crown.Positions[idx[t * 3]];
            var b = crown.Positions[idx[t * 3 + 1]];
            var c = crown.Positions[idx[t * 3 + 2]];
            var cx = (a.X + b.X + c.X) / 3.0;
            var cy = (a.Y + b.Y + c.Y) / 3.0;
            var cz = (a.Z + b.Z + c.Z) / 3.0;
            z[t] = cz;
            minZ = Math.Min(minZ, cz);
            maxZ = Math.Max(maxZ, cz);
            var face = Vector3D.CrossProduct(b - a, c - a);
            var outward = cx * face.X + cy * face.Y + cz * face.Z;
            if (labels[t] == ClinicalSurface.Occlusal)
            {
                if (outward >= 0) nrmOut++;
                else nrmIn++;
            }
        }
        var zSpan = Math.Max(1e-9, maxZ - minZ);
        var neighbors = CrownSurfaceClassifier.BuildNeighbors(idx, nTri);
        var nSkirt = 0;
        var skirtMinZ01 = 1d;
        var skirtMaxZ01 = 0d;
        var skirtHist = new int[5];
        for (var t = 0; t < nTri; t++)
        {
            var z01 = (z[t] - minZ) / zSpan;
            if (border[t])
            {
                nBorder++;
                if (labels[t] == ClinicalSurface.Occlusal)
                    nBorderRed++;
                else
                {
                    nBorderNotRed++;
                    notRedHist[(int)labels[t]]++;
                }
            }
            if (labels[t] == ClinicalSurface.Occlusal) continue;
            var nextToRed = false;
            foreach (var nb in neighbors[t])
            {
                if (labels[nb] == ClinicalSurface.Occlusal)
                {
                    nextToRed = true;
                    break;
                }
            }
            if (!nextToRed) continue;
            if (z01 > 0.38) continue;
            nSkirt++;
            skirtMinZ01 = Math.Min(skirtMinZ01, z01);
            skirtMaxZ01 = Math.Max(skirtMaxZ01, z01);
            skirtHist[(int)labels[t]]++;
        }

        var own = ToothSurfaceTopology.ValidateOwnership(labels);
        return "{\"fdi\":\"" + fdi +
               "\",\"nTri\":" + nTri +
               ",\"nBorder\":" + nBorder +
               ",\"nBorderRed\":" + nBorderRed +
               ",\"nBorderNotRed\":" + nBorderNotRed +
               ",\"borderNotRedB\":" + notRedHist[1] +
               ",\"borderNotRedL\":" + notRedHist[2] +
               ",\"borderNotRedM\":" + notRedHist[3] +
               ",\"borderNotRedD\":" + notRedHist[4] +
               ",\"nLowSkirtNonRed\":" + nSkirt +
               ",\"skirtMinZ01\":" + F(nSkirt == 0 ? 0 : skirtMinZ01) +
               ",\"skirtMaxZ01\":" + F(nSkirt == 0 ? 0 : skirtMaxZ01) +
               ",\"skirtB\":" + skirtHist[1] +
               ",\"skirtL\":" + skirtHist[2] +
               ",\"skirtM\":" + skirtHist[3] +
               ",\"skirtD\":" + skirtHist[4] +
               ",\"redNrmOut\":" + nrmOut +
               ",\"redNrmIn\":" + nrmIn +
               ",\"dup\":" + own.Dup +
               ",\"unassigned\":" + own.Unassigned + "}";
    }



    private static bool[] OpenBoundary(MeshGeometry3D crown, int nTri)
    {
        var idx = crown.TriangleIndices;
        var count = new Dictionary<long, int>();
        void Add(int a, int b)
        {
            var lo = Math.Min(a, b);
            var hi = Math.Max(a, b);
            var key = ((long)lo << 32) | (uint)hi;
            count[key] = count.TryGetValue(key, out var n) ? n + 1 : 1;
        }
        for (var t = 0; t < nTri; t++)
        {
            Add(idx[t * 3], idx[t * 3 + 1]);
            Add(idx[t * 3 + 1], idx[t * 3 + 2]);
            Add(idx[t * 3 + 2], idx[t * 3]);
        }
        var border = new bool[nTri];
        bool Open(int a, int b)
        {
            var lo = Math.Min(a, b);
            var hi = Math.Max(a, b);
            var key = ((long)lo << 32) | (uint)hi;
            return count.TryGetValue(key, out var n) && n == 1;
        }
        for (var t = 0; t < nTri; t++)
        {
            var a = idx[t * 3];
            var b = idx[t * 3 + 1];
            var c = idx[t * 3 + 2];
            if (Open(a, b) || Open(b, c) || Open(c, a))
                border[t] = true;
        }
        return border;
    }

    private static ClinicalSurface[] ReadLabels(string path, int nTri)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        var text = doc.RootElement.GetProperty("labels").GetString()
                   ?? throw new InvalidDataException(path);
        if (text.Length != nTri)
            throw new InvalidDataException(path + " label count " + text.Length + " != " + nTri);
        var labels = new ClinicalSurface[nTri];
        for (var i = 0; i < nTri; i++)
            labels[i] = (ClinicalSurface)(text[i] - '0');
        return labels;
    }

    private static string? Find(string relative)
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var a = Path.Combine(dir, "MyOrganizer.Wpf", relative);
            var b = Path.Combine(dir, relative);
            if (File.Exists(a)) return a;
            if (File.Exists(b)) return b;
            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }
        return null;
    }

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
