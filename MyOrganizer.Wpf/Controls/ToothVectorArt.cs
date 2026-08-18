using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace MyOrganizer.Wpf.Controls;

internal enum ToothPartKind
{
    Root,
    Crown,
    Highlight,
    Cervix,
    Fissure
}

internal readonly record struct ToothVectorPart(
    Geometry Data,
    Brush Fill,
    Brush Stroke,
    double StrokeThickness,
    ToothPartKind Kind);

internal sealed class ToothVectorModel
{
    public required IReadOnlyList<ToothVectorPart> Parts { get; init; }
    public required Geometry Crown { get; init; }
    public required Geometry Root { get; init; }
    public required Geometry Body { get; init; }
    public required Geometry Canal { get; init; }
    public required Geometry Implant { get; init; }
    public required Geometry Buccal { get; init; }
    public required Geometry Lingual { get; init; }
    public required Geometry Mesial { get; init; }
    public required Geometry Distal { get; init; }
    public required Geometry Occlusal { get; init; }
}

/// <summary>
/// Side-view odontogram art drawn as overlapping vector parts (roots, crown, highlight).
/// Authored crown-up / roots-down, mesial on the left. ToothControl flips for arch and quadrant.
/// </summary>
internal static class ToothVectorArt
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static ToothVectorModel Build(string fdi)
    {
        var visual = ToothFdi.Visual(fdi);
        var upper = ToothFdi.IsUpper(fdi);

        var spec = visual switch
        {
            ToothVisualType.CentralIncisor => Incisor(central: true),
            ToothVisualType.LateralIncisor => Incisor(central: false),
            ToothVisualType.Canine => Canine(),
            ToothVisualType.FirstPremolar => Premolar(first: true, twoRoots: upper),
            ToothVisualType.SecondPremolar => Premolar(first: false, twoRoots: false),
            ToothVisualType.FirstMolar => Molar(58, threeRoots: upper),
            ToothVisualType.SecondMolar => Molar(52, threeRoots: upper),
            _ => Molar(46, threeRoots: upper)
        };

        var crown = spec.Crown;
        var root = spec.Root;
        var body = Union(root, crown);
        var bounds = crown.Bounds;
        var pad = Math.Max(1.1, bounds.Width * 0.07);
        var buccalH = bounds.Height * 0.34;
        var lingualH = bounds.Height * 0.28;
        var sideW = bounds.Width * 0.26;

        return new ToothVectorModel
        {
            Parts = spec.Parts,
            Crown = crown,
            Root = root,
            Body = body,
            Canal = spec.Canal,
            Implant = ImplantScrew(spec.CervixY, spec.ApexY, spec.CrownW),
            Buccal = InsideRound(crown, new Rect(bounds.X + pad, bounds.Y, bounds.Width - 2 * pad, buccalH)),
            Lingual = InsideRound(crown, new Rect(bounds.X + pad, bounds.Bottom - lingualH, bounds.Width - 2 * pad, lingualH)),
            Mesial = InsideRound(crown, new Rect(bounds.X, bounds.Y + buccalH * 0.72, sideW, bounds.Height * 0.42)),
            Distal = InsideRound(crown, new Rect(bounds.Right - sideW, bounds.Y + buccalH * 0.72, sideW, bounds.Height * 0.42)),
            Occlusal = InsideEllipse(crown, bounds)
        };
    }

    private readonly record struct Spec(
        IReadOnlyList<ToothVectorPart> Parts,
        Geometry Crown,
        Geometry Root,
        Geometry Canal,
        double CervixY,
        double ApexY,
        double CrownW);

    private const double Cx = 44;
    private const double CervixY = 56;
    private const double ApexY = 120;

    private static Spec Incisor(bool central)
    {
        var w = central ? 32.0 : 26.0;
        var yTop = central ? 14.0 : 16.0;
        var crown = ShovelCrown(Cx, yTop, CervixY, w, point: 0);
        var root = Tube(Cx, CervixY - 12, ApexY, w * 0.44, 5.6, 1.2);
        return Assemble(
            [root],
            [crown],
            Highlight(Cx, yTop, w, CervixY - yTop),
            Fissure: null,
            CanalLine(Cx, CervixY, ApexY - 14),
            w);
    }

    private static Spec Canine()
    {
        const double w = 34;
        const double yTop = 10;
        var crown = ShovelCrown(Cx, yTop, CervixY, w, point: 6.5);
        var root = Tube(Cx, CervixY - 12, ApexY + 1, 11.4, 5.4, 1.8);
        return Assemble(
            [root],
            [crown],
            Highlight(Cx, yTop + 6, w, CervixY - yTop),
            null,
            CanalLine(Cx, CervixY, ApexY - 14),
            w);
    }

    private static Spec Premolar(bool first, bool twoRoots)
    {
        var w = first ? 40.0 : 38.0;
        const double yTop = 13;
        var crown = CuspCrown(Cx, yTop, CervixY, w, cusps: 2);
        List<Geometry> roots;
        Geometry canal;
        if (twoRoots)
        {
            var mesial = Tube(35, CervixY - 11, ApexY, 9.2, 5.0, -3.6);
            var distal = Tube(53, CervixY - 11, ApexY - 1, 9.0, 5.0, 3.6);
            roots = [mesial, distal];
            canal = Combine(CanalLine(35, CervixY, ApexY - 14), CanalLine(53, CervixY, ApexY - 14));
        }
        else
        {
            var one = Tube(Cx, CervixY - 11, ApexY, 10.4, 5.4, 1.2);
            roots = [one];
            canal = CanalLine(Cx, CervixY, ApexY - 14);
        }

        return Assemble(
            roots,
            [crown],
            Highlight(Cx, yTop + 2, w, CervixY - yTop),
            Combine(FissureLine(Cx, yTop + 11, CervixY - 14), FissureCross(Cx, yTop + 22, w * 0.18)),
            canal,
            w);
    }

    private static Spec Molar(double w, bool threeRoots)
    {
        const double yTop = 12;
        var size = w / 58.0;
        var crown = CuspCrown(Cx, yTop, CervixY, w, cusps: 3);
        var span = w * 0.28;
        var mesial = Tube(Cx - span, CervixY - 11, ApexY, Math.Max(8.6, 10.2 * size), 5.2, -4.2 * size);
        var distal = Tube(Cx + span, CervixY - 11, ApexY - 1, Math.Max(8.4, 10.0 * size), 5.2, 4.2 * size);
        List<Geometry> roots = [mesial, distal];
        var canal = Combine(
            CanalLine(Cx - span, CervixY, ApexY - 12),
            CanalLine(Cx + span, CervixY, ApexY - 14));
        if (threeRoots)
        {
            var palatal = Tube(Cx, CervixY - 11, ApexY - 6, Math.Max(7.8, 9.2 * size), 4.8, 0);
            roots.Insert(0, palatal);
            canal = Combine(canal, CanalLine(Cx, CervixY, ApexY - 16));
        }

        return Assemble(
            roots,
            [crown],
            Highlight(Cx, yTop + 2, w, CervixY - yTop),
            Combine(FissureLine(Cx, yTop + 12, CervixY - 15), FissureCross(Cx, yTop + 24, w * 0.22)),
            canal,
            w);
    }

    private static Spec Assemble(
        IReadOnlyList<Geometry> roots,
        IReadOnlyList<Geometry> crowns,
        Geometry highlight,
        Geometry? Fissure,
        Geometry canal,
        double crownW)
    {
        var parts = new List<ToothVectorPart>();
        foreach (var root in roots)
            parts.Add(new ToothVectorPart(root, RootFill, RootStroke, 0.45, ToothPartKind.Root));
        foreach (var crown in crowns)
            parts.Add(new ToothVectorPart(crown, EnamelFill, CrownStroke, 0.55, ToothPartKind.Crown));
        parts.Add(new ToothVectorPart(highlight, HighlightFill, Brushes.Transparent, 0, ToothPartKind.Highlight));
        if (Fissure is not null)
            parts.Add(new ToothVectorPart(Fissure, Brushes.Transparent, FissureStroke, 0.85, ToothPartKind.Fissure));

        return new Spec(parts, Union(crowns.ToArray()), Union(roots.ToArray()), canal, CervixY, ApexY, crownW);
    }

    private static Geometry ShovelCrown(double cx, double yTop, double yCervix, double width, double point)
    {
        var left = cx - width / 2;
        var right = cx + width / 2;
        var mid = (yTop + yCervix) / 2;
        var incisal = yTop - point;
        return P(
            $"M {N(left)},{N(yCervix)} " +
            $"C {N(left - 1.6)},{N(mid)} {N(left + 1.5)},{N(yTop + 11)} {N(left + width * 0.1)},{N(yTop + 3.5)} " +
            $"C {N(cx - width * 0.22)},{N(incisal)} {N(cx + width * 0.22)},{N(incisal)} {N(right - width * 0.1)},{N(yTop + 3.5)} " +
            $"C {N(right - 1.5)},{N(yTop + 11)} {N(right + 1.6)},{N(mid)} {N(right)},{N(yCervix)} " +
            $"C {N(cx + width * 0.2)},{N(yCervix + 3.2)} {N(cx - width * 0.2)},{N(yCervix + 3.2)} {N(left)},{N(yCervix)} Z");
    }

    private static Geometry CuspCrown(double cx, double yTop, double yCervix, double width, int cusps)
    {
        var left = cx - width / 2;
        var right = cx + width / 2;
        var mid = (yTop + yCervix) / 2;
        var valley = yTop + Math.Min(6.5, width * 0.11);
        if (cusps >= 3)
        {
            return P(
                $"M {N(left)},{N(yCervix)} " +
                $"C {N(left - 2.8)},{N(mid)} {N(left + 1)},{N(valley + 2)} {N(left + width * 0.14)},{N(valley)} " +
                $"C {N(left + width * 0.22)},{N(yTop)} {N(left + width * 0.30)},{N(yTop)} {N(left + width * 0.36)},{N(valley - 0.8)} " +
                $"C {N(cx - 5)},{N(yTop - 2.2)} {N(cx + 5)},{N(yTop - 2.2)} {N(left + width * 0.64)},{N(valley - 0.8)} " +
                $"C {N(left + width * 0.70)},{N(yTop)} {N(left + width * 0.78)},{N(yTop)} {N(left + width * 0.86)},{N(valley)} " +
                $"C {N(right - 1)},{N(valley + 2)} {N(right + 2.8)},{N(mid)} {N(right)},{N(yCervix)} " +
                $"C {N(cx + width * 0.22)},{N(yCervix + 3.4)} {N(cx - width * 0.22)},{N(yCervix + 3.4)} {N(left)},{N(yCervix)} Z");
        }

        return P(
            $"M {N(left)},{N(yCervix)} " +
            $"C {N(left - 2.4)},{N(mid)} {N(left + 1)},{N(valley + 1)} {N(left + width * 0.22)},{N(yTop + 1.2)} " +
            $"C {N(cx - 6)},{N(valley + 0.8)} {N(cx + 6)},{N(valley + 0.8)} {N(right - width * 0.22)},{N(yTop + 1.2)} " +
            $"C {N(right - 1)},{N(valley + 1)} {N(right + 2.4)},{N(mid)} {N(right)},{N(yCervix)} " +
            $"C {N(cx + width * 0.2)},{N(yCervix + 3.2)} {N(cx - width * 0.2)},{N(yCervix + 3.2)} {N(left)},{N(yCervix)} Z");
    }

    private static Geometry Tube(double cx, double y0, double y1, double wCervix, double apexR, double bend)
    {
        var apexX = cx + bend;
        var shoulder = y0 + 10;
        var mid = y0 + (y1 - y0) * 0.42;
        var wMid = wCervix * 0.88;
        var r = Math.Max(4.4, apexR);
        var yCap = y1 - r;
        return P(
            $"M {N(cx - wCervix)},{N(shoulder)} " +
            $"C {N(cx - wCervix)},{N(y0)} {N(cx + wCervix)},{N(y0)} {N(cx + wCervix)},{N(shoulder)} " +
            $"C {N(cx + wCervix * 0.96)},{N(mid)} {N(apexX + wMid)},{N(yCap - 8)} {N(apexX + r)},{N(yCap)} " +
            $"C {N(apexX + r)},{N(y1)} {N(apexX - r)},{N(y1)} {N(apexX - r)},{N(yCap)} " +
            $"C {N(apexX - wMid)},{N(yCap - 8)} {N(cx - wCervix * 0.96)},{N(mid)} {N(cx - wCervix)},{N(shoulder)} Z");
    }

    private static Geometry Highlight(double cx, double yTop, double width, double height) => P(
        $"M {N(cx - width * 0.26)},{N(yTop + height * 0.2)} " +
        $"C {N(cx - width * 0.06)},{N(yTop + height * 0.1)} {N(cx + width * 0.16)},{N(yTop + height * 0.12)} {N(cx + width * 0.28)},{N(yTop + height * 0.24)} " +
        $"C {N(cx + width * 0.1)},{N(yTop + height * 0.32)} {N(cx - width * 0.1)},{N(yTop + height * 0.3)} {N(cx - width * 0.26)},{N(yTop + height * 0.2)} Z");

    private static Geometry CanalLine(double cx, double y0, double y1) =>
        P($"M {N(cx)},{N(y0)} C {N(cx + 0.8)},{N((y0 + y1) / 2)} {N(cx - 0.6)},{N(y1 - 8)} {N(cx)},{N(y1)}");

    private static Geometry FissureLine(double cx, double y0, double y1) => P($"M {N(cx)},{N(y0)} L {N(cx)},{N(y1)}");

    private static Geometry FissureCross(double cx, double y, double half) => P($"M {N(cx - half)},{N(y)} L {N(cx + half)},{N(y)}");

    private static Geometry ImplantScrew(double yCervix, double yApex, double crownW)
    {
        const double cx = 44;
        var half = Math.Clamp(crownW * 0.17, 4.0, 7.0);
        var y1 = yCervix - 10;
        var y2 = yApex - 6;
        var sb = new System.Text.StringBuilder();
        sb.Append(Inv, $"M {N(cx - half)},{N(y1)} C {N(cx - 2)},{N(y1 - 2)} {N(cx + 2)},{N(y1 - 2)} {N(cx + half)},{N(y1)} ");
        var steps = 7;
        for (var i = 1; i <= steps; i++)
        {
            var t = i / (double)steps;
            var y = y1 + (y2 - y1) * t;
            var side = i % 2 == 0 ? -1 : 1;
            var w = half * (1 - t * 0.42);
            sb.Append(Inv, $"L {N(cx + side * w)},{N(y)} ");
        }
        sb.Append(Inv, $"L {N(cx)},{N(y2 + 5)} ");
        for (var i = steps; i >= 1; i--)
        {
            var t = i / (double)steps;
            var y = y1 + (y2 - y1) * t;
            var side = i % 2 == 0 ? 1 : -1;
            var w = half * (1 - t * 0.42);
            sb.Append(Inv, $"L {N(cx + side * w)},{N(y)} ");
        }
        sb.Append("Z");
        return P(sb.ToString());
    }

    private static Geometry InsideRound(Geometry crown, Rect region)
    {
        var radius = Math.Min(6, Math.Min(region.Width, region.Height) * 0.35);
        var g = new CombinedGeometry(
            GeometryCombineMode.Intersect,
            crown,
            new RectangleGeometry(region, radius, radius));
        g.Freeze();
        return g;
    }

    private static Geometry InsideEllipse(Geometry crown, Rect bounds)
    {
        var g = new CombinedGeometry(
            GeometryCombineMode.Intersect,
            crown,
            new EllipseGeometry(
                new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height * 0.42),
                bounds.Width * 0.28,
                bounds.Height * 0.22));
        g.Freeze();
        return g;
    }

    private static Geometry Union(params Geometry[] items)
    {
        Geometry acc = items[0];
        for (var i = 1; i < items.Length; i++)
            acc = new CombinedGeometry(GeometryCombineMode.Union, acc, items[i]);
        if (!acc.IsFrozen)
            acc.Freeze();
        return acc;
    }

    private static Geometry Combine(Geometry a, Geometry b)
    {
        var g = new CombinedGeometry(GeometryCombineMode.Union, a, b);
        g.Freeze();
        return g;
    }

    private static Geometry P(string data)
    {
        var g = Geometry.Parse(data);
        g.Freeze();
        return g;
    }

    private static string N(double v) => v.ToString("0.##", Inv);

    internal static readonly Brush RootFill = CreateRoot();
    internal static readonly Brush EnamelFill = CreateEnamel();
    internal static readonly Brush RootStroke = Freeze(Color.FromRgb(0xC2, 0xA0, 0x78));
    internal static readonly Brush CrownStroke = Freeze(Color.FromRgb(0xD4, 0xC0, 0xA0));
    private static readonly Brush HighlightFill = Freeze(Color.FromArgb(0x4A, 0xFF, 0xFF, 0xFF));
    private static readonly Brush FissureStroke = Freeze(Color.FromArgb(0x78, 0xB8, 0xA4, 0x88));

    private static Brush CreateRoot()
    {
        var b = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0.5),
            EndPoint = new Point(1, 0.5)
        };
        b.GradientStops.Add(new GradientStop(Color.FromRgb(0xB4, 0x90, 0x64), 0));
        b.GradientStops.Add(new GradientStop(Color.FromRgb(0xEC, 0xD4, 0xB0), 0.36));
        b.GradientStops.Add(new GradientStop(Color.FromRgb(0xF6, 0xE8, 0xCC), 0.5));
        b.GradientStops.Add(new GradientStop(Color.FromRgb(0xE2, 0xC6, 0x9C), 0.7));
        b.GradientStops.Add(new GradientStop(Color.FromRgb(0xA8, 0x86, 0x5C), 1));
        b.Freeze();
        return b;
    }

    private static Brush CreateEnamel()
    {
        var b = new RadialGradientBrush
        {
            GradientOrigin = new Point(0.36, 0.22),
            Center = new Point(0.46, 0.4),
            RadiusX = 0.78,
            RadiusY = 0.9
        };
        b.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xFC, 0xF6), 0));
        b.GradientStops.Add(new GradientStop(Color.FromRgb(0xF6, 0xEE, 0xDC), 0.42));
        b.GradientStops.Add(new GradientStop(Color.FromRgb(0xE8, 0xD6, 0xB6), 0.78));
        b.GradientStops.Add(new GradientStop(Color.FromRgb(0xD0, 0xB6, 0x92), 1));
        b.Freeze();
        return b;
    }

    private static SolidColorBrush Freeze(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
