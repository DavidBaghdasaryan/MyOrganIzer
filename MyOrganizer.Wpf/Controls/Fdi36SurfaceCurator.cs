using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Unused. Runtime FDI 36 maps use <see cref="Fdi16SurfaceCurator.ApplyGeometry"/>.
/// </summary>
internal static class Fdi36SurfaceCurator
{
    public static ClinicalSurfaceMap Apply(ClinicalSurfaceMap automatic)
    {
        var crown = automatic.SourceCrown;
        var nTri = automatic.TriangleSurface.Length;
        var labels = (ClinicalSurface[])automatic.TriangleSurface.Clone();
        var feat = Measure(crown);
        var neighbors = CrownSurfaceClassifier.BuildNeighbors(crown.TriangleIndices, nTri);
        var peeled = new int[5];

        for (var t = 0; t < nTri; t++)
        {
            if (labels[t] != ClinicalSurface.Occlusal) continue;
            if (IsChewingSurface(feat, t)) continue;
            var wall = CrownSurfaceClassifier.AxialSurface(feat.Centroids[t], feat.Normals[t], feat.AxialCenter);
            labels[t] = wall;
            peeled[(int)wall]++;
        }

        ShrinkToChewingTable(labels, feat, peeled);
        PeelOuterWalls(labels, neighbors, feat, peeled);
        RetractOutwardWalls(labels, neighbors, feat, peeled);
        KeepLargestAxial(labels, neighbors);
        DropTinyOcclusal(labels, neighbors);
        FillInteriorHoles(labels, neighbors, feat);

        foreach (var kv in Fdi36ManualOverrides.Triangles)
        {
            if ((uint)kv.Key < (uint)nTri)
                labels[kv.Key] = kv.Value;
        }

        var counts = new int[5];
        foreach (var lab in labels)
            counts[(int)lab]++;

        var map = new ClinicalSurfaceMap
        {
            SourceCrown = crown,
            TriangleSurface = labels,
            OcclusalDirection = automatic.OcclusalDirection,
            Counts = counts
        };
        for (var t = 0; t < nTri; t++)
        {
            if (labels[t] != automatic.TriangleSurface[t])
                map.Overrides[t] = labels[t];
        }

        return map;
    }

    private static int CountLowFace(Features feat, ClinicalSurface[] labels)
    {
        var n = 0;
        for (var t = 0; t < labels.Length; t++)
        {
            if (labels[t] == ClinicalSurface.Occlusal && feat.Facing[t] < 0.22)
                n++;
        }
        return n;
    }

    private static bool IsChewingSurface(Features f, int t)
    {
        var z = f.Z01[t];
        var face = f.Facing[t];
        var env = Math.Max(1e-9, f.EnvR[t]);
        var ratio = f.Radius[t] / env;
        var outward = Outward(f, t);

        if (z < 0.54) return false;
        if (outward > 0.62 && face < 0.40) return false;
        if (ratio >= 0.92 && face < 0.42) return false;

        if (ratio <= 0.78 && z >= 0.58 && face >= 0.08) return true;
        if (ratio <= 0.88 && z >= 0.62 && face >= 0.24) return true;
        if (ratio <= 0.96 && z >= 0.68 && face >= 0.36) return true;
        if (z >= 0.78 && face >= 0.42 && ratio <= 1.04) return true;
        return false;
    }

    private static double Outward(Features f, int t)
    {
        var n = f.Normals[t];
        var nxy = new Vector3D(n.X, n.Y, 0);
        var pxy = new Vector3D(
            f.Centroids[t].X - f.AxialCenter.X,
            f.Centroids[t].Y - f.AxialCenter.Y,
            0);
        if (nxy.LengthSquared < 1e-10 || pxy.LengthSquared < 1e-10)
            return 0;
        nxy.Normalize();
        pxy.Normalize();
        return Vector3D.DotProduct(nxy, pxy);
    }

