using System.Windows.Media;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Frozen occlusal geometry for maxillary first molar FDI 16.
/// Canonical layout: buccal top, lingual bottom, mesial left, distal right.
/// <see cref="InteractiveTooth"/> X-flips by quadrant so mesial faces the midline.
/// Design space 200×200.
/// </summary>
internal static class OcclusalMolar16Geometry
{
    internal const double Canvas = 200;

    internal sealed record Set(
        Geometry Outline,
        Geometry Occlusal,
        Geometry Buccal,
        Geometry Lingual,
        Geometry Mesial,
        Geometry Distal,
        Geometry Highlight,
        Geometry Fissure);

    private static readonly Lazy<Set> Cache = new(Build);

    public static Set Get() => Cache.Value;

    private static Set Build()
    {
        var outline = P(
            "M 46,54 " +
            "C 36,72 32,96 36,118 " +
            "C 42,146 58,166 84,174 " +
            "C 104,180 128,176 148,164 " +
            "C 168,150 180,128 182,104 " +
            "C 184,78 174,54 154,42 " +
            "C 132,28 98,26 74,36 " +
            "C 58,44 50,48 46,54 Z");

        var occlusalIsland = P(
            "M 70,74 " +
            "C 86,62 118,62 134,76 " +
            "C 148,90 150,110 140,126 " +
            "C 128,142 102,148 84,140 " +
            "C 68,132 62,110 66,92 " +
            "C 68,82 68,78 70,74 Z");

        var occlusal = Combine(GeometryCombineMode.Intersect, outline, occlusalIsland);
        var ring = Combine(GeometryCombineMode.Exclude, outline, occlusal);

        var buccalClip = P(
            "M 18,8 " +
            "C 58,-6 148,-4 186,16 " +
            "C 176,48 150,78 100,80 " +
            "C 52,78 28,48 18,8 Z");
        var lingualClip = P(
            "M 16,194 " +
            "C 56,210 150,208 186,186 " +
            "C 172,150 142,122 100,122 " +
            "C 58,122 28,150 16,194 Z");
        var mesialClip = P(
            "M -8,40 " +
            "C -18,100 4,168 52,186 " +
            "C 78,140 76,78 52,36 " +
            "C 30,22 6,24 -8,40 Z");

        var buccal = Combine(GeometryCombineMode.Intersect, ring, buccalClip);
        var afterBuccal = Combine(GeometryCombineMode.Exclude, ring, buccal);
        var lingual = Combine(GeometryCombineMode.Intersect, afterBuccal, lingualClip);
        var afterLingual = Combine(GeometryCombineMode.Exclude, afterBuccal, lingual);
        var mesial = Combine(GeometryCombineMode.Intersect, afterLingual, mesialClip);
        var distal = Combine(GeometryCombineMode.Exclude, afterLingual, mesial);

        var highlight = Combine(GeometryCombineMode.Intersect, occlusal, P(
            "M 82,82 " +
            "C 96,72 116,74 126,88 " +
            "C 114,90 96,90 82,82 Z"));

        var fissure = P(
            "M 92,84 C 102,94 108,112 104,128 " +
            "M 78,80 C 96,90 122,92 140,84 " +
            "M 100,72 C 102,94 102,116 96,134 " +
            "M 84,100 C 74,108 70,118 72,128 " +
            "M 120,98 C 132,104 138,114 136,124");

        if (occlusal.GetArea() < 80 || buccal.GetArea() < 40 || lingual.GetArea() < 40 ||
            mesial.GetArea() < 40 || distal.GetArea() < 40)
        {
            throw new InvalidOperationException(
                "OcclusalMolar16Geometry produced an empty surface. " +
                $"O={occlusal.GetArea():0} B={buccal.GetArea():0} L={lingual.GetArea():0} " +
                $"M={mesial.GetArea():0} D={distal.GetArea():0}");
        }

        return new Set(outline, occlusal, buccal, lingual, mesial, distal, highlight, fissure);
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
}
