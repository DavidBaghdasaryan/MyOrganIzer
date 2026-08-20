using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Canonical FDI 16 axes after the Dundee left molar is crown-up:
/// +Z occlusal, +Y buccal, −Y palatal, +X mesial, −X distal.
/// Do not change this mapping without an anatomical review.
/// </summary>
internal static class ToothMeshOrient
{
    /// <summary>
    /// Canonical maxillary second-molar axes after crown-up.
    /// Same 3-root palatal yaw as AlignFdi16, applied to FDI17_High.obj only.
    /// Do not copy FDI 16 triangle indices or world coordinates.
    /// </summary>
    public static void AlignMaxillarySecondMolar(MeshGeometry3D mesh, StlMeshStats stats)
    {
        AlignFdi16(mesh, stats);
        EnsureOutwardWinding(mesh, stats);
    }

    /// <summary>
    /// Canonical mandibular second-molar axes after crown-up.
    /// Same 2-root mesial yaw as AlignFdi36, applied to FDI37_High.obj only.
    /// Do not copy FDI 36 triangle indices or world coordinates.
    /// </summary>
    public static void AlignMandibularSecondMolar(MeshGeometry3D mesh, StlMeshStats stats)
    {
        AlignFdi36(mesh, stats);
        EnsureOutwardWinding(mesh, stats);
    }

    /// <summary>
    /// Canonical mandibular third-molar axes after crown-up.
    /// Same 2-root mesial yaw as AlignFdi36, applied to FDI38_High.obj only.
    /// Do not copy FDI 36/37 triangle indices or world coordinates.
    /// </summary>
    public static void AlignMandibularThirdMolar(MeshGeometry3D mesh, StlMeshStats stats)
    {
        AlignFdi36(mesh, stats);
        EnsureOutwardWinding(mesh, stats);
    }

    /// <summary>
    /// Canonical maxillary third-molar axes after crown-up.
    /// Same 3-root palatal yaw as AlignFdi16, applied to FDI18_High.obj only.
    /// Do not copy FDI 16/17 triangle indices or world coordinates.
    /// </summary>
    public static void AlignMaxillaryThirdMolar(MeshGeometry3D mesh, StlMeshStats stats)
    {
        AlignFdi16(mesh, stats);
        EnsureOutwardWinding(mesh, stats);
    }

    public static void AlignFdi16(MeshGeometry3D mesh, StlMeshStats stats)
    {
        var pts = mesh.Positions;
        if (pts.Count == 0) return;

        Bounds(pts, out var min, out var max);
        var zSpan = Math.Max(1e-9, max.Z - min.Z);
        var apicalCut = min.Z + 0.18 * zSpan;
        var apical = new List<Point3D>();
        foreach (var p in pts)
        {
            if (p.Z <= apicalCut)
                apical.Add(p);
        }

        var clusters = ClusterXy(apical, 3);
        stats.RootClusters = clusters.Count;
        if (clusters.Count < 3)
            return;

        var cents = clusters.Select(Centroid).ToArray();
        var pal = PalatalIndex(cents);
        var buccal = new[] { 0, 1, 2 }.Where(i => i != pal).ToArray();
        var mb = Area(clusters[buccal[0]]) >= Area(clusters[buccal[1]]) ? buccal[0] : buccal[1];
        var db = buccal[0] == mb ? buccal[1] : buccal[0];

        stats.Palatal = Fmt(cents[pal]);
        stats.Mb = Fmt(cents[mb]);
        stats.Db = Fmt(cents[db]);

        var palatal = new Vector3D(cents[pal].X, cents[pal].Y, 0);
        if (palatal.LengthSquared < 1e-12)
            return;
        palatal.Normalize();
        // Rotate palatal into -Y.
        var yaw = Math.Atan2(-palatal.X, -palatal.Y);
        RotateZ(pts, yaw);
        stats.YawDeg = yaw * 180.0 / Math.PI;

        var palAfter = RotateVec(cents[pal], yaw);
        var mbAfter = RotateVec(cents[mb], yaw);
        var dbAfter = RotateVec(cents[db], yaw);
        if (mbAfter.X < 0)
        {
            MirrorXKeepWinding(pts, mesh.TriangleIndices);
            stats.FlippedX = true;
            mbAfter = new Point3D(-mbAfter.X, mbAfter.Y, mbAfter.Z);
            palAfter = new Point3D(-palAfter.X, palAfter.Y, palAfter.Z);
            dbAfter = new Point3D(-dbAfter.X, dbAfter.Y, dbAfter.Z);
        }

        Recenter(pts);
        Bounds(pts, out min, out max);
        stats.Dx = max.X - min.X;
        stats.Dy = max.Y - min.Y;
        stats.Dz = max.Z - min.Z;
        stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;
        stats.Palatal = Fmt(palAfter);
        stats.Mb = Fmt(mbAfter);
        stats.Db = Fmt(dbAfter);
    }

