using System.Globalization;
using System.IO;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Normalized topology cleanup extracted from the approved FDI 16 map.
/// Operates in each tooth's own canonical space. Never copies FDI 16 indices
/// or world coordinates. Does not run on the frozen FDI 16 Apply() path.
/// </summary>
internal static class ToothSurfaceTopology
{
    private const int IslandSize = 28;
    private const double HighTableZ = 0.48;
    private const double HysteresisDeg = 12;

    public static void CleanNormalized(MeshGeometry3D crown, ClinicalSurface[] labels, List<int>[] neighbors)
    {
        var feat = Measure(crown);
        var before = Analyze(labels, neighbors, feat);
        RetractUnstable(labels, neighbors, feat, highTableOnly: false, passes: 4);
        FillEnclosedHoles(labels, neighbors, 64);
        AssignUpperCrownWalls(labels, feat);
        RetractUnstable(labels, neighbors, feat, highTableOnly: true, passes: 8);
        FillEnclosedHoles(labels, neighbors, 48);
        KeepLargestAxial(labels, neighbors);
        DropIslands(labels, neighbors);
        RetractUnstable(labels, neighbors, feat, highTableOnly: false, passes: 4);
        KeepLargestAxial(labels, neighbors);
        var after = Analyze(labels, neighbors, feat);
        var ownership = ValidateOwnership(labels);
        AgentLog("A", "topology-clean",
            "{\"before\":" + before +
            ",\"after\":" + after +
            ",\"dup\":" + ownership.Dup +
            ",\"unassigned\":" + ownership.Unassigned + "}");
        if (ownership.Dup != 0 || ownership.Unassigned != 0)
            throw new InvalidDataException(
                "surface-map ownership invalid dup=" + ownership.Dup +
                " unassigned=" + ownership.Unassigned);
    }

    public static string AnalyzeJson(MeshGeometry3D crown, ClinicalSurface[] labels)
    {
        var nTri = labels.Length;
        var neighbors = CrownSurfaceClassifier.BuildNeighbors(crown.TriangleIndices, nTri);
        return Analyze(labels, neighbors, Measure(crown));
    }

    public static void LogAnalyze(string hypothesisId, string fdi, string pipeline, MeshGeometry3D crown, ClinicalSurface[] labels)
    {
        var json = AnalyzeJson(crown, labels);
        AgentLog(hypothesisId, "topology",
            "{\"fdi\":\"" + fdi + "\",\"pipeline\":\"" + pipeline + "\",\"stats\":" + json + "}");
    }

    public static (int Dup, int Unassigned) ValidateOwnership(ClinicalSurface[] labels)
    {
        var unassigned = 0;
        for (var t = 0; t < labels.Length; t++)
        {
            var s = (int)labels[t];
            if ((uint)s > 4)
                unassigned++;
        }
        return (0, unassigned);
    }

    /// <summary>
    /// Maxillary occlusal table (low z01) must follow the same angular
    /// Buccal/Palatal/Mesial/Distal sectors as the upper walls. Shared
    /// CleanNormalized skips z01 &lt; 0.38, which left palatal tongues on
    /// the chewing table. Does not reassign cervical red. Mandibular
    /// Generate paths do not call this.
    /// </summary>
    public static void AssignLowTableSectors(MeshGeometry3D crown, ClinicalSurface[] labels)
    {
        var feat = Measure(crown);
        var hyster = HysteresisDeg * Math.PI / 180.0;
        var palatalInDistal = 0;
        var palatalInMesial = 0;
        var moved = 0;
        var table = 0;
        for (var t = 0; t < labels.Length; t++)
        {
            if (labels[t] == ClinicalSurface.Occlusal) continue;
            if (feat.Z01[t] >= HighTableZ) continue;
            table++;
            var next = SectorOf(feat.Centroids[t], feat.AxialCenter, labels[t], hyster);
            if (labels[t] == ClinicalSurface.Palatal && next == ClinicalSurface.Distal)
                palatalInDistal++;
            if (labels[t] == ClinicalSurface.Palatal && next == ClinicalSurface.Mesial)
                palatalInMesial++;
            if (next != labels[t])
                moved++;
            labels[t] = next;
        }
        AgentLog("C", "table-sectors",
            "{\"table\":" + table +
            ",\"moved\":" + moved +
            ",\"palatalInDistal\":" + palatalInDistal +
            ",\"palatalInMesial\":" + palatalInMesial + "}");
    }

