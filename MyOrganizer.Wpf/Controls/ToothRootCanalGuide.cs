using System.IO;
using System.Windows.Media.Media3D;
using MyOrganizer.Wpf.Dental;

namespace MyOrganizer.Wpf.Controls;

internal readonly record struct CanalSample(Point3D Point, double Radius);

/// <summary>
/// Thin canal polylines sampled from the approved FDI 36 root mesh.
/// Mesial = greater +X (AlignFdi36). Does not modify the tooth model.
/// </summary>
internal static class ToothRootCanalGuide
{
    public static IReadOnlyDictionary<string, IReadOnlyList<CanalSample>> PathsFromRoot(string fdi, MeshGeometry3D? root)
    {
        var result = new Dictionary<string, IReadOnlyList<CanalSample>>(StringComparer.OrdinalIgnoreCase);
        if (!string.Equals(ToothAssetRegistry.Normalize(fdi), "36", StringComparison.Ordinal) ||
            root is null ||
            root.Positions.Count == 0)
            return result;

        var pts = root.Positions;
        var minZ = double.PositiveInfinity;
        var maxZ = double.NegativeInfinity;
        foreach (var p in pts)
        {
            if (p.Z < minZ) minZ = p.Z;
            if (p.Z > maxZ) maxZ = p.Z;
        }

        var zSpan = Math.Max(1e-6, maxZ - minZ);
        var apicalCut = minZ + 0.20 * zSpan;
        var apical = new List<Point3D>();
        foreach (var p in pts)
        {
            if (p.Z <= apicalCut)
                apical.Add(p);
        }

        var clusters = ClusterXy(apical, 2);
        if (clusters.Count < 2)
            return result;

        var c0 = Centroid(clusters[0]);
        var c1 = Centroid(clusters[1]);
        var mesialI = c0.X >= c1.X ? 0 : 1;
        var distalI = mesialI == 0 ? 1 : 0;
        var seeds = new[] { Centroid(clusters[mesialI]), Centroid(clusters[distalI]) };
        var ids = new[] { ToothRootCanalCatalog.Mesial, ToothRootCanalCatalog.Distal };

        var z0 = maxZ - 0.12 * zSpan;
        var z1 = minZ + 0.10 * zSpan;
        const int slices = 10;
        for (var i = 0; i < 2; i++)
        {
            var path = new List<CanalSample>(slices);
            Point3D? last = null;
            for (var s = 0; s < slices; s++)
            {
                var t = s / (double)(slices - 1);
                var z = z0 + (z1 - z0) * t;
                var band = 0.70 * zSpan / slices;
                double sx = 0, sy = 0, sz = 0, rAcc = 0;
                var n = 0;
                foreach (var p in pts)
                {
                    if (Math.Abs(p.Z - z) > band)
                        continue;
                    var d0 = Dist2(p, seeds[0]);
                    var d1 = Dist2(p, seeds[1]);
                    var own = i == 0 ? d0 <= d1 : d1 < d0;
                    if (!own)
                        continue;
                    sx += p.X;
                    sy += p.Y;
                    sz += p.Z;
                    n++;
                }

                if (n < 6)
                {
                    if (last is Point3D keep)
                        path.Add(new CanalSample(new Point3D(keep.X, keep.Y, z), path[^1].Radius));
                    continue;
                }

                var c = new Point3D(sx / n, sy / n, sz / n);
                foreach (var p in pts)
                {
                    if (Math.Abs(p.Z - z) > band)
                        continue;
                    var d0 = Dist2(p, seeds[0]);
                    var d1 = Dist2(p, seeds[1]);
                    var own = i == 0 ? d0 <= d1 : d1 < d0;
                    if (!own)
                        continue;
                    rAcc = Math.Max(rAcc, Math.Sqrt(Dist2(p, c)));
                }

                last = c;
                path.Add(new CanalSample(c, Math.Max(0.04, rAcc)));
            }

            if (path.Count >= 2)
                result[ids[i]] = path;
        }

        // #region agent log
        try
        {
            result.TryGetValue(ToothRootCanalCatalog.Mesial, out var mp);
            result.TryGetValue(ToothRootCanalCatalog.Distal, out var dp);
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"endo-canal-v2\",\"hypothesisId\":\"C\"" +
                       ",\"location\":\"ToothRootCanalGuide.PathsFromRoot\",\"message\":\"36-axes\"" +
                       ",\"data\":{\"mesialX\":" + F(seeds[0].X) + ",\"distalX\":" + F(seeds[1].X) +
                       ",\"mesialY\":" + F(seeds[0].Y) + ",\"distalY\":" + F(seeds[1].Y) +
                       ",\"mesialN\":" + (mp?.Count ?? 0) + ",\"distalN\":" + (dp?.Count ?? 0) +
                       ",\"z0\":" + F(z0) + ",\"z1\":" + F(z1) +
                       ",\"minZ\":" + F(minZ) + ",\"maxZ\":" + F(maxZ) +
                       ",\"dx\":" + F(seeds[0].X - seeds[1].X) + "}" +
                       ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line);
        }
        catch { }
        // #endregion
        return result;
    }

    public static IReadOnlyList<Point3D> VisibleInside(
        IReadOnlyList<CanalSample> path, Vector3D look)
    {
        if (look.LengthSquared < 1e-12)
            look = new Vector3D(0, 0, -1);
        look.Normalize();
        var towardCam = -look;
        var pts = new List<Point3D>(path.Count);
        foreach (var s in path)
        {
            var pull = towardCam * (s.Radius * 0.55);
            pts.Add(new Point3D(s.Point.X + pull.X, s.Point.Y + pull.Y, s.Point.Z + pull.Z * 0.12));
        }
        return pts;
    }

    public static MeshGeometry3D Tube(IReadOnlyList<Point3D> path, double radius)
    {
        var mesh = new MeshGeometry3D();
        if (path.Count < 2)
            return mesh;
        const int sides = 6;
        for (var i = 0; i < path.Count; i++)
        {
            var tangent = i == 0
                ? path[1] - path[0]
                : i == path.Count - 1
                    ? path[i] - path[i - 1]
                    : path[i + 1] - path[i - 1];
            if (tangent.LengthSquared < 1e-12)
                tangent = new Vector3D(0, 0, -1);
            tangent.Normalize();
            var binorm = Vector3D.CrossProduct(tangent, Math.Abs(tangent.Z) > 0.9 ? new Vector3D(0, 1, 0) : new Vector3D(0, 0, 1));
            if (binorm.LengthSquared < 1e-12)
                binorm = new Vector3D(1, 0, 0);
            binorm.Normalize();
            var norm = Vector3D.CrossProduct(binorm, tangent);
            norm.Normalize();
            for (var s = 0; s < sides; s++)
            {
                var a = s * (2 * Math.PI / sides);
                var offset = (Math.Cos(a) * binorm + Math.Sin(a) * norm) * radius;
                mesh.Positions.Add(path[i] + offset);
            }
        }

        for (var i = 0; i < path.Count - 1; i++)
        {
            var a = i * sides;
            var b = (i + 1) * sides;
            for (var s = 0; s < sides; s++)
            {
                var s2 = (s + 1) % sides;
                mesh.TriangleIndices.Add(a + s);
                mesh.TriangleIndices.Add(b + s);
                mesh.TriangleIndices.Add(b + s2);
                mesh.TriangleIndices.Add(a + s);
                mesh.TriangleIndices.Add(b + s2);
                mesh.TriangleIndices.Add(a + s2);
            }
        }

        mesh.Freeze();
        return mesh;
    }

    private static string F(double v) => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

    private static List<List<Point3D>> ClusterXy(List<Point3D> pts, int k)
    {
        var result = new List<List<Point3D>>();
        if (pts.Count < k)
            return result;
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
                    minD = Math.Min(minD, Dist2(pts[i], seeds[t]));
                if (minD > bestD)
                {
                    bestD = minD;
                    bestI = i;
                }
            }
            seeds[s] = pts[bestI];
        }

        var assign = new int[pts.Count];
        for (var iter = 0; iter < 16; iter++)
        {
            for (var i = 0; i < pts.Count; i++)
            {
                var best = 0;
                var bestD = double.PositiveInfinity;
                for (var s = 0; s < k; s++)
                {
                    var d = Dist2(pts[i], seeds[s]);
                    if (d >= bestD)
                        continue;
                    bestD = d;
                    best = s;
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
                if (counts[s] == 0)
                    continue;
                seeds[s] = new Point3D(sums[s].X / counts[s], sums[s].Y / counts[s], sums[s].Z / counts[s]);
            }
        }

        for (var s = 0; s < k; s++)
            result.Add([]);
        for (var i = 0; i < pts.Count; i++)
            result[assign[i]].Add(pts[i]);
        return result.Where(c => c.Count > 0).ToList();
    }

    private static Point3D Centroid(List<Point3D> pts)
    {
        double x = 0, y = 0, z = 0;
        foreach (var p in pts)
        {
            x += p.X;
            y += p.Y;
            z += p.Z;
        }
        var n = Math.Max(1, pts.Count);
        return new Point3D(x / n, y / n, z / n);
    }

    private static double Dist2(Point3D a, Point3D b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}
