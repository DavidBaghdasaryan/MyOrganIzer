using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MyOrganizer.Wpf.Dental;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Odontogram overlay: thin red canal lines for the treated root/canal IDs
/// from <see cref="ToothRootCanalCatalog"/>. Reference tooth: FDI 36.
/// </summary>
public sealed class EndodonticOdontogramVisual : FrameworkElement
{
    public static readonly DependencyProperty FdiNumberProperty =
        DependencyProperty.Register(
            nameof(FdiNumber),
            typeof(string),
            typeof(EndodonticOdontogramVisual),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnSpecChanged));

    public static readonly DependencyProperty CanalIdsProperty =
        DependencyProperty.Register(
            nameof(CanalIds),
            typeof(string),
            typeof(EndodonticOdontogramVisual),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender, OnSpecChanged));

    private static readonly Brush CanalBrush = CreateBrush();
    private string? _fdi;
    private int _bmpW;
    private int _bmpH;
    private Geometry? _rootClip;
    private Geometry? _canals;
    private int _drawn;

    public EndodonticOdontogramVisual()
    {
        IsHitTestVisible = false;
        SnapsToDevicePixels = true;
    }

    public string? FdiNumber
    {
        get => (string?)GetValue(FdiNumberProperty);
        set => SetValue(FdiNumberProperty, value);
    }

    public string? CanalIds
    {
        get => (string?)GetValue(CanalIdsProperty);
        set => SetValue(CanalIdsProperty, value);
    }

    private static void OnSpecChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EndodonticOdontogramVisual visual)
            visual.Rebuild();
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (_canals is null || ActualWidth < 2 || ActualHeight < 2 || _bmpW < 2 || _bmpH < 2)
            return;
        var s = Math.Min(ActualWidth / _bmpW, ActualHeight / _bmpH);
        var ox = (ActualWidth - _bmpW * s) / 2;
        var oy = (ActualHeight - _bmpH * s) / 2;
        var pen = new Pen(CanalBrush, 1.15 / Math.Max(s, 0.01))
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        dc.PushTransform(new MatrixTransform(s, 0, 0, s, ox, oy));
        if (_rootClip is not null)
            dc.PushClip(_rootClip);
        dc.DrawGeometry(null, pen, _canals);
        if (_rootClip is not null)
            dc.Pop();
        dc.Pop();
    }

    private void Rebuild()
    {
        _canals = null;
        _rootClip = null;
        _drawn = 0;
        _fdi = ToothAssetRegistry.Normalize(FdiNumber ?? "");
        var selected = ToothRootCanalCatalog.Normalize(_fdi, (CanalIds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (selected.Count == 0 || !ToothFdi.TryParse(_fdi, out _))
            return;
        if (!ToothRootCanalCatalog.HasChoices(_fdi))
            return;

        var thumb = OdontogramThumbStore.Get(_fdi);
        if (!TryCopyPixels(thumb, out var px, out var w, out var h) ||
            !TryBounds(px, w, h, out var minY, out var maxY))
            return;

        _bmpW = w;
        _bmpH = h;
        var upper = ToothFdi.IsUpper(_fdi);
        var occlusalY = upper ? maxY : minY;
        var apexY = upper ? minY : maxY;
        var dir = upper ? -1 : 1;
        var toothH = maxY - minY + 1;
        var startY = ClampY(occlusalY + dir * Math.Max(8, (int)(toothH * 0.36)) + dir * 2, minY, maxY);
        var tipY = ClampY(apexY - dir * 2, minY, maxY);
        if ((tipY - startY) * dir < 8)
            return;

        _rootClip = BuildRootClip(px, w, h, startY, tipY, dir);
        var traces = FitRootAxes(px, w, startY, tipY, dir, selected, ToothFdi.MesialOnLeft(_fdi));
        _drawn = traces.Count;
        if (traces.Count > 0)
            _canals = BuildCanalGeometry(traces);
        // #region agent log
        try
        {
            var xs = string.Join(";", traces.Select(t =>
                t[0].X.ToString("0.0") + "-" + t[^1].X.ToString("0.0")));
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"endo-canal-v2\",\"hypothesisId\":\"B\"" +
                       ",\"location\":\"EndodonticOdontogramVisual.Rebuild\",\"message\":\"selected-canals\"" +
                       ",\"data\":{\"fdi\":\"" + _fdi + "\",\"ids\":\"" + string.Join(",", selected) +
                       "\",\"drawn\":" + _drawn + ",\"lenPx\":" + ((tipY - startY) * dir) +
                       ",\"axes\":\"" + xs + "\"}" +
                       ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line);
        }
        catch { }
        // #endregion
        InvalidateVisual();
    }

    private static List<List<Point>> FitRootAxes(
        int[] px, int w, int startY, int tipY, int dir, HashSet<string> selected, bool mesialOnLeft)
    {
        var startSpans = SpansAt(px, w, startY, dir, 4);
        var midY = startY + dir * (Math.Abs(tipY - startY) / 2);
        var midSpans = SpansAt(px, w, midY, dir, 6);
        var tipSpans = SpansAt(px, w, tipY - dir * 3, dir, 6);
        var canals = new List<List<Point>>();
        foreach (var id in new[] { ToothRootCanalCatalog.Mesial, ToothRootCanalCatalog.Distal })
        {
            if (!selected.Contains(id))
                continue;
            var mesial = id == ToothRootCanalCatalog.Mesial;
            var x0 = AxisX(startSpans, mesial, mesialOnLeft, w);
            var x1 = AxisX(midSpans, mesial, mesialOnLeft, w);
            var x2 = AxisX(tipSpans, mesial, mesialOnLeft, w);
            canals.Add(
            [
                new Point(x0, startY + 0.5),
                new Point(x1, midY + 0.5),
                new Point(x2, tipY + 0.5)
            ]);
        }
        return canals;
    }

    private static List<Span> SpansAt(int[] px, int w, int y, int dir, int search)
    {
        for (var i = 0; i <= search; i++)
        {
            var row = Spans(px, w, y + dir * i);
            if (row.Count > 0)
                return row;
            if (i == 0)
                continue;
            row = Spans(px, w, y - dir * i);
            if (row.Count > 0)
                return row;
        }
        return [];
    }

    private static double AxisX(IReadOnlyList<Span> spans, bool mesial, bool mesialOnLeft, int w)
    {
        var leftBias = mesial == mesialOnLeft;
        if (spans.Count >= 2)
        {
            var a = spans[0];
            var b = spans[^1];
            return leftBias ? Mid(a) : Mid(b);
        }
        if (spans.Count == 1)
        {
            var s = spans[0];
            var t = leftBias ? 0.34 : 0.66;
            return s.Left + (s.Right - s.Left) * t;
        }
        return w * (leftBias ? 0.38 : 0.62);
    }

    private static Geometry BuildCanalGeometry(List<List<Point>> traces)
    {
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            foreach (var pts in traces)
            {
                ctx.BeginFigure(pts[0], false, false);
                if (pts.Count >= 3)
                    ctx.QuadraticBezierTo(pts[1], pts[2], true, true);
                else
                    ctx.LineTo(pts[^1], true, true);
            }
        }
        geo.Freeze();
        return geo;
    }

    private static Geometry? BuildRootClip(int[] px, int w, int h, int startY, int tipY, int dir)
    {
        var left = new List<Point>();
        var right = new List<Point>();
        for (var y = startY; y != tipY + dir; y += dir)
        {
            if (!RowRange(px, w, y, out var lo, out var hi))
                continue;
            left.Add(new Point(lo - 0.4, y + 0.5));
            right.Add(new Point(hi + 0.4, y + 0.5));
        }
        if (left.Count < 3)
            return null;
        var fig = new PathFigure { StartPoint = left[0], IsClosed = true, IsFilled = true };
        for (var i = 1; i < left.Count; i++)
            fig.Segments.Add(new LineSegment(left[i], false));
        for (var i = right.Count - 1; i >= 0; i--)
            fig.Segments.Add(new LineSegment(right[i], false));
        var geo = new PathGeometry([fig]);
        geo.Freeze();
        return geo;
    }

    private static bool TryCopyPixels(ImageSource? src, out int[] px, out int w, out int h)
    {
        px = [];
        w = 0;
        h = 0;
        if (src is not BitmapSource bmp)
            return false;
        BitmapSource ready = bmp;
        if (bmp.Format != PixelFormats.Bgra32 && bmp.Format != PixelFormats.Pbgra32)
            ready = new FormatConvertedBitmap(bmp, PixelFormats.Bgra32, null, 0);
        w = ready.PixelWidth;
        h = ready.PixelHeight;
        if (w < 4 || h < 4)
            return false;
        px = new int[w * h];
        ready.CopyPixels(px, w * 4, 0);
        return true;
    }

    private static bool TryBounds(int[] px, int w, int h, out int minY, out int maxY)
    {
        minY = h;
        maxY = -1;
        for (var y = 0; y < h; y++)
        {
            if (!RowRange(px, w, y, out _, out _))
                continue;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }
        return maxY >= minY + 8;
    }

    private static bool RowRange(int[] px, int w, int y, out int lo, out int hi)
    {
        lo = w;
        hi = -1;
        if (y < 0)
            return false;
        var row = y * w;
        if ((uint)row >= (uint)px.Length)
            return false;
        var last = Math.Min(w, px.Length - row);
        for (var x = 0; x < last; x++)
        {
            if (((px[row + x] >> 24) & 255) < 40)
                continue;
            if (x < lo) lo = x;
            if (x > hi) hi = x;
        }
        return hi >= lo;
    }

    private static List<Span> Spans(int[] px, int w, int y)
    {
        var list = new List<Span>();
        if (y < 0)
            return list;
        var row = y * w;
        if ((uint)row >= (uint)px.Length)
            return list;
        var last = Math.Min(w, px.Length - row);
        int? start = null;
        for (var x = 0; x <= last; x++)
        {
            var on = x < last && ((px[row + x] >> 24) & 255) >= 40;
            if (on)
            {
                start ??= x;
                continue;
            }
            if (start is not int s)
                continue;
            var e = x - 1;
            if (e - s >= 2)
            {
                if (list.Count > 0 && s - list[^1].Right <= 2)
                    list[^1] = new Span(list[^1].Left, e);
                else
                    list.Add(new Span(s, e));
            }
            start = null;
        }
        return list;
    }

    private static double Mid(Span s) => (s.Left + s.Right) / 2.0;
    private static int ClampY(int y, int minY, int maxY) => Math.Min(maxY, Math.Max(minY, y));

    private static Brush CreateBrush()
    {
        var brush = new SolidColorBrush(Color.FromArgb(0xE6, 0xC6, 0x28, 0x28));
        brush.Freeze();
        return brush;
    }

    private readonly record struct Span(int Left, int Right);
}
