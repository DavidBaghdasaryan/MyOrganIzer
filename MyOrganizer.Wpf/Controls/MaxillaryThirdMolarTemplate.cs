using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Reusable maxillary third-molar segmentation rules in tooth-local space
/// after family orientation: +Z occlusal, +Y buccal, −Y palatal.
/// This family uses the same 3-root yaw as AlignFdi16 on FDI18_High.obj only.
/// Laterality.Right (FDI 18): no post-align MirrorX (AlignFdi16 inverts the
/// Dundee left source into right space).
/// Laterality.Left (FDI 28): post-align MirrorX keeps buccal/palatal/occlusal
/// and opposite chirality; Mesial/Distal labels are then swapped so names stay
/// anatomical. Color 0 is the low-z01 cervical neck. Never copies triangle
/// indices or world coordinates from FDI 16 or 17.
/// </summary>
internal static class MaxillaryThirdMolarTemplate
{
    public const string OrientationProfile = "MaxillaryThirdMolar";
    public const string PipelineSource =
        "maxillary-third-molar-template+cervical-red-band+palatal-distal-flank";

    public static MeshLoadOptions LoadOptions(ToothSide laterality) => new()
    {
        MirrorX = laterality == ToothSide.Left,
        OrientFdi16 = false,
        OrientationProfile = OrientationProfile
    };

    public static ClinicalSurfaceMap Generate(MeshGeometry3D crown, ToothSide laterality = ToothSide.Right)
    {
        var map = Fdi16SurfaceCurator.ApplyGeometry(
            CrownSurfaceClassifier.Classify(crown, applyFdi16Overrides: false));
        if (laterality == ToothSide.Left)
            SwapMesialDistal(map);
        ToothSurfaceTopology.RetractHighTablePalatalFromDistal(
            map.SourceCrown, map.TriangleSurface, laterality);
        map.Overrides.Clear();
        Array.Clear(map.Counts);
        foreach (var s in map.TriangleSurface)
            map.Counts[(int)s]++;
        return map;
    }

    private static void SwapMesialDistal(ClinicalSurfaceMap map)
    {
        var labels = map.TriangleSurface;
        for (var i = 0; i < labels.Length; i++)
            labels[i] = FlipMd(labels[i]);
        foreach (var key in map.Overrides.Keys.ToArray())
            map.Overrides[key] = FlipMd(map.Overrides[key]);
        (map.Counts[3], map.Counts[4]) = (map.Counts[4], map.Counts[3]);
    }

    private static ClinicalSurface FlipMd(ClinicalSurface s) => s switch
    {
        ClinicalSurface.Mesial => ClinicalSurface.Distal,
        ClinicalSurface.Distal => ClinicalSurface.Mesial,
        _ => s
    };
}