    private static void ShrinkToChewingTable(ClinicalSurface[] labels, Features feat, int[] peeled)
    {
        var tight = TightTableEnvelope(feat);
        for (var t = 0; t < labels.Length; t++)
        {
            if (labels[t] != ClinicalSurface.Occlusal) continue;
            var dx = feat.Centroids[t].X - feat.TableOrigin.X;
            var dy = feat.Centroids[t].Y - feat.TableOrigin.Y;
            var r = Math.Sqrt(dx * dx + dy * dy);
            var env = Math.Max(1e-9, EnvAt(tight, dx, dy));
            var ratio = r / env;
            var z = feat.Z01[t];
            var face = feat.Facing[t];
            var keep = (ratio <= 0.90 && z >= 0.64 && face >= 0.28)
                || (ratio <= 0.82 && z >= 0.60 && face >= 0.10)
                || (ratio <= 1.00 && z >= 0.74 && face >= 0.52)
                || (ratio <= 0.72 && z >= 0.62);
            if (keep) continue;
            var wall = CrownSurfaceClassifier.AxialSurface(feat.Centroids[t], feat.Normals[t], feat.AxialCenter);
            labels[t] = wall;
            peeled[(int)wall]++;
        }
    }

    private static double[] TightTableEnvelope(Features feat)
    {
        const int bins = 72;
        var origin = feat.TableOrigin;
        var maxR = new double[bins];
        var has = new bool[bins];
        for (var t = 0; t < feat.Centroids.Length; t++)
        {
            if (feat.Z01[t] < 0.70 || feat.Facing[t] < 0.48) continue;
            var dx = feat.Centroids[t].X - origin.X;
            var dy = feat.Centroids[t].Y - origin.Y;
            var r = Math.Sqrt(dx * dx + dy * dy);
            var bin = AngleBin(dx, dy, bins);
            if (r >= maxR[bin])
            {
                maxR[bin] = r;
                has[bin] = true;
            }
        }
        FillBins(maxR, has);
        return maxR;
    }

    private static int AngleBin(double dx, double dy, int bins)
    {
        var ang = Math.Atan2(dy, dx);
        var bin = (int)Math.Floor((ang + Math.PI) / (2 * Math.PI) * bins);
        return Math.Clamp(bin, 0, bins - 1);
    }

    private static void FillBins(double[] maxR, bool[] has)
    {
        var bins = maxR.Length;
        for (var i = 0; i < bins; i++)
        {
            if (has[i])
            {
                maxR[i] *= 0.90;
                continue;
            }
            var prev = i;
            var back = 0;
            do { prev = (prev - 1 + bins) % bins; back++; } while (!has[prev] && back < bins);
            var next = i;
            var fwd = 0;
            do { next = (next + 1) % bins; fwd++; } while (!has[next] && fwd < bins);
            if (!has[prev] && !has[next]) continue;
            if (!has[prev]) maxR[i] = maxR[next];
            else if (!has[next]) maxR[i] = maxR[prev];
            else maxR[i] = (maxR[prev] * fwd + maxR[next] * back) / Math.Max(1, back + fwd);
            maxR[i] *= 0.90;
        }
    }

    private static double EnvAt(double[] envelope, double dx, double dy)
    {
        var bins = envelope.Length;
        var u = (Math.Atan2(dy, dx) + Math.PI) / (2 * Math.PI) * bins;
        var i0 = ((int)Math.Floor(u) % bins + bins) % bins;
        var i1 = (i0 + 1) % bins;
        var frac = u - Math.Floor(u);
        return envelope[i0] * (1 - frac) + envelope[i1] * frac;
    }

