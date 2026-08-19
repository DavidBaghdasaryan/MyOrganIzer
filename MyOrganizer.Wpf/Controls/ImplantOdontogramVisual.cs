using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Odontogram-only implant glyph: anatomical crown, short abutment, screw.
/// Tuned first for FDI 24; other FDI values use the same pipeline unpolished.
/// </summary>
public sealed class ImplantOdontogramVisual : FrameworkElement
{
    public static readonly DependencyProperty FdiNumberProperty =
        DependencyProperty.Register(
            nameof(FdiNumber),
            typeof(string),
            typeof(ImplantOdontogramVisual),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnSpecChanged));

    public static readonly DependencyProperty ToothTypeProperty =
        DependencyProperty.Register(
            nameof(ToothType),
            typeof(ToothKind),
            typeof(ImplantOdontogramVisual),
            new FrameworkPropertyMetadata(ToothKind.Premolar, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty JawProperty =
        DependencyProperty.Register(
            nameof(Jaw),
            typeof(ToothJaw),
            typeof(ImplantOdontogramVisual),
            new FrameworkPropertyMetadata(ToothJaw.Maxilla, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly Dictionary<string, ImageSource> Cache = new(StringComparer.Ordinal);
    private const int ScrewArgb = unchecked((int)0xFF6B7280);
    private const int ScrewHiArgb = unchecked((int)0xFF9CA3AF);
    private const int ScrewEdgeArgb = unchecked((int)0xFF4B5563);
    private const int AbutmentArgb = unchecked((int)0xFF8B939E);
    private const int AbutmentHiArgb = unchecked((int)0xFFB0B6BE);

    public ImplantOdontogramVisual()
    {
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
    }

    public ImplantOdontogramVisual(ToothKind toothType, ToothJaw jaw, string fdiNumber)
        : this()
    {
        ToothType = toothType;
        Jaw = jaw;
        FdiNumber = fdiNumber;
    }

    public string? FdiNumber
    {
        get => (string?)GetValue(FdiNumberProperty);
        set => SetValue(FdiNumberProperty, value);
    }

    public ToothKind ToothType
    {
        get => (ToothKind)GetValue(ToothTypeProperty);
        set => SetValue(ToothTypeProperty, value);
    }

    public ToothJaw Jaw
    {
        get => (ToothJaw)GetValue(JawProperty);
        set => SetValue(JawProperty, value);
    }

    private static void OnSpecChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ImplantOdontogramVisual visual)
            return;
        var fdi = visual.FdiNumber;
        if (string.IsNullOrWhiteSpace(fdi))
            return;
        visual.ToothType = ToothFdi.Kind(fdi);
        visual.Jaw = ToothFdi.IsUpper(fdi) ? ToothJaw.Maxilla : ToothJaw.Mandible;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var fdi = FdiNumber;
        if (string.IsNullOrWhiteSpace(fdi) || ActualWidth < 1 || ActualHeight < 1)
            return;
        var image = GetOrCompose(fdi);
        if (image is null)
            return;
        var srcW = image.Width;
        var srcH = image.Height;
        if (srcW < 1 || srcH < 1)
            return;
        var scale = Math.Min(ActualWidth / srcW, ActualHeight / srcH);
        var dw = srcW * scale;
        var dh = srcH * scale;
        var x = (ActualWidth - dw) / 2;
        var y = (ActualHeight - dh) / 2;
        drawingContext.DrawImage(image, new Rect(x, y, dw, dh));
    }

    private static ImageSource? GetOrCompose(string fdi)
    {
        fdi = ToothAssetRegistry.Normalize(fdi);
        if (Cache.TryGetValue(fdi, out var hit))
            return hit;
        try
        {
            var composed = Compose(fdi);
            if (composed is not null)
                Cache[fdi] = composed;
            return composed;
        }
        catch (Exception ex)
        {
            // #region agent log
            try
            {
                var line = "{\"sessionId\":\"ee2893\",\"runId\":\"implant-visual\",\"hypothesisId\":\"A\",\"location\":\"ImplantOdontogramVisual.GetOrCompose\",\"message\":\"compose-crash\",\"data\":{\"fdi\":\"" + fdi +
                           "\",\"error\":\"" + ex.GetType().Name + "\",\"msg\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") +
                           "\"},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
                File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line);
            }
            catch { }
            // #endregion
            return null;
        }
    }

    private static ImageSource? Compose(string fdi)
    {
        if (!ToothAssetRegistry.TryGet(fdi, out var asset) || !asset.RuntimeImported)
            return null;
        var crownBmp = OdontogramThumbRenderer.RenderImplantCrown(asset);
        if (crownBmp is null)
            return null;

        var w = crownBmp.PixelWidth;
        var h = crownBmp.PixelHeight;
        var dest = new int[w * h];
        crownBmp.CopyPixels(dest, w * 4, 0);
        var upper = asset.Jaw == ToothJaw.Maxilla;
        ShiftTowardOcclusal(dest, w, h, upper, 1);
        const int extra = 10;
        StretchCrownTowardScrew(dest, w, h, upper, extra);
        if (!FindCervix(dest, w, h, upper, out var cervixY, out var cx, out var cervixW, out var crownH, out var firstRowW, out var maxRowW))
            return crownBmp;

        const int overlap = 4;
        const int abutH = 10;
        const int capH = 9;
        var screwHalf = ClampRange(cervixW * 0.32, 16.0, 18.0);
        var abutMax = Math.Max(screwHalf + 2.0, cervixW * 0.52);
        var abutWide = ClampRange(cervixW * 0.48, screwHalf + 1.8, abutMax);
        const int tipPad = 0;
        var screwStart = cervixY + (upper ? -1 : 1) * (abutH - 2);
        var screwTip = upper ? tipPad : h - 1 - tipPad;
        var screwPx = Math.Abs(screwTip - screwStart);
        var crownLayer = (int[])dest.Clone();

        // #region agent log
        try
        {
            var inv = CultureInfo.InvariantCulture;
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"implant-visual\",\"hypothesisId\":\"A\",\"location\":\"ImplantOdontogramVisual.Compose\",\"message\":\"cervix\",\"data\":{\"fdi\":\"" + fdi +
                       "\",\"cervixY\":" + cervixY + ",\"cervixW\":" + cervixW + ",\"firstRowW\":" + firstRowW +
                       ",\"maxRowW\":" + maxRowW + ",\"crownH\":" + crownH + ",\"screwHalf\":" + screwHalf.ToString("0.00", inv) +
                       ",\"abutWide\":" + abutWide.ToString("0.00", inv) + ",\"abutMax\":" + abutMax.ToString("0.00", inv) +
                       ",\"screwStart\":" + screwStart + ",\"screwTip\":" + screwTip + ",\"screwPx\":" + screwPx +
                       ",\"extra\":" + extra + ",\"capH\":" + capH +
                       ",\"coneTipHalf\":" + Math.Max(2.0, screwHalf * 0.14).ToString("0.00", inv) +
                       ",\"emergeHalf\":" + screwHalf.ToString("0.00", inv) +
                       ",\"cone\":\"full\"" +
                       "},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line);
        }
        catch { }
        // #endregion

        DrawScrew(dest, w, h, cx, cervixY, upper, abutH, screwHalf, tipPad, capH);
        DrawAbutment(dest, w, h, cx, cervixY, upper, overlap, abutH, abutWide, screwHalf);
        OverlayCrownBody(dest, crownLayer, w, h, cervixY, upper);
        OverlayCrownCap(dest, crownLayer, w, h, cx, cervixY, upper, capH, screwHalf + 2.2);

        var bmp = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
        bmp.WritePixels(new Int32Rect(0, 0, w, h), dest, w * 4, 0);
        bmp.Freeze();

        // #region agent log
        try
        {
            var inv = CultureInfo.InvariantCulture;
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"implant-visual\",\"hypothesisId\":\"V\",\"location\":\"ImplantOdontogramVisual.Compose\",\"message\":\"compose\",\"data\":{\"fdi\":\"" + fdi +
                       "\",\"toothType\":\"" + ToothFdi.Kind(fdi) + "\",\"jaw\":\"" + asset.Jaw +
                       "\",\"upper\":" + (upper ? "true" : "false") + ",\"cervixY\":" + cervixY +
                       ",\"cervixW\":" + cervixW + ",\"crownH\":" + crownH + ",\"cx\":" + cx +
                       ",\"abutH\":" + abutH + ",\"overlap\":" + overlap +
                       ",\"extra\":" + extra + ",\"capH\":" + capH +
                       ",\"abutWide\":" + abutWide.ToString("0.0", inv) +
                       ",\"screwHalf\":" + screwHalf.ToString("0.0", inv) +
                       ",\"tuned24\":false" +
                       "},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line);
            if (fdi is "24" or "34")
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                using var fs = File.Create(@"c:\Users\david\source\repos\MyOrganIzer\debug-implant-" + fdi + ".png");
                encoder.Save(fs);
            }
        }
        catch { }
        // #endregion

        return bmp;
    }

    private static double ClampRange(double value, double min, double max)
    {
        if (min > max)
            (min, max) = (max, min);
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static bool FindCervix(
        int[] px, int w, int h, bool upper,
        out int cervixY, out int cx, out int cervixW, out int crownH, out int firstRowW, out int maxRowW)
    {
        cervixY = 0;
        cx = w / 2;
        cervixW = 0;
        crownH = 0;
        firstRowW = 0;
        maxRowW = 0;
        var minY = h;
        var maxY = -1;
        long sumX = 0;
        var n = 0;
        for (var y = 0; y < h; y++)
        {
            var span = RowSpan(px, w, h, y);
            if (span == 0)
                continue;
            if (span > maxRowW) maxRowW = span;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
            var row = y * w;
            for (var x = 0; x < w; x++)
            {
                if (((px[row + x] >> 24) & 255) < 40)
                    continue;
                sumX += x;
                n++;
            }
        }
        if (n == 0 || maxY < minY)
            return false;
        crownH = maxY - minY + 1;
        cx = (int)(sumX / n);
        firstRowW = RowSpan(px, w, h, upper ? minY : maxY);
        var need = Math.Max(16, (int)(maxRowW * 0.45));
        cervixY = upper ? minY : maxY;
        cervixW = firstRowW;
        var steps = Math.Min(12, crownH);
        for (var i = 0; i < steps; i++)
        {
            var y = upper ? minY + i : maxY - i;
            var span = RowSpan(px, w, h, y);
            if (span < need)
                continue;
            cervixY = y;
            cervixW = span;
            break;
        }
        if (cervixW < 8)
            cervixW = Math.Max(firstRowW, maxRowW);
        return cervixW >= 4;
    }

    private static int RowSpan(int[] px, int w, int h, int y)
    {
        if ((uint)y >= (uint)h)
            return 0;
        var row = y * w;
        var left = w;
        var right = -1;
        for (var x = 0; x < w; x++)
        {
            if (((px[row + x] >> 24) & 255) < 40)
                continue;
            if (x < left) left = x;
            if (x > right) right = x;
        }
        return right < left ? 0 : right - left + 1;
    }

    private static void DrawAbutment(
        int[] dest, int w, int h, int cx, int cervixY, bool upper,
        int overlap, int abutH, double wideHalf, double screwHalf)
    {
        var dir = upper ? -1 : 1;
        var y0 = cervixY - dir * overlap;
        var y1 = cervixY + dir * abutH;
        var yMin = Math.Max(0, Math.Min(y0, y1));
        var yMax = Math.Min(h - 1, Math.Max(y0, y1));
        var span = Math.Max(1, abutH + overlap);
        for (var y = yMin; y <= yMax; y++)
        {
            var t = ((y - cervixY) * dir + overlap) / (double)span;
            if (t < 0 || t > 1)
                continue;
            var s = t * t * (3 - 2 * t);
            var half = wideHalf + (screwHalf - wideHalf) * s;
            FillSpan(dest, w, h, y, cx, half, AbutmentArgb, AbutmentHiArgb, ScrewEdgeArgb);
        }
    }

    private static void StretchCrownTowardScrew(int[] dest, int w, int h, bool upper, int extra)
    {
        if (extra <= 0 || !OpaqueY(dest, w, h, out var minY, out var maxY))
            return;
        var occlusal = upper ? maxY : minY;
        var oldH = maxY - minY + 1;
        var newH = oldH + extra;
        var copy = (int[])dest.Clone();
        Array.Clear(dest);
        for (var i = 0; i < newH; i++)
        {
            var srcI = Math.Min(oldH - 1, (int)Math.Round(i * (oldH - 1) / (double)(newH - 1)));
            var dstY = upper ? occlusal - i : occlusal + i;
            var srcY = upper ? occlusal - srcI : occlusal + srcI;
            if ((uint)dstY >= (uint)h || (uint)srcY >= (uint)h)
                continue;
            Array.Copy(copy, srcY * w, dest, dstY * w, w);
        }
    }

    private static void OverlayCrownBody(int[] dest, int[] crown, int w, int h, int cervixY, bool upper)
    {
        for (var y = 0; y < h; y++)
        {
            if (upper ? y <= cervixY : y >= cervixY)
                continue;
            var row = y * w;
            for (var x = 0; x < w; x++)
            {
                var p = crown[row + x];
                if (((p >> 24) & 255) < 40)
                    continue;
                dest[row + x] = p;
            }
        }
    }

    private static void OverlayCrownCap(
        int[] dest, int[] crown, int w, int h, int cx, int cervixY, bool upper,
        int capH, double endHalf)
    {
        var dir = upper ? -1 : 1;
        var srcY = cervixY - dir * 3;
        if ((uint)srcY >= (uint)h)
            srcY = cervixY;
        var srcHalf = RowSpan(crown, w, h, srcY) / 2.0;
        if (srcHalf < 4)
            srcHalf = Math.Max(4, RowSpan(crown, w, h, cervixY) / 2.0);
        var span = Math.Max(1, capH);
        for (var i = 0; i <= capH; i++)
        {
            var y = cervixY + dir * i;
            if ((uint)y >= (uint)h)
                continue;
            var u = i / (double)span;
            var round = Math.Sqrt(Math.Max(0, 1 - u * u));
            var half = endHalf + (srcHalf - endHalf) * round;
            var x0 = Math.Max(0, (int)Math.Floor(cx - half));
            var x1 = Math.Min(w - 1, (int)Math.Ceiling(cx + half));
            var row = y * w;
            var srcRow = srcY * w;
            for (var x = x0; x <= x1; x++)
            {
                var t = half < 0.5 ? 0 : (x - cx) / half;
                var sx = (int)Math.Round(cx + t * srcHalf);
                if ((uint)sx >= (uint)w)
                    continue;
                var p = crown[srcRow + sx];
                if (((p >> 24) & 255) < 40)
                    continue;
                dest[row + x] = p;
            }
        }
    }

    private static void ShiftTowardOcclusal(int[] dest, int w, int h, bool upper, int pad)
    {
        if (!OpaqueY(dest, w, h, out var minY, out var maxY))
            return;
        var shift = upper ? (h - 1 - pad) - maxY : pad - minY;
        if (shift == 0)
            return;
        var copy = (int[])dest.Clone();
        Array.Clear(dest);
        for (var y = minY; y <= maxY; y++)
        {
            var ny = y + shift;
            if ((uint)ny >= (uint)h)
                continue;
            Array.Copy(copy, y * w, dest, ny * w, w);
        }
    }

    private static bool OpaqueY(int[] px, int w, int h, out int minY, out int maxY)
    {
        minY = h;
        maxY = -1;
        for (var y = 0; y < h; y++)
        {
            if (RowSpan(px, w, h, y) == 0)
                continue;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }
        return maxY >= minY;
    }

    private static void DrawScrew(
        int[] dest, int w, int h, int cx, int cervixY, bool upper, int abutH, double headHalf, int tipPad, int capH)
    {
        var dir = upper ? -1 : 1;
        var start = cervixY + dir * (abutH - 2);
        var tip = upper ? tipPad : h - 1 - tipPad;
        var yMin = Math.Max(0, Math.Min(start, tip));
        var yMax = Math.Min(h - 1, Math.Max(start, tip));
        var span = Math.Max(1, Math.Abs(tip - start));
        var tipHalf = Math.Max(2.0, headHalf * 0.14);
        for (var y = yMin; y <= yMax; y++)
        {
            var dist = (y - start) * dir;
            if (dist < 0 || dist > span)
                continue;
            var along = dist / (double)span;
            var half = headHalf + (tipHalf - headHalf) * along;
            if ((dist & 2) != 0)
                half += 0.40 * (half / headHalf);
            FillSpan(dest, w, h, y, cx, half, ScrewArgb, ScrewHiArgb, ScrewEdgeArgb);
        }
    }

    private static void FillSpan(
        int[] dest, int w, int h, int y, int cx, double half, int fill, int hi, int edge)
    {
        if ((uint)y >= (uint)h)
            return;
        var x0 = Math.Max(0, (int)Math.Floor(cx - half));
        var x1 = Math.Min(w - 1, (int)Math.Ceiling(cx + half));
        var mid = (x0 + x1) / 2;
        var row = y * w;
        for (var x = x0; x <= x1; x++)
        {
            if (x == x0 || x == x1)
                dest[row + x] = edge;
            else if (x <= mid - 1)
                dest[row + x] = hi;
            else
                dest[row + x] = fill;
        }
    }
}
