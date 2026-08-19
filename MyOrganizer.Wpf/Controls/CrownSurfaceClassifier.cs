using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Classifies existing Dundee crown triangles into five clinical surfaces
/// in tooth-local FDI 16 space. Does not mutate the source mesh.
/// Canonical axes: +Z occlusal, +Y buccal, −Y palatal, +X mesial, −X distal.
/// </summary>
internal static class CrownSurfaceClassifier
{
    private const int EnvelopeBins = 72;
    private const int IslandSize = 28;

    public static ClinicalSurfaceMap Classify(MeshGeometry3D crown) =>
        Classify(crown, applyFdi16Overrides: true);

    public static ClinicalSurfaceMap Classify(MeshGeometry3D crown, bool applyFdi16Overrides) =>
        Classify(crown, applyFdi16Overrides, occlusalDirection: null, premolarTable: false);

    public static ClinicalSurfaceMap Classify(
        MeshGeometry3D crown,
        bool applyFdi16Overrides,
        Vector3D? occlusalDirection,
        bool premolarTable = false)
    {
        var idx = crown.TriangleIndices;
        var nTri = idx.Count / 3;
        var labels = new ClinicalSurface[nTri];
        if (nTri == 0)
        {
            return new ClinicalSurfaceMap
            {
                SourceCrown = crown,
                TriangleSurface = labels,
                OcclusalDirection = new Vector3D(0, 0, 1)
            };
        }

        var centroids = new Point3D[nTri];
        var normals = new Vector3D[nTri];
        var min = new Point3D(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
        var max = new Point3D(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);
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
            min = new Point3D(Math.Min(min.X, centroid.X), Math.Min(min.Y, centroid.Y), Math.Min(min.Z, centroid.Z));
            max = new Point3D(Math.Max(max.X, centroid.X), Math.Max(max.Y, centroid.Y), Math.Max(max.Z, centroid.Z));
            sx += centroid.X;
            sy += centroid.Y;
            sz += centroid.Z;
        }

        var center = new Point3D(sx / nTri, sy / nTri, sz / nTri);
        var occlusalDir = occlusalDirection ?? DetectOcclusalDirection(centroids, normals, min.Z, max.Z);
        if (occlusalDir.LengthSquared < 1e-12)
            occlusalDir = new Vector3D(0, 0, 1);
        else
            occlusalDir.Normalize();
        var along = new double[nTri];
        var minAlong = double.PositiveInfinity;
        var maxAlong = double.NegativeInfinity;
        for (var t = 0; t < nTri; t++)
        {
            var v = centroids[t] - center;
            along[t] = v.X * occlusalDir.X + v.Y * occlusalDir.Y + v.Z * occlusalDir.Z;
            minAlong = Math.Min(minAlong, along[t]);
            maxAlong = Math.Max(maxAlong, along[t]);
        }
        var alongSpan = Math.Max(1e-9, maxAlong - minAlong);
        var z01 = new double[nTri];
        var facing = new double[nTri];
        for (var t = 0; t < nTri; t++)
        {
            z01[t] = (along[t] - minAlong) / alongSpan;
            facing[t] = Vector3D.DotProduct(normals[t], occlusalDir);
        }

        var axialCenter = AxialCenter(centroids, z01);
        var neighbors = BuildNeighbors(idx, nTri);
        Point3D tableOrigin;
        double envMin, envMax, envMean;
        var envelope = premolarTable
            ? BuildPremolarEnvelope(centroids, z01, out tableOrigin, out envMin, out envMax, out envMean)
            : BuildOcclusalEnvelope(centroids, z01, facing, out tableOrigin, out envMin, out envMax, out envMean);
        var radius = new double[nTri];
        var envR = new double[nTri];
        for (var t = 0; t < nTri; t++)
        {
            var dx = centroids[t].X - tableOrigin.X;
            var dy = centroids[t].Y - tableOrigin.Y;
            radius[t] = Math.Sqrt(dx * dx + dy * dy);
            envR[t] = EnvelopeRadius(envelope, dx, dy);
        }

        var occlusal = new bool[nTri];
        var seedCount = 0;
        for (var t = 0; t < nTri; t++)
        {
            if (premolarTable)
            {
                if (!IsPremolarOcclusalSeed(z01[t], facing[t], radius[t], envR[t]))
                    continue;
            }
            else if (!IsOcclusalSeed(z01[t], facing[t], radius[t], envR[t]))
                continue;
            occlusal[t] = true;
            seedCount++;
        }
        GrowOcclusal(occlusal, neighbors, z01, facing, radius, envR, premolarTable);
        var occlusalGrown = 0;
        for (var t = 0; t < nTri; t++)
        {
            if (occlusal[t])
            {
                labels[t] = ClinicalSurface.Occlusal;
                occlusalGrown++;
            }
            else
            {
                labels[t] = AxialSurface(centroids[t], normals[t], axialCenter);
            }
        }

        var islandsBefore = CountIslands(labels, neighbors, IslandSize);
        var leftoverBefore = Leftover(labels, neighbors);
        Cleanup(labels, neighbors, z01, facing);
        var greenInPink = RetractPalatalFromDistal(labels, neighbors, centroids, axialCenter);
        if (applyFdi16Overrides)
            ApplyManualOverrides(labels);
        var islandsAfter = CountIslands(labels, neighbors, IslandSize);

        var counts = new int[5];
        foreach (var lab in labels)
            counts[(int)lab]++;
        var largest = LargestComponents(labels, neighbors);
        var leftoverAfter = Leftover(labels, neighbors);
        var occZ = 0d;
        var occLowFace = 0;
        var occN = 0;
        for (var t = 0; t < nTri; t++)
        {
            if (labels[t] != ClinicalSurface.Occlusal) continue;
            occZ += z01[t];
            occN++;
            if (facing[t] < 0.15) occLowFace++;
        }

        var map = new ClinicalSurfaceMap
        {
            SourceCrown = crown,
            TriangleSurface = labels,
            OcclusalDirection = occlusalDir,
            Counts = counts
        };
        if (applyFdi16Overrides)
        {
            foreach (var kv in Fdi16ManualOverrides.Triangles)
            {
                if ((uint)kv.Key < (uint)nTri)
                    map.Overrides[kv.Key] = kv.Value;
            }
        }

        // #region agent log
        AgentLog("B", "classified",
            "{\"nTri\":" + nTri +
            ",\"occlusalDir\":\"" + F(occlusalDir.X) + "," + F(occlusalDir.Y) + "," + F(occlusalDir.Z) + "\"" +
            ",\"minAlong\":" + F(minAlong) + ",\"maxAlong\":" + F(maxAlong) +
            ",\"seed\":" + seedCount + ",\"occlusalGrown\":" + occlusalGrown +
            ",\"occlusal\":" + counts[0] + ",\"buccal\":" + counts[1] +
            ",\"palatal\":" + counts[2] + ",\"mesial\":" + counts[3] + ",\"distal\":" + counts[4] +
            ",\"sum\":" + (counts[0] + counts[1] + counts[2] + counts[3] + counts[4]) +
            ",\"apply16ov\":" + (applyFdi16Overrides ? "true" : "false") +
            ",\"overrides\":" + (applyFdi16Overrides ? Fdi16ManualOverrides.Triangles.Count : 0) +
            ",\"greenInPink\":" + greenInPink + "}");
        AgentLog("G", "cleanup",
            "{\"islandsBefore\":" + islandsBefore + ",\"islandsAfter\":" + islandsAfter +
            ",\"largestO\":" + largest[0] + ",\"largestB\":" + largest[1] +
            ",\"largestP\":" + largest[2] + ",\"largestM\":" + largest[3] + ",\"largestD\":" + largest[4] +
            ",\"leftB0\":" + leftoverBefore[1] + ",\"leftD0\":" + leftoverBefore[4] +
            ",\"leftO\":" + leftoverAfter[0] + ",\"leftB\":" + leftoverAfter[1] +
            ",\"leftP\":" + leftoverAfter[2] + ",\"leftM\":" + leftoverAfter[3] + ",\"leftD\":" + leftoverAfter[4] +
            ",\"occMeanZ\":" + F(occN == 0 ? 0 : occZ / occN) + ",\"occLowFace\":" + occLowFace +
            ",\"envMin\":" + F(envMin) + ",\"envMax\":" + F(envMax) + ",\"envMean\":" + F(envMean) +
            ",\"envAsym\":" + F(envMax - envMin) + "}");
        // #endregion

        return map;
    }

