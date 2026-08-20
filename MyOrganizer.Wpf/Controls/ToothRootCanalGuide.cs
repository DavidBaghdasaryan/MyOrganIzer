using System.Windows.Media.Media3D;
using MyOrganizer.Wpf.Dental;

namespace MyOrganizer.Wpf.Controls;

internal readonly record struct CanalSample(Point3D Point, double Radius);

/// <summary>
/// Thin canal polylines sampled from each tooth's approved root mesh.
/// FDI 36 remains the reference: Mesial = greater +X after AlignFdi36.
/// Does not modify tooth models.
/// </summary>
internal static class ToothRootCanalGuide
{
    public static IReadOnlyDictionary<string, IReadOnlyList<CanalSample>> PathsFromRoot(
        string fdi, MeshGeometry3D? root, bool mirrored = false, double crownMeanZ = 0, double rootMeanZ = 0)
    {
        var result = new Dictionary<string, IReadOnlyList<CanalSample>>(StringComparer.OrdinalIgnoreCase);
        fdi = ToothAssetRegistry.Normalize(fdi ?? "");
        var defs = ToothRootCanalCatalog.ForFdi(fdi);
        if (defs.Count == 0 || root is null || root.Positions.Count == 0)
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
        var apexIsMaxZ = Math.Abs(rootMeanZ - crownMeanZ) > 1e-4
            ? rootMeanZ > crownMeanZ
            : false;
        var sign = apexIsMaxZ ? 1.0 : -1.0;
        var apexZ = apexIsMaxZ ? maxZ : minZ;
        var cervixZ = apexIsMaxZ ? minZ : maxZ;
        var apicalCut = apexZ - sign * 0.20 * zSpan;
        var apical = new List<Point3D>();
        foreach (var p in pts)
        {
            if (apexIsMaxZ ? p.Z >= apicalCut : p.Z <= apicalCut)
                apical.Add(p);
        }

        var mesialSign = mirrored ? -1 : 1;
        if (!TrySeeds(defs, apical, mesialSign, out var ids, out var seeds))
            return result;

        var fusedBp = defs.Any(d => d.Spatial == CanalSpatial.Buccal) &&
                      seeds.Length >= 2 && Dist2(seeds[0], seeds[1]) < 0.05;
        var z0 = cervixZ + sign * 0.12 * zSpan;
        var z1 = apexZ - sign * 0.10 * zSpan;
        for (var i = 0; i < ids.Length; i++)
        {
            var ySign = defs[i].Spatial == CanalSpatial.Buccal ? 1.0 : -1.0;
            var path = fusedBp
                ? TraceInside(pts, z0, z1, zSpan, ySign)
                : Trace(pts, seeds, i, z0, z1, zSpan);
            if (path.Count >= 2)
                result[ids[i]] = path;
        }

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

    public static IReadOnlyList<Point3D> Centerline(IReadOnlyList<CanalSample> path)
    {
        var pts = new Point3D[path.Count];
        for (var i = 0; i < path.Count; i++)
            pts[i] = path[i].Point;
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

    private static bool TrySeeds(
        IReadOnlyList<ToothRootCanalDefinition> defs,
        List<Point3D> apical,
        int mesialSign,
        out string[] ids,
        out Point3D[] seeds)
    {
        ids = defs.Select(d => d.Id).ToArray();
        seeds = new Point3D[defs.Count];
        if (apical.Count < 8)
            return false;

        var hasMd = defs.Any(d => d.Spatial is CanalSpatial.Mesial or CanalSpatial.Distal);
        var hasMolar3 = defs.Any(d => d.Spatial == CanalSpatial.Mesiobuccal);
        var hasBp = defs.Any(d => d.Spatial is CanalSpatial.Buccal) &&
                    defs.Any(d => d.Spatial is CanalSpatial.Palatal or CanalSpatial.Lingual);

        if (hasMolar3)
            return AssignMaxillaryMolar(defs, apical, mesialSign, seeds);
        if (hasMd)
            return AssignMesialDistal(defs, apical, mesialSign, seeds);
        if (hasBp)
            return AssignBuccalInner(defs, apical, seeds);

        var c = Centroid(apical);
        for (var i = 0; i < defs.Count; i++)
            seeds[i] = c;
        return true;
    }

    private static bool AssignMesialDistal(
        IReadOnlyList<ToothRootCanalDefinition> defs,
        List<Point3D> apical,
        int mesialSign,
        Point3D[] seeds)
    {
        var clusters = ClusterXy(apical, 2);
        Point3D mesial;
        Point3D distal;
        if (clusters.Count >= 2)
        {
            var c0 = Centroid(clusters[0]);
            var c1 = Centroid(clusters[1]);
            var mesialFirst = c0.X * mesialSign >= c1.X * mesialSign;
            mesial = mesialFirst ? c0 : c1;
            distal = mesialFirst ? c1 : c0;
        }
        else
        {
            var c = Centroid(apical);
            var dx = SpreadX(apical) * 0.22;
            mesial = new Point3D(c.X + mesialSign * dx, c.Y, c.Z);
            distal = new Point3D(c.X - mesialSign * dx, c.Y, c.Z);
        }

        for (var i = 0; i < defs.Count; i++)
        {
            seeds[i] = defs[i].Spatial == CanalSpatial.Mesial ? mesial : distal;
        }
        return true;
    }

    private static bool AssignMaxillaryMolar(
        IReadOnlyList<ToothRootCanalDefinition> defs,
        List<Point3D> apical,
        int mesialSign,
        Point3D[] seeds)
    {
        var clusters = ClusterXy(apical, 3);
        Point3D palatal;
        Point3D mb;
        Point3D db;
        if (clusters.Count >= 3)
        {
            var cents = clusters.Select(Centroid).ToArray();
            var palI = 0;
            for (var i = 1; i < cents.Length; i++)
            {
                if (cents[i].Y < cents[palI].Y)
                    palI = i;
            }
            var buccal = Enumerable.Range(0, cents.Length).Where(i => i != palI).ToArray();
            var a = cents[buccal[0]];
            var b = cents[buccal[1]];
            var aMesial = a.X * mesialSign >= b.X * mesialSign;
            mb = aMesial ? a : b;
            db = aMesial ? b : a;
            palatal = cents[palI];
        }
        else if (clusters.Count == 2)
        {
            var c0 = Centroid(clusters[0]);
            var c1 = Centroid(clusters[1]);
            palatal = c0.Y <= c1.Y ? c0 : c1;
            var buccal = c0.Y <= c1.Y ? c1 : c0;
            var dx = Math.Max(0.04, SpreadX(apical) * 0.18);
            mb = new Point3D(buccal.X + mesialSign * dx, buccal.Y, buccal.Z);
            db = new Point3D(buccal.X - mesialSign * dx, buccal.Y, buccal.Z);
        }
        else
        {
            var c = Centroid(apical);
            var dx = Math.Max(0.04, SpreadX(apical) * 0.22);
            var dy = Math.Max(0.04, SpreadY(apical) * 0.22);
            palatal = new Point3D(c.X, c.Y - dy, c.Z);
            mb = new Point3D(c.X + mesialSign * dx, c.Y + dy * 0.4, c.Z);
            db = new Point3D(c.X - mesialSign * dx, c.Y + dy * 0.4, c.Z);
        }

        for (var i = 0; i < defs.Count; i++)
        {
            seeds[i] = defs[i].Spatial switch
            {
                CanalSpatial.Mesiobuccal => mb,
                CanalSpatial.Distobuccal => db,
                _ => palatal
            };
        }
        PullTowardBarycenter(seeds, 0.72);
        return true;
    }

    private static void PullTowardBarycenter(Point3D[] seeds, double t)
    {
        if (seeds.Length < 2)
            return;
        double x = 0, y = 0, z = 0;
        foreach (var s in seeds)
        {
            x += s.X;
            y += s.Y;
            z += s.Z;
        }
        var n = seeds.Length;
        var o = new Point3D(x / n, y / n, z / n);
        for (var i = 0; i < seeds.Length; i++)
        {
            seeds[i] = new Point3D(
                o.X + (seeds[i].X - o.X) * t,
                o.Y + (seeds[i].Y - o.Y) * t,
                o.Z + (seeds[i].Z - o.Z) * t);
        }
    }

    private static bool AssignBuccalInner(
        IReadOnlyList<ToothRootCanalDefinition> defs,
        List<Point3D> apical,
        Point3D[] seeds)
    {
        var clusters = ClusterXy(apical, 2);
        Point3D buccal;
        Point3D inner;
        if (clusters.Count >= 2)
        {
            var c0 = Centroid(clusters[0]);
            var c1 = Centroid(clusters[1]);
            if (Dist2(c0, c1) >= 0.05)
            {
                var buccalFirst = c0.Y >= c1.Y;
                buccal = buccalFirst ? c0 : c1;
                inner = buccalFirst ? c1 : c0;
            }
            else
            {
                SplitSingleRoot(apical, out buccal, out inner);
            }
        }
        else
        {
            SplitSingleRoot(apical, out buccal, out inner);
        }

        for (var i = 0; i < defs.Count; i++)
            seeds[i] = defs[i].Spatial == CanalSpatial.Buccal ? buccal : inner;
        if (Dist2(buccal, inner) >= 0.05)
            PullTowardBarycenter(seeds, 0.72);
        return true;
    }

    private static void SplitSingleRoot(List<Point3D> apical, out Point3D buccal, out Point3D inner)
    {
        var c = Centroid(apical);
        var dy = Math.Max(0.02, SpreadY(apical) * 0.18);
        buccal = new Point3D(c.X, c.Y + dy, c.Z);
        inner = new Point3D(c.X, c.Y - dy, c.Z);
    }

    private static List<CanalSample> TraceInside(
        Point3DCollection pts, double z0, double z1, double zSpan, double ySign)
    {
        const int slices = 10;
        var path = new List<CanalSample>(slices);
        for (var s = 0; s < slices; s++)
        {
            var t = s / (double)(slices - 1);
            var z = z0 + (z1 - z0) * t;
            var band = 0.70 * zSpan / slices;
            double sx = 0, sy = 0, sz = 0;
            var n = 0;
            foreach (var p in pts)
            {
                if (Math.Abs(p.Z - z) > band)
                    continue;
                sx += p.X;
                sy += p.Y;
                sz += p.Z;
                n++;
            }
            if (n < 6)
                continue;
            var c = new Point3D(sx / n, sy / n, sz / n);
            var ry = 0.0;
            foreach (var p in pts)
            {
                if (Math.Abs(p.Z - z) > band)
                    continue;
                ry = Math.Max(ry, Math.Abs(p.Y - c.Y));
            }
            path.Add(new CanalSample(
                new Point3D(c.X, c.Y + ySign * 0.28 * ry, c.Z),
                Math.Max(0.03, ry)));
        }
        return path;
    }

    private static List<CanalSample> Trace(
        Point3DCollection pts,
        Point3D[] seeds,
        int i,
        double z0,
        double z1,
        double zSpan)
    {
        const int slices = 10;
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
                if (Owner(p, seeds) != i)
                    continue;
                sx += p.X;
                sy += p.Y;
                sz += p.Z;
                n++;
            }

            if (n == 0)
                continue;
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
                if (Owner(p, seeds) != i)
                    continue;
                rAcc = Math.Max(rAcc, Math.Sqrt(Dist2(p, c)));
            }

            last = c;
            path.Add(new CanalSample(c, Math.Max(0.04, rAcc)));
        }
        return path;
    }

    private static int Owner(Point3D p, Point3D[] seeds)
    {
        var best = 0;
        var bestD = Dist2(p, seeds[0]);
        for (var s = 1; s < seeds.Length; s++)
        {
            var d = Dist2(p, seeds[s]);
            if (d >= bestD)
                continue;
            bestD = d;
            best = s;
        }
        return best;
    }

    private static string F(double v) => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

    private static double SpreadX(List<Point3D> pts)
    {
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var p in pts)
        {
            if (p.X < min) min = p.X;
            if (p.X > max) max = p.X;
        }
        return Math.Max(0, max - min);
    }

    private static double SpreadY(List<Point3D> pts)
    {
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var p in pts)
        {
            if (p.Y < min) min = p.Y;
            if (p.Y > max) max = p.Y;
        }
        return Math.Max(0, max - min);
    }

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
