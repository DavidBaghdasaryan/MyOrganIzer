using System.Globalization;
using System.IO;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Normalized occlusal footprint measured from the frozen FDI 16 map:
/// 8.477% of crown triangles, all at z01 ≥ 0.718, inner to the table envelope.
/// Applied in the target tooth's own bounds. No FDI 16 indices or world units.
/// </summary>
internal static class NormalizedOcclusalMask
{
    public const double GoldenFraction = 0.08477;
    public const double GoldenMinZ01 = 0.718;
    public const double MaxInnerRatio = 0.92;

    public static void Apply(MeshGeometry3D crown, ClinicalSurface[] labels)
    {
        var idx = crown.TriangleIndices;
        var nTri = labels.Length;
        var centroids = new Point3D[nTri];
        var normals = new Vector3D[nTri];
        var minZ = double.PositiveInfinity;
        var maxZ = double.NegativeInfinity;
        var sx = 0d;
        var sy = 0d;
        for (var t = 0; t < nTri; t++)
        {
            var a = crown.Positions[idx[t * 3]];
            var b = crown.Positions[idx[t * 3 + 1]];
            var c = crown.Positions[idx[t * 3 + 2]];
            var p = new Point3D((a.X + b.X + c.X) / 3.0, (a.Y + b.Y + c.Y) / 3.0, (a.Z + b.Z + c.Z) / 3.0);
            centroids[t] = p;
            var nrm = Vector3D.CrossProduct(b - a, c - a);
            if (nrm.LengthSquared < 1e-18)
                nrm = new Vector3D(0, 0, 1);
            else
                nrm.Normalize();
            normals[t] = nrm;
            minZ = Math.Min(minZ, p.Z);
            maxZ = Math.Max(maxZ, p.Z);
            sx += p.X;
            sy += p.Y;
        }

        var zSpan = Math.Max(1e-9, maxZ - minZ);
        var z01 = new double[nTri];
        var facing = new double[nTri];
        for (var t = 0; t < nTri; t++)
        {
            z01[t] = (centroids[t].Z - minZ) / zSpan;
            facing[t] = normals[t].Z;
        }

        var ax = 0d;
        var ay = 0d;
        var an = 0;
        var tsx = 0d;
        var tsy = 0d;
        var tn = 0;
        for (var t = 0; t < nTri; t++)
        {
            if (z01[t] >= 0.18 && z01[t] <= 0.62)
            {
                ax += centroids[t].X;
                ay += centroids[t].Y;
                an++;
            }
            if (z01[t] >= 0.70 && facing[t] >= 0.40)
            {
                tsx += centroids[t].X;
                tsy += centroids[t].Y;
                tn++;
            }
        }
        var axial = an == 0 ? new Point3D(sx / nTri, sy / nTri, 0) : new Point3D(ax / an, ay / an, 0);
        var origin = tn == 0 ? axial : new Point3D(tsx / tn, tsy / tn, 0);
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
            if (r > maxR[bin]) maxR[bin] = r;
        }
        for (var i = 0; i < bins; i++)
        {
            if (maxR[i] > 1e-9) continue;
            maxR[i] = Math.Max(maxR[(i + bins - 1) % bins], maxR[(i + 1) % bins]);
        }

        var candidates = new List<(int T, double Ratio, double Z, double Face)>();
        for (var t = 0; t < nTri; t++)
        {
            if (z01[t] < GoldenMinZ01 || facing[t] < 0.20) continue;
            var dx = centroids[t].X - origin.X;
            var dy = centroids[t].Y - origin.Y;
            var r = Math.Sqrt(dx * dx + dy * dy);
            var u = (Math.Atan2(dy, dx) + Math.PI) / (2 * Math.PI) * bins;
            var i0 = ((int)Math.Floor(u) % bins + bins) % bins;
            var i1 = (i0 + 1) % bins;
            var frac = u - Math.Floor(u);
            var env = Math.Max(1e-9, maxR[i0] * (1 - frac) + maxR[i1] * frac);
            var ratio = r / env;
            if (ratio > MaxInnerRatio) continue;
            candidates.Add((t, ratio, z01[t], facing[t]));
        }

        candidates.Sort((a, b) =>
        {
            var c = a.Ratio.CompareTo(b.Ratio);
            return c != 0 ? c : b.Z.CompareTo(a.Z);
        });

        var budget = Math.Max(1, (int)Math.Round(GoldenFraction * nTri));
        var keep = new bool[nTri];
        var taken = 0;
        foreach (var cand in candidates)
        {
            if (taken >= budget) break;
            keep[cand.T] = true;
            taken++;
        }

        var released = 0;
        for (var t = 0; t < nTri; t++)
        {
            if (keep[t])
            {
                labels[t] = ClinicalSurface.Occlusal;
                continue;
            }
            if (labels[t] != ClinicalSurface.Occlusal) continue;
            labels[t] = CrownSurfaceClassifier.AxialSurface(centroids[t], normals[t], axial);
            released++;
        }

        AgentLog("A", "applied-occlusal-mask",
            "{\"candidates\":" + candidates.Count +
            ",\"budget\":" + budget +
            ",\"taken\":" + taken +
            ",\"released\":" + released +
            ",\"goldenFrac\":" + GoldenFraction.ToString("0.####", CultureInfo.InvariantCulture) +
            ",\"minZ01\":" + GoldenMinZ01.ToString("0.###", CultureInfo.InvariantCulture) + "}");
    }

    // #region agent log
    private static void AgentLog(string hypothesisId, string message, string dataJson)
    {
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi16-template\",\"hypothesisId\":\"" + hypothesisId +
                   "\",\"location\":\"NormalizedOcclusalMask.cs\",\"message\":\"" + message +
                   "\",\"data\":" + dataJson + ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { }
    }
    // #endregion
}
