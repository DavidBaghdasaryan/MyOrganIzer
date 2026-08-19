using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Reusable mandibular third-molar segmentation rules in tooth-local space
/// after family orientation: +Z occlusal, +Y buccal, −Y lingual.
/// Laterality.Left (FDI 38): no post-align MirrorX; +X mesial, −X distal.
/// Laterality.Right (FDI 48): post-align MirrorX keeps buccal/lingual/occlusal
/// and opposite chirality; Mesial/Distal labels are then swapped so names stay
/// anatomical. Color 0 is the low-z01 cervical neck. Never copies triangle
/// indices or world coordinates from FDI 36 or 37.
/// </summary>
internal static class MandibularThirdMolarTemplate
{
    public const string OrientationProfile = "MandibularThirdMolar";
    public const string PipelineSource =
        "mandibular-third-molar-template+cervical-red-band+seal-wall-holes+smooth-table-seams";

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
        var swapped = false;
        if (laterality == ToothSide.Right)
        {
            SwapMesialDistal(map);
            swapped = true;
        }
        ToothSurfaceTopology.SealHighCrownWallHoles(map.SourceCrown, map.TriangleSurface);
        ToothSurfaceTopology.SmoothHighTableAxialSeams(map.SourceCrown, map.TriangleSurface);
        map.Overrides.Clear();
        Array.Clear(map.Counts);
        foreach (var s in map.TriangleSurface)
            map.Counts[(int)s]++;
        // #region agent log
        try
        {
            var n = map.TriangleSurface.Length;
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi38-template\",\"hypothesisId\":\"D\",\"location\":\"MandibularThirdMolarTemplate.cs\",\"message\":\"generated\",\"data\":{\"laterality\":\"" +
                       laterality + "\",\"mdSwapped\":" + (swapped ? "true" : "false") +
                       ",\"nTri\":" + n + ",\"occlusal\":" + map.Counts[0] + ",\"buccal\":" + map.Counts[1] +
                       ",\"lingual\":" + map.Counts[2] + ",\"mesial\":" + map.Counts[3] +
                       ",\"distal\":" + map.Counts[4] + ",\"overrides\":" + map.Overrides.Count +
                       ",\"copied36TriangleIds\":false" +
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