    /// <summary>
    /// Canonical maxillary first-premolar axes after crown-up (LEFT family space):
    /// +Z occlusal, +Y buccal, −Y palatal, +X mesial, −X distal.
    /// Two apical roots give the buccal–palatal axis; the taller occlusal cusp
    /// is buccal; the buccal cusp sits slightly mesial. Right laterality is a
    /// post-align MirrorX in the loader — not this method.
    /// </summary>
    public static void AlignMaxillaryFirstPremolar(MeshGeometry3D mesh, StlMeshStats stats)
    {
        var pts = mesh.Positions;
        if (pts.Count == 0) return;

        Bounds(pts, out var min, out var max);
        var zSpan = Math.Max(1e-9, max.Z - min.Z);
        var apicalCut = min.Z + 0.18 * zSpan;
        var apical = new List<Point3D>();
        foreach (var p in pts)
        {
            if (p.Z <= apicalCut)
                apical.Add(p);
        }

        var clusters = ClusterXy(apical, 2);
        stats.RootClusters = clusters.Count;
        if (clusters.Count >= 2)
        {
            var c0 = Centroid(clusters[0]);
            var c1 = Centroid(clusters[1]);
            var bp = new Vector3D(c1.X - c0.X, c1.Y - c0.Y, 0);
            if (bp.LengthSquared >= 1e-12)
            {
                bp.Normalize();
                var yaw = Math.Atan2(bp.X, bp.Y);
                RotateZ(pts, -yaw);
                stats.YawDeg = -yaw * 180.0 / Math.PI;
            }
        }

        Bounds(pts, out min, out max);
        zSpan = Math.Max(1e-9, max.Z - min.Z);
        var zCut = max.Z - 0.14 * zSpan;
        double sumPos = 0, sumNeg = 0;
        var nPos = 0;
        var nNeg = 0;
        foreach (var p in pts)
        {
            if (p.Z < zCut) continue;
            if (p.Y >= 0)
            {
                sumPos += p.Z;
                nPos++;
            }
            else
            {
                sumNeg += p.Z;
                nNeg++;
            }
        }
        var meanPos = nPos > 0 ? sumPos / nPos : 0;
        var meanNeg = nNeg > 0 ? sumNeg / nNeg : 0;
        stats.Palatal = meanNeg.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                        "/" + meanPos.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        if (meanNeg > meanPos)
        {
            MirrorYKeepWinding(pts, mesh.TriangleIndices);
            stats.FlippedX = true;
        }

        double bx = 0;
        var bn = 0;
        Bounds(pts, out min, out max);
        zSpan = Math.Max(1e-9, max.Z - min.Z);
        zCut = max.Z - 0.14 * zSpan;
        foreach (var p in pts)
        {
            if (p.Z < zCut || p.Y < 0) continue;
            bx += p.X;
            bn++;
        }
        if (bn > 0 && bx / bn < 0)
        {
            MirrorXKeepWinding(pts, mesh.TriangleIndices);
            stats.FlippedX = true;
        }

        Recenter(pts);
        Bounds(pts, out min, out max);
        stats.Dx = max.X - min.X;
        stats.Dy = max.Y - min.Y;
        stats.Dz = max.Z - min.Z;
        stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;

        zCut = min.Z + 0.18 * Math.Max(1e-9, stats.Dz);
        var apical2 = new List<Point3D>();
        foreach (var p in pts)
        {
            if (p.Z <= zCut)
                apical2.Add(p);
        }
        var roots = ClusterXy(apical2, 2);
        if (roots.Count >= 2)
        {
            var r0 = Centroid(roots[0]);
            var r1 = Centroid(roots[1]);
            var pal = r0.Y <= r1.Y ? r0 : r1;
            var buc = r0.Y <= r1.Y ? r1 : r0;
            stats.Palatal = Fmt(pal);
            stats.Mb = Fmt(buc);
            stats.Db = Fmt(pal);
        }

        EnsureOutwardWinding(mesh, stats);
    }

    /// <summary>
    /// Canonical maxillary second-premolar axes after crown-up (LEFT family space):
    /// +Z occlusal, +Y buccal, −Y palatal, +X mesial, −X distal.
    /// One root is typical; two apical clusters or two occlusal cusps give the
    /// buccal–palatal yaw. Taller cusp is buccal. Right laterality is a
    /// post-align MirrorX in the loader — not this method.
    /// </summary>
    public static void AlignMaxillarySecondPremolar(MeshGeometry3D mesh, StlMeshStats stats)
    {
        var pts = mesh.Positions;
        if (pts.Count == 0) return;

        Bounds(pts, out var min, out var max);
        var zSpan = Math.Max(1e-9, max.Z - min.Z);
        var apicalCut = min.Z + 0.18 * zSpan;
        var apical = new List<Point3D>();
        foreach (var p in pts)
        {
            if (p.Z <= apicalCut)
                apical.Add(p);
        }

        var clusters = ClusterXy(apical, 2);
        stats.RootClusters = clusters.Count;
        if (clusters.Count >= 2)
        {
            var c0 = Centroid(clusters[0]);
            var c1 = Centroid(clusters[1]);
            var bp = new Vector3D(c1.X - c0.X, c1.Y - c0.Y, 0);
            if (bp.LengthSquared >= 1e-12)
            {
                bp.Normalize();
                var yaw = Math.Atan2(bp.X, bp.Y);
                RotateZ(pts, -yaw);
                stats.YawDeg = -yaw * 180.0 / Math.PI;
            }
        }
        else
        {
            var zHi = max.Z - 0.14 * zSpan;
            var occlusal = new List<Point3D>();
            foreach (var p in pts)
            {
                if (p.Z >= zHi)
                    occlusal.Add(p);
            }
            var cusps = ClusterXy(occlusal, 2);
            if (cusps.Count >= 2)
            {
                var c0 = Centroid(cusps[0]);
                var c1 = Centroid(cusps[1]);
                var bp = new Vector3D(c1.X - c0.X, c1.Y - c0.Y, 0);
                if (bp.LengthSquared >= 1e-12)
                {
                    bp.Normalize();
                    var yaw = Math.Atan2(bp.X, bp.Y);
                    RotateZ(pts, -yaw);
                    stats.YawDeg = -yaw * 180.0 / Math.PI;
                }
            }
        }

        Bounds(pts, out min, out max);
        zSpan = Math.Max(1e-9, max.Z - min.Z);
        var zCut = max.Z - 0.14 * zSpan;
        double sumPos = 0, sumNeg = 0;
        var nPos = 0;
        var nNeg = 0;
        foreach (var p in pts)
        {
            if (p.Z < zCut) continue;
            if (p.Y >= 0)
            {
                sumPos += p.Z;
                nPos++;
            }
            else
            {
                sumNeg += p.Z;
                nNeg++;
            }
        }
        var meanPos = nPos > 0 ? sumPos / nPos : 0;
        var meanNeg = nNeg > 0 ? sumNeg / nNeg : 0;
        stats.Palatal = meanNeg.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                        "/" + meanPos.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        if (meanNeg > meanPos)
        {
            MirrorYKeepWinding(pts, mesh.TriangleIndices);
            stats.FlippedX = true;
        }

        double bx = 0;
        var bn = 0;
        Bounds(pts, out min, out max);
        zSpan = Math.Max(1e-9, max.Z - min.Z);
        zCut = max.Z - 0.14 * zSpan;
        foreach (var p in pts)
        {
            if (p.Z < zCut || p.Y < 0) continue;
            bx += p.X;
            bn++;
        }
        if (bn > 0 && bx / bn < 0)
        {
            MirrorXKeepWinding(pts, mesh.TriangleIndices);
            stats.FlippedX = true;
        }

        Recenter(pts);
        Bounds(pts, out min, out max);
        stats.Dx = max.X - min.X;
        stats.Dy = max.Y - min.Y;
        stats.Dz = max.Z - min.Z;
        stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;

        zCut = min.Z + 0.18 * Math.Max(1e-9, stats.Dz);
        var apical2 = new List<Point3D>();
        foreach (var p in pts)
        {
            if (p.Z <= zCut)
                apical2.Add(p);
        }
        var roots = ClusterXy(apical2, 2);
        if (roots.Count >= 2)
        {
            var r0 = Centroid(roots[0]);
            var r1 = Centroid(roots[1]);
            var pal = r0.Y <= r1.Y ? r0 : r1;
            var buc = r0.Y <= r1.Y ? r1 : r0;
            stats.Palatal = Fmt(pal);
            stats.Mb = Fmt(buc);
            stats.Db = Fmt(pal);
        }

        EnsureOutwardWinding(mesh, stats);
    }

