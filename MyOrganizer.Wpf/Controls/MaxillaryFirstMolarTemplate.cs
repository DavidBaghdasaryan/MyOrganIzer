using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Reusable maxillary first-molar segmentation rules extracted from approved FDI 16.
/// Geometry is classified in tooth-local space after family orientation:
/// +Z occlusal, +Y buccal, −Y palatal.
/// Frozen FDI 16 (Right) is geometrically unmirrored (3-root AlignFdi16).
/// Laterality.Left (FDI 26): post-align MirrorX keeps buccal/palatal/occlusal
/// and opposite chirality; Mesial/Distal labels are then swapped so names stay
/// anatomical. Never copies triangle indices or world coordinates.
/// </summary>
internal static class MaxillaryFirstMolarTemplate
{
    public const string OrientationProfile = "MaxillaryFirstMolar";
    public const string PipelineSource =
        "maxillary-first-molar-template+high-cervical-band+upper-wall-topology";

    public static MeshLoadOptions LoadOptions(ToothSide laterality) => new()
    {
        MirrorX = laterality == ToothSide.Left,
        OrientFdi16 = true,
        OrientationProfile = OrientationProfile
    };

    public static ClinicalSurfaceMap Generate(MeshGeometry3D crown, ToothSide laterality = ToothSide.Right)
    {
        var map = Fdi16SurfaceCurator.ApplyMaxillaryGeometry(
            CrownSurfaceClassifier.Classify(crown, applyFdi16Overrides: false));
        var swapped = false;
        if (laterality == ToothSide.Left)
        {
            SwapMesialDistal(map);
            swapped = true;
        }
        // #region agent log
        try
        {
            var n = map.TriangleSurface.Length;
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi26-template\",\"hypothesisId\":\"D\",\"location\":\"MaxillaryFirstMolarTemplate.cs\",\"message\":\"generated\",\"data\":{\"laterality\":\"" +
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
