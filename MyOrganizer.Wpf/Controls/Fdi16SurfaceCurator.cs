using System.Globalization;
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
    public static ClinicalSurfaceMap Apply(ClinicalSurfaceMap automatic) =>
        ApplyCore(automatic, applyFdi16TriangleOverrides: true);

    public static ClinicalSurfaceMap ApplyGeometry(ClinicalSurfaceMap automatic) =>
        ApplyCore(automatic, applyFdi16TriangleOverrides: false);

    /// <summary>
    /// Maxillary first-molar family rules in this mesh's normalized space.
    /// Does not apply FDI 16 triangle IDs and does not use the mandibular
    /// low-z01 cervical band (that paints the chewing table on this family).
    /// Color 0 is a high-z01 cervical/neck band matching approved FDI 16's role.
    /// </summary>
    public static ClinicalSurfaceMap ApplyMaxillaryGeometry(ClinicalSurfaceMap automatic) =>
        ApplyCore(automatic, applyFdi16TriangleOverrides: false, highCervicalBand: true);

    /// <summary>
    /// Premolar family: color 0 is the cervical/neck band (like the approved
    /// molars). Chewing table is B/P/M/D. This mesh is crown-up, so the band
    /// is grown from the low-z01 CEJ. No FDI 16 triangle IDs.
    /// </summary>
    public static ClinicalSurfaceMap ApplyPremolarGeometry(ClinicalSurfaceMap automatic) =>
        ApplyCore(automatic, applyFdi16TriangleOverrides: false, premolarChewing: true);

    private static ClinicalSurfaceMap ApplyCore(
        ClinicalSurfaceMap automatic,
        bool applyFdi16TriangleOverrides,
        bool highCervicalBand = false,
        bool premolarChewing = false)
    {
        var crown = automatic.SourceCrown;
        var nTri = automatic.TriangleSurface.Length;
        var labels = (ClinicalSurface[])automatic.TriangleSurface.Clone();
        var feat = Measure(crown);
        var neighbors = CrownSurfaceClassifier.BuildNeighbors(crown.TriangleIndices, nTri);
        var peeled = new int[5];

        if (premolarChewing)
        {
            // Color 0 is the cervical neck band, same visual role as 16/26/36/46.
            // This mesh is crown-up (+Z occlusal), so the CEJ is low z01 — not
            // the maxillary-molar high-z01 convention. Walls already classified
            // stay; the chewing table is recast to B/P/M/D.
            PlaceCervicalRedBand(crown, labels, neighbors, feat, peeled);
        }
        else
        {
            for (var t = 0; t < nTri; t++)
            {
                if (labels[t] != ClinicalSurface.Occlusal) continue;
                if (IsChewingSurface(feat, t)) continue;
                var wall = CrownSurfaceClassifier.AxialSurface(feat.Centroids[t], feat.Normals[t], feat.AxialCenter);
                labels[t] = wall;
                peeled[(int)wall]++;
            }

            PeelOuterBuccalPalatal(labels, neighbors, feat, peeled);
        }

        if (applyFdi16TriangleOverrides)
        {
            foreach (var kv in Fdi16ManualOverrides.Triangles)
            {
                if ((uint)kv.Key < (uint)nTri)
                    labels[kv.Key] = kv.Value;
            }
        }
        else if (highCervicalBand)
        {
            StripLowOcclusal(labels, feat, peeled);
            PlaceHighCervicalBand(crown, labels, neighbors, feat);
        }
        else if (!premolarChewing)
        {
            PlaceCervicalRedBand(crown, labels, neighbors, feat, peeled);
        }

        KeepLargestAxial(labels, neighbors);
        DropTinyOcclusalIslands(labels, neighbors);
        if (!applyFdi16TriangleOverrides)
            ToothSurfaceTopology.CleanNormalized(crown, labels, neighbors);
        if (highCervicalBand)
        {
            ToothSurfaceTopology.AssignLowTableSectors(crown, labels);
            KeepLargestAxial(labels, neighbors);
            DropTinyOcclusalIslands(labels, neighbors);
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

        // #region agent log
        AgentLog("A", "curated",
            "{\"apply16ov\":" + (applyFdi16TriangleOverrides ? "true" : "false") +
            ",\"highCervicalBand\":" + (highCervicalBand ? "true" : "false") +
            ",\"premolarChewing\":" + (premolarChewing ? "true" : "false") +
            ",\"peeledB\":" + peeled[1] + ",\"peeledP\":" + peeled[2] +
            ",\"peeledM\":" + peeled[3] + ",\"peeledD\":" + peeled[4] +
            ",\"overrides\":" + map.Overrides.Count +
            ",\"occlusal\":" + counts[0] + ",\"buccal\":" + counts[1] +
            ",\"palatal\":" + counts[2] + ",\"mesial\":" + counts[3] + ",\"distal\":" + counts[4] + "}");
        // #endregion
        return map;
    }

    /// <summary>
    /// Premolar Occlusal is the chewing table in this tooth's z01 space:
    /// high and relatively inner, not the axial walls. Does not use molar
    /// facing thresholds (two-cusp slopes often face below 0.15).
    /// </summary>
    private static void PeelPremolarWalls(ClinicalSurface[] labels, Features feat, int[] peeled)
    {
        var nTri = labels.Length;
        const int bins = 72;
        var maxR = new double[bins];
        var has = new bool[bins];
        var sx = 0d;
        var sy = 0d;
        var nHi = 0;
        for (var t = 0; t < nTri; t++)
        {
            if (feat.Z01[t] < 0.70) continue;
            sx += feat.Centroids[t].X;
            sy += feat.Centroids[t].Y;
            nHi++;
        }
        var ox = nHi == 0 ? feat.AxialCenter.X : sx / nHi;
        var oy = nHi == 0 ? feat.AxialCenter.Y : sy / nHi;
        for (var t = 0; t < nTri; t++)
        {
            if (feat.Z01[t] < 0.70) continue;
            var dx = feat.Centroids[t].X - ox;
            var dy = feat.Centroids[t].Y - oy;
            var r = Math.Sqrt(dx * dx + dy * dy);
            var bin = AngleBin(feat.Centroids[t], new Point3D(ox, oy, 0), bins);
            if (r >= maxR[bin])
            {
                maxR[bin] = r;
                has[bin] = true;
            }
        }
        for (var i = 0; i < bins; i++)
        {
            if (has[i]) continue;
            var prev = maxR[(i + bins - 1) % bins];
            var next = maxR[(i + 1) % bins];
            maxR[i] = Math.Max(prev, next);
        }

        var kept = 0;
        var ratioSum = 0d;
        var nOcc = 0;
        var b70 = 0;
        var b78 = 0;
        var b86 = 0;
        var b94 = 0;
        var bOver = 0;
        for (var t = 0; t < nTri; t++)
        {
            if (labels[t] != ClinicalSurface.Occlusal) continue;
            nOcc++;
            var dx = feat.Centroids[t].X - ox;
            var dy = feat.Centroids[t].Y - oy;
            var r = Math.Sqrt(dx * dx + dy * dy);
            var bin = AngleBin(feat.Centroids[t], new Point3D(ox, oy, 0), bins);
            var env = Math.Max(1e-9, maxR[bin]);
            var ratio = r / env;
            ratioSum += ratio;
            if (ratio <= 0.70) b70++;
            else if (ratio <= 0.78) b78++;
            else if (ratio <= 0.86) b86++;
            else if (ratio <= 0.94) b94++;
            else bOver++;
            if (feat.Z01[t] >= 0.50 && ratio <= 0.78)
            {
                kept++;
                continue;
            }
            if (feat.Z01[t] >= 0.58 && ratio <= 0.86)
            {
                kept++;
                continue;
            }
            var wall = CrownSurfaceClassifier.AxialSurface(feat.Centroids[t], feat.Normals[t], feat.AxialCenter);
            labels[t] = wall;
            peeled[(int)wall]++;
        }
        // #region agent log
        AgentLog("D", "premolar-peel",
            "{\"nOcc\":" + nOcc +
            ",\"kept\":" + kept +
            ",\"nHi\":" + nHi +
            ",\"meanRatio\":" + (nOcc == 0 ? "0" : (ratioSum / nOcc).ToString("0.000", CultureInfo.InvariantCulture)) +
            ",\"r70\":" + b70 + ",\"r78\":" + b78 + ",\"r86\":" + b86 + ",\"r94\":" + b94 + ",\"rOver\":" + bOver + "}");
        // #endregion
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

    /// <summary>
    /// Frozen FDI 16 occlusal occupies only the high table: min z01 0.718, mean 0.873.
    /// Apply that same normalized height window to other teeth. Never copies FDI 16
    /// triangle indices or world coordinates.
    /// </summary>
    private static void RestrictOcclusalToGoldenFootprint(
        ClinicalSurface[] labels, Features feat, int[] peeled)
    {
        const double goldenMinZ01 = 0.718;
        for (var t = 0; t < labels.Length; t++)
        {
            if (labels[t] != ClinicalSurface.Occlusal) continue;
            var env = Math.Max(1e-9, feat.EnvR[t]);
            var inner = feat.Radius[t] / env <= 0.90;
            if (feat.Z01[t] >= goldenMinZ01 && feat.Facing[t] >= 0.36 && inner)
                continue;
            var wall = CrownSurfaceClassifier.AxialSurface(feat.Centroids[t], feat.Normals[t], feat.AxialCenter);
            labels[t] = wall;
            peeled[(int)wall]++;
        }
    }

    /// <summary>
    /// Chewing table on this family is low z01. Color 0 must stay the high
    /// cervical band (approved FDI 16 min z01 ≈ 0.70), never the table.
    /// </summary>
    private static void StripLowOcclusal(ClinicalSurface[] labels, Features feat, int[] peeled)
    {
        const double neckFloor = 0.70;
        for (var t = 0; t < labels.Length; t++)
        {
            if (labels[t] != ClinicalSurface.Occlusal) continue;
            if (feat.Z01[t] >= neckFloor) continue;
            var wall = CrownSurfaceClassifier.AxialSurface(feat.Centroids[t], feat.Normals[t], feat.AxialCenter);
            labels[t] = wall;
            peeled[(int)wall]++;
        }
    }

    /// <summary>
    /// Approved FDI 16 color-0 role: high-z01 cervical/neck band in this mesh's
    /// own z01 space. Grow from the crown open boundary down to the approved
    /// normalized floor (0.70), same anatomical thickness role as FDI 16.
    /// Never paints z01 &lt; 0.35 (table). Rules only — no FDI 16 triangle indices.
    /// </summary>
    private static void PlaceHighCervicalBand(
        MeshGeometry3D crown, ClinicalSurface[] labels, List<int>[] neighbors, Features feat)
    {
        var nTri = labels.Length;
        var border = CrownBoundaryTriangles(crown, nTri);
        const int bins = 72;
        var cejZ01 = new double[bins];
        Array.Fill(cejZ01, 0d);
        for (var t = 0; t < nTri; t++)
        {
            if (!border[t]) continue;
            var bin = AngleBin(feat.Centroids[t], feat.AxialCenter, bins);
            cejZ01[bin] = Math.Max(cejZ01[bin], feat.Z01[t]);
        }
        for (var i = 0; i < bins; i++)
        {
            if (cejZ01[i] > 1e-6) continue;
            var prev = cejZ01[(i + bins - 1) % bins];
            var next = cejZ01[(i + 1) % bins];
            cejZ01[i] = Math.Max(prev, next);
        }

        var band = new bool[nTri];
        var q = new Queue<int>();
        var flipped = 0;
        for (var t = 0; t < nTri; t++)
        {
            if (!border[t] || feat.Z01[t] < 0.70) continue;
            band[t] = true;
            q.Enqueue(t);
            flipped++;
        }

        var dilated = 0;
        while (q.Count > 0)
        {
            var t = q.Dequeue();
            var bin = AngleBin(feat.Centroids[t], feat.AxialCenter, bins);
            var floor = Math.Max(0.70, cejZ01[bin] - 0.22);
            foreach (var nb in neighbors[t])
            {
                if (band[nb]) continue;
                if (feat.Z01[nb] < floor) continue;
                if (feat.Z01[nb] < 0.68) continue;
                band[nb] = true;
                q.Enqueue(nb);
                dilated++;
            }
        }

        for (var pass = 0; pass < 4; pass++)
        {
            var changed = 0;
            for (var t = 0; t < nTri; t++)
            {
                if (band[t]) continue;
                if (feat.Z01[t] < 0.70) continue;
                var nOcc = 0;
                foreach (var nb in neighbors[t])
                    if (band[nb]) nOcc++;
                if (nOcc < 3) continue;
                band[t] = true;
                changed++;
                dilated++;
            }
            if (changed == 0) break;
        }

        for (var t = 0; t < nTri; t++)
        {
            if (!band[t]) continue;
            labels[t] = ClinicalSurface.Occlusal;
        }

        var occ = 0;
        var occZ = 0d;
        var occMin = 1d;
        var high = 0;
        var low = 0;
        for (var t = 0; t < nTri; t++)
        {
            if (labels[t] != ClinicalSurface.Occlusal) continue;
            occ++;
            occZ += feat.Z01[t];
            occMin = Math.Min(occMin, feat.Z01[t]);
            if (feat.Z01[t] >= 0.70) high++;
            if (feat.Z01[t] < 0.35) low++;
        }
        // #region agent log
        AgentLog("A", "high-cervical-band",
            "{\"flipped\":" + flipped +
            ",\"dilated\":" + dilated +
            ",\"occ\":" + occ +
            ",\"meanZ01\":" + (occ == 0 ? "0" : (occZ / occ).ToString("0.000", CultureInfo.InvariantCulture)) +
            ",\"minZ01\":" + (occ == 0 ? "0" : occMin.ToString("0.000", CultureInfo.InvariantCulture)) +
            ",\"lowTable\":" + low +
            ",\"highNeck\":" + high +
            ",\"usedLowBand\":false" +
            ",\"bfsFloor\":0.70}");
        // #endregion
    }

    /// <summary>
    /// Color 0 (coral) as a cervical band grown from the crown–root open boundary
    /// in this tooth's own z01 space. Used only by ApplyGeometry (not the frozen
    /// FDI 16 Apply() path).
    /// </summary>
    private static void PlaceCervicalRedBand(
        MeshGeometry3D crown, ClinicalSurface[] labels, List<int>[] neighbors, Features feat, int[] peeled)
    {
        var nTri = labels.Length;
        for (var t = 0; t < nTri; t++)
        {
            if (labels[t] != ClinicalSurface.Occlusal) continue;
            var wall = CrownSurfaceClassifier.AxialSurface(feat.Centroids[t], feat.Normals[t], feat.AxialCenter);
            labels[t] = wall;
            peeled[(int)wall]++;
        }

        var cej = CrownBoundaryTriangles(crown, nTri);
        for (var t = 0; t < nTri; t++)
        {
            if (feat.Z01[t] < 0.06)
                cej[t] = true;
        }

        const int bins = 72;
        var cejZ01 = new double[bins];
        Array.Fill(cejZ01, 1d);
        for (var t = 0; t < nTri; t++)
        {
            if (!cej[t]) continue;
            var bin = AngleBin(feat.Centroids[t], feat.AxialCenter, bins);
            cejZ01[bin] = Math.Min(cejZ01[bin], feat.Z01[t]);
        }
        for (var i = 0; i < bins; i++)
        {
            if (cejZ01[i] < 0.99) continue;
            var prev = cejZ01[(i + bins - 1) % bins];
            var next = cejZ01[(i + 1) % bins];
            cejZ01[i] = Math.Min(prev, next);
        }

        var band = new bool[nTri];
        var q = new Queue<int>();
        for (var t = 0; t < nTri; t++)
        {
            if (!cej[t]) continue;
            band[t] = true;
            q.Enqueue(t);
        }

        while (q.Count > 0)
        {
            var t = q.Dequeue();
            var bin = AngleBin(feat.Centroids[t], feat.AxialCenter, bins);
            var ceiling = Math.Min(0.34, cejZ01[bin] + 0.20);
            foreach (var nb in neighbors[t])
            {
                if (band[nb]) continue;
                if (feat.Z01[nb] > ceiling) continue;
                if (feat.Z01[nb] > 0.28 && feat.Facing[nb] > 0.38) continue;
                if (feat.Z01[nb] > 0.42) continue;
                band[nb] = true;
                q.Enqueue(nb);
            }
        }

        for (var pass = 0; pass < 4; pass++)
        {
            var changed = 0;
            for (var t = 0; t < nTri; t++)
            {
                if (band[t]) continue;
                if (feat.Z01[t] > 0.32) continue;
                var nOcc = 0;
                foreach (var nb in neighbors[t])
                    if (band[nb]) nOcc++;
                if (nOcc < 3) continue;
                band[t] = true;
                changed++;
            }
            if (changed == 0) break;
        }

        for (var t = 0; t < nTri; t++)
        {
            if (!band[t]) continue;
            labels[t] = ClinicalSurface.Occlusal;
        }

        var seen = new bool[nTri];
        for (var i = 0; i < nTri; i++)
        {
            if (seen[i] || labels[i] != ClinicalSurface.Occlusal) continue;
            var stack = new Stack<int>();
            var comp = new List<int>();
            stack.Push(i);
            seen[i] = true;
            var touchesCej = false;
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                comp.Add(t);
                if (cej[t] || feat.Z01[t] < 0.10) touchesCej = true;
                foreach (var nb in neighbors[t])
                {
                    if (seen[nb] || labels[nb] != ClinicalSurface.Occlusal) continue;
                    seen[nb] = true;
                    stack.Push(nb);
                }
            }
            if (touchesCej) continue;
            foreach (var t in comp)
            {
                labels[t] = CrownSurfaceClassifier.AxialSurface(
                    feat.Centroids[t], feat.Normals[t], feat.AxialCenter);
            }
        }

        var occ = 0;
        var occZ = 0d;
        var high = 0;
        var low = 0;
        for (var t = 0; t < nTri; t++)
        {
            if (labels[t] != ClinicalSurface.Occlusal) continue;
            occ++;
            occZ += feat.Z01[t];
            if (feat.Z01[t] >= 0.70) high++;
            if (feat.Z01[t] < 0.35) low++;
        }
        // #region agent log
        AgentLog("A", "cervical-band",
            "{\"occ\":" + occ +
            ",\"pct\":" + (nTri == 0 ? "0" : (100.0 * occ / nTri).ToString("0.0", CultureInfo.InvariantCulture)) +
            ",\"meanZ01\":" + (occ == 0 ? "0" : (occZ / occ).ToString("0.000", CultureInfo.InvariantCulture)) +
            ",\"lowCervical\":" + low +
            ",\"highTable\":" + high +
            ",\"cejSeeds\":" + CountTrue(cej) + "}");
        // #endregion
    }

    private static int CountTrue(bool[] flags)
    {
        var n = 0;
        foreach (var f in flags)
            if (f) n++;
        return n;
    }

    private static int AngleBin(Point3D p, Point3D center, int bins)
    {
        var u = (Math.Atan2(p.Y - center.Y, p.X - center.X) + Math.PI) / (2 * Math.PI) * bins;
        var bin = (int)Math.Floor(u);
        return Math.Clamp(bin, 0, bins - 1);
    }

    private static bool[] CrownBoundaryTriangles(MeshGeometry3D crown, int nTri)
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
            var a = idx[t * 3];
            var b = idx[t * 3 + 1];
            var c = idx[t * 3 + 2];
            Add(a, b);
            Add(b, c);
            Add(c, a);
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
