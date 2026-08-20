using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Static occlusal illustration of FDI 16. No hit-testing, no procedure colors.
/// Buccal up, palatal down, distal left, mesial right.
/// </summary>
public partial class OcclusalMolar16Art : UserControl
{
    public OcclusalMolar16Art()
    {
        InitializeComponent();
        Loaded += (_, _) => Paint();
    }

    private void Paint()
    {
        PartCanvas.Children.Clear();

        var outline = G(
            "M 50,38 " +
            "C 58,32 70,34 92,40 " +
            "C 126,48 150,54 166,58 " +
            "C 176,62 180,72 176,86 " +
            "C 170,118 158,148 144,162 " +
            "C 136,172 118,178 92,176 " +
            "C 64,168 40,150 32,124 " +
            "C 24,100 28,70 40,50 " +
            "C 44,42 48,38 50,38 Z");

        var lowTable = Intersect(outline, G(
            "M 58,56 " +
            "C 88,48 128,56 154,68 " +
            "C 166,86 160,124 140,148 " +
            "C 112,164 72,154 52,128 " +
            "C 42,104 44,74 58,56 Z"));

        var mbRidge = Intersect(outline, G(
            "M 166,58 " +
            "C 148,62 132,74 120,96 " +
            "C 132,88 150,72 166,62 " +
            "C 168,58 168,58 166,58 Z"));
        var dbRidge = Intersect(outline, G(
            "M 50,40 " +
            "C 70,46 92,58 108,88 " +
            "C 92,72 68,52 50,42 " +
            "C 50,40 50,40 50,40 Z"));
        var mpRidge = Intersect(outline, G(
            "M 144,160 " +
            "C 136,140 124,122 112,108 " +
            "C 128,122 140,144 146,160 " +
            "C 144,162 144,160 144,160 Z"));
        var dpRidge = Intersect(outline, G(
            "M 38,144 " +
            "C 56,136 78,124 96,114 " +
            "C 78,128 54,140 38,144 " +
            "C 38,144 38,144 38,144 Z"));
        var ridge = Intersect(outline, G(
            "M 138,148 " +
            "C 114,122 88,88 66,54 " +
            "C 86,50 116,84 140,118 " +
            "C 150,134 148,146 138,148 Z"));

        var centralFossa = Intersect(outline, G(
            "M 100,104 " +
            "C 114,100 128,108 128,122 " +
            "C 126,134 110,140 98,134 " +
            "C 88,128 90,110 100,104 Z"));
        var mesialFossa = Intersect(outline, G(
            "M 150,90 " +
            "C 162,88 170,98 166,108 " +
            "C 158,116 146,112 144,102 " +
            "C 144,94 146,90 150,90 Z"));
        var distalFossa = Intersect(outline, G(
            "M 60,104 " +
            "C 72,100 82,108 78,120 " +
            "C 70,128 58,124 54,114 " +
            "C 54,106 56,104 60,104 Z"));

        var mesialRidge = Intersect(outline, G(
            "M 166,72 " +
            "C 180,96 176,132 154,156 " +
            "C 166,128 172,96 166,72 Z"));
        var distalRidge = Intersect(outline, G(
            "M 38,78 " +
            "C 24,102 28,132 48,150 " +
            "C 34,126 30,100 38,78 Z"));

        Add(outline, Enamel, Edge, 0.3);
        Add(lowTable, TableWash, null, 0);
        Add(mbRidge, CuspMb, null, 0);
        Add(dbRidge, CuspDb, null, 0);
        Add(mpRidge, CuspMp, null, 0);
        Add(dpRidge, CuspDp, null, 0);
        Add(mesialRidge, RidgeLight, null, 0);
        Add(distalRidge, RidgeLight, null, 0);
        Add(ridge, ObliqueLight, null, 0);
        Add(centralFossa, FossaDeep, null, 0);
        Add(mesialFossa, FossaMid, null, 0);
        Add(distalFossa, FossaMid, null, 0);
        Add(Intersect(outline, new EllipseGeometry(new Point(160, 56), 6, 4.5)), TipLight, null, 0);
        Add(Intersect(outline, new EllipseGeometry(new Point(58, 44), 5, 4)), TipLight, null, 0);
        Add(Intersect(outline, new EllipseGeometry(new Point(136, 148), 8, 6)), TipLight, null, 0);

    }