    private static void PeelOuterWalls(ClinicalSurface[] labels, List<int>[] neighbors, Features feat, int[] peeled)
    {
        for (var pass = 0; pass < 8; pass++)
        {
            var changed = 0;
            for (var t = 0; t < labels.Length; t++)
            {
                if (labels[t] != ClinicalSurface.Occlusal) continue;
                if (IsChewingSurface(feat, t) && feat.Facing[t] >= 0.30 && feat.Z01[t] >= 0.64)
                    continue;
                var chosen = MatchingWall(labels, neighbors, feat, t, minNb: 1);
                if (chosen is null) continue;
                labels[t] = chosen.Value;
                peeled[(int)chosen.Value]++;
                changed++;
            }
            if (changed == 0) break;
        }
    }

    private static void RetractOutwardWalls(ClinicalSurface[] labels, List<int>[] neighbors, Features feat, int[] peeled)
    {
        for (var pass = 0; pass < 6; pass++)
        {
            var changed = 0;
            for (var t = 0; t < labels.Length; t++)
            {
                if (labels[t] != ClinicalSurface.Occlusal) continue;
                var z = feat.Z01[t];
                var face = feat.Facing[t];
                var outward = Outward(feat, t);
                var env = Math.Max(1e-9, feat.EnvR[t]);
                var ratio = feat.Radius[t] / env;
                var wallLike = face < 0.20
                    || (z < 0.58 && face < 0.48)
                    || (outward > 0.58 && face < 0.40)
                    || (ratio >= 0.90 && face < 0.44 && z < 0.82);
                if (!wallLike) continue;
                var wall = MatchingWall(labels, neighbors, feat, t, minNb: 1)
                    ?? CrownSurfaceClassifier.AxialSurface(feat.Centroids[t], feat.Normals[t], feat.AxialCenter);
                labels[t] = wall;
                peeled[(int)wall]++;
                changed++;
            }
            if (changed == 0) break;
        }
    }

    private static ClinicalSurface? MatchingWall(
        ClinicalSurface[] labels, List<int>[] neighbors, Features feat, int t, int minNb)
    {
        var dy = feat.Centroids[t].Y - feat.AxialCenter.Y;
        var dx = feat.Centroids[t].X - feat.AxialCenter.X;
        var bNb = 0;
        var lNb = 0;
        var mNb = 0;
        var dNb = 0;
        foreach (var nb in neighbors[t])
        {
            switch (labels[nb])
            {
                case ClinicalSurface.Buccal: bNb++; break;
                case ClinicalSurface.Palatal: lNb++; break;
                case ClinicalSurface.Mesial: mNb++; break;
                case ClinicalSurface.Distal: dNb++; break;
            }
        }
        if (Math.Abs(dy) >= Math.Abs(dx) * 0.72)
        {
            if (dy > 0 && bNb >= minNb) return ClinicalSurface.Buccal;
            if (dy < 0 && lNb >= minNb) return ClinicalSurface.Palatal;
        }
        else
        {
            if (dx > 0 && mNb >= minNb) return ClinicalSurface.Mesial;
            if (dx < 0 && dNb >= minNb) return ClinicalSurface.Distal;
        }
        if (bNb >= minNb && bNb >= lNb && bNb >= mNb && bNb >= dNb && dy > 0)
            return ClinicalSurface.Buccal;
        if (lNb >= minNb && lNb >= bNb && lNb >= mNb && lNb >= dNb && dy < 0)
            return ClinicalSurface.Palatal;
        if (mNb >= minNb && mNb >= dNb && dx > 0) return ClinicalSurface.Mesial;
        if (dNb >= minNb && dNb >= mNb && dx < 0) return ClinicalSurface.Distal;
        return null;
    }

    private static void FillInteriorHoles(ClinicalSurface[] labels, List<int>[] neighbors, Features feat)
    {
        var next = (ClinicalSurface[])labels.Clone();
        for (var t = 0; t < labels.Length; t++)
        {
            if (labels[t] == ClinicalSurface.Occlusal) continue;
            if (!IsChewingSurface(feat, t)) continue;
            var occ = 0;
            foreach (var nb in neighbors[t])
            {
                if (labels[nb] == ClinicalSurface.Occlusal) occ++;
            }
            if (occ < 3) continue;
            next[t] = ClinicalSurface.Occlusal;
        }
        Array.Copy(next, labels, labels.Length);
    }

