using System.Windows.Media;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Frozen 2D odontogram geometries. Canonical orientation is buccal-top, mesial-left,
/// in a 100×100 design box. Families have different silhouettes — they are not one
/// path scaled to different sizes. ToothControl mirrors for arch and quadrant.
/// </summary>
internal static class ToothGeometries
{
    internal readonly record struct SurfaceSet(
        Geometry Outline,
        Geometry Buccal,
        Geometry Lingual,
        Geometry Mesial,
        Geometry Distal,
        Geometry Occlusal,
        Geometry Highlight);

    public static SurfaceSet Get(ToothKind kind) => kind switch
    {
        ToothKind.Incisor => Incisor(),
        ToothKind.Canine => Canine(),
        ToothKind.Premolar => Premolar(),
        _ => Molar()
    };

    // Narrow shovel crown, straight walls, flat incisal table.
    private static SurfaceSet Incisor() => new(
        Outline: Parse("M 40,10 H 60 Q 64,10 64,16 L 63,78 Q 50,94 37,78 L 36,16 Q 36,10 40,10 Z"),
        Buccal: Parse("M 39,12 L 61,12 L 59,30 L 41,30 Z"),
        Lingual: Parse("M 41,70 L 59,70 L 61,86 L 39,86 Z"),
        Mesial: Parse("M 37,30 L 44,34 L 44,70 L 37,70 Z"),
        Distal: Parse("M 63,30 L 56,34 L 56,70 L 63,70 Z"),
        Occlusal: Parse("M 44,32 L 56,32 L 56,68 L 44,68 Z"),
        Highlight: Parse("M 42,13 H 58 Q 54,18 50,18 Q 46,18 42,13 Z"));

    // Diamond / cuspid: pointed incisal, narrower neck than the premolar.
    private static SurfaceSet Canine() => new(
        Outline: Parse("M 50,5 L 68,32 L 63,82 Q 50,97 37,82 L 32,32 Z"),
        Buccal: Parse("M 50,9 L 64,32 L 36,32 Z"),
        Lingual: Parse("M 40,72 L 60,72 L 62,86 L 38,86 Z"),
        Mesial: Parse("M 34,34 L 44,38 L 44,70 L 37,70 Z"),
        Distal: Parse("M 66,34 L 56,38 L 56,70 L 63,70 Z"),
        Occlusal: Parse("M 44,36 L 56,36 L 58,66 L 42,66 Z"),
        Highlight: Parse("M 46,11 L 54,11 L 58,22 L 42,22 Z"));

    // Bicuspid: two-cusp buccal outline, medium occlusal table.
    private static SurfaceSet Premolar() => new(
        Outline: Parse("M 24,22 C 28,8 40,6 45,14 C 50,6 55,6 60,14 C 65,6 76,8 76,22 C 88,36 88,56 82,74 C 74,94 50,98 18,74 C 12,56 12,36 24,22 Z"),
        Buccal: Parse("M 28,16 C 36,10 48,12 50,16 C 52,12 64,10 72,16 L 68,34 L 32,34 Z"),
        Lingual: Parse("M 30,78 C 40,90 60,90 70,78 L 66,64 L 34,64 Z"),
        Mesial: Parse("M 18,28 C 16,42 16,58 22,72 L 34,64 L 34,36 Z"),
        Distal: Parse("M 82,28 C 84,42 84,58 78,72 L 66,64 L 66,36 Z"),
        Occlusal: Parse("M 36,36 C 42,30 58,30 64,36 C 70,42 70,56 64,62 C 58,68 42,68 36,62 C 30,56 30,42 36,36 Z"),
        Highlight: Parse("M 32,14 C 40,10 48,12 50,14 C 52,12 60,10 68,14 C 60,20 40,20 32,14 Z"));

    // Broad four-cusp crown and a large occlusal table.
    private static SurfaceSet Molar() => new(
        Outline: Parse("M 10,22 C 12,8 26,4 34,12 C 42,4 48,4 50,12 C 52,4 58,4 66,12 C 74,4 88,8 90,22 C 98,38 98,58 90,74 C 80,96 50,98 10,74 C 2,58 2,38 10,22 Z"),
        Buccal: Parse("M 16,16 C 26,8 38,10 42,14 C 46,10 54,10 58,14 C 62,10 74,8 84,16 L 78,34 L 22,34 Z"),
        Lingual: Parse("M 18,82 C 30,92 70,92 82,82 L 76,66 L 24,66 Z"),
        Mesial: Parse("M 8,26 C 6,42 6,58 12,74 L 26,64 L 26,36 Z"),
        Distal: Parse("M 92,26 C 94,42 94,58 88,74 L 74,64 L 74,36 Z"),
        Occlusal: Parse("M 28,36 C 36,28 64,28 72,36 C 80,44 80,58 72,66 C 64,74 36,74 28,66 C 20,58 20,44 28,36 Z"),
        Highlight: Parse("M 22,12 C 34,6 66,6 78,12 C 66,18 34,18 22,12 Z"));

    private static Geometry Parse(string data)
    {
        var g = Geometry.Parse(data);
        g.Freeze();
        return g;
    }
}

internal static class ToothBrushes
{
    public static readonly Brush Outline = Freeze(Color.FromRgb(0x8D, 0x78, 0x58));
    public static readonly Brush Seam = Freeze(Color.FromRgb(0xC4, 0xB0, 0x8E));
    public static readonly Brush Hover = Freeze(Color.FromArgb(0x58, 0x7D, 0xD3, 0xEA));
    public static readonly Brush HoverStroke = Freeze(Color.FromRgb(0x38, 0xA8, 0xC8));
    public static readonly Brush Selected = Freeze(Color.FromArgb(0x8C, 0x2F, 0x6F, 0xDB));
    public static readonly Brush SelectedHover = Freeze(Color.FromArgb(0xA8, 0x3B, 0x82, 0xF6));
    public static readonly Brush SelectedStroke = Freeze(Color.FromRgb(0x1E, 0x40, 0xAF));
    public static readonly Brush WholeSelected = Freeze(Color.FromRgb(0x1D, 0x4E, 0xD8));
    public static readonly Brush Number = Freeze(Color.FromRgb(0x16, 0x3A, 0x5F));
    public static readonly Brush Highlight = Freeze(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF));
    public static readonly Brush BuccalTint = Freeze(Color.FromArgb(0x38, 0xF6, 0xED, 0xDC));
    public static readonly Brush OcclusalTint = Freeze(Color.FromArgb(0x42, 0xE2, 0xCC, 0x9A));
    public static readonly Brush LingualTint = Freeze(Color.FromArgb(0x38, 0xE0, 0xD4, 0xBE));
    public static readonly Brush ProximalTint = Freeze(Color.FromArgb(0x30, 0xEE, 0xE4, 0xD0));

    public static readonly LinearGradientBrush Enamel = CreateEnamel();

    private static LinearGradientBrush CreateEnamel()
    {
        var g = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0.28, 0.05),
            EndPoint = new System.Windows.Point(0.85, 1)
        };
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xFC, 0xF6), 0));
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0xF0, 0xE4, 0xC8), 0.45));
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0xD4, 0xBC, 0x8E), 1));
        g.Freeze();
        return g;
    }

    private static SolidColorBrush Freeze(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