    /// <summary>
    /// Canonical mandibular first-premolar axes after crown-up (LEFT family space):
    /// +Z occlusal, +Y buccal, −Y lingual, +X mesial, −X distal.
    /// Taller occlusal cusp is buccal (unlike mandibular molars, whose lingual
    /// cusps are taller). Right laterality is a post-align MirrorX in the loader.
    /// </summary>
    public static void AlignMandibularFirstPremolar(MeshGeometry3D mesh, StlMeshStats stats)
    {
        var pts = mesh.Positions;
        if (pts.Count == 0) return;

        Bounds(pts, out var min, out var max);
        var zSpan = Math.Max(1e-9, max.Z - min.Z);
        var apicalCut = min.Z + 0.18 * zSpan;
        var apical = new List<Point3D>();
        foreach (var p in pts)
        {
            if (p.Z <= apicalCut)
                apical.Add(p);
        }

        var clusters = ClusterXy(apical, 2);
        stats.RootClusters = clusters.Count;
        if (clusters.Count >= 2)
        {
            var c0 = Centroid(clusters[0]);
            var c1 = Centroid(clusters[1]);
            var bp = new Vector3D(c1.X - c0.X, c1.Y - c0.Y, 0);
            if (bp.LengthSquared >= 1e-12)
            {
                bp.Normalize();
                var yaw = Math.Atan2(bp.X, bp.Y);
                RotateZ(pts, -yaw);
                stats.YawDeg = -yaw * 180.0 / Math.PI;
            }
        }
        else
        {
            var zHi = max.Z - 0.14 * zSpan;
            var occlusal = new List<Point3D>();
            foreach (var p in pts)
            {
                if (p.Z >= zHi)
                    occlusal.Add(p);
            }
            var cusps = ClusterXy(occlusal, 2);
            if (cusps.Count >= 2)
            {
                var c0 = Centroid(cusps[0]);
                var c1 = Centroid(cusps[1]);
                var bp = new Vector3D(c1.X - c0.X, c1.Y - c0.Y, 0);
                if (bp.LengthSquared >= 1e-12)
                {
                    bp.Normalize();
                    var yaw = Math.Atan2(bp.X, bp.Y);
                    RotateZ(pts, -yaw);
                    stats.YawDeg = -yaw * 180.0 / Math.PI;
                }
            }
        }

        Bounds(pts, out min, out max);
        zSpan = Math.Max(1e-9, max.Z - min.Z);
        var zCut = max.Z - 0.14 * zSpan;
        double sumPos = 0, sumNeg = 0;
        var nPos = 0;
        var nNeg = 0;
        foreach (var p in pts)
        {
            if (p.Z < zCut) continue;
            if (p.Y >= 0)
            {
                sumPos += p.Z;
                nPos++;
            }
            else
            {
                sumNeg += p.Z;
                nNeg++;
            }
        }
        var meanPos = nPos > 0 ? sumPos / nPos : 0;
        var meanNeg = nNeg > 0 ? sumNeg / nNeg : 0;
        stats.Palatal = meanNeg.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                        "/" + meanPos.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        if (meanNeg > meanPos)
        {
            MirrorYKeepWinding(pts, mesh.TriangleIndices);
            stats.FlippedX = true;
        }

        double bx = 0;
        var bn = 0;
        Bounds(pts, out min, out max);
        zSpan = Math.Max(1e-9, max.Z - min.Z);
        zCut = max.Z - 0.14 * zSpan;
        foreach (var p in pts)
        {
            if (p.Z < zCut || p.Y < 0) continue;
            bx += p.X;
            bn++;
        }
        if (bn > 0 && bx / bn < 0)
        {
            MirrorXKeepWinding(pts, mesh.TriangleIndices);
            stats.FlippedX = true;
        }

        Recenter(pts);
        Bounds(pts, out min, out max);
        stats.Dx = max.X - min.X;
        stats.Dy = max.Y - min.Y;
        stats.Dz = max.Z - min.Z;
        stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;

        zCut = min.Z + 0.18 * Math.Max(1e-9, stats.Dz);
        var apical2 = new List<Point3D>();
        foreach (var p in pts)
        {
            if (p.Z <= zCut)
                apical2.Add(p);
        }
        var roots = ClusterXy(apical2, 2);
        if (roots.Count >= 2)
        {
            var r0 = Centroid(roots[0]);
            var r1 = Centroid(roots[1]);
            var lin = r0.Y <= r1.Y ? r0 : r1;
            var buc = r0.Y <= r1.Y ? r1 : r0;
            stats.Palatal = Fmt(lin);
            stats.Mb = Fmt(buc);
            stats.Db = Fmt(lin);
        }

        EnsureOutwardWinding(mesh, stats);
    }