    /// <summary>
    /// Palatal (green) peninsula on the high chewing table into Distal (purple).
    /// Maxillary second-molar family only. Does not reassign cervical Occlusal.
    /// Frozen 16/26/36/46 generate paths do not call this.
    /// Right: distal is −X. Left (after M/D swap): distal is +X.
    /// </summary>
    public static int RetractHighTablePalatalFromDistal(
        MeshGeometry3D crown, ClinicalSurface[] labels, ToothSide laterality = ToothSide.Right)
    {
        var feat = Measure(crown);
        var nTri = labels.Length;
        var neighbors = CrownSurfaceClassifier.BuildNeighbors(crown.TriangleIndices, nTri);
        var moved = 0;
        for (var t = 0; t < nTri; t++)
        {
            if (labels[t] != ClinicalSurface.Palatal) continue;
            if (feat.Z01[t] < 0.42) continue;
            var ax = feat.Centroids[t].X - feat.AxialCenter.X;
            var ay = feat.Centroids[t].Y - feat.AxialCenter.Y;
            if (laterality == ToothSide.Left)
                ax = -ax;
            var distal = -ax;
            var palatal = -ay;
            var ang = Math.Atan2(ay, ax);
            var sectorDistal = NearestSector(ang) == ClinicalSurface.Distal;
            var pastDiagonal = distal > 0 && distal >= palatal * 0.78;
            if (!sectorDistal && !pastDiagonal) continue;
            labels[t] = ClinicalSurface.Distal;
            moved++;
        }

        var voted = 0;
        for (var pass = 0; pass < 10; pass++)
        {
            var changed = 0;
            var next = (ClinicalSurface[])labels.Clone();
            for (var t = 0; t < nTri; t++)
            {
                if (labels[t] != ClinicalSurface.Palatal) continue;
                if (feat.Z01[t] < 0.42) continue;
                var nP = 0;
                var nD = 0;
                foreach (var nb in neighbors[t])
                {
                    if (labels[nb] == ClinicalSurface.Palatal) nP++;
                    else if (labels[nb] == ClinicalSurface.Distal) nD++;
                }
                var tongue = nP <= 1 && nD >= 1;
                var spur = nD >= 2 && nD >= nP;
                if (!tongue && !spur) continue;
                next[t] = ClinicalSurface.Distal;
                voted++;
                changed++;
            }
            Array.Copy(next, labels, nTri);
            if (changed == 0) break;
        }

        KeepLargestAxial(labels, neighbors);
        DropIslands(labels, neighbors);
        AgentLog("C", "palatal-distal-flank",
            "{\"moved\":" + moved + ",\"voted\":" + voted + "}");
        return moved + voted;
    }

