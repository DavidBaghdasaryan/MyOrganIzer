using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Renders the visible healthy layers of a ZoliQua odontogram SVG as native WPF paths.
/// No React/HTML. Static only — no hit-testing or treatment overlays.
/// </summary>
public partial class SvgToothView : UserControl
{
    public static readonly DependencyProperty AssetNameProperty =
        DependencyProperty.Register(
            nameof(AssetName),
            typeof(string),
            typeof(SvgToothView),
            new PropertyMetadata("16_occl", OnAssetChanged));

    public static readonly DependencyProperty IncludeGroupIdProperty =
        DependencyProperty.Register(
            nameof(IncludeGroupId),
            typeof(string),
            typeof(SvgToothView),
            new PropertyMetadata("tooth", OnAssetChanged));

    public static readonly DependencyProperty IncludeBackdropProperty =
        DependencyProperty.Register(
            nameof(IncludeBackdrop),
            typeof(bool),
            typeof(SvgToothView),
            new PropertyMetadata(true, OnAssetChanged));

    public SvgToothView()
    {
        InitializeComponent();
        Loaded += (_, _) => Paint();
    }

    public string AssetName
    {
        get => (string)GetValue(AssetNameProperty);
        set => SetValue(AssetNameProperty, value);
    }

    public string IncludeGroupId
    {
        get => (string)GetValue(IncludeGroupIdProperty);
        set => SetValue(IncludeGroupIdProperty, value);
    }

    public bool IncludeBackdrop
    {
        get => (bool)GetValue(IncludeBackdropProperty);
        set => SetValue(IncludeBackdropProperty, value);
    }