    /// <summary>
    /// Canonical mandibular second-premolar axes after crown-up (LEFT family space):
    /// +Z occlusal, +Y buccal, −Y lingual, +X mesial, −X distal.
    /// Taller occlusal cusp is buccal. Right laterality is a post-align MirrorX
    /// in the loader — not this method.
    /// </summary>
    public static void AlignMandibularSecondPremolar(MeshGeometry3D mesh, StlMeshStats stats)
    {
        var pts = mesh.Positions;
        if (pts.Count == 0) return;

        Bounds(pts, out var min, out var max);
        var zSpan = Math.Max(1e-9, max.Z - min.Z);
        var apicalCut = min.Z + 0.18 * zSpan;
        var apical = new List<Point3D>();
        foreach (var p in pts)
        {
            if (p.Z <= apicalCut)
                apical.Add(p);
        }

        var clusters = ClusterXy(apical, 2);
        stats.RootClusters = clusters.Count;
        if (clusters.Count >= 2)
        {
            var c0 = Centroid(clusters[0]);
            var c1 = Centroid(clusters[1]);
            var bp = new Vector3D(c1.X - c0.X, c1.Y - c0.Y, 0);
            if (bp.LengthSquared >= 1e-12)
            {
                bp.Normalize();
                var yaw = Math.Atan2(bp.X, bp.Y);
                RotateZ(pts, -yaw);
                stats.YawDeg = -yaw * 180.0 / Math.PI;
            }
        }
        else
        {
            var zHi = max.Z - 0.14 * zSpan;
            var occlusal = new List<Point3D>();
            foreach (var p in pts)
            {
                if (p.Z >= zHi)
                    occlusal.Add(p);
            }
            var cusps = ClusterXy(occlusal, 2);
            if (cusps.Count >= 2)
            {
                var c0 = Centroid(cusps[0]);
                var c1 = Centroid(cusps[1]);
                var bp = new Vector3D(c1.X - c0.X, c1.Y - c0.Y, 0);
                if (bp.LengthSquared >= 1e-12)
                {
                    bp.Normalize();
                    var yaw = Math.Atan2(bp.X, bp.Y);
                    RotateZ(pts, -yaw);
                    stats.YawDeg = -yaw * 180.0 / Math.PI;
                }
            }
        }

        Bounds(pts, out min, out max);
        zSpan = Math.Max(1e-9, max.Z - min.Z);
        var zCut = max.Z - 0.14 * zSpan;
        double sumPos = 0, sumNeg = 0;
        var nPos = 0;
        var nNeg = 0;
        foreach (var p in pts)
        {
            if (p.Z < zCut) continue;
            if (p.Y >= 0)
            {
                sumPos += p.Z;
                nPos++;
            }
            else
            {
                sumNeg += p.Z;
                nNeg++;
            }
        }
        var meanPos = nPos > 0 ? sumPos / nPos : 0;
        var meanNeg = nNeg > 0 ? sumNeg / nNeg : 0;
        stats.Palatal = meanNeg.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                        "/" + meanPos.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        if (meanNeg > meanPos)
        {
            MirrorYKeepWinding(pts, mesh.TriangleIndices);
            stats.FlippedX = true;
        }

        double bx = 0;
        var bn = 0;
        Bounds(pts, out min, out max);
        zSpan = Math.Max(1e-9, max.Z - min.Z);
        zCut = max.Z - 0.14 * zSpan;
        foreach (var p in pts)
        {
            if (p.Z < zCut || p.Y < 0) continue;
            bx += p.X;
            bn++;
        }
        if (bn > 0 && bx / bn < 0)
        {
            MirrorXKeepWinding(pts, mesh.TriangleIndices);
            stats.FlippedX = true;
        }

        Recenter(pts);
        Bounds(pts, out min, out max);
        stats.Dx = max.X - min.X;
        stats.Dy = max.Y - min.Y;
        stats.Dz = max.Z - min.Z;
        stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;

        zCut = min.Z + 0.18 * Math.Max(1e-9, stats.Dz);
        var apical2 = new List<Point3D>();
        foreach (var p in pts)
        {
            if (p.Z <= zCut)
                apical2.Add(p);
        }
        var roots = ClusterXy(apical2, 2);
        if (roots.Count >= 2)
        {
            var r0 = Centroid(roots[0]);
            var r1 = Centroid(roots[1]);
            var lin = r0.Y <= r1.Y ? r0 : r1;
            var buc = r0.Y <= r1.Y ? r1 : r0;
            stats.Palatal = Fmt(lin);
            stats.Mb = Fmt(buc);
            stats.Db = Fmt(lin);
        }

        EnsureOutwardWinding(mesh, stats);
    }