    private static string Analyze(ClinicalSurface[] labels, List<int>[] neighbors, Features feat)
    {
        var n = labels.Length;
        var comps = new int[5];
        var islands = new int[5];
        var tiny = new int[5];
        for (var s = 0; s < 5; s++)
        {
            var list = Components(labels, neighbors, (ClinicalSurface)s);
            comps[s] = list.Count;
            foreach (var c in list)
            {
                if (c.Count <= 2) tiny[s]++;
                if (c.Count < IslandSize) islands[s]++;
            }
        }

        var tongues = 0;
        var checker = 0;
        var highMix = 0;
        var holes = 0;
        for (var t = 0; t < n; t++)
        {
            var same = 0;
            var distinct = 0;
            var seen = 0;
            foreach (var nb in neighbors[t])
            {
                if (labels[nb] == labels[t]) same++;
                var bit = 1 << (int)labels[nb];
                if ((seen & bit) == 0)
                {
                    seen |= bit;
                    distinct++;
                }
            }
            if (same <= 1 && neighbors[t].Count >= 2)
                tongues++;
            if (same <= 1 && distinct >= 3)
                checker++;
            if (feat.Z01[t] >= HighTableZ && distinct >= 3)
                highMix++;
        }

        foreach (var s in new[] { 0, 1, 2, 3, 4 })
        {
            foreach (var comp in Components(labels, neighbors, (ClinicalSurface)s))
            {
                if (comp.Count >= 80) continue;
                var surround = Surround(labels, neighbors, comp, (ClinicalSurface)s);
                if (surround is not null)
                    holes++;
            }
        }

        var surfaces = new string[5];
        for (var s = 0; s < 5; s++)
        {
            var list = Components(labels, neighbors, (ClinicalSurface)s);
            var total = 0;
            var largest = 0;
            foreach (var c in list)
            {
                total += c.Count;
                if (c.Count > largest) largest = c.Count;
            }
            var pct = total == 0 ? 100 : 100.0 * largest / total;
            surfaces[s] = "{\"s\":" + s +
                          ",\"n\":" + total +
                          ",\"comps\":" + list.Count +
                          ",\"largestPct\":" + pct.ToString("0.#", CultureInfo.InvariantCulture) + "}";
        }

        return "{\"n\":" + n +
               ",\"comps\":[" + string.Join(",", comps) + "]" +
               ",\"islands\":[" + string.Join(",", islands) + "]" +
               ",\"tiny\":[" + string.Join(",", tiny) + "]" +
               ",\"tongues\":" + tongues +
               ",\"checker\":" + checker +
               ",\"highMix\":" + highMix +
               ",\"holes\":" + holes +
               ",\"surfaces\":[" + string.Join(",", surfaces) + "]}";
    }

    /// <summary>
    /// Smooth the four axial walls on the upper crown only. Never reassigns
    /// cervical red (Occlusal / color 0).
    /// </summary>
    private static void AssignUpperCrownWalls(ClinicalSurface[] labels, Features feat)
    {
        var hyster = HysteresisDeg * Math.PI / 180.0;
        for (var t = 0; t < labels.Length; t++)
        {
            if (labels[t] == ClinicalSurface.Occlusal)
                continue;
            if (feat.Z01[t] < 0.38)
                continue;
            labels[t] = SectorOf(feat.Centroids[t], feat.AxialCenter, labels[t], hyster);
        }
    }

    private static ClinicalSurface SectorOf(Point3D p, Point3D center, ClinicalSurface? current, double hyster)
    {
        var ang = Math.Atan2(p.Y - center.Y, p.X - center.X);
        if (current is ClinicalSurface keep && keep != ClinicalSurface.Occlusal)
        {
            if (InSector(ang, keep, hyster))
                return keep;
        }
        return NearestSector(ang);
    }

    private static bool InSector(double ang, ClinicalSurface surface, double hyster)
    {
        return surface switch
        {
            ClinicalSurface.Mesial => AngleIn(ang, -0.25 * Math.PI - hyster, 0.25 * Math.PI + hyster),
            ClinicalSurface.Buccal => AngleIn(ang, 0.25 * Math.PI - hyster, 0.75 * Math.PI + hyster),
            ClinicalSurface.Distal => AngleIn(ang, 0.75 * Math.PI - hyster, 1.25 * Math.PI + hyster),
            ClinicalSurface.Palatal => AngleIn(ang, -0.75 * Math.PI - hyster, -0.25 * Math.PI + hyster),
            _ => false
        };
    }

    private static bool AngleIn(double ang, double lo, double hi)
    {
        while (ang < lo) ang += 2 * Math.PI;
        while (ang > hi) ang -= 2 * Math.PI;
        return ang >= lo && ang <= hi;
    }

    private static ClinicalSurface NearestSector(double ang)
    {
        var mesial = Math.Abs(NormalizeAngle(ang));
        var buccal = Math.Abs(NormalizeAngle(ang - 0.5 * Math.PI));
        var distal = Math.Abs(NormalizeAngle(ang - Math.PI));
        var palatal = Math.Abs(NormalizeAngle(ang + 0.5 * Math.PI));
        var best = ClinicalSurface.Mesial;
        var score = mesial;
        if (buccal < score) { score = buccal; best = ClinicalSurface.Buccal; }
        if (distal < score) { score = distal; best = ClinicalSurface.Distal; }
        if (palatal < score) best = ClinicalSurface.Palatal;
        return best;
    }