    private static void OnAssetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SvgToothView view && view.IsLoaded)
            view.Paint();
    }

    private void Paint()
    {
        PartCanvas.Children.Clear();
        var stats = new RenderStats();
        try
        {
            var svg = LoadSvg(AssetName, stats);
            if (svg is null)
            {
                AgentLog("D", "svg-load-failed", stats.ToJson());
                return;
            }

            var viewBox = ParseViewBox(svg.DocumentElement?.GetAttribute("viewBox"));
            PartCanvas.Width = viewBox.Width;
            PartCanvas.Height = viewBox.Height;
            stats.ViewBoxW = viewBox.Width;
            stats.ViewBoxH = viewBox.Height;

            var brushes = ParseGradients(svg, stats);
            if (IncludeBackdrop)
            {
                var backdrop = FindGroup(svg.DocumentElement, "base");
                if (backdrop is not null)
                    Walk(backdrop, brushes, stats);
                else
                    stats.MissingGroup = "base";
            }

            var root = FindGroup(svg.DocumentElement, IncludeGroupId);
            if (root is null)
            {
                stats.MissingGroup = IncludeGroupId;
                AgentLog("C", "include-group-missing", stats.ToJson());
                return;
            }

            Walk(root, brushes, stats);
            stats.Union = UnionBounds();
            stats.ChildCount = PartCanvas.Children.Count;
            AgentLog("E", "svg-wpf-render", stats.ToJson());
        }
        catch (Exception ex)
        {
            stats.Error = ex.GetType().Name + ": " + ex.Message;
            AgentLog("A", "svg-wpf-exception", stats.ToJson());
        }
    }

    private Rect UnionBounds()
    {
        var union = Rect.Empty;
        foreach (var child in PartCanvas.Children.OfType<System.Windows.Shapes.Path>())
        {
            if (child.Data is null)
                continue;
            var b = child.Data.Bounds;
            union = union.IsEmpty ? b : Rect.Union(union, b);
        }
        return union;
    }

    private static XmlDocument? LoadSvg(string assetName, RenderStats stats)
    {
        var safe = (assetName ?? "").Trim();
        if (safe.Length == 0)
            return null;
        if (!safe.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            safe += ".svg";

        var pack = new Uri(
            "pack://application:,,,/MyOrganizer.Wpf;component/Assets/ThirdParty/ZoliQua/" + safe,
            UriKind.Absolute);
        stats.PackUri = pack.ToString();

        var stream = Application.GetResourceStream(pack)?.Stream;
        if (stream is null)
        {
            stats.LoadFailed = true;
            return null;
        }

        using (stream)
        {
            var doc = new XmlDocument { XmlResolver = null };
            doc.Load(stream);
            return doc;
        }
    }

    private static Rect ParseViewBox(string? viewBox)
    {
        if (string.IsNullOrWhiteSpace(viewBox))
            return new Rect(0, 0, 48.2, 41.5);
        var parts = viewBox.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
            return new Rect(0, 0, 48.2, 41.5);
        return new Rect(
            Num(parts[0]), Num(parts[1]), Num(parts[2]), Num(parts[3]));
    }

    private static XmlElement? FindGroup(XmlElement? root, string id)
    {
        if (root is null)
            return null;
        if (string.Equals(root.GetAttribute("id"), id, StringComparison.Ordinal))
            return root;
        foreach (var child in root.ChildNodes.OfType<XmlElement>())
        {
            var found = FindGroup(child, id);
            if (found is not null)
                return found;
        }
        return null;
    }

    private Dictionary<string, Brush> ParseGradients(XmlDocument svg, RenderStats stats)
    {
        var map = new Dictionary<string, Brush>(StringComparer.Ordinal);
        foreach (var el in svg.GetElementsByTagName("*").OfType<XmlElement>())
        {
            var name = el.LocalName;
            if (name is not "linearGradient" and not "radialGradient")
                continue;
            var id = el.GetAttribute("id");
            if (string.IsNullOrEmpty(id))
                continue;
            var brush = name == "linearGradient"
                ? MakeLinear(el, stats)
                : MakeRadial(el, stats);
            if (brush is not null)
                map[id] = brush;
        }
        stats.GradientCount = map.Count;
        return map;
    }

    private static Brush? MakeLinear(XmlElement el, RenderStats stats)
    {
        var stops = ReadStops(el);
        if (stops.Count == 0)
            return null;
        var x1 = Attr(el, "x1", 0);
        var y1 = Attr(el, "y1", 0);
        var x2 = Attr(el, "x2", 1);
        var y2 = Attr(el, "y2", 0);
        var matrix = ParseTransform(el.GetAttribute("gradientTransform"));
        var start = matrix.Transform(new Point(x1, y1));
        var end = matrix.Transform(new Point(x2, y2));
        var brush = new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute,
            StartPoint = start,
            EndPoint = end
        };
        foreach (var stop in stops)
            brush.GradientStops.Add(stop);
        try { brush.Freeze(); } catch { /* keep unfrozen if needed */ }
        stats.LinearOk++;
        return brush;
    }

    private static Brush? MakeRadial(XmlElement el, RenderStats stats)
    {
        var stops = ReadStops(el);
        if (stops.Count == 0)
            return null;
        var cx = Attr(el, "cx", 0);
        var cy = Attr(el, "cy", 0);
        var fx = Attr(el, "fx", cx);
        var fy = Attr(el, "fy", cy);
        var r = Attr(el, "r", 1);
        var matrix = ParseTransform(el.GetAttribute("gradientTransform"));
        var center = matrix.Transform(new Point(cx, cy));
        var origin = matrix.Transform(new Point(fx, fy));
        var edgeX = matrix.Transform(new Point(cx + r, cy));
        var edgeY = matrix.Transform(new Point(cx, cy + r));
        var brush = new RadialGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute,
            Center = center,
            GradientOrigin = origin,
            RadiusX = Dist(center, edgeX),
            RadiusY = Dist(center, edgeY)
        };
        foreach (var stop in stops)
            brush.GradientStops.Add(stop);
        try { brush.Freeze(); } catch { /* keep unfrozen if needed */ }
        stats.RadialOk++;
        return brush;
    }

    private static List<GradientStop> ReadStops(XmlElement gradient)
    {
        var list = new List<GradientStop>();
        foreach (var stop in gradient.ChildNodes.OfType<XmlElement>())
        {
            if (stop.LocalName != "stop")
                continue;
            var offset = Attr(stop, "offset", 0);
            var color = ParseColor(stop.GetAttribute("stop-color"), 1);
            var opacity = Attr(stop, "stop-opacity", 1);
            if (opacity < 1)
                color = Color.FromArgb((byte)Math.Clamp(opacity * color.A, 0, 255), color.R, color.G, color.B);
            list.Add(new GradientStop(color, offset));
        }
        return list;
    }

    private void Walk(XmlElement el, Dictionary<string, Brush> brushes, RenderStats stats)
    {
        if (IsHidden(el))
        {
            stats.SkippedHidden++;
            return;
        }

        switch (el.LocalName)
        {
            case "path":
                AddPath(el, brushes, stats);
                break;
            case "polygon":
                AddPolygon(el, brushes, stats);
                break;
            case "circle":
            case "ellipse":
                AddEllipse(el, brushes, stats);
                break;
            case "line":
                AddLine(el, brushes, stats);
                break;
        }

        foreach (var child in el.ChildNodes.OfType<XmlElement>())
            Walk(child, brushes, stats);
    }

    private void AddPath(XmlElement el, Dictionary<string, Brush> brushes, RenderStats stats)
    {
        var d = el.GetAttribute("d");
        if (string.IsNullOrWhiteSpace(d))
            return;
        Geometry? geometry = null;
        try
        {
            geometry = Geometry.Parse("F1 " + d);
        }
        catch
        {
            try { geometry = Geometry.Parse("F1 " + d.Replace(',', ' ')); }
            catch (Exception ex)
            {
                stats.ParseFail++;
                stats.LastParseError = ex.Message;
                AgentLog("A", "path-parse-fail",
                    "{\"id\":\"" + Esc(el.GetAttribute("id")) + "\",\"error\":\"" + Esc(ex.Message) + "\"}");
                return;
            }
        }

        stats.PathOk++;
        AddShape(el, geometry, brushes, stats);
    }

    private void AddPolygon(XmlElement el, Dictionary<string, Brush> brushes, RenderStats stats)
    {
        var raw = el.GetAttribute("points");
        var nums = Regex.Matches(raw, @"-?\d*\.?\d+(?:[eE][+-]?\d+)?");
        if (nums.Count < 4)
            return;
        var fig = new PathFigure { IsClosed = true };
        fig.StartPoint = new Point(Num(nums[0].Value), Num(nums[1].Value));
        for (var i = 2; i + 1 < nums.Count; i += 2)
            fig.Segments.Add(new LineSegment(new Point(Num(nums[i].Value), Num(nums[i + 1].Value)), true));
        var pg = new PathGeometry { FillRule = FillRule.Nonzero };
        pg.Figures.Add(fig);
        pg.Freeze();
        stats.PathOk++;
        AddShape(el, pg, brushes, stats);
    }

    private void AddEllipse(XmlElement el, Dictionary<string, Brush> brushes, RenderStats stats)
    {
        double cx, cy, rx, ry;
        if (el.LocalName == "circle")
        {
            cx = Attr(el, "cx", 0);
            cy = Attr(el, "cy", 0);
            rx = ry = Attr(el, "r", 0);
        }
        else
        {
            cx = Attr(el, "cx", 0);
            cy = Attr(el, "cy", 0);
            rx = Attr(el, "rx", 0);
            ry = Attr(el, "ry", 0);
        }
        var g = new EllipseGeometry(new Point(cx, cy), rx, ry);
        g.Freeze();
        stats.PathOk++;
        AddShape(el, g, brushes, stats);
    }

    private void AddLine(XmlElement el, Dictionary<string, Brush> brushes, RenderStats stats)
    {
        var g = new LineGeometry(
            new Point(Attr(el, "x1", 0), Attr(el, "y1", 0)),
            new Point(Attr(el, "x2", 0), Attr(el, "y2", 0)));
        g.Freeze();
        stats.PathOk++;
        AddShape(el, g, brushes, stats);
    }

    private void AddShape(XmlElement el, Geometry geometry, Dictionary<string, Brush> brushes, RenderStats stats)
    {
        var style = ParseStyle(el);
        var fill = ResolveBrush(style, "fill", brushes, stats, defaultFill: Brushes.Black);
        var stroke = ResolveBrush(style, "stroke", brushes, stats, defaultFill: null);
        var strokeWidth = ParseStrokeWidth(style);
        if (!style.ContainsKey("stroke-width") && stroke is not null)
            strokeWidth = 1;

        var path = new System.Windows.Shapes.Path
        {
            Data = geometry,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = strokeWidth,
            StrokeMiterLimit = 10,
            Stretch = Stretch.None,
            IsHitTestVisible = false
        };
        if (style.TryGetValue("opacity", out var op) && double.TryParse(op, NumberStyles.Float, CultureInfo.InvariantCulture, out var o))
            path.Opacity = o;

        PartCanvas.Children.Add(path);
        var id = el.GetAttribute("id");
        if (id is "background-cusp" or "tooth-base")
        {
            var b = geometry.Bounds;
            AgentLog("D", "primary-contour",
                "{\"id\":\"" + id + "\",\"x\":" + F(b.X) + ",\"y\":" + F(b.Y) +
                ",\"w\":" + F(b.Width) + ",\"h\":" + F(b.Height) +
                ",\"aspect\":" + F(b.Height < 0.001 ? 0 : b.Width / b.Height) +
                ",\"fillKind\":\"" + (fill?.GetType().Name ?? "null") + "\"}");
        }
    }

    private static Brush? ResolveBrush(
        Dictionary<string, string> style,
        string key,
        Dictionary<string, Brush> brushes,
        RenderStats stats,
        Brush? defaultFill)
    {
        if (!style.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            return key == "fill" ? defaultFill : null;
        raw = raw.Trim();
        if (raw.Equals("none", StringComparison.OrdinalIgnoreCase))
            return null;
        if (raw.StartsWith("url(", StringComparison.OrdinalIgnoreCase))
        {
            var id = raw;
            var hash = id.IndexOf('#');
            var end = id.IndexOf(')', Math.Max(hash, 0));
            if (hash >= 0 && end > hash)
                id = id[(hash + 1)..end];
            if (brushes.TryGetValue(id, out var g))
            {
                stats.UrlFillOk++;
                return g;
            }
            stats.UrlFillMiss++;
            AgentLog("B", "gradient-miss", "{\"id\":\"" + Esc(id) + "\"}");
            return Frozen(Color.FromRgb(0xE8, 0xDC, 0xD0));
        }

        var color = ParseColor(raw, 1);
        return Frozen(color);
    }

    private static Dictionary<string, string> ParseStyle(XmlElement el)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var attr in new[] { "fill", "stroke", "stroke-width", "opacity", "fill-opacity", "stroke-opacity" })
        {
            if (el.HasAttribute(attr))
                map[attr] = el.GetAttribute(attr);
        }
        var style = el.GetAttribute("style");
        if (string.IsNullOrWhiteSpace(style))
            return map;
        foreach (var part in style.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var colon = part.IndexOf(':');
            if (colon <= 0)
                continue;
            map[part[..colon].Trim()] = part[(colon + 1)..].Trim();
        }
        return map;
    }

    private static bool IsHidden(XmlElement el)
    {
        if (el.GetAttribute("data-active") == "0")
            return true;
        var style = el.GetAttribute("style");
        if (style.Contains("display: none", StringComparison.OrdinalIgnoreCase) ||
            style.Contains("display:none", StringComparison.OrdinalIgnoreCase))
            return true;
        var display = el.GetAttribute("display");
        return display.Equals("none", StringComparison.OrdinalIgnoreCase);
    }

    private static double ParseStrokeWidth(Dictionary<string, string> style)
    {
        if (!style.TryGetValue("stroke-width", out var raw))
            return 0;
        raw = raw.Replace("px", "", StringComparison.OrdinalIgnoreCase).Trim();
        return Num(raw);
    }

    private static Matrix ParseTransform(string? raw)
    {
        var m = Matrix.Identity;
        if (string.IsNullOrWhiteSpace(raw))
            return m;
        foreach (Match match in Regex.Matches(raw, @"(translate|scale|rotate|matrix|skewX|skewY)\s*\(([^)]*)\)"))
        {
            var cmd = match.Groups[1].Value;
            var args = Regex.Matches(match.Groups[2].Value, @"-?\d*\.?\d+(?:[eE][+-]?\d+)?")
                .Select(x => Num(x.Value))
                .ToArray();
            var t = Matrix.Identity;
            switch (cmd)
            {
                case "translate":
                    t.Translate(args.ElementAtOrDefault(0), args.Length > 1 ? args[1] : 0);
                    break;
                case "scale":
                    t.Scale(args.ElementAtOrDefault(0), args.Length > 1 ? args[1] : args.ElementAtOrDefault(0));
                    break;
                case "rotate":
                    if (args.Length >= 3)
                    {
                        t.Translate(args[1], args[2]);
                        t.Rotate(args[0]);
                        t.Translate(-args[1], -args[2]);
                    }
                    else
                        t.Rotate(args.ElementAtOrDefault(0));
                    break;
                case "matrix" when args.Length >= 6:
                    t = new Matrix(args[0], args[1], args[2], args[3], args[4], args[5]);
                    break;
                case "skewX":
                    t.Skew(args.ElementAtOrDefault(0), 0);
                    break;
                case "skewY":
                    t.Skew(0, args.ElementAtOrDefault(0));
                    break;
            }
            m.Append(t);
        }
        return m;
    }

    private static Color ParseColor(string raw, double opacity)
    {
        raw = (raw ?? "").Trim();
        if (raw.Length == 0)
            return Colors.Black;
        if (raw.Equals("none", StringComparison.OrdinalIgnoreCase))
            return Colors.Transparent;
        Color color;
        if (raw.StartsWith('#'))
        {
            var hex = raw[1..];
            if (hex.Length == 3)
                hex = string.Concat(hex.Select(c => $"{c}{c}"));
            if (hex.Length == 6 && uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
                color = Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
            else
                color = Colors.Black;
        }
        else if (!ColorConverterTry(raw, out color))
            color = Colors.Black;

        if (opacity < 1)
            color.A = (byte)Math.Clamp(opacity * color.A, 0, 255);
        return color;
    }

    private static bool ColorConverterTry(string raw, out Color color)
    {
        try
        {
            if (ColorConverter.ConvertFromString(raw) is Color c)
            {
                color = c;
                return true;
            }
        }
        catch { /* named color miss */ }
        color = Colors.Black;
        return false;
    }

    private static Brush Frozen(Color color)
    {
        var b = new SolidColorBrush(color);
        b.Freeze();
        return b;
    }

    private static double Attr(XmlElement el, string name, double fallback)
    {
        var v = el.GetAttribute(name);
        return string.IsNullOrWhiteSpace(v) ? fallback : Num(v);
    }

    private static double Num(string s) =>
        double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : 0;

    private static double Dist(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    // #region agent log
    private static void AgentLog(string hypothesisId, string message, string dataJson)
    {
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"zoli-svg\",\"hypothesisId\":\"" + hypothesisId +
                   "\",\"location\":\"SvgToothView.xaml.cs:Paint\",\"message\":\"" + message +
                   "\",\"data\":" + dataJson + ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { /* debug ingest must not affect the tooth */ }
    }
    // #endregion

    private sealed class RenderStats
    {
        public string PackUri = "";
        public bool LoadFailed;
        public string? MissingGroup;
        public string? Error;
        public double ViewBoxW;
        public double ViewBoxH;
        public int GradientCount;
        public int LinearOk;
        public int RadialOk;
        public int PathOk;
        public int ParseFail;
        public int SkippedHidden;
        public int UrlFillOk;
        public int UrlFillMiss;
        public int ChildCount;
        public string? LastParseError;
        public Rect Union;

        public string ToJson() =>
            "{" +
            "\"packUri\":\"" + Esc(PackUri) + "\"," +
            "\"loadFailed\":" + (LoadFailed ? "true" : "false") + "," +
            "\"missingGroup\":\"" + Esc(MissingGroup ?? "") + "\"," +
            "\"error\":\"" + Esc(Error ?? "") + "\"," +
            "\"viewBoxW\":" + F(ViewBoxW) + "," +
            "\"viewBoxH\":" + F(ViewBoxH) + "," +
            "\"gradientCount\":" + GradientCount + "," +
            "\"linearOk\":" + LinearOk + "," +
            "\"radialOk\":" + RadialOk + "," +
            "\"pathOk\":" + PathOk + "," +
            "\"parseFail\":" + ParseFail + "," +
            "\"skippedHidden\":" + SkippedHidden + "," +
            "\"urlFillOk\":" + UrlFillOk + "," +
            "\"urlFillMiss\":" + UrlFillMiss + "," +
            "\"childCount\":" + ChildCount + "," +
            "\"lastParseError\":\"" + Esc(LastParseError ?? "") + "\"," +
            "\"unionX\":" + F(Union.IsEmpty ? 0 : Union.X) + "," +
            "\"unionY\":" + F(Union.IsEmpty ? 0 : Union.Y) + "," +
            "\"unionW\":" + F(Union.IsEmpty ? 0 : Union.Width) + "," +
            "\"unionH\":" + F(Union.IsEmpty ? 0 : Union.Height) +
            "}";
    }
}