    private static void KeepLargestAxial(ClinicalSurface[] labels, List<int>[] neighbors)
    {
        for (var s = 1; s < 5; s++)
        {
            var surface = (ClinicalSurface)s;
            var comps = Components(labels, neighbors, surface);
            if (comps.Count <= 1) continue;
            var largest = comps.OrderByDescending(c => c.Count).First();
            foreach (var comp in comps)
            {
                if (comp == largest) continue;
                var votes = new int[5];
                foreach (var t in comp)
                {
                    foreach (var nb in neighbors[t])
                    {
                        if (labels[nb] != surface)
                            votes[(int)labels[nb]]++;
                    }
                }
                var best = surface;
                var n = 0;
                for (var k = 0; k < 5; k++)
                {
                    if (votes[k] <= n) continue;
                    n = votes[k];
                    best = (ClinicalSurface)k;
                }
                if (n == 0 || best == surface) continue;
                foreach (var t in comp)
                    labels[t] = best;
            }
        }
    }

    private static void DropTinyOcclusal(ClinicalSurface[] labels, List<int>[] neighbors)
    {
        foreach (var comp in Components(labels, neighbors, ClinicalSurface.Occlusal))
        {
            if (comp.Count >= 36) continue;
            var votes = new int[5];
            foreach (var t in comp)
            {
                foreach (var nb in neighbors[t])
                {
                    if (labels[nb] != ClinicalSurface.Occlusal)
                        votes[(int)labels[nb]]++;
                }
            }
            var best = ClinicalSurface.Buccal;
            var n = 0;
            for (var s = 1; s < 5; s++)
            {
                if (votes[s] <= n) continue;
                n = votes[s];
                best = (ClinicalSurface)s;
            }
            if (n == 0) continue;
            foreach (var t in comp)
                labels[t] = best;
        }
    }

