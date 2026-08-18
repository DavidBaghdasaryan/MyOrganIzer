using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Canonical FDI 16 axes after the Dundee left molar is crown-up:
/// +Z occlusal, +Y buccal, −Y palatal, +X mesial, −X distal.
/// Do not change this mapping without an anatomical review.
/// </summary>
internal static class ToothMeshOrient
{
    public static void AlignFdi16(MeshGeometry3D mesh, StlMeshStats stats)
    {
        var pts = mesh.Positions;
        if (pts.Count == 0) return;

        Bounds(pts, out var min, out var max);
        var zSpan = Math.Max(1e-9, max.Z - min.Z);
        var apicalCut = min.Z + 0.18 * zSpan;
        var apical = new List<Point3D>();
        foreach (var p in pts)
        {
            if (p.Z <= apicalCut)
                apical.Add(p);
        }

        var clusters = ClusterXy(apical, 3);
        stats.RootClusters = clusters.Count;
        if (clusters.Count < 3)
            return;

        var cents = clusters.Select(Centroid).ToArray();
        var pal = PalatalIndex(cents);
        var buccal = new[] { 0, 1, 2 }.Where(i => i != pal).ToArray();
        var mb = Area(clusters[buccal[0]]) >= Area(clusters[buccal[1]]) ? buccal[0] : buccal[1];
        var db = buccal[0] == mb ? buccal[1] : buccal[0];

        stats.Palatal = Fmt(cents[pal]);
        stats.Mb = Fmt(cents[mb]);
        stats.Db = Fmt(cents[db]);

        var palatal = new Vector3D(cents[pal].X, cents[pal].Y, 0);
        if (palatal.LengthSquared < 1e-12)
            return;
        palatal.Normalize();
        // Rotate palatal into -Y.
        var yaw = Math.Atan2(-palatal.X, -palatal.Y);
        RotateZ(pts, yaw);
        stats.YawDeg = yaw * 180.0 / Math.PI;

        var palAfter = RotateVec(cents[pal], yaw);
        var mbAfter = RotateVec(cents[mb], yaw);
        var dbAfter = RotateVec(cents[db], yaw);
        if (mbAfter.X < 0)
        {
            MirrorXKeepWinding(pts, mesh.TriangleIndices);
            stats.FlippedX = true;
            mbAfter = new Point3D(-mbAfter.X, mbAfter.Y, mbAfter.Z);
            palAfter = new Point3D(-palAfter.X, palAfter.Y, palAfter.Z);
            dbAfter = new Point3D(-dbAfter.X, dbAfter.Y, dbAfter.Z);
        }

        Recenter(pts);
        Bounds(pts, out min, out max);
        stats.Dx = max.X - min.X;
        stats.Dy = max.Y - min.Y;
        stats.Dz = max.Z - min.Z;
        stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;
        stats.Palatal = Fmt(palAfter);
        stats.Mb = Fmt(mbAfter);
        stats.Db = Fmt(dbAfter);
    }

    /// <summary>
    /// Canonical FDI 36 axes after crown-up:
    /// +Z occlusal, +Y buccal, −Y lingual, +X mesial, −X distal.
    /// Uses two mandibular roots (larger = mesial) and taller occlusal cusps as lingual.
    /// Do not call AlignFdi16 for this mesh.
    /// </summary>
    public static void AlignFdi36(MeshGeometry3D mesh, StlMeshStats stats)
    {
        var pts = mesh.Positions;
        if (pts.Count == 0) return;

        Bounds(pts, out var min, out var max);
        var zSpan = Math.Max(1e-9, max.Z - min.Z);
        var apicalCut = min.Z + 0.18 * zSpan;
        var apical = new List<Point3D>();
        foreach (var p in pts)
        {
            if (p.Z <= apicalCut)
                apical.Add(p);
        }

        var clusters = ClusterXy(apical, 2);
        stats.RootClusters = clusters.Count;
        var md = new Vector3D(1, 0, 0);
        if (clusters.Count >= 2)
        {
            var cents = clusters.Select(Centroid).ToArray();
            var mesialI = Area(clusters[0]) >= Area(clusters[1]) ? 0 : 1;
            var distalI = mesialI == 0 ? 1 : 0;
            stats.Mb = Fmt(cents[mesialI]);
            stats.Db = Fmt(cents[distalI]);
            md = new Vector3D(cents[mesialI].X - cents[distalI].X, cents[mesialI].Y - cents[distalI].Y, 0);
        }
        else
        {
            var dx = max.X - min.X;
            var dy = max.Y - min.Y;
            md = dx >= dy ? new Vector3D(1, 0, 0) : new Vector3D(0, 1, 0);
        }

        if (md.LengthSquared < 1e-12)
            return;
        md.Normalize();
        var yaw = Math.Atan2(md.Y, md.X);
        RotateZ(pts, -yaw);
        stats.YawDeg = -yaw * 180.0 / Math.PI;

        Bounds(pts, out min, out max);
        zSpan = Math.Max(1e-9, max.Z - min.Z);
        var zCut = max.Z - 0.12 * zSpan;
        double sumPos = 0, sumNeg = 0;
        var nPos = 0;
        var nNeg = 0;
        foreach (var p in pts)
        {
            if (p.Z < zCut) continue;
            if (p.Y >= 0)
            {
                sumPos += p.Z;
                nPos++;
            }
            else
            {
                sumNeg += p.Z;
                nNeg++;
            }
        }
        var meanPos = nPos > 0 ? sumPos / nPos : 0;
        var meanNeg = nNeg > 0 ? sumNeg / nNeg : 0;
        stats.Palatal = meanNeg.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                        "/" + meanPos.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        if (meanPos > meanNeg)
        {
            MirrorYKeepWinding(pts, mesh.TriangleIndices);
            stats.FlippedX = true;
        }

        Recenter(pts);
        Bounds(pts, out min, out max);
        stats.Dx = max.X - min.X;
        stats.Dy = max.Y - min.Y;
        stats.Dz = max.Z - min.Z;
        stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;
    }

