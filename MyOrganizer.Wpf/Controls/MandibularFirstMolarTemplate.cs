using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Reusable mandibular first-molar segmentation rules extracted from approved FDI 36.
/// Geometry is classified in tooth-local space after family orientation:
/// +Z occlusal, +Y buccal, −Y lingual.
/// Laterality.Left (FDI 36): +X mesial, −X distal.
/// Laterality.Right (FDI 46): post-align MirrorX keeps buccal/lingual/occlusal
/// and opposite chirality; Mesial/Distal labels are then swapped so names stay
/// anatomical. Never copies triangle indices or world coordinates.
/// </summary>
internal static class MandibularFirstMolarTemplate
{
    public const string OrientationProfile = "MandibularFirstMolar";
    public const string PipelineSource = "mandibular-first-molar-template+cervical-red-band+upper-wall-topology";

    public static MeshLoadOptions LoadOptions(ToothSide laterality) => new()
    {
        MirrorX = laterality == ToothSide.Right,
        OrientFdi16 = false,
        OrientationProfile = OrientationProfile
    };

    public static ClinicalSurfaceMap Generate(MeshGeometry3D crown, ToothSide laterality = ToothSide.Left)
    {
        var map = Fdi16SurfaceCurator.ApplyGeometry(
            CrownSurfaceClassifier.Classify(crown, applyFdi16Overrides: false));
        if (laterality == ToothSide.Right)
            SwapMesialDistal(map);
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