    public static MeshGeometry3D OverlayMesh(MeshGeometry3D crown, IEnumerable<int> triangles, double normalEps)
    {
        var mesh = new MeshGeometry3D();
        var idx = crown.TriangleIndices;
        var nSrc = 0;
        foreach (var t in triangles)
        {
            nSrc++;
            var i0 = idx[t * 3];
            var i1 = idx[t * 3 + 1];
            var i2 = idx[t * 3 + 2];
            var a = crown.Positions[i0];
            var b = crown.Positions[i1];
            var c = crown.Positions[i2];
            var face = Vector3D.CrossProduct(b - a, c - a);
            if (face.LengthSquared < 1e-18)
                face = new Vector3D(0, 0, 1);
            else
                face.Normalize();
            var baseIndex = mesh.Positions.Count;
            mesh.Positions.Add(a + face * normalEps);
            mesh.Positions.Add(b + face * normalEps);
            mesh.Positions.Add(c + face * normalEps);
            mesh.TriangleIndices.Add(baseIndex);
            mesh.TriangleIndices.Add(baseIndex + 1);
            mesh.TriangleIndices.Add(baseIndex + 2);
        }
        // #region agent log
        try
        {
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"cej-seam2\",\"hypothesisId\":\"I\",\"location\":\"CrownSurfaceClassifier.cs\",\"message\":\"overlay-mesh\",\"data\":{\"nSrc\":" +
                       nSrc + ",\"sidewalls\":0,\"eps\":" +
                       normalEps.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) +
                       ",\"lift\":\"faceNormal\"},\"timestamp\":" +
                       DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line);
        }
        catch { }
        // #endregion
        return mesh;
    }

    private static Vector3D DetectOcclusalDirection(Point3D[] centroids, Vector3D[] normals, double minZ, double maxZ)
    {
        var span = Math.Max(1e-9, maxZ - minZ);
        var hi = minZ + 0.80 * span;
        var acc = new Vector3D();
        var n = 0;
        for (var i = 0; i < centroids.Length; i++)
        {
            if (centroids[i].Z < hi) continue;
            acc += normals[i];
            n++;
        }
        var occlusal = n == 0 ? new Vector3D(0, 0, 1) : acc;
        if (occlusal.LengthSquared < 1e-12)
            occlusal = new Vector3D(0, 0, 1);
        occlusal.Normalize();
        occlusal = new Vector3D(0, 0, occlusal.Z >= 0 ? 1 : -1);
        return occlusal;
    }

    private static Point3D AxialCenter(Point3D[] centroids, double[] z01)
    {
        var sx = 0d;
        var sy = 0d;
        var n = 0;
        for (var t = 0; t < centroids.Length; t++)
        {
            if (z01[t] < 0.18 || z01[t] > 0.62) continue;
            sx += centroids[t].X;
            sy += centroids[t].Y;
            n++;
        }
        if (n == 0)
        {
            sx = 0;
            sy = 0;
            foreach (var p in centroids)
            {
                sx += p.X;
                sy += p.Y;
            }
            n = centroids.Length;
        }
        return new Point3D(sx / n, sy / n, 0);
    }

    private static bool IsOcclusalSeed(double z, double face, double r, double env)
    {
        var inside = r <= env * 1.02;
        var near = r <= env * 1.08;
        if (inside && z > 0.50) return true;
        if (inside && z > 0.44 && face > 0.10) return true;
        if (near && z > 0.58 && face > 0.30) return true;
        if (z > 0.70 && face > 0.40) return true;
        if (z > 0.56 && face > 0.68) return true;
        return false;
    }

    private static bool IsPremolarOcclusalSeed(double z, double face, double r, double env)
    {
        var inside = r <= env * 0.88;
        if (inside && z > 0.54) return true;
        if (inside && z > 0.48 && face > -0.05) return true;
        return false;
    }

    private static bool CanGrowPremolarOcclusal(
        int t, bool[] occlusal, List<int>[] neighbors, double[] z01, double[] facing, double[] radius, double[] envR)
    {
        var z = z01[t];
        var r = radius[t];
        var env = envR[t];
        if (z < 0.48) return false;
        if (r > env * 0.98) return false;
        var occN = 0;
        foreach (var nb in neighbors[t])
        {
            if (occlusal[nb]) occN++;
        }
        if (z > 0.54 && r <= env * 0.82) return true;
        if (occN >= 2 && z > 0.52 && r <= env * 0.88) return true;
        if (facing[t] > 0.12 && z > 0.52 && r <= env * 0.92) return true;
        return false;
    }

    private static double[] BuildPremolarEnvelope(
        Point3D[] centroids, double[] z01, out Point3D origin, out double envMin, out double envMax, out double envMean)
    {
        var sx = 0d;
        var sy = 0d;
        var n = 0;
        for (var t = 0; t < centroids.Length; t++)
        {
            if (z01[t] < 0.62) continue;
            sx += centroids[t].X;
            sy += centroids[t].Y;
            n++;
        }
        origin = n == 0 ? new Point3D(0, 0, 0) : new Point3D(sx / n, sy / n, 0);
        var maxR = new double[EnvelopeBins];
        var has = new bool[EnvelopeBins];
        for (var t = 0; t < centroids.Length; t++)
        {
            if (z01[t] < 0.62) continue;
            var dx = centroids[t].X - origin.X;
            var dy = centroids[t].Y - origin.Y;
            var r = Math.Sqrt(dx * dx + dy * dy);
            var bin = AngleBin(dx, dy);
            if (r >= maxR[bin])
            {
                maxR[bin] = r;
                has[bin] = true;
            }
        }

        FillEmptyBins(maxR, has);
        envMin = double.PositiveInfinity;
        envMax = 0;
        var sum = 0d;
        var filled = 0;
        for (var i = 0; i < EnvelopeBins; i++)
        {
            maxR[i] *= 0.86;
            if (maxR[i] <= 1e-9) continue;
            envMin = Math.Min(envMin, maxR[i]);
            envMax = Math.Max(envMax, maxR[i]);
            sum += maxR[i];
            filled++;
        }
        if (filled == 0)
        {
            envMin = 0;
            envMax = 0;
            envMean = 0;
        }
        else
        {
            envMean = sum / filled;
        }
        return maxR;
    }

    private static void GrowOcclusal(
        bool[] occlusal, List<int>[] neighbors, double[] z01, double[] facing, double[] radius, double[] envR, bool premolarTable)
    {
        var q = new Queue<int>();
        for (var t = 0; t < occlusal.Length; t++)
        {
            if (occlusal[t])
                q.Enqueue(t);
        }
        while (q.Count > 0)
        {
            var t = q.Dequeue();
            foreach (var nb in neighbors[t])
            {
                if (occlusal[nb]) continue;
                if (premolarTable)
                {
                    if (!CanGrowPremolarOcclusal(nb, occlusal, neighbors, z01, facing, radius, envR))
                        continue;
                }
                else if (!CanGrowOcclusal(nb, occlusal, neighbors, z01, facing, radius, envR))
                    continue;
                occlusal[nb] = true;
                q.Enqueue(nb);
            }
        }
    }

    private static bool CanGrowOcclusal(int t, bool[] occlusal, List<int>[] neighbors, double[] z01, double[] facing, double[] radius, double[] envR)
    {
        var z = z01[t];
        var face = facing[t];
        var r = radius[t];
        var env = envR[t];
        if (z < 0.40) return false;
        var occN = 0;
        foreach (var nb in neighbors[t])
        {
            if (occlusal[nb]) occN++;
        }
        if (face < 0.12 && r > env * 1.02) return false;
        if (r <= env && z > 0.48) return true;
        if (occN >= 2 && z > 0.52 && r <= env * 1.08 && face > 0.16) return true;
        if (face > 0.30 && z > 0.55 && r <= env * 1.10) return true;
        return false;
    }

    private static double[] BuildOcclusalEnvelope(Point3D[] centroids, double[] z01, double[] facing, out Point3D origin, out double envMin, out double envMax, out double envMean)
    {
        var sx = 0d;
        var sy = 0d;
        var n = 0;
        for (var t = 0; t < centroids.Length; t++)
        {
            if (z01[t] < 0.66 || facing[t] < 0.38) continue;
            sx += centroids[t].X;
            sy += centroids[t].Y;
            n++;
        }
        origin = n == 0 ? new Point3D(0, 0, 0) : new Point3D(sx / n, sy / n, 0);
        var maxR = new double[EnvelopeBins];
        var has = new bool[EnvelopeBins];
        for (var t = 0; t < centroids.Length; t++)
        {
            if (z01[t] < 0.66 || facing[t] < 0.38) continue;
            var dx = centroids[t].X - origin.X;
            var dy = centroids[t].Y - origin.Y;
            var r = Math.Sqrt(dx * dx + dy * dy);
            var bin = AngleBin(dx, dy);
            if (r >= maxR[bin])
            {
                maxR[bin] = r;
                has[bin] = true;
            }
        }

        FillEmptyBins(maxR, has);
        envMin = double.PositiveInfinity;
        envMax = 0;
        var sum = 0d;
        var filled = 0;
        for (var i = 0; i < EnvelopeBins; i++)
        {
            maxR[i] *= 1.04;
            if (maxR[i] <= 1e-9) continue;
            envMin = Math.Min(envMin, maxR[i]);
            envMax = Math.Max(envMax, maxR[i]);
            sum += maxR[i];
            filled++;
        }
        if (filled == 0)
        {
            envMin = 0;
            envMax = 0;
            envMean = 0;
        }
        else
        {
            envMean = sum / filled;
        }
        return maxR;
    }

    private static void FillEmptyBins(double[] maxR, bool[] has)
    {
        for (var i = 0; i < maxR.Length; i++)
        {
            if (has[i]) continue;
            var prev = i;
            var stepsBack = 0;
            do
            {
                prev = (prev - 1 + maxR.Length) % maxR.Length;
                stepsBack++;
            } while (!has[prev] && stepsBack < maxR.Length);
            var next = i;
            var stepsFwd = 0;
            do
            {
                next = (next + 1) % maxR.Length;
                stepsFwd++;
            } while (!has[next] && stepsFwd < maxR.Length);
            if (!has[prev] && !has[next])
            {
                maxR[i] = 0;
                continue;
            }
            if (!has[prev]) maxR[i] = maxR[next];
            else if (!has[next]) maxR[i] = maxR[prev];
            else
            {
                var w = stepsBack + stepsFwd;
                maxR[i] = (maxR[prev] * stepsFwd + maxR[next] * stepsBack) / Math.Max(1, w);
            }
        }
    }

    private static int AngleBin(double dx, double dy)
    {
        var ang = Math.Atan2(dy, dx);
        var bin = (int)Math.Floor((ang + Math.PI) / (2 * Math.PI) * EnvelopeBins);
        return Math.Clamp(bin, 0, EnvelopeBins - 1);
    }

    private static double EnvelopeRadius(double[] envelope, double dx, double dy)
    {
        var ang = Math.Atan2(dy, dx);
        var u = (ang + Math.PI) / (2 * Math.PI) * EnvelopeBins;
        var i0 = ((int)Math.Floor(u) % EnvelopeBins + EnvelopeBins) % EnvelopeBins;
        var i1 = (i0 + 1) % EnvelopeBins;
        var frac = u - Math.Floor(u);
        return envelope[i0] * (1 - frac) + envelope[i1] * frac;
    }

    internal static ClinicalSurface AxialSurface(Point3D centroid, Vector3D normal, Point3D center)
    {
        var nxy = new Vector3D(normal.X, normal.Y, 0);
        var pxy = new Vector3D(centroid.X - center.X, centroid.Y - center.Y, 0);
        if (nxy.LengthSquared < 1e-8)
            nxy = pxy;
        if (pxy.LengthSquared < 1e-8)
            pxy = nxy;
        if (nxy.LengthSquared < 1e-8)
            return ClinicalSurface.Buccal;
        nxy.Normalize();
        pxy.Normalize();
        var blend = 0.46 * nxy + 0.54 * pxy;
        var buccal = blend.Y;
        var palatal = -blend.Y;
        var mesial = blend.X;
        var distal = -blend.X;
        var best = ClinicalSurface.Buccal;
        var score = buccal;
        if (palatal > score) { score = palatal; best = ClinicalSurface.Palatal; }
        if (mesial > score) { score = mesial; best = ClinicalSurface.Mesial; }
        if (distal > score) { best = ClinicalSurface.Distal; }
        return best;
    }

    internal static List<int>[] BuildNeighbors(Int32Collection idx, int nTri)
    {
        var edge = new Dictionary<(int, int), List<int>>();
        for (var t = 0; t < nTri; t++)
        {
            var a = idx[t * 3];
            var b = idx[t * 3 + 1];
            var c = idx[t * 3 + 2];
            AddEdge(edge, a, b, t);
            AddEdge(edge, b, c, t);
            AddEdge(edge, c, a, t);
        }
        var neighbors = new List<int>[nTri];
        for (var i = 0; i < nTri; i++)
            neighbors[i] = [];
        foreach (var pair in edge.Values)
        {
            for (var i = 0; i < pair.Count; i++)
            for (var j = i + 1; j < pair.Count; j++)
            {
                if (!neighbors[pair[i]].Contains(pair[j]))
                    neighbors[pair[i]].Add(pair[j]);
                if (!neighbors[pair[j]].Contains(pair[i]))
                    neighbors[pair[j]].Add(pair[i]);
            }
        }
        return neighbors;
    }

    private static void AddEdge(Dictionary<(int, int), List<int>> edge, int a, int b, int tri)
    {
        var key = a < b ? (a, b) : (b, a);
        if (!edge.TryGetValue(key, out var list))
        {
            list = [];
            edge[key] = list;
        }
        list.Add(tri);
    }

    private static void Cleanup(ClinicalSurface[] labels, List<int>[] neighbors, double[] z01, double[] facing)
    {
        ReassignIsolated(labels, neighbors, z01, facing);
        ReassignSmallComponents(labels, neighbors);
        KeepLargestComponents(labels, neighbors);
        MajoritySmooth(labels, neighbors, z01, facing, 4);
        ReassignIsolated(labels, neighbors, z01, facing);
        ReassignSmallComponents(labels, neighbors);
        KeepLargestComponents(labels, neighbors);
        MajoritySmooth(labels, neighbors, z01, facing, 2);
    }

    private static int RetractPalatalFromDistal(
        ClinicalSurface[] labels, List<int>[] neighbors, Point3D[] centroids, Point3D axialCenter)
    {
        var moved = 0;
        for (var t = 0; t < labels.Length; t++)
        {
            if (labels[t] != ClinicalSurface.Palatal) continue;
            var dx = centroids[t].X - axialCenter.X;
            var dy = centroids[t].Y - axialCenter.Y;
            var distal = -dx;
            var palatal = -dy;
            if (distal <= 0) continue;
            if (distal < palatal * 0.88) continue;
            labels[t] = ClinicalSurface.Distal;
            moved++;
        }

        for (var pass = 0; pass < 6; pass++)
        {
            var changed = 0;
            var next = (ClinicalSurface[])labels.Clone();
            for (var t = 0; t < labels.Length; t++)
            {
                if (labels[t] != ClinicalSurface.Palatal) continue;
                if (centroids[t].X > axialCenter.X) continue;
                var distalNb = 0;
                var palNb = 0;
                foreach (var nb in neighbors[t])
                {
                    if (labels[nb] == ClinicalSurface.Distal) distalNb++;
                    else if (labels[nb] == ClinicalSurface.Palatal) palNb++;
                }
                if (distalNb <= palNb) continue;
                next[t] = ClinicalSurface.Distal;
                changed++;
            }
            if (changed == 0) break;
            Array.Copy(next, labels, labels.Length);
            moved += changed;
        }

        KeepLargestComponents(labels, neighbors);
        return moved;
    }

    private static void ReassignIsolated(ClinicalSurface[] labels, List<int>[] neighbors, double[] z01, double[] facing)
    {
        var next = (ClinicalSurface[])labels.Clone();
        for (var t = 0; t < labels.Length; t++)
        {
            if (neighbors[t].Count == 0) continue;
            var same = 0;
            var votes = new int[5];
            foreach (var nb in neighbors[t])
            {
                votes[(int)labels[nb]]++;
                if (labels[nb] == labels[t]) same++;
            }
            if (same >= 2) continue;
            if (labels[t] == ClinicalSurface.Occlusal && z01[t] > 0.62 && facing[t] > 0.50)
                continue;
            var majority = Majority(votes, labels[t]);
            if (majority != labels[t])
                next[t] = majority;
        }
        Array.Copy(next, labels, labels.Length);
    }

    private static void ReassignSmallComponents(ClinicalSurface[] labels, List<int>[] neighbors)
    {
        var totals = new int[5];
        foreach (var lab in labels)
            totals[(int)lab]++;

        for (var s = 0; s < 5; s++)
        {
            var surface = (ClinicalSurface)s;
            foreach (var comp in Components(labels, neighbors, surface))
            {
                if (comp.Count >= IslandSize && comp.Count >= 0.012 * totals[s])
                    continue;
                var votes = new int[5];
                foreach (var t in comp)
                {
                    foreach (var nb in neighbors[t])
                    {
                        if (labels[nb] != surface)
                            votes[(int)labels[nb]]++;
                    }
                }
                var target = Majority(votes, surface);
                if (target == surface) continue;
                foreach (var t in comp)
                    labels[t] = target;
            }
        }
    }

    private static void MajoritySmooth(ClinicalSurface[] labels, List<int>[] neighbors, double[] z01, double[] facing, int passes)
    {
        for (var pass = 0; pass < passes; pass++)
        {
            var next = (ClinicalSurface[])labels.Clone();
            for (var t = 0; t < labels.Length; t++)
            {
                var nCount = neighbors[t].Count;
                if (nCount < 2) continue;
                var votes = new int[5];
                foreach (var nb in neighbors[t])
                    votes[(int)labels[nb]]++;
                var majority = 0;
                var majorityClass = labels[t];
                for (var s = 0; s < 5; s++)
                {
                    if (votes[s] <= majority) continue;
                    majority = votes[s];
                    majorityClass = (ClinicalSurface)s;
                }
                if (majorityClass == labels[t]) continue;
                if (majority * 3 < nCount * 2) continue;
                if (labels[t] == ClinicalSurface.Occlusal && facing[t] > 0.55 && z01[t] > 0.58)
                    continue;
                if (majorityClass == ClinicalSurface.Occlusal && facing[t] < 0.12 && z01[t] < 0.50)
                    continue;
                next[t] = majorityClass;
            }
            Array.Copy(next, labels, labels.Length);
        }
    }

    private static ClinicalSurface Majority(int[] votes, ClinicalSurface fallback)
    {
        var best = fallback;
        var n = 0;
        for (var s = 0; s < 5; s++)
        {
            if (votes[s] <= n) continue;
            n = votes[s];
            best = (ClinicalSurface)s;
        }
        return n == 0 ? fallback : best;
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

    private static void KeepLargestComponents(ClinicalSurface[] labels, List<int>[] neighbors)
    {
        for (var s = 0; s < 5; s++)
        {
            var surface = (ClinicalSurface)s;
            var comps = Components(labels, neighbors, surface);
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
                var target = Majority(votes, surface);
                if (target == surface) continue;
                foreach (var t in comp)
                    labels[t] = target;
            }
        }
    }

    private static int[] Leftover(ClinicalSurface[] labels, List<int>[] neighbors)
    {
        var totals = new int[5];
        foreach (var lab in labels)
            totals[(int)lab]++;
        var largest = LargestComponents(labels, neighbors);
        var leftover = new int[5];
        for (var s = 0; s < 5; s++)
            leftover[s] = Math.Max(0, totals[s] - largest[s]);
        return leftover;
    }

    private static int CountIslands(ClinicalSurface[] labels, List<int>[] neighbors, int maxSize)
    {
        var n = 0;
        for (var s = 0; s < 5; s++)
        {
            foreach (var comp in Components(labels, neighbors, (ClinicalSurface)s))
            {
                if (comp.Count < maxSize)
                    n++;
            }
        }
        return n;
    }

    private static int[] LargestComponents(ClinicalSurface[] labels, List<int>[] neighbors)
    {
        var largest = new int[5];
        for (var s = 0; s < 5; s++)
        {
            foreach (var comp in Components(labels, neighbors, (ClinicalSurface)s))
                largest[s] = Math.Max(largest[s], comp.Count);
        }
        return largest;
    }

    private static void ApplyManualOverrides(ClinicalSurface[] labels)
    {
        foreach (var kv in Fdi16ManualOverrides.Triangles)
        {
            if ((uint)kv.Key < (uint)labels.Length)
                labels[kv.Key] = kv.Value;
        }
    }

    // #region agent log
    private static void AgentLog(string hypothesisId, string message, string dataJson)
    {
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"post-fix\",\"hypothesisId\":\"" + hypothesisId +
                   "\",\"location\":\"CrownSurfaceClassifier.cs\",\"message\":\"" + message +
                   "\",\"data\":" + dataJson + ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { /* lab logging must not break rendering */ }
    }

    private static string F(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);
    // #endregion
}
