using System.Globalization;
using System.IO;
using System.Windows.Media;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Buccal view of maxillary first molar (FDI 16), traced from the user's atlas plates.
/// Scale: 1 mm = 12 design units. Roots toward the palate (up), crown toward the bite (down).
/// Canonical: mesial left. InteractiveTooth X-flips by quadrant. Canvas 200×280.
/// </summary>
internal static class FacialMolar16Geometry
{
    internal const double CanvasWidth = 200;
    internal const double CanvasHeight = 280;

    internal sealed record Set(
        Geometry Crown,
        Geometry Trunk,
        Geometry PalatalRoot,
        Geometry MesialRoot,
        Geometry DistalRoot,
        Geometry Occlusal,
        Geometry Buccal,
        Geometry Lingual,
        Geometry Mesial,
        Geometry Distal,
        Geometry Highlight,
        Geometry Fissure,
        Geometry Cervix);

    private static readonly Lazy<Set> Cache = new(Build);

    public static Set Get() => Cache.Value;

    // Atlas: crown 7.5 mm, root 12–13 mm, MD 10 mm, cervix 7 mm, trunk 4 mm.
    private const double PalatalRootLen = 13.0 * 12;
    private const double CejY = 18 + PalatalRootLen;

    private static Set Build()
    {
        var crown = P(
            "M 58,174 " +
            "C 50,188 44,208 42,226 " +
            "C 44,240 58,254 76,256 " +
            "C 88,250 96,242 100,236 " +
            "C 108,244 124,256 140,254 " +
            "C 156,248 162,230 158,210 " +
            "C 154,192 148,178 142,174 " +
            "C 118,168 82,168 58,174 Z");

        var trunk = P(
            "M 58,174 " +
            "C 82,168 118,168 142,174 " +
            "C 146,158 144,140 136,126 " +
            "C 122,118 78,118 64,126 " +
            "C 56,140 54,158 58,174 Z");

        var palatalRoot = P(
            "M 86,128 " +
            "C 82,96 86,52 96,28 " +
            "C 100,12 110,12 114,28 " +
            "C 122,52 118,96 114,128 " +
            "C 108,136 92,136 86,128 Z");

        var mesialRoot = P(
            "M 58,130 " +
            "C 42,102 36,64 44,36 " +
            "C 48,18 62,16 68,34 " +
            "C 74,62 76,98 78,128 " +
            "C 72,138 60,138 58,130 Z");

        var distalRoot = P(
            "M 122,128 " +
            "C 126,98 132,62 146,38 " +
            "C 154,20 168,22 164,40 " +
            "C 158,66 150,100 142,128 " +
            "C 136,138 124,136 122,128 Z");

        var occlusalClip = P(
            "M 32,220 " +
            "C 70,272 130,272 168,220 " +
            "C 148,246 52,246 32,220 Z");
        var palatalClip = P(
            "M 46,160 " +
            "C 82,154 118,154 154,160 " +
            "C 156,196 44,196 46,160 Z");
        var mesialClip = P(
            "M 8,170 " +
            "C -2,210 16,250 52,262 " +
            "C 62,236 64,208 58,186 " +
            "C 48,172 24,168 8,170 Z");
        var distalClip = P(
            "M 192,170 " +
            "C 202,210 184,250 148,262 " +
            "C 138,236 136,208 142,186 " +
            "C 152,172 176,168 192,170 Z");

        var occlusal = Combine(GeometryCombineMode.Intersect, crown, occlusalClip);
        var afterOcclusal = Combine(GeometryCombineMode.Exclude, crown, occlusal);
        var lingual = Combine(GeometryCombineMode.Intersect, afterOcclusal, palatalClip);
        var afterLingual = Combine(GeometryCombineMode.Exclude, afterOcclusal, lingual);
        var mesial = Combine(GeometryCombineMode.Intersect, afterLingual, mesialClip);
        var afterMesial = Combine(GeometryCombineMode.Exclude, afterLingual, mesial);
        var distal = Combine(GeometryCombineMode.Intersect, afterMesial, distalClip);
        var buccal = Combine(GeometryCombineMode.Exclude, afterMesial, distal);

        var highlight = P(
            "M 56,200 C 72,186 92,184 108,198 C 92,210 70,212 56,200 Z");
        var fissure = P(
            "M 100,236 C 100,214 100,196 100,182");
        var cervix = P(
            "M 58,174 C 82,168 118,168 142,174");

        var occlusalW = crown.Bounds.Width;
        var cervixW = trunk.Bounds.Width;
        var crownH = crown.Bounds.Height;
        var rootH = CejY - Math.Min(palatalRoot.Bounds.Top, Math.Min(mesialRoot.Bounds.Top, distalRoot.Bounds.Top));

        // #region agent log
        AgentLog("R1", "FacialMolar16Geometry.cs:Build", "atlas proportions",
            "{\"view\":\"buccal\",\"rootsUp\":true" +
            ",\"crownH\":" + F(crownH) +
            ",\"rootH\":" + F(rootH) +
            ",\"crownRootRatio\":" + F(rootH < 0.001 ? 0 : crownH / rootH) +
            ",\"expectCrownRoot\":" + F(7.5 / 12.0) +
            ",\"occlusalW\":" + F(occlusalW) +
            ",\"cervixW\":" + F(cervixW) +
            ",\"cervixOcclusalRatio\":" + F(occlusalW < 0.001 ? 0 : cervixW / occlusalW) +
            ",\"expectCervixOcclusal\":0.7" +
            ",\"crownTop\":" + F(crown.Bounds.Top) +
            ",\"rootTop\":" + F(Math.Min(palatalRoot.Bounds.Top, Math.Min(mesialRoot.Bounds.Top, distalRoot.Bounds.Top))) +
            ",\"occlusalArea\":" + F(occlusal.GetArea()) +
            ",\"buccalArea\":" + F(buccal.GetArea()) +
            ",\"lingualArea\":" + F(lingual.GetArea()) +
            ",\"mesialArea\":" + F(mesial.GetArea()) +
            ",\"distalArea\":" + F(distal.GetArea()) + "}");
        // #endregion

        return new Set(crown, trunk, palatalRoot, mesialRoot, distalRoot, occlusal, buccal, lingual, mesial, distal, highlight, fissure, cervix);
    }

    private static Geometry P(string data)
    {
        var g = Geometry.Parse(data);
        g.Freeze();
        return g;
    }

    private static Geometry Combine(GeometryCombineMode mode, Geometry a, Geometry b)
    {
        var g = new CombinedGeometry(mode, a, b);
        g.Freeze();
        return g;
    }

    // #region agent log
    private static void AgentLog(string hypothesisId, string location, string message, string dataJson)
    {
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"refit\",\"hypothesisId\":\"" + hypothesisId +
                   "\",\"location\":\"" + location + "\",\"message\":\"" + message +
                   "\",\"data\":" + dataJson + ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { /* debug ingest must not affect the tooth */ }
    }

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    // #endregion
}