    private static List<List<int>> Components(ClinicalSurface[] labels, List<int>[] neighbors, ClinicalSurface surface)
    {
        var seen = new bool[labels.Length];
        var result = new List<List<int>>();
        for (var i = 0; i < labels.Length; i++)
        {
            if (seen[i] || labels[i] != surface) continue;
            var stack = new Stack<int>();
            var comp = new List<int>();
            stack.Push(i);
            seen[i] = true;
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                comp.Add(t);
                foreach (var nb in neighbors[t])
                {
                    if (seen[nb] || labels[nb] != surface) continue;
                    seen[nb] = true;
                    stack.Push(nb);
                }
            }
            result.Add(comp);
        }
        return result;
    }

    private static Features Measure(MeshGeometry3D crown)
    {
        var idx = crown.TriangleIndices;
        var nTri = idx.Count / 3;
        var centroids = new Point3D[nTri];
        var normals = new Vector3D[nTri];
        var minZ = double.PositiveInfinity;
        var maxZ = double.NegativeInfinity;
        for (var t = 0; t < nTri; t++)
        {
            var a = crown.Positions[idx[t * 3]];
            var b = crown.Positions[idx[t * 3 + 1]];
            var c = crown.Positions[idx[t * 3 + 2]];
            centroids[t] = new Point3D((a.X + b.X + c.X) / 3.0, (a.Y + b.Y + c.Y) / 3.0, (a.Z + b.Z + c.Z) / 3.0);
            var nrm = Vector3D.CrossProduct(b - a, c - a);
            if (nrm.LengthSquared < 1e-18)
                nrm = new Vector3D(0, 0, 1);
            else
                nrm.Normalize();
            normals[t] = nrm;
            minZ = Math.Min(minZ, centroids[t].Z);
            maxZ = Math.Max(maxZ, centroids[t].Z);
        }
        var zSpan = Math.Max(1e-9, maxZ - minZ);
        var z01 = new double[nTri];
        var facing = new double[nTri];
        for (var t = 0; t < nTri; t++)
        {
            z01[t] = (centroids[t].Z - minZ) / zSpan;
            facing[t] = Vector3D.DotProduct(normals[t], new Vector3D(0, 0, 1));
        }
        var ax = 0d;
        var ay = 0d;
        var an = 0;
        for (var t = 0; t < nTri; t++)
        {
            if (z01[t] < 0.16 || z01[t] > 0.58) continue;
            ax += centroids[t].X;
            ay += centroids[t].Y;
            an++;
        }
        var axial = an == 0 ? new Point3D(0, 0, 0) : new Point3D(ax / an, ay / an, 0);
        var sx = 0d;
        var sy = 0d;
        var tn = 0;
        for (var t = 0; t < nTri; t++)
        {
            if (z01[t] < 0.64 || facing[t] < 0.36) continue;
            sx += centroids[t].X;
            sy += centroids[t].Y;
            tn++;
        }
        var origin = tn == 0 ? axial : new Point3D(sx / tn, sy / tn, 0);
        const int bins = 72;
        var maxBin = new double[bins];
        var has = new bool[bins];
        for (var t = 0; t < nTri; t++)
        {
            if (z01[t] < 0.64 || facing[t] < 0.36) continue;
            var dx = centroids[t].X - origin.X;
            var dy = centroids[t].Y - origin.Y;
            var r = Math.Sqrt(dx * dx + dy * dy);
            var ang = Math.Atan2(dy, dx);
            var bin = Math.Clamp((int)Math.Floor((ang + Math.PI) / (2 * Math.PI) * bins), 0, bins - 1);
            if (r >= maxBin[bin])
            {
                maxBin[bin] = r;
                has[bin] = true;
            }
        }
        for (var i = 0; i < bins; i++)
        {
            if (has[i]) { maxBin[i] *= 1.05; continue; }
            var prev = i;
            var back = 0;
            do { prev = (prev - 1 + bins) % bins; back++; } while (!has[prev] && back < bins);
            var next = i;
            var fwd = 0;
            do { next = (next + 1) % bins; fwd++; } while (!has[next] && fwd < bins);
            if (!has[prev] && !has[next]) continue;
            if (!has[prev]) maxBin[i] = maxBin[next];
            else if (!has[next]) maxBin[i] = maxBin[prev];
            else maxBin[i] = (maxBin[prev] * fwd + maxBin[next] * back) / Math.Max(1, back + fwd);
            maxBin[i] *= 1.05;
        }
        var radius = new double[nTri];
        var envR = new double[nTri];
        for (var t = 0; t < nTri; t++)
        {
            var dx = centroids[t].X - origin.X;
            var dy = centroids[t].Y - origin.Y;
            radius[t] = Math.Sqrt(dx * dx + dy * dy);
            var u = (Math.Atan2(dy, dx) + Math.PI) / (2 * Math.PI) * bins;
            var i0 = ((int)Math.Floor(u) % bins + bins) % bins;
            var i1 = (i0 + 1) % bins;
            var frac = u - Math.Floor(u);
            envR[t] = Math.Max(1e-9, maxBin[i0] * (1 - frac) + maxBin[i1] * frac);
        }
        return new Features(centroids, normals, z01, facing, radius, envR, axial, origin);
    }

    private readonly record struct Features(
        Point3D[] Centroids,
        Vector3D[] Normals,
        double[] Z01,
        double[] Facing,
        double[] Radius,
        double[] EnvR,
        Point3D AxialCenter,
        Point3D TableOrigin);

}

internal static class Fdi36ManualOverrides
{
    public static readonly Dictionary<int, ClinicalSurface> Triangles = new();
}
