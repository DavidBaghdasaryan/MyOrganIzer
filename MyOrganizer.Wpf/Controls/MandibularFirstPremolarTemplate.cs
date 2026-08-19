using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Reusable mandibular first-premolar segmentation rules. Classifies in
/// tooth-local space after family orientation:
/// +Z occlusal, +Y buccal, −Y lingual.
/// Laterality.Left (FDI 34): +X mesial, −X distal; no post-align MirrorX.
/// Laterality.Right (FDI 44): post-align MirrorX; Mesial/Distal labels swap
/// so names stay anatomical. Never copies maxillary-premolar or molar triangle indices.
/// Occlusal/color 0 is the cervical neck band. The chewing table is
/// Buccal/Lingual/Mesial/Distal. Inner surface is Lingual (enum Palatal).
/// </summary>
internal static class MandibularFirstPremolarTemplate
{
    public const string OrientationProfile = "MandibularFirstPremolar";
    public const string PipelineSource =
        "mandibular-first-premolar-template+cervical-occlusal+upper-wall-topology";

    public static MeshLoadOptions LoadOptions(ToothSide laterality) => new()
    {
        MirrorX = laterality == ToothSide.Right,
        OrientFdi16 = false,
        OrientationProfile = OrientationProfile
    };

    public static ClinicalSurfaceMap Generate(MeshGeometry3D crown, ToothSide laterality = ToothSide.Left)
    {
        var map = Fdi16SurfaceCurator.ApplyPremolarGeometry(
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
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi34-template\",\"hypothesisId\":\"D\",\"location\":\"MandibularFirstPremolarTemplate.cs\",\"message\":\"generated\",\"data\":{\"laterality\":\"" +
                       laterality + "\",\"mdSwapped\":" + (swapped ? "true" : "false") +
                       ",\"nTri\":" + n + ",\"occlusal\":" + map.Counts[0] + ",\"buccal\":" + map.Counts[1] +
                       ",\"lingual\":" + map.Counts[2] + ",\"mesial\":" + map.Counts[3] +
                       ",\"distal\":" + map.Counts[4] + ",\"copiedMaxPremolarTriangleIds\":false" +
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