    private static double NormalizeAngle(double a)
    {
        while (a > Math.PI) a -= 2 * Math.PI;
        while (a < -Math.PI) a += 2 * Math.PI;
        return a;
    }

    private static void RetractUnstable(
        ClinicalSurface[] labels, List<int>[] neighbors, Features feat, bool highTableOnly, int passes)
    {
        for (var pass = 0; pass < passes; pass++)
        {
            var next = (ClinicalSurface[])labels.Clone();
            var changed = 0;
            for (var t = 0; t < labels.Length; t++)
            {
                if (highTableOnly && feat.Z01[t] < HighTableZ)
                    continue;
                if (labels[t] == ClinicalSurface.Occlusal && feat.Z01[t] < 0.36)
                    continue;
                var votes = new int[5];
                var same = 0;
                foreach (var nb in neighbors[t])
                {
                    votes[(int)labels[nb]]++;
                    if (labels[nb] == labels[t]) same++;
                }
                if (same >= 2) continue;
                if (neighbors[t].Count < 2) continue;
                var target = Majority(votes, labels[t]);
                if (target == labels[t]) continue;
                if (target == ClinicalSurface.Occlusal && feat.Z01[t] > 0.42)
                    continue;
                next[t] = target;
                changed++;
            }
            if (changed == 0) break;
            Array.Copy(next, labels, labels.Length);
        }
    }

    private static void FillEnclosedHoles(ClinicalSurface[] labels, List<int>[] neighbors, int maxSize)
    {
        for (var s = 0; s < 5; s++)
        {
            var surface = (ClinicalSurface)s;
            foreach (var comp in Components(labels, neighbors, surface))
            {
                if (comp.Count >= maxSize) continue;
                var surround = Surround(labels, neighbors, comp, surface);
                if (surround is null) continue;
                foreach (var t in comp)
                    labels[t] = surround.Value;
            }
        }
    }

    private static ClinicalSurface? Surround(
        ClinicalSurface[] labels, List<int>[] neighbors, List<int> comp, ClinicalSurface surface)
    {
        var votes = new int[5];
        var marked = new bool[labels.Length];
        foreach (var t in comp)
            marked[t] = true;
        foreach (var t in comp)
        {
            foreach (var nb in neighbors[t])
            {
                if (marked[nb]) continue;
                votes[(int)labels[nb]]++;
            }
        }
        var best = surface;
        var n = 0;
        for (var s = 0; s < 5; s++)
        {
            if (votes[s] <= n) continue;
            n = votes[s];
            best = (ClinicalSurface)s;
        }
        if (n == 0 || best == surface) return null;
        var foreign = 0;
        for (var s = 0; s < 5; s++)
        {
            if (s != (int)best) foreign += votes[s];
        }
        return foreign * 3 <= n ? best : null;
    }

    private static void KeepLargestAxial(ClinicalSurface[] labels, List<int>[] neighbors)
    {
        for (var s = 1; s < 5; s++)
        {
            var surface = (ClinicalSurface)s;
            var comps = Components(labels, neighbors, surface);
            if (comps.Count <= 1) continue;
            var largest = comps[0];
            foreach (var c in comps)
            {
                if (c.Count > largest.Count)
                    largest = c;
            }
            foreach (var c in comps)
            {
                if (ReferenceEquals(c, largest)) continue;
                var votes = new int[5];
                foreach (var t in c)
                {
                    foreach (var nb in neighbors[t])
                    {
                        if (labels[nb] != surface)
                            votes[(int)labels[nb]]++;
                    }
                }
                var best = Majority(votes, surface);
                if (best == surface) continue;
                foreach (var t in c)
                    labels[t] = best;
            }
        }
    }

