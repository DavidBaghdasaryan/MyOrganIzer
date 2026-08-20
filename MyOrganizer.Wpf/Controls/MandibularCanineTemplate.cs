using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Reusable mandibular-canine segmentation rules. Classifies in tooth-local
/// space after family orientation:
/// +Z incisal, +Y buccal, −Y lingual.
/// Laterality.Left (FDI 33): +X mesial, −X distal; no post-align MirrorX.
/// Laterality.Right (FDI 43): post-align MirrorX; Mesial/Distal labels swap
/// so names stay anatomical. Never copies maxillary-canine or premolar triangle indices.
/// Occlusal/color 0 is the cervical neck band. The crown walls are
/// Buccal/Lingual/Mesial/Distal. Inner surface is Lingual (enum Palatal).
/// </summary>
internal static class MandibularCanineTemplate
{
    public const string OrientationProfile = "MandibularCanine";
    public const string PipelineSource =
        "mandibular-canine-template+cervical-occlusal+cingulum-lingual";

    public static MeshLoadOptions LoadOptions(ToothSide laterality) => new()
    {
        MirrorX = laterality == ToothSide.Right,
        OrientFdi16 = false,
        OrientationProfile = OrientationProfile
    };

    public static ClinicalSurfaceMap Generate(MeshGeometry3D crown, ToothSide laterality = ToothSide.Left)
    {
        var map = Fdi16SurfaceCurator.ApplyMandibularCanineGeometry(
            CrownSurfaceClassifier.Classify(
                crown,
                applyFdi16Overrides: false,
                occlusalDirection: new Vector3D(0, 0, 1),
                premolarTable: true));
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