    /// <summary>
    /// Canonical maxillary-central-incisor axes after crown-up (LEFT family space):
    /// +Z incisal, +Y buccal, −Y palatal, +X mesial, −X distal.
    /// Palatal is the cervical cingulum offset from the incisal-edge centroid;
    /// mesial is opposite the cingulum's distal offset (fallback: shorter
    /// incisal ridge). Right laterality is a post-align MirrorX in the loader.
    /// </summary>
    public static void AlignMaxillaryCentralIncisor(MeshGeometry3D mesh, StlMeshStats stats)
    {
        var pts = mesh.Positions;
        if (pts.Count == 0) return;

        Bounds(pts, out var min, out var max);
        var zSpan = Math.Max(1e-9, max.Z - min.Z);
        var apicalCut = min.Z + 0.18 * zSpan;
        var apical = new List<Point3D>();
        foreach (var p in pts)
        {
            if (p.Z <= apicalCut)
                apical.Add(p);
        }
        var clusters = ClusterXy(apical, 2);
        stats.RootClusters = clusters.Count == 0 ? 1 : clusters.Count;

        var incisalCut = max.Z - 0.12 * zSpan;
        var cingLo = min.Z + 0.52 * zSpan;
        var cingHi = min.Z + 0.72 * zSpan;
        double edgeX = 0, edgeY = 0, edgeZ = 0;
        var edgeN = 0;
        double cingX = 0, cingY = 0;
        var cingN = 0;
        foreach (var p in pts)
        {
            if (p.Z >= incisalCut)
            {
                edgeX += p.X;
                edgeY += p.Y;
                edgeZ += p.Z;
                edgeN++;
            }
            if (p.Z >= cingLo && p.Z <= cingHi)
            {
                cingX += p.X;
                cingY += p.Y;
                cingN++;
            }
        }
        if (edgeN == 0 || cingN == 0)
        {
            Recenter(pts);
            Bounds(pts, out min, out max);
            stats.Dx = max.X - min.X;
            stats.Dy = max.Y - min.Y;
            stats.Dz = max.Z - min.Z;
            stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;
            EnsureOutwardWinding(mesh, stats);
            return;
        }
        edgeX /= edgeN;
        edgeY /= edgeN;
        edgeZ /= edgeN;
        cingX /= cingN;
        cingY /= cingN;
        var ox = cingX - edgeX;
        var oy = cingY - edgeY;
        if (ox * ox + oy * oy < 1e-12)
        {
            Recenter(pts);
            Bounds(pts, out min, out max);
            stats.Dx = max.X - min.X;
            stats.Dy = max.Y - min.Y;
            stats.Dz = max.Z - min.Z;
            stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;
            EnsureOutwardWinding(mesh, stats);
            return;
        }

        var yaw = Math.Atan2(ox, -oy);
        RotateZ(pts, -yaw);
        stats.YawDeg = -yaw * 180.0 / Math.PI;

        var edge = RotateVec(new Point3D(edgeX, edgeY, edgeZ), -yaw);
        var cing = RotateVec(new Point3D(cingX, cingY, 0), -yaw);
        if (cing.Y > edge.Y)
        {
            MirrorYKeepWinding(pts, mesh.TriangleIndices);
            stats.FlippedX = true;
            edge = new Point3D(edge.X, -edge.Y, edge.Z);
            cing = new Point3D(cing.X, -cing.Y, cing.Z);
        }

        Bounds(pts, out min, out max);
        zSpan = Math.Max(1e-9, max.Z - min.Z);
        var hiCut = max.Z - 0.14 * zSpan;
        double minHiX = double.PositiveInfinity;
        double maxHiX = double.NegativeInfinity;
        double edgeHiX = 0;
        var edgeHiN = 0;
        foreach (var p in pts)
        {
            if (p.Z < hiCut) continue;
            minHiX = Math.Min(minHiX, p.X);
            maxHiX = Math.Max(maxHiX, p.X);
            edgeHiX += p.X;
            edgeHiN++;
        }
        if (Math.Abs(cing.X - edge.X) > 1e-4)
        {
            if (cing.X > edge.X)
            {
                MirrorXKeepWinding(pts, mesh.TriangleIndices);
                stats.FlippedX = true;
            }
        }
        else if (edgeHiN > 0)
        {
            edgeHiX /= edgeHiN;
            var mesialLen = maxHiX - edgeHiX;
            var distalLen = edgeHiX - minHiX;
            if (mesialLen > distalLen)
            {
                MirrorXKeepWinding(pts, mesh.TriangleIndices);
                stats.FlippedX = true;
            }
        }

        Recenter(pts);
        Bounds(pts, out min, out max);
        stats.Dx = max.X - min.X;
        stats.Dy = max.Y - min.Y;
        stats.Dz = max.Z - min.Z;
        stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;
        stats.Palatal = Fmt(cing);
        stats.Mb = Fmt(edge);
        stats.Db = Fmt(edge);
        EnsureOutwardWinding(mesh, stats);
    }

    /// <summary>
    /// Canonical maxillary-lateral-incisor axes after crown-up (LEFT family space):
    /// +Z incisal, +Y buccal, −Y palatal, +X mesial, −X distal.
    /// Same cingulum/incisal cues as the central, on this mesh only.
    /// Right laterality is a post-align MirrorX in the loader.
    /// </summary>
    public static void AlignMaxillaryLateralIncisor(MeshGeometry3D mesh, StlMeshStats stats) =>
        AlignMaxillaryCentralIncisor(mesh, stats);

