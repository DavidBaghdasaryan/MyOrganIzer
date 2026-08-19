using System.Globalization;
using System.IO;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Spatial layout of a surface map in tooth-local space. Used to compare
/// FDI 16 (golden) against other teeth without copying triangle indices.
/// </summary>
internal static class ToothSurfaceLayoutStats
{
    public static string Json(string fdi, string pipeline, MeshGeometry3D crown, ClinicalSurface[] labels)
    {
        var idx = crown.TriangleIndices;
        var n = labels.Length;
        var z = new double[n];
        var minZ = double.PositiveInfinity;
        var maxZ = double.NegativeInfinity;
        var sx = new double[5];
        var sy = new double[5];
        var sz = new double[5];
        var count = new int[5];
        for (var t = 0; t < n; t++)
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
            var s = (int)labels[t];
            sx[s] += cx;
            sy[s] += cy;
            sz[s] += cz;
            count[s]++;
        }

        var zSpan = Math.Max(1e-9, maxZ - minZ);
        var zSum = new double[5];
        var zMin = new double[5];
        var zMax = new double[5];
        var low = new int[5];
        var high = new int[5];
        for (var s = 0; s < 5; s++)
        {
            zMin[s] = 1;
            zMax[s] = 0;
        }
        for (var t = 0; t < n; t++)
        {
            var s = (int)labels[t];
            var z01 = (z[t] - minZ) / zSpan;
            zSum[s] += z01;
            zMin[s] = Math.Min(zMin[s], z01);
            zMax[s] = Math.Max(zMax[s], z01);
            if (z01 < 0.35) low[s]++;
            if (z01 >= 0.70) high[s]++;
        }

        string One(int s)
        {
            var nn = count[s];
            var pct = n == 0 ? 0 : 100.0 * nn / n;
            var meanZ01 = nn == 0 ? 0 : zSum[s] / nn;
            var mx = nn == 0 ? 0 : sx[s] / nn;
            var my = nn == 0 ? 0 : sy[s] / nn;
            var mz = nn == 0 ? 0 : sz[s] / nn;
            return "{\"n\":" + nn +
                   ",\"pct\":" + F(pct) +
                   ",\"meanX\":" + F(mx) +
                   ",\"meanY\":" + F(my) +
                   ",\"meanZ\":" + F(mz) +
                   ",\"meanZ01\":" + F(meanZ01) +
                   ",\"minZ01\":" + F(nn == 0 ? 0 : zMin[s]) +
                   ",\"maxZ01\":" + F(nn == 0 ? 0 : zMax[s]) +
                   ",\"lowCervical\":" + low[s] +
                   ",\"highTable\":" + high[s] + "}";
        }

        return "{\"fdi\":\"" + fdi +
               "\",\"pipeline\":\"" + pipeline +
               "\",\"nTri\":" + n +
               ",\"occlusal\":" + One(0) +
               ",\"buccal\":" + One(1) +
               ",\"inner\":" + One(2) +
               ",\"mesial\":" + One(3) +
               ",\"distal\":" + One(4) + "}";
    }

    public static void Log(string hypothesisId, string fdi, string pipeline, MeshGeometry3D crown, ClinicalSurface[] labels)
    {
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi16-template\",\"hypothesisId\":\"" + hypothesisId +
                   "\",\"location\":\"ToothSurfaceLayoutStats.cs\",\"message\":\"layout\"" +
                   ",\"data\":" + Json(fdi, pipeline, crown, labels) +
                   ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { }
    }

    public readonly record struct RedHeight(int Count, double Min, double Mean, double Max, double PctLow, double PctHigh)
    {
        public string Json(string fdi) =>
            "{\"fdi\":\"" + fdi +
            "\",\"redCount\":" + Count +
            ",\"redMinZ01\":" + F(Min) +
            ",\"redMeanZ01\":" + F(Mean) +
            ",\"redMaxZ01\":" + F(Max) +
            ",\"redPctCervical\":" + F(PctLow) +
            ",\"redPctTable\":" + F(PctHigh) +
            ",\"cervicalZone\":" + (Mean <= 0.40 && PctHigh <= 10 ? "true" : "false") + "}";
    }

    public static RedHeight RedHeightOf(MeshGeometry3D crown, ClinicalSurface[] labels)
    {
        var idx = crown.TriangleIndices;
        var n = labels.Length;
        var minZ = double.PositiveInfinity;
        var maxZ = double.NegativeInfinity;
        var z = new double[n];
        for (var t = 0; t < n; t++)
        {
            var a = crown.Positions[idx[t * 3]];
            var b = crown.Positions[idx[t * 3 + 1]];
            var c = crown.Positions[idx[t * 3 + 2]];
            z[t] = (a.Z + b.Z + c.Z) / 3.0;
            minZ = Math.Min(minZ, z[t]);
            maxZ = Math.Max(maxZ, z[t]);
        }
        var span = Math.Max(1e-9, maxZ - minZ);
        var count = 0;
        var sum = 0d;
        var lo = 1d;
        var hi = 0d;
        var low = 0;
        var high = 0;
        for (var t = 0; t < n; t++)
        {
            if (labels[t] != ClinicalSurface.Occlusal) continue;
            var z01 = (z[t] - minZ) / span;
            count++;
            sum += z01;
            lo = Math.Min(lo, z01);
            hi = Math.Max(hi, z01);
            if (z01 < 0.35) low++;
            if (z01 >= 0.70) high++;
        }
        return new RedHeight(
            count,
            count == 0 ? 0 : lo,
            count == 0 ? 0 : sum / count,
            count == 0 ? 0 : hi,
            count == 0 ? 0 : 100.0 * low / count,
            count == 0 ? 0 : 100.0 * high / count);
    }

    public static void LogRedHeight(string hypothesisId, string fdi, MeshGeometry3D crown, ClinicalSurface[] labels)
    {
        var red = RedHeightOf(crown, labels);
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi16-template\",\"hypothesisId\":\"" + hypothesisId +
                   "\",\"location\":\"ToothSurfaceLayoutStats.cs\",\"message\":\"red-height\"" +
                   ",\"data\":" + red.Json(fdi) +
                   ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { }
    }

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