    private void Add(Geometry data, Brush fill, Brush? stroke, double thickness)
    {
        PartCanvas.Children.Add(new System.Windows.Shapes.Path
        {
            Data = data,
            Fill = fill,
            Stroke = stroke ?? Brushes.Transparent,
            StrokeThickness = thickness,
            StrokeLineJoin = PenLineJoin.Round,
            IsHitTestVisible = false
        });
    }

    private static Geometry G(string data)
    {
        var g = Geometry.Parse(data);
        g.Freeze();
        return g;
    }

    private static Geometry Intersect(Geometry outline, Geometry inner)
    {
        var g = new CombinedGeometry(GeometryCombineMode.Intersect, outline, inner);
        g.Freeze();
        return g;
    }

    private static readonly Brush Enamel = Radial(
        new Point(0.36, 0.26), new Point(0.48, 0.46), 0.88, 0.70,
        (0, Color.FromRgb(0xFB, 0xF3, 0xE4)),
        (0.42, Color.FromRgb(0xF0, 0xE2, 0xC6)),
        (0.78, Color.FromRgb(0xE2, 0xCD, 0xAB)),
        (1, Color.FromRgb(0xD2, 0xB6, 0x8E)));

    private static readonly Brush TableWash = Solid(0x28, 0xC8, 0xA8, 0x7C);
    private static readonly Brush CuspMp = Radial(
        new Point(0.46, 0.40), new Point(0.50, 0.48), 0.62, 0.58,
        (0, Color.FromArgb(0x66, 0xFF, 0xF8, 0xEC)),
        (1, Color.FromArgb(0x00, 0xF0, 0xE2, 0xC6)));
    private static readonly Brush CuspMb = Radial(
        new Point(0.42, 0.36), new Point(0.48, 0.44), 0.60, 0.55,
        (0, Color.FromArgb(0x58, 0xFF, 0xF7, 0xEA)),
        (1, Color.FromArgb(0x00, 0xF0, 0xE2, 0xC6)));
    private static readonly Brush CuspDb = Radial(
        new Point(0.40, 0.34), new Point(0.48, 0.44), 0.58, 0.52,
        (0, Color.FromArgb(0x48, 0xFF, 0xF6, 0xE8)),
        (1, Color.FromArgb(0x00, 0xF0, 0xE2, 0xC6)));
    private static readonly Brush CuspDp = Radial(
        new Point(0.44, 0.38), new Point(0.50, 0.46), 0.55, 0.50,
        (0, Color.FromArgb(0x3A, 0xFF, 0xF5, 0xE6)),
        (1, Color.FromArgb(0x00, 0xF0, 0xE2, 0xC6)));
    private static readonly Brush ObliqueLight = Solid(0x58, 0xFF, 0xF4, 0xE4);
    private static readonly Brush RidgeLight = Solid(0x2C, 0xFF, 0xF8, 0xEE);
    private static readonly Brush FossaDeep = Radial(
        new Point(0.5, 0.5), new Point(0.5, 0.5), 0.55, 0.55,
        (0, Color.FromArgb(0x52, 0xA8, 0x84, 0x58)),
        (1, Color.FromArgb(0x00, 0xC8, 0xA8, 0x7C)));
    private static readonly Brush FossaMid = Radial(
        new Point(0.5, 0.5), new Point(0.5, 0.5), 0.55, 0.55,
        (0, Color.FromArgb(0x3A, 0xB0, 0x8C, 0x62)),
        (1, Color.FromArgb(0x00, 0xC8, 0xA8, 0x7C)));
    private static readonly Brush Valley = Solid(0x42, 0xA8, 0x84, 0x5A);
    private static readonly Brush TipLight = Radial(
        new Point(0.5, 0.5), new Point(0.5, 0.5), 0.5, 0.5,
        (0, Color.FromArgb(0x50, 0xFF, 0xFB, 0xF2)),
        (1, Color.FromArgb(0x00, 0xFF, 0xF8, 0xEC)));
    private static readonly Brush Edge = Solid(0xFF, 0xD4, 0xC0, 0xA0);

    private static Brush Radial(Point origin, Point center, double rx, double ry, params (double o, Color c)[] stops)
    {
        var b = new RadialGradientBrush
        {
            GradientOrigin = origin,
            Center = center,
            RadiusX = rx,
            RadiusY = ry
        };
        foreach (var (o, c) in stops)
            b.GradientStops.Add(new GradientStop(c, o));
        b.Freeze();
        return b;
    }

    private static Brush Solid(byte a, byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
        brush.Freeze();
        return brush;
    }

}