    /// <summary>
    /// Canonical maxillary-canine axes after crown-up (LEFT family space):
    /// +Z incisal, +Y buccal, −Y palatal, +X mesial, −X distal.
    /// Palatal is the cervical cingulum offset from the cusp; mesial is the
    /// shorter incisal ridge. Right laterality is a post-align MirrorX in the
    /// loader — not this method.
    /// </summary>
    public static void AlignMaxillaryCanine(MeshGeometry3D mesh, StlMeshStats stats)
    {
        var pts = mesh.Positions;
        if (pts.Count == 0) return;

        Bounds(pts, out var min, out var max);
        var zSpan = Math.Max(1e-9, max.Z - min.Z);
        var apicalCut = min.Z + 0.18 * zSpan;
        var apical = new List<Point3D>();
        foreach (var p in pts)
        {
            if (p.Z <= apicalCut)
                apical.Add(p);
        }
        var clusters = ClusterXy(apical, 2);
        stats.RootClusters = clusters.Count == 0 ? 1 : clusters.Count;

        var cuspCut = max.Z - 0.08 * zSpan;
        var cingLo = min.Z + 0.58 * zSpan;
        var cingHi = min.Z + 0.76 * zSpan;
        double cuspX = 0, cuspY = 0, cuspZ = 0;
        var cuspN = 0;
        double cingX = 0, cingY = 0;
        var cingN = 0;
        foreach (var p in pts)
        {
            if (p.Z >= cuspCut)
            {
                cuspX += p.X;
                cuspY += p.Y;
                cuspZ += p.Z;
                cuspN++;
            }
            if (p.Z >= cingLo && p.Z <= cingHi)
            {
                cingX += p.X;
                cingY += p.Y;
                cingN++;
            }
        }
        if (cuspN == 0 || cingN == 0)
        {
            Recenter(pts);
            Bounds(pts, out min, out max);
            stats.Dx = max.X - min.X;
            stats.Dy = max.Y - min.Y;
            stats.Dz = max.Z - min.Z;
            stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;
            EnsureOutwardWinding(mesh, stats);
            return;
        }
        cuspX /= cuspN;
        cuspY /= cuspN;
        cuspZ /= cuspN;
        cingX /= cingN;
        cingY /= cingN;
        var ox = cingX - cuspX;
        var oy = cingY - cuspY;
        if (ox * ox + oy * oy < 1e-12)
        {
            Recenter(pts);
            Bounds(pts, out min, out max);
            stats.Dx = max.X - min.X;
            stats.Dy = max.Y - min.Y;
            stats.Dz = max.Z - min.Z;
            stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;
            EnsureOutwardWinding(mesh, stats);
            return;
        }

        var yaw = Math.Atan2(ox, -oy);
        RotateZ(pts, -yaw);
        stats.YawDeg = -yaw * 180.0 / Math.PI;

        var cusp = RotateVec(new Point3D(cuspX, cuspY, cuspZ), -yaw);
        var cing = RotateVec(new Point3D(cingX, cingY, 0), -yaw);
        if (cing.Y > cusp.Y)
        {
            MirrorYKeepWinding(pts, mesh.TriangleIndices);
            stats.FlippedX = true;
            cusp = new Point3D(cusp.X, -cusp.Y, cusp.Z);
        }

        Bounds(pts, out min, out max);
        zSpan = Math.Max(1e-9, max.Z - min.Z);
        var hiCut = max.Z - 0.14 * zSpan;
        double minHiX = double.PositiveInfinity;
        double maxHiX = double.NegativeInfinity;
        double cuspHiX = 0;
        var cuspHiN = 0;
        foreach (var p in pts)
        {
            if (p.Z < hiCut) continue;
            minHiX = Math.Min(minHiX, p.X);
            maxHiX = Math.Max(maxHiX, p.X);
            cuspHiX += p.X;
            cuspHiN++;
        }
        if (cuspHiN > 0)
        {
            cuspHiX /= cuspHiN;
            var mesialLen = maxHiX - cuspHiX;
            var distalLen = cuspHiX - minHiX;
            if (mesialLen > distalLen)
            {
                MirrorXKeepWinding(pts, mesh.TriangleIndices);
                stats.FlippedX = true;
            }
        }

        Recenter(pts);
        Bounds(pts, out min, out max);
        stats.Dx = max.X - min.X;
        stats.Dy = max.Y - min.Y;
        stats.Dz = max.Z - min.Z;
        stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;
        stats.Palatal = Fmt(cing);
        stats.Mb = Fmt(cusp);
        stats.Db = Fmt(cusp);
        EnsureOutwardWinding(mesh, stats);
    }

    /// <summary>
    /// Canonical mandibular-canine axes after crown-up (LEFT family space):
    /// +Z incisal, +Y buccal, −Y lingual, +X mesial, −X distal.
    /// Lingual is the cervical cingulum offset from the cusp; mesial is the
    /// shorter incisal ridge. Right laterality is a post-align MirrorX in the
    /// loader — not this method.
    /// </summary>
    public static void AlignMandibularCanine(MeshGeometry3D mesh, StlMeshStats stats)
    {
        var pts = mesh.Positions;
        if (pts.Count == 0) return;

        Bounds(pts, out var min, out var max);
        var zSpan = Math.Max(1e-9, max.Z - min.Z);
        var apicalCut = min.Z + 0.18 * zSpan;
        var apical = new List<Point3D>();
        foreach (var p in pts)
        {
            if (p.Z <= apicalCut)
                apical.Add(p);
        }
        var clusters = ClusterXy(apical, 2);
        stats.RootClusters = clusters.Count == 0 ? 1 : clusters.Count;

        var cuspCut = max.Z - 0.08 * zSpan;
        var cingLo = min.Z + 0.58 * zSpan;
        var cingHi = min.Z + 0.76 * zSpan;
        double cuspX = 0, cuspY = 0, cuspZ = 0;
        var cuspN = 0;
        double cingX = 0, cingY = 0;
        var cingN = 0;
        foreach (var p in pts)
        {
            if (p.Z >= cuspCut)
            {
                cuspX += p.X;
                cuspY += p.Y;
                cuspZ += p.Z;
                cuspN++;
            }
            if (p.Z >= cingLo && p.Z <= cingHi)
            {
                cingX += p.X;
                cingY += p.Y;
                cingN++;
            }
        }
        if (cuspN == 0 || cingN == 0)
        {
            Recenter(pts);
            Bounds(pts, out min, out max);
            stats.Dx = max.X - min.X;
            stats.Dy = max.Y - min.Y;
            stats.Dz = max.Z - min.Z;
            stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;
            EnsureOutwardWinding(mesh, stats);
            return;
        }
        cuspX /= cuspN;
        cuspY /= cuspN;
        cuspZ /= cuspN;
        cingX /= cingN;
        cingY /= cingN;
        var ox = cingX - cuspX;
        var oy = cingY - cuspY;
        if (ox * ox + oy * oy < 1e-12)
        {
            Recenter(pts);
            Bounds(pts, out min, out max);
            stats.Dx = max.X - min.X;
            stats.Dy = max.Y - min.Y;
            stats.Dz = max.Z - min.Z;
            stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;
            EnsureOutwardWinding(mesh, stats);
            return;
        }

        var yaw = Math.Atan2(ox, -oy);
        RotateZ(pts, -yaw);
        stats.YawDeg = -yaw * 180.0 / Math.PI;

        var cusp = RotateVec(new Point3D(cuspX, cuspY, cuspZ), -yaw);
        var cing = RotateVec(new Point3D(cingX, cingY, 0), -yaw);
        if (cing.Y > cusp.Y)
        {
            MirrorYKeepWinding(pts, mesh.TriangleIndices);
            stats.FlippedX = true;
            cusp = new Point3D(cusp.X, -cusp.Y, cusp.Z);
        }

        Bounds(pts, out min, out max);
        zSpan = Math.Max(1e-9, max.Z - min.Z);
        var hiCut = max.Z - 0.14 * zSpan;
        double minHiX = double.PositiveInfinity;
        double maxHiX = double.NegativeInfinity;
        double cuspHiX = 0;
        var cuspHiN = 0;
        foreach (var p in pts)
        {
            if (p.Z < hiCut) continue;
            minHiX = Math.Min(minHiX, p.X);
            maxHiX = Math.Max(maxHiX, p.X);
            cuspHiX += p.X;
            cuspHiN++;
        }
        if (cuspHiN > 0)
        {
            cuspHiX /= cuspHiN;
            var mesialLen = maxHiX - cuspHiX;
            var distalLen = cuspHiX - minHiX;
            if (mesialLen > distalLen)
            {
                MirrorXKeepWinding(pts, mesh.TriangleIndices);
                stats.FlippedX = true;
            }
        }

        Recenter(pts);
        Bounds(pts, out min, out max);
        stats.Dx = max.X - min.X;
        stats.Dy = max.Y - min.Y;
        stats.Dz = max.Z - min.Z;
        stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;
        stats.Palatal = Fmt(cing);
        stats.Mb = Fmt(cusp);
        stats.Db = Fmt(cusp);
        EnsureOutwardWinding(mesh, stats);
    }

