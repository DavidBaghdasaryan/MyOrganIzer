using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Reusable maxillary second-molar segmentation rules in tooth-local space
/// after family orientation: +Z occlusal, +Y buccal, −Y palatal.
/// Frozen FDI 16 (Right) is geometrically unmirrored (3-root AlignFdi16).
/// This family uses the same 3-root yaw on FDI17_High.obj only.
/// Laterality.Right (FDI 17): no post-align MirrorX.
/// Laterality.Left (FDI 27): post-align MirrorX keeps buccal/palatal/occlusal
/// and opposite chirality; Mesial/Distal labels are then swapped so names stay
/// anatomical. Color 0 is the low-z01 cervical neck (this mesh is crown-up;
/// the frozen 16 high-z01 band would miss the CEJ). Never copies triangle
/// indices or world coordinates from FDI 16.
/// </summary>
internal static class MaxillarySecondMolarTemplate
{
    public const string OrientationProfile = "MaxillarySecondMolar";
    public const string PipelineSource =
        "maxillary-second-molar-template+cervical-red-band+palatal-distal-flank";

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
        var swapped = false;
        if (laterality == ToothSide.Left)
        {
            SwapMesialDistal(map);
            swapped = true;
        }
        ToothSurfaceTopology.RetractHighTablePalatalFromDistal(
            map.SourceCrown, map.TriangleSurface, laterality);
        map.Overrides.Clear();
        Array.Clear(map.Counts);
        foreach (var s in map.TriangleSurface)
            map.Counts[(int)s]++;
        // #region agent log
        try
        {
            var n = map.TriangleSurface.Length;
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi17-template\",\"hypothesisId\":\"D\",\"location\":\"MaxillarySecondMolarTemplate.cs\",\"message\":\"generated\",\"data\":{\"laterality\":\"" +
                       laterality + "\",\"mdSwapped\":" + (swapped ? "true" : "false") +
                       ",\"nTri\":" + n + ",\"occlusal\":" + map.Counts[0] + ",\"buccal\":" + map.Counts[1] +
                       ",\"palatal\":" + map.Counts[2] + ",\"mesial\":" + map.Counts[3] +
                       ",\"distal\":" + map.Counts[4] + ",\"overrides\":" + map.Overrides.Count +
                       ",\"copied16TriangleIds\":false" +
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