    private static int PalatalIndex(Point3D[] c)
    {
        var best = 0;
        var bestD = double.NegativeInfinity;
        for (var i = 0; i < 3; i++)
        {
            var a = c[(i + 1) % 3];
            var b = c[(i + 2) % 3];
            var mid = new Point3D(0.5 * (a.X + b.X), 0.5 * (a.Y + b.Y), 0);
            var d = (c[i].X - mid.X) * (c[i].X - mid.X) + (c[i].Y - mid.Y) * (c[i].Y - mid.Y);
            if (d > bestD)
            {
                bestD = d;
                best = i;
            }
        }
        return best;
    }

    private static List<List<Point3D>> ClusterXy(List<Point3D> pts, int k)
    {
        var result = new List<List<Point3D>>();
        if (pts.Count < k) return result;

        var seeds = FarthestPoints(pts, k);
        var assign = new int[pts.Count];
        for (var iter = 0; iter < 16; iter++)
        {
            for (var i = 0; i < pts.Count; i++)
            {
                var best = 0;
                var bestD = double.PositiveInfinity;
                for (var s = 0; s < k; s++)
                {
                    var dx = pts[i].X - seeds[s].X;
                    var dy = pts[i].Y - seeds[s].Y;
                    var d = dx * dx + dy * dy;
                    if (d < bestD)
                    {
                        bestD = d;
                        best = s;
                    }
                }
                assign[i] = best;
            }
            var sums = new Point3D[k];
            var counts = new int[k];
            for (var i = 0; i < pts.Count; i++)
            {
                var p = pts[i];
                var a = assign[i];
                sums[a] = new Point3D(sums[a].X + p.X, sums[a].Y + p.Y, sums[a].Z + p.Z);
                counts[a]++;
            }
            for (var s = 0; s < k; s++)
            {
                if (counts[s] == 0) continue;
                seeds[s] = new Point3D(sums[s].X / counts[s], sums[s].Y / counts[s], sums[s].Z / counts[s]);
            }
        }

        for (var s = 0; s < k; s++)
            result.Add([]);
        for (var i = 0; i < pts.Count; i++)
            result[assign[i]].Add(pts[i]);
        return result.Where(c => c.Count > 0).ToList();
    }

    private static Point3D[] FarthestPoints(List<Point3D> pts, int k)
    {
        var seeds = new Point3D[k];
        seeds[0] = pts[0];
        for (var s = 1; s < k; s++)
        {
            var bestI = 0;
            var bestD = double.NegativeInfinity;
            for (var i = 0; i < pts.Count; i++)
            {
                var minD = double.PositiveInfinity;
                for (var t = 0; t < s; t++)
                {
                    var dx = pts[i].X - seeds[t].X;
                    var dy = pts[i].Y - seeds[t].Y;
                    minD = Math.Min(minD, dx * dx + dy * dy);
                }
                if (minD > bestD)
                {
                    bestD = minD;
                    bestI = i;
                }
            }
            seeds[s] = pts[bestI];
        }
        return seeds;
    }

    private static Point3D Centroid(List<Point3D> pts)
    {
        var x = 0d;
        var y = 0d;
        var z = 0d;
        foreach (var p in pts)
        {
            x += p.X;
            y += p.Y;
            z += p.Z;
        }
        var n = Math.Max(1, pts.Count);
        return new Point3D(x / n, y / n, z / n);
    }

    private static double Area(List<Point3D> pts) => pts.Count;

    private static void RotateZ(Point3DCollection pts, double yaw)
    {
        var c = Math.Cos(yaw);
        var s = Math.Sin(yaw);
        for (var i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            pts[i] = new Point3D(p.X * c - p.Y * s, p.X * s + p.Y * c, p.Z);
        }
    }

    private static Point3D RotateVec(Point3D p, double yaw)
    {
        var c = Math.Cos(yaw);
        var s = Math.Sin(yaw);
        return new Point3D(p.X * c - p.Y * s, p.X * s + p.Y * c, p.Z);
    }

    private static void MirrorXKeepWinding(Point3DCollection pts, Int32Collection idx)
    {
        for (var i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            pts[i] = new Point3D(-p.X, p.Y, p.Z);
        }
        for (var i = 0; i + 2 < idx.Count; i += 3)
            (idx[i + 1], idx[i + 2]) = (idx[i + 2], idx[i + 1]);
    }

    private static void MirrorYKeepWinding(Point3DCollection pts, Int32Collection idx)
    {
        for (var i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            pts[i] = new Point3D(p.X, -p.Y, p.Z);
        }
        for (var i = 0; i + 2 < idx.Count; i += 3)
            (idx[i + 1], idx[i + 2]) = (idx[i + 2], idx[i + 1]);
    }

    private static void Recenter(Point3DCollection pts)
    {
        Bounds(pts, out _, out _, out var c);
        for (var i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            pts[i] = new Point3D(p.X - c.X, p.Y - c.Y, p.Z - c.Z);
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

    private static void Bounds(Point3DCollection pts, out Point3D min, out Point3D max)
        => Bounds(pts, out min, out max, out _);

    private static string Fmt(Point3D p)
        => p.X.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "," +
           p.Y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
}
