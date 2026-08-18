using System.IO;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// FDI 16 triangle curation applied after automatic classification.
/// Peels the Occlusal lid off the external walls and records overrides.
/// Does not change the Dundee mesh, materials, lights, or camera.
/// </summary>
internal static class Fdi16SurfaceCurator
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

        PeelOuterBuccalPalatal(labels, neighbors, feat, peeled);

        foreach (var kv in Fdi16ManualOverrides.Triangles)
        {
            if ((uint)kv.Key < (uint)nTri)
                labels[kv.Key] = kv.Value;
        }

        KeepLargestAxial(labels, neighbors);
        DropTinyOcclusalIslands(labels, neighbors);

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

        // #region agent log
        AgentLog("I", "curated",
            "{\"peeledB\":" + peeled[1] + ",\"peeledP\":" + peeled[2] +
            ",\"peeledM\":" + peeled[3] + ",\"peeledD\":" + peeled[4] +
            ",\"overrides\":" + map.Overrides.Count +
            ",\"occlusal\":" + counts[0] + ",\"buccal\":" + counts[1] +
            ",\"palatal\":" + counts[2] + ",\"mesial\":" + counts[3] + ",\"distal\":" + counts[4] + "}");
        // #endregion
        return map;
    }

    private static bool IsChewingSurface(Features f, int t)
    {
        var z = f.Z01[t];
        var face = f.Facing[t];
        var env = Math.Max(1e-9, f.EnvR[t]);
        var ratio = f.Radius[t] / env;
        var dx = f.Centroids[t].X - f.AxialCenter.X;
        var dy = f.Centroids[t].Y - f.AxialCenter.Y;
        var buccalPalatal = Math.Abs(dy) >= Math.Abs(dx) * 0.82;

        if (ratio <= 0.90 && z >= 0.48) return true;
        if (!buccalPalatal && ratio <= 0.98 && z >= 0.55 && face >= 0.16) return true;
        if (buccalPalatal && ratio <= 0.94 && z >= 0.55 && face >= 0.18) return true;
        if (ratio <= 1.02 && z >= 0.64 && face >= 0.32) return true;
        if (z >= 0.74 && face >= 0.36) return true;
        return false;
    }

    private static void PeelOuterBuccalPalatal(
        ClinicalSurface[] labels, List<int>[] neighbors, Features feat, int[] peeled)
    {
        for (var pass = 0; pass < 4; pass++)
        {
            var changed = 0;
            for (var t = 0; t < labels.Length; t++)
            {
                if (labels[t] != ClinicalSurface.Occlusal) continue;
                if (feat.Facing[t] >= 0.28) continue;
                if (feat.Z01[t] >= 0.72 && feat.Facing[t] >= 0.22) continue;
                var env = Math.Max(1e-9, feat.EnvR[t]);
                var ratio = feat.Radius[t] / env;
                if (ratio < 0.90) continue;
                var dy = feat.Centroids[t].Y - feat.AxialCenter.Y;
                var dx = feat.Centroids[t].X - feat.AxialCenter.X;
                if (Math.Abs(dy) < Math.Abs(dx) * 0.75) continue;

                var bNb = 0;
                var pNb = 0;
                foreach (var nb in neighbors[t])
                {
                    if (labels[nb] == ClinicalSurface.Buccal) bNb++;
                    else if (labels[nb] == ClinicalSurface.Palatal) pNb++;
                }

                ClinicalSurface? wall = null;
                if (dy > 0 && bNb >= 2)
                    wall = ClinicalSurface.Buccal;
                else if (dy < 0 && pNb >= 2)
                    wall = ClinicalSurface.Palatal;
                if (wall is null) continue;
                labels[t] = wall.Value;
                peeled[(int)wall.Value]++;
                changed++;
            }
            if (changed == 0) break;
        }
    }

    private static void KeepLargestAxial(ClinicalSurface[] labels, List<int>[] neighbors)
    {
        for (var s = 1; s < 5; s++)
        {
            var surface = (ClinicalSurface)s;
            var comps = ComponentsOf(labels, neighbors, surface);
            if (comps.Count <= 1) continue;
            var largest = comps[0];
            foreach (var comp in comps)
            {
                if (comp.Count > largest.Count)
                    largest = comp;
            }
            foreach (var comp in comps)
            {
                if (ReferenceEquals(comp, largest)) continue;
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

    private static void DropTinyOcclusalIslands(ClinicalSurface[] labels, List<int>[] neighbors)
    {
        foreach (var comp in ComponentsOf(labels, neighbors, ClinicalSurface.Occlusal))
        {
            if (comp.Count >= 40) continue;
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

    private static List<List<int>> ComponentsOf(ClinicalSurface[] labels, List<int>[] neighbors, ClinicalSurface surface)
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
        var sx = 0d;
        var sy = 0d;
        var sz = 0d;
        for (var t = 0; t < nTri; t++)
        {
            var a = crown.Positions[idx[t * 3]];
            var b = crown.Positions[idx[t * 3 + 1]];
            var c = crown.Positions[idx[t * 3 + 2]];
            var centroid = new Point3D((a.X + b.X + c.X) / 3.0, (a.Y + b.Y + c.Y) / 3.0, (a.Z + b.Z + c.Z) / 3.0);
            centroids[t] = centroid;
            var nrm = Vector3D.CrossProduct(b - a, c - a);
            if (nrm.LengthSquared < 1e-18)
                nrm = new Vector3D(0, 0, 1);
            else
                nrm.Normalize();
            normals[t] = nrm;
            minZ = Math.Min(minZ, centroid.Z);
            maxZ = Math.Max(maxZ, centroid.Z);
            sx += centroid.X;
            sy += centroid.Y;
            sz += centroid.Z;
        }

        var center = new Point3D(sx / nTri, sy / nTri, sz / nTri);
        var zSpan = Math.Max(1e-9, maxZ - minZ);
        var z01 = new double[nTri];
        var facing = new double[nTri];
        var occlusalDir = new Vector3D(0, 0, 1);
        for (var t = 0; t < nTri; t++)
        {
            z01[t] = (centroids[t].Z - minZ) / zSpan;
            facing[t] = Vector3D.DotProduct(normals[t], occlusalDir);
        }

        var ax = 0d;
        var ay = 0d;
        var an = 0;
        for (var t = 0; t < nTri; t++)
        {
            if (z01[t] < 0.18 || z01[t] > 0.62) continue;
            ax += centroids[t].X;
            ay += centroids[t].Y;
            an++;
        }
        var axialCenter = an == 0 ? center : new Point3D(ax / an, ay / an, 0);

        var tsx = 0d;
        var tsy = 0d;
        var tn = 0;
        for (var t = 0; t < nTri; t++)
        {
            if (z01[t] < 0.70 || facing[t] < 0.40) continue;
            tsx += centroids[t].X;
            tsy += centroids[t].Y;
            tn++;
        }
        var origin = tn == 0 ? new Point3D(0, 0, 0) : new Point3D(tsx / tn, tsy / tn, 0);
        const int bins = 72;
        var maxR = new double[bins];
        for (var t = 0; t < nTri; t++)
        {
            if (z01[t] < 0.70 || facing[t] < 0.40) continue;
            var dx = centroids[t].X - origin.X;
            var dy = centroids[t].Y - origin.Y;
            var r = Math.Sqrt(dx * dx + dy * dy);
            var bin = (int)Math.Floor((Math.Atan2(dy, dx) + Math.PI) / (2 * Math.PI) * bins);
            bin = Math.Clamp(bin, 0, bins - 1);
            if (r >= maxR[bin])
                maxR[bin] = r;
        }
        for (var i = 0; i < bins; i++)
        {
            if (maxR[i] > 1e-9) continue;
            var prev = maxR[(i + bins - 1) % bins];
            var next = maxR[(i + 1) % bins];
            maxR[i] = Math.Max(prev, next);
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
            envR[t] = maxR[i0] * (1 - frac) + maxR[i1] * frac;
        }

        return new Features(centroids, normals, z01, facing, radius, envR, axialCenter);
    }

    private sealed record Features(
        Point3D[] Centroids,
        Vector3D[] Normals,
        double[] Z01,
        double[] Facing,
        double[] Radius,
        double[] EnvR,
        Point3D AxialCenter);

    // #region agent log
    private static void AgentLog(string hypothesisId, string message, string dataJson)
    {
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"curated-occlusal\",\"hypothesisId\":\"" + hypothesisId +
                   "\",\"location\":\"Fdi16SurfaceCurator.cs\",\"message\":\"" + message +
                   "\",\"data\":" + dataJson + ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { /* lab logging must not break rendering */ }
    }
    // #endregion
}
