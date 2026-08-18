using System.Globalization;
using System.IO;
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
    public static ClinicalSurfaceMap Classify(MeshGeometry3D crown)
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
        var occlusalDir = DetectOcclusalDirection(centroids, normals, min.Z, max.Z);
        var minAlong = double.PositiveInfinity;
        var maxAlong = double.NegativeInfinity;
        var along = new double[nTri];
        for (var t = 0; t < nTri; t++)
        {
            var v = centroids[t] - center;
            along[t] = v.X * occlusalDir.X + v.Y * occlusalDir.Y + v.Z * occlusalDir.Z;
            minAlong = Math.Min(minAlong, along[t]);
            maxAlong = Math.Max(maxAlong, along[t]);
        }
        var alongSpan = Math.Max(1e-9, maxAlong - minAlong);

        for (var t = 0; t < nTri; t++)
        {
            var z01 = (along[t] - minAlong) / alongSpan;
            var facing = Math.Max(0, Vector3D.DotProduct(normals[t], occlusalDir));
            var occlusal = (facing > 0.22 && z01 > 0.58)
                           || (facing > 0.45 && z01 > 0.42)
                           || (facing > 0.72 && z01 > 0.28);
            labels[t] = occlusal ? ClinicalSurface.Occlusal : AxialSurface(centroids[t], normals[t], center);
        }

        Smooth(labels, BuildNeighbors(idx, nTri), normals, occlusalDir);
        var counts = new int[5];
        foreach (var lab in labels)
            counts[(int)lab]++;

        // #region agent log
        AgentLog("B", "classified",
            "{\"nTri\":" + nTri +
            ",\"occlusalDir\":\"" + F(occlusalDir.X) + "," + F(occlusalDir.Y) + "," + F(occlusalDir.Z) + "\"" +
            ",\"minAlong\":" + F(minAlong) + ",\"maxAlong\":" + F(maxAlong) +
            ",\"occlusal\":" + counts[0] + ",\"buccal\":" + counts[1] +
            ",\"palatal\":" + counts[2] + ",\"mesial\":" + counts[3] + ",\"distal\":" + counts[4] +
            ",\"sum\":" + (counts[0] + counts[1] + counts[2] + counts[3] + counts[4]) + "}");
        // #endregion

        return new ClinicalSurfaceMap
        {
            SourceCrown = crown,
            TriangleSurface = labels,
            OcclusalDirection = occlusalDir,
            Counts = counts
        };
    }

    public static MeshGeometry3D OverlayMesh(MeshGeometry3D crown, IEnumerable<int> triangles, double normalEps)
    {
        var mesh = new MeshGeometry3D();
        var idx = crown.TriangleIndices;
        foreach (var t in triangles)
        {
            var i0 = idx[t * 3];
            var i1 = idx[t * 3 + 1];
            var i2 = idx[t * 3 + 2];
            var a = crown.Positions[i0];
            var b = crown.Positions[i1];
            var c = crown.Positions[i2];
            var n = Vector3D.CrossProduct(b - a, c - a);
            if (n.LengthSquared < 1e-18)
                n = new Vector3D(0, 0, 1);
            else
                n.Normalize();
            var o = n * normalEps;
            var baseIndex = mesh.Positions.Count;
            mesh.Positions.Add(a + o);
            mesh.Positions.Add(b + o);
            mesh.Positions.Add(c + o);
            mesh.TriangleIndices.Add(baseIndex);
            mesh.TriangleIndices.Add(baseIndex + 1);
            mesh.TriangleIndices.Add(baseIndex + 2);
        }
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
        if (Math.Abs(occlusal.Z) < 0.35)
            occlusal = new Vector3D(0, 0, Math.Sign(occlusal.Z) == 0 ? 1 : Math.Sign(occlusal.Z));
        else
            occlusal = new Vector3D(0, 0, occlusal.Z >= 0 ? 1 : -1);
        return occlusal;
    }

    private static ClinicalSurface AxialSurface(Point3D centroid, Vector3D normal, Point3D center)
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
        var blend = 0.68 * nxy + 0.32 * pxy;
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

    private static List<int>[] BuildNeighbors(Int32Collection idx, int nTri)
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
                neighbors[pair[i]].Add(pair[j]);
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

    private static void Smooth(ClinicalSurface[] labels, List<int>[] neighbors, Vector3D[] normals, Vector3D occlusalDir)
    {
        for (var pass = 0; pass < 2; pass++)
        {
            var next = (ClinicalSurface[])labels.Clone();
            for (var t = 0; t < labels.Length; t++)
            {
                var counts = new int[5];
                foreach (var nb in neighbors[t])
                    counts[(int)labels[nb]]++;
                var majority = 0;
                var majorityClass = labels[t];
                for (var s = 0; s < 5; s++)
                {
                    if (counts[s] <= majority) continue;
                    majority = counts[s];
                    majorityClass = (ClinicalSurface)s;
                }
                if (majority < 2 || majorityClass == labels[t])
                    continue;
                var facing = Vector3D.DotProduct(normals[t], occlusalDir);
                if (labels[t] == ClinicalSurface.Occlusal && facing > 0.62 && majorityClass != ClinicalSurface.Occlusal)
                    continue;
                next[t] = majorityClass;
            }
            Array.Copy(next, labels, labels.Length);
        }
    }

    // #region agent log
    private static void AgentLog(string hypothesisId, string message, string dataJson)
    {
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"seg-v1\",\"hypothesisId\":\"" + hypothesisId +
                   "\",\"location\":\"CrownSurfaceClassifier.cs\",\"message\":\"" + message +
                   "\",\"data\":" + dataJson + ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { /* lab logging must not break rendering */ }
    }

    private static string F(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);
    // #endregion
}
