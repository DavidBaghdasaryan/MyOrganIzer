using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Reusable maxillary second-premolar segmentation rules. Classifies in
/// tooth-local space after family orientation:
/// +Z occlusal, +Y buccal, −Y palatal.
/// Laterality.Left (FDI 25): +X mesial, −X distal; no post-align MirrorX.
/// Laterality.Right (FDI 15): post-align MirrorX; Mesial/Distal labels swap
/// so names stay anatomical. Never copies first-premolar or molar triangle indices.
/// Occlusal/color 0 is the cervical neck band. The chewing table is
/// Buccal/Palatal/Mesial/Distal.
/// </summary>
internal static class MaxillarySecondPremolarTemplate
{
    public const string OrientationProfile = "MaxillarySecondPremolar";
    public const string PipelineSource =
        "maxillary-second-premolar-template+cervical-occlusal+upper-wall-topology";

    public static MeshLoadOptions LoadOptions(ToothSide laterality) => new()
    {
        MirrorX = laterality == ToothSide.Right,
        OrientFdi16 = false,
        OrientationProfile = OrientationProfile
    };

    public static ClinicalSurfaceMap Generate(MeshGeometry3D crown, ToothSide laterality = ToothSide.Right)
    {
        var map = Fdi16SurfaceCurator.ApplyPremolarGeometry(
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