    private static void DropIslands(ClinicalSurface[] labels, List<int>[] neighbors)
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
                var surround = Surround(labels, neighbors, comp, surface);
                if (surround is null) continue;
                foreach (var t in comp)
                    labels[t] = surround.Value;
            }
        }
    }

    /// <summary>
    /// Extra majority smoothing on the chewing table so B/L/M/D seams follow
    /// neighbors instead of stair-stepping across fissures. Never reassigns
    /// cervical Occlusal. Mandibular third-molar generate path only.
    /// </summary>
    public static void SmoothHighTableAxialSeams(MeshGeometry3D crown, ClinicalSurface[] labels)
    {
        var neighbors = CrownSurfaceClassifier.BuildNeighbors(crown.TriangleIndices, labels.Length);
        var feat = Measure(crown);
        RetractUnstable(labels, neighbors, feat, highTableOnly: true, passes: 12);
        FillEnclosedHoles(labels, neighbors, 128);
        var flipped = 0;
        for (var pass = 0; pass < 6; pass++)
        {
            var changed = 0;
            for (var t = 0; t < labels.Length; t++)
            {
                if (feat.Z01[t] < HighTableZ) continue;
                if (labels[t] == ClinicalSurface.Occlusal) continue;
                var votes = new int[5];
                var same = 0;
                foreach (var nb in neighbors[t])
                {
                    votes[(int)labels[nb]]++;
                    if (labels[nb] == labels[t]) same++;
                }
                if (neighbors[t].Count < 3) continue;
                if (same * 2 >= neighbors[t].Count) continue;
                var target = Majority(votes, labels[t]);
                if (target == labels[t] || target == ClinicalSurface.Occlusal) continue;
                labels[t] = target;
                changed++;
            }
            flipped += changed;
            if (changed == 0) break;
        }
        FillEnclosedHoles(labels, neighbors, 96);
        AgentLog("B", "smooth-high-table-seams",
            "{\"flipped\":" + flipped + "}");
    }

    /// <summary>
    /// Close 1–2 triangle holes in the high crown walls so enamel does not
    /// show through the overlay. Never reassigns the cervical coral band.
    /// </summary>
    public static void SealHighCrownWallHoles(MeshGeometry3D crown, ClinicalSurface[] labels)
    {
        var neighbors = CrownSurfaceClassifier.BuildNeighbors(crown.TriangleIndices, labels.Length);
        var feat = Measure(crown);
        var absorbed = 0;
        for (var pass = 0; pass < 8; pass++)
        {
            var changed = 0;
            for (var t = 0; t < labels.Length; t++)
            {
                if (feat.Z01[t] < 0.42)
                    continue;
                var votes = new int[5];
                var same = 0;
                foreach (var nb in neighbors[t])
                {
                    votes[(int)labels[nb]]++;
                    if (labels[nb] == labels[t])
                        same++;
                }
                if (same >= 2)
                    continue;
                if (neighbors[t].Count < 2)
                    continue;
                var target = Majority(votes, labels[t]);
                if (target == labels[t])
                    continue;
                if (target == ClinicalSurface.Occlusal)
                    continue;
                labels[t] = target;
                changed++;
            }
            absorbed += changed;
            if (changed == 0)
                break;
        }
        FillEnclosedHoles(labels, neighbors, 96);
        AgentLog("B", "seal-high-crown-holes",
            "{\"absorbed\":" + absorbed + "}");
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

    private static Features Measure(MeshGeometry3D crown)
    {
        var idx = crown.TriangleIndices;
        var nTri = idx.Count / 3;
        var centroids = new Point3D[nTri];
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
            var a = crown.Positions[idx[t * 3]];
            var b = crown.Positions[idx[t * 3 + 1]];
            var c = crown.Positions[idx[t * 3 + 2]];
            var nrm = Vector3D.CrossProduct(b - a, c - a);
            if (nrm.LengthSquared < 1e-18)
                nrm = new Vector3D(0, 0, 1);
            else
                nrm.Normalize();
            facing[t] = nrm.Z;
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
        var center = an == 0
            ? new Point3D(sx / nTri, sy / nTri, 0)
            : new Point3D(ax / an, ay / an, 0);
        return new Features(centroids, z01, facing, center);
    }

    private sealed record Features(Point3D[] Centroids, double[] Z01, double[] Facing, Point3D AxialCenter);

    // #region agent log
    private static void AgentLog(string hypothesisId, string message, string dataJson)
    {
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi16-template\",\"hypothesisId\":\"" + hypothesisId +
                   "\",\"location\":\"ToothSurfaceTopology.cs\",\"message\":\"" + message +
                   "\",\"data\":" + dataJson + ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { }
    }
    // #endregion
}