    /// <summary>
    /// Canonical mandibular-central-incisor axes after crown-up (LEFT family space):
    /// +Z incisal, +Y buccal, −Y lingual, +X mesial, −X distal.
    /// Same cingulum/incisal cues as the maxillary central, on this mesh only.
    /// Right laterality is a post-align MirrorX in the loader.
    /// </summary>
    public static void AlignMandibularCentralIncisor(MeshGeometry3D mesh, StlMeshStats stats) =>
        AlignMaxillaryCentralIncisor(mesh, stats);

    /// <summary>
    /// Canonical mandibular-lateral-incisor axes after crown-up (LEFT family space):
    /// +Z incisal, +Y buccal, −Y lingual, +X mesial, −X distal.
    /// Same cingulum/incisal cues as the mandibular central, on this mesh only.
    /// Right laterality is a post-align MirrorX in the loader.
    /// </summary>
    public static void AlignMandibularLateralIncisor(MeshGeometry3D mesh, StlMeshStats stats) =>
        AlignMaxillaryCentralIncisor(mesh, stats);

    /// <summary>
    /// Canonical FDI 36 axes after crown-up:
    /// +Z occlusal, +Y buccal, −Y lingual, +X mesial, −X distal.
    /// Uses two mandibular roots (larger = mesial) and taller occlusal cusps as lingual.
    /// Do not call AlignFdi16 for this mesh.
    /// </summary>
    public static void AlignFdi36(MeshGeometry3D mesh, StlMeshStats stats)
    {
        var pts = mesh.Positions;
        if (pts.Count == 0) return;

        Bounds(pts, out var min, out var max);
        var zSpan = Math.Max(1e-9, max.Z - min.Z);
        var apicalCut = min.Z + 0.18 * zSpan;
        var apical = new List<Point3D>();
        foreach (var p in pts)
        {
            if (p.Z <= apicalCut)
                apical.Add(p);
        }

        var clusters = ClusterXy(apical, 2);
        stats.RootClusters = clusters.Count;
        var md = new Vector3D(1, 0, 0);
        if (clusters.Count >= 2)
        {
            var cents = clusters.Select(Centroid).ToArray();
            var mesialI = Area(clusters[0]) >= Area(clusters[1]) ? 0 : 1;
            var distalI = mesialI == 0 ? 1 : 0;
            stats.Mb = Fmt(cents[mesialI]);
            stats.Db = Fmt(cents[distalI]);
            md = new Vector3D(cents[mesialI].X - cents[distalI].X, cents[mesialI].Y - cents[distalI].Y, 0);
        }
        else
        {
            var dx = max.X - min.X;
            var dy = max.Y - min.Y;
            md = dx >= dy ? new Vector3D(1, 0, 0) : new Vector3D(0, 1, 0);
        }

        if (md.LengthSquared < 1e-12)
            return;
        md.Normalize();
        var yaw = Math.Atan2(md.Y, md.X);
        RotateZ(pts, -yaw);
        stats.YawDeg = -yaw * 180.0 / Math.PI;

        Bounds(pts, out min, out max);
        zSpan = Math.Max(1e-9, max.Z - min.Z);
        var zCut = max.Z - 0.12 * zSpan;
        double sumPos = 0, sumNeg = 0;
        var nPos = 0;
        var nNeg = 0;
        foreach (var p in pts)
        {
            if (p.Z < zCut) continue;
            if (p.Y >= 0)
            {
                sumPos += p.Z;
                nPos++;
            }
            else
            {
                sumNeg += p.Z;
                nNeg++;
            }
        }
        var meanPos = nPos > 0 ? sumPos / nPos : 0;
        var meanNeg = nNeg > 0 ? sumNeg / nNeg : 0;
        stats.Palatal = meanNeg.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                        "/" + meanPos.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        if (meanPos > meanNeg)
        {
            MirrorYKeepWinding(pts, mesh.TriangleIndices);
            stats.FlippedX = true;
        }

        Recenter(pts);
        Bounds(pts, out min, out max);
        stats.Dx = max.X - min.X;
        stats.Dy = max.Y - min.Y;
        stats.Dz = max.Z - min.Z;
        stats.XyAspect = stats.Dy < 1e-9 ? 0 : stats.Dx / stats.Dy;
    }

    private static int PalatalIndex(Point3D[] c)
    {
        var best = 0;
        var bestD = double.NegativeInfinity;
        for (var i = 0; i < 3; i++)
        {
            var a = c[(i + 1) % 3];
            var b = c[(i + 2) % 3];
            var mid = new Point3D(0.5 * (a.X + b.X), 0.5 * (a.Y + b.Y), 0);
            var d = (c[i].X - mid.X) * (c[i].X - mid.X) + (c[i].Y - mid.Y) * (c[i].Y - mid.Y);
            if (d > bestD)
            {
                bestD = d;
                best = i;
            }
        }
        return best;
    }

