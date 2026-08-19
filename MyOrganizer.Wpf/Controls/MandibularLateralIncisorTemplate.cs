using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Reusable mandibular-lateral-incisor segmentation rules. Classifies in
/// tooth-local space after family orientation:
/// +Z incisal, +Y buccal, −Y lingual.
/// Laterality.Left (FDI 32): +X mesial, −X distal; no post-align MirrorX.
/// Laterality.Right (FDI 42): post-align MirrorX; Mesial/Distal labels
/// swap so names stay anatomical. Never copies central-incisor triangle indices.
/// Occlusal/color 0 is the cervical neck band (visible collar, like 31/33).
/// The crown walls are Buccal/Lingual/Mesial/Distal. Inner surface is
/// Lingual (enum Palatal).
/// </summary>
internal static class MandibularLateralIncisorTemplate
{
    public const string OrientationProfile = "MandibularLateralIncisor";
    public const string PipelineSource =
        "mandibular-lateral-incisor-template+cervical-occlusal+cingulum-lingual";

    public static MeshLoadOptions LoadOptions(ToothSide laterality) => new()
    {
        MirrorX = laterality == ToothSide.Right,
        OrientFdi16 = false,
        OrientationProfile = OrientationProfile
    };

    public static ClinicalSurfaceMap Generate(MeshGeometry3D crown, ToothSide laterality = ToothSide.Left)
    {
        var map = Fdi16SurfaceCurator.ApplyMandibularIncisorGeometry(
            CrownSurfaceClassifier.Classify(
                crown,
                applyFdi16Overrides: false,
                occlusalDirection: new Vector3D(0, 0, 1),
                premolarTable: true));
        var swapped = false;
        if (laterality == ToothSide.Right)
        {
            SwapMesialDistal(map);
            swapped = true;
        }
        // #region agent log
        try
        {
            var n = map.TriangleSurface.Length;
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi32-template\",\"hypothesisId\":\"D\",\"location\":\"MandibularLateralIncisorTemplate.cs\",\"message\":\"generated\",\"data\":{\"laterality\":\"" +
                       laterality + "\",\"mdSwapped\":" + (swapped ? "true" : "false") +
                       ",\"nTri\":" + n + ",\"occlusal\":" + map.Counts[0] + ",\"buccal\":" + map.Counts[1] +
                       ",\"lingual\":" + map.Counts[2] + ",\"mesial\":" + map.Counts[3] +
                       ",\"distal\":" + map.Counts[4] + ",\"copiedMandibularCentralTriangleIds\":false" +
                       "},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            System.IO.File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line);
        }
        catch { }
        // #endregion
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