    private static List<List<Point3D>> ClusterXy(List<Point3D> pts, int k)
    {
        var result = new List<List<Point3D>>();
        if (pts.Count < k) return result;

        var seeds = FarthestPoints(pts, k);
        var assign = new int[pts.Count];
        for (var iter = 0; iter < 16; iter++)
        {
            for (var i = 0; i < pts.Count; i++)
            {
                var best = 0;
                var bestD = double.PositiveInfinity;
                for (var s = 0; s < k; s++)
                {
                    var dx = pts[i].X - seeds[s].X;
                    var dy = pts[i].Y - seeds[s].Y;
                    var d = dx * dx + dy * dy;
                    if (d < bestD)
                    {
                        bestD = d;
                        best = s;
                    }
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
                if (counts[s] == 0) continue;
                seeds[s] = new Point3D(sums[s].X / counts[s], sums[s].Y / counts[s], sums[s].Z / counts[s]);
            }
        }

        for (var s = 0; s < k; s++)
            result.Add([]);
        for (var i = 0; i < pts.Count; i++)
            result[assign[i]].Add(pts[i]);
        return result.Where(c => c.Count > 0).ToList();
    }

    private static Point3D[] FarthestPoints(List<Point3D> pts, int k)
    {
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
                {
                    var dx = pts[i].X - seeds[t].X;
                    var dy = pts[i].Y - seeds[t].Y;
                    minD = Math.Min(minD, dx * dx + dy * dy);
                }
                if (minD > bestD)
                {
                    bestD = minD;
                    bestI = i;
                }
            }
            seeds[s] = pts[bestI];
        }
        return seeds;
    }

    private static Point3D Centroid(List<Point3D> pts)
    {
        var x = 0d;
        var y = 0d;
        var z = 0d;
        foreach (var p in pts)
        {
            x += p.X;
            y += p.Y;
            z += p.Z;
        }
        var n = Math.Max(1, pts.Count);
        return new Point3D(x / n, y / n, z / n);
    }

    private static double Area(List<Point3D> pts) => pts.Count;

    private static void RotateZ(Point3DCollection pts, double yaw)
    {
        var c = Math.Cos(yaw);
        var s = Math.Sin(yaw);
        for (var i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            pts[i] = new Point3D(p.X * c - p.Y * s, p.X * s + p.Y * c, p.Z);
        }
    }

    private static Point3D RotateVec(Point3D p, double yaw)
    {
        var c = Math.Cos(yaw);
        var s = Math.Sin(yaw);
        return new Point3D(p.X * c - p.Y * s, p.X * s + p.Y * c, p.Z);
    }

    /// <summary>
    /// Premolar Dundee winding faces inward, so shared overlay lift
    /// (along the face normal) hid every surface inside the crown.
    /// Swap triangle winding only when a majority of faces point inward.
    /// Does not change overlay materials, eps, or molar aligners.
    /// </summary>
    private static void EnsureOutwardWinding(MeshGeometry3D mesh, StlMeshStats stats)
    {
        var pts = mesh.Positions;
        var idx = mesh.TriangleIndices;
        var nTri = idx.Count / 3;
        var outward = 0;
        var inward = 0;
        for (var t = 0; t < nTri; t++)
        {
            var a = pts[idx[t * 3]];
            var b = pts[idx[t * 3 + 1]];
            var c = pts[idx[t * 3 + 2]];
            var cx = (a.X + b.X + c.X) / 3.0;
            var cy = (a.Y + b.Y + c.Y) / 3.0;
            var cz = (a.Z + b.Z + c.Z) / 3.0;
            var face = Vector3D.CrossProduct(b - a, c - a);
            if (cx * face.X + cy * face.Y + cz * face.Z >= 0)
                outward++;
            else
                inward++;
        }
        if (inward > outward)
        {
            for (var i = 0; i + 2 < idx.Count; i += 3)
                (idx[i + 1], idx[i + 2]) = (idx[i + 2], idx[i + 1]);
        }
    }

    private static void MirrorXKeepWinding(Point3DCollection pts, Int32Collection idx)
    {
        for (var i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            pts[i] = new Point3D(-p.X, p.Y, p.Z);
        }
        for (var i = 0; i + 2 < idx.Count; i += 3)
            (idx[i + 1], idx[i + 2]) = (idx[i + 2], idx[i + 1]);
    }

    private static void MirrorYKeepWinding(Point3DCollection pts, Int32Collection idx)
    {
        for (var i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            pts[i] = new Point3D(p.X, -p.Y, p.Z);
        }
        for (var i = 0; i + 2 < idx.Count; i += 3)
            (idx[i + 1], idx[i + 2]) = (idx[i + 2], idx[i + 1]);
    }

    private static void Recenter(Point3DCollection pts)
    {
        Bounds(pts, out _, out _, out var c);
        for (var i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            pts[i] = new Point3D(p.X - c.X, p.Y - c.Y, p.Z - c.Z);
        }
    }

    private static void Bounds(Point3DCollection pts, out Point3D min, out Point3D max, out Point3D centroid)
    {
        var minX = double.PositiveInfinity;
        var minY = minX;
        var minZ = minX;
        var maxX = double.NegativeInfinity;
        var maxY = maxX;
        var maxZ = maxX;
        var sx = 0d;
        var sy = 0d;
        var sz = 0d;
        foreach (var p in pts)
        {
            minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
            minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
            minZ = Math.Min(minZ, p.Z); maxZ = Math.Max(maxZ, p.Z);
            sx += p.X; sy += p.Y; sz += p.Z;
        }
        var n = Math.Max(1, pts.Count);
        min = new Point3D(minX, minY, minZ);
        max = new Point3D(maxX, maxY, maxZ);
        centroid = new Point3D(sx / n, sy / n, sz / n);
    }

    private static void Bounds(Point3DCollection pts, out Point3D min, out Point3D max)
        => Bounds(pts, out min, out max, out _);

    private static string Fmt(Point3D p)
        => p.X.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "," +
           p.Y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
}
