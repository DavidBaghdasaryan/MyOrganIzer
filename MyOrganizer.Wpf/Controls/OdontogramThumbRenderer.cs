using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Static buccal thumbnails rasterized from the approved aligned 3D meshes.
/// Does not change those meshes. Odontogram cells are not interactive 3D.
/// </summary>
internal static class OdontogramThumbRenderer
{
    public const int Width = 80;
    public const int Height = 108;

    public static string GenerateAll()
    {
        var dir = OutputDirectory() ?? throw new DirectoryNotFoundException("Assets/Teeth folder not found.");
        Directory.CreateDirectory(dir);
        var n = 0;
        foreach (var asset in ToothAssetRegistry.All)
        {
            if (!asset.RuntimeImported || string.IsNullOrWhiteSpace(asset.RuntimeMesh))
                continue;
            var bmp = Render(asset);
            if (bmp is null)
                continue;
            var path = Path.Combine(dir, FileName(asset.FdiNumber));
            SavePng(bmp, path);
            n++;
        }
        return dir + " (" + n + " thumbs)";
    }

    public static BitmapSource? Render(ToothAssetDefinition asset)
    {
        if (string.IsNullOrWhiteSpace(asset.RuntimeMesh))
            return null;
        using var stream = OpenMesh(asset.RuntimeMesh);
        if (stream is null)
            return null;
        var parts = StlToothLoader.LoadAlignedParts(stream, out _, LoadOptions(asset));
        return Rasterize(asset, parts);
    }

    /// <summary>
    /// Crown-only raster for implant visuals. Same camera as the odontogram
    /// thumb so neighbor size matches; root mesh is not drawn.
    /// Does not change packed natural-tooth thumbnails.
    /// </summary>
    internal static BitmapSource? RenderImplantCrown(ToothAssetDefinition asset)
    {
        if (asset.ToothKind is ToothKind.Canine or ToothKind.Incisor)
            return CropNaturalCrown(asset);
        if (string.IsNullOrWhiteSpace(asset.RuntimeMesh))
            return null;
        using var stream = OpenMesh(asset.RuntimeMesh);
        if (stream is null)
            return null;
        var parts = StlToothLoader.LoadAlignedParts(stream, out _, LoadOptions(asset));
        return RasterizeImplantCrown(asset, parts);
    }

    /// <summary>
    /// Canine/incisor crown+root material split leaves a disconnected stub.
    /// Crop the approved odontogram thumbnail so the implant crown matches
    /// the natural cell; do not change 3D meshes.
    /// </summary>
    private static BitmapSource? CropNaturalCrown(ToothAssetDefinition asset)
    {
        var src = OdontogramThumbStore.Get(asset.FdiNumber) as BitmapSource ?? Render(asset);
        if (src is null)
            return null;
        var w = src.PixelWidth;
        var h = src.PixelHeight;
        var px = new int[w * h];
        var copy = src;
        if (copy.Format != PixelFormats.Bgra32)
            copy = new FormatConvertedBitmap(copy, PixelFormats.Bgra32, null, 0);
        copy.CopyPixels(px, w * 4, 0);
        var upper = asset.Jaw == ToothJaw.Maxilla;
        if (!FindThumbCervix(px, w, h, upper, out var cej))
            return src;
        if (upper)
        {
            for (var y = 0; y <= cej; y++)
                Array.Clear(px, y * w, w);
        }
        else
        {
            for (var y = cej; y < h; y++)
                Array.Clear(px, y * w, w);
        }
        var bmp = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
        bmp.WritePixels(new Int32Rect(0, 0, w, h), px, w * 4, 0);
        bmp.Freeze();
        return bmp;
    }

    private static bool FindThumbCervix(int[] px, int w, int h, bool upper, out int cej)
    {
        cej = upper ? 0 : h - 1;
        var start = upper ? h - 1 : 0;
        var dir = upper ? -1 : 1;
        var seenCrown = false;
        for (var i = 0; i < h; i++)
        {
            var y = start + dir * i;
            if ((uint)y >= (uint)h)
                break;
            CrownRootCounts(px, w, y, out var crown, out var root);
            if (crown > 6)
                seenCrown = true;
            if (!seenCrown)
                continue;
            if (root > crown && root > 6)
            {
                cej = y;
                return true;
            }
        }
        return false;
    }

    private static void CrownRootCounts(int[] px, int w, int y, out int crown, out int root)
    {
        crown = 0;
        root = 0;
        var row = y * w;
        for (var x = 0; x < w; x++)
        {
            var p = px[row + x];
            var a = (p >> 24) & 255;
            if (a < 40)
                continue;
            var r = (p >> 16) & 255;
            var g = (p >> 8) & 255;
            var b = p & 255;
            var lum = (r + g + b) / 3;
            var chroma = ((r + g) / 2) - b;
            if (lum > 198 && chroma < 22)
                crown++;
            else if (chroma > 12 && lum is > 130 and < 205)
                root++;
        }
    }

    private static BitmapSource RasterizeImplantCrown(ToothAssetDefinition asset, ToothMeshParts parts)
    {
        var w = Width;
        var h = Height;
        var argb = new int[w * h];
        var zbuf = new float[w * h];
        Array.Fill(zbuf, float.MinValue);

        var minX = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var minZ = double.PositiveInfinity;
        var maxZ = double.NegativeInfinity;
        Bound(parts.Crown, ref minX, ref maxX, ref minZ, ref maxZ);
        Bound(parts.Root, ref minX, ref maxX, ref minZ, ref maxZ);
        Bound(parts.Cervical, ref minX, ref maxX, ref minZ, ref maxZ);
        if (double.IsInfinity(minX))
            return Empty();

        var mandibular = asset.Jaw == ToothJaw.Mandible;
        var crownRgb = mandibular ? 0xF3EFE6 : 0xF8F6F1;
        var light = new Vector3D(0.22, 0.88, 0.42);
        light.Normalize();
        var crownDown = CrownAtBottom(asset, parts);

        Draw(parts.Cervical, argb, zbuf, w, h, minX, maxX, minZ, maxZ, crownDown, crownRgb, light);
        Draw(parts.Crown, argb, zbuf, w, h, minX, maxX, minZ, maxZ, crownDown, crownRgb, light);
        Outline(argb, zbuf, w, h);

        var bmp = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
        bmp.WritePixels(new Int32Rect(0, 0, w, h), argb, w * 4, 0);
        bmp.Freeze();
        return bmp;
    }

    internal static MeshLoadOptions LoadOptions(ToothAssetDefinition asset) => new()
    {
        MirrorX = asset.MirrorX,
        OrientFdi16 = asset.OrientationProfile is "ApprovedFdi16"
            or MaxillaryFirstMolarTemplate.OrientationProfile
            or MaxillarySecondMolarTemplate.OrientationProfile,
        OrientationProfile = asset.OrientationProfile
    };

    internal static string FileName(string fdi) => "FDI" + fdi + "_thumb.png";

    internal static string PackUri(string fdi) =>
        "pack://application:,,,/MyOrganizer.Wpf;component/Assets/Teeth/Odontogram/" + FileName(fdi);

    private static BitmapSource Rasterize(ToothAssetDefinition asset, ToothMeshParts parts)
    {
        var w = Width;
        var h = Height;
        var argb = new int[w * h];
        var zbuf = new float[w * h];
        Array.Fill(zbuf, float.MinValue);

        var minX = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var minZ = double.PositiveInfinity;
        var maxZ = double.NegativeInfinity;
        Bound(parts.Crown, ref minX, ref maxX, ref minZ, ref maxZ);
        Bound(parts.Root, ref minX, ref maxX, ref minZ, ref maxZ);
        Bound(parts.Cervical, ref minX, ref maxX, ref minZ, ref maxZ);
        if (double.IsInfinity(minX))
            return Empty();

        var mandibular = asset.Jaw == ToothJaw.Mandible;
        var crownRgb = mandibular ? 0xF3EFE6 : 0xF8F6F1;
        var rootRgb = mandibular ? 0xE7DBC4 : 0xE2D4B2;
        var cervixRgb = mandibular ? 0xEDE6D7 : 0xEEE6D8;
        var light = new Vector3D(0.22, 0.88, 0.42);
        light.Normalize();
        var crownDown = CrownAtBottom(asset, parts);

        Draw(parts.Root, argb, zbuf, w, h, minX, maxX, minZ, maxZ, crownDown, rootRgb, light);
        Draw(parts.Cervical, argb, zbuf, w, h, minX, maxX, minZ, maxZ, crownDown, cervixRgb, light);
        Draw(parts.Crown, argb, zbuf, w, h, minX, maxX, minZ, maxZ, crownDown, crownRgb, light);
        Outline(argb, zbuf, w, h);

        var bmp = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
        bmp.WritePixels(new Int32Rect(0, 0, w, h), argb, w * 4, 0);
        bmp.Freeze();
        return bmp;
    }

    /// <summary>
    /// Upper cells: roots toward the top, crowns toward the arch gap.
    /// Lower cells: crowns toward the arch gap, roots toward the bottom.
    /// Uses crown/root centroids so teeth whose mesh Z is inverted still
    /// read top-to-bottom like the rest of the arch.
    /// </summary>
    private static bool CrownAtBottom(ToothAssetDefinition asset, ToothMeshParts parts)
    {
        var crownZ = MeanZ(parts.Crown);
        var rootZ = MeanZ(parts.Root);
        var crownIsHighZ = crownZ >= rootZ;
        return asset.Jaw == ToothJaw.Maxilla ? crownIsHighZ : !crownIsHighZ;
    }

    private static double MeanZ(MeshGeometry3D mesh)
    {
        var n = mesh.Positions.Count;
        if (n == 0)
            return 0;
        double sum = 0;
        foreach (var p in mesh.Positions)
            sum += p.Z;
        return sum / n;
    }

    private static void Bound(MeshGeometry3D mesh, ref double minX, ref double maxX, ref double minZ, ref double maxZ)
    {
        foreach (var p in mesh.Positions)
        {
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Z < minZ) minZ = p.Z;
            if (p.Z > maxZ) maxZ = p.Z;
        }
    }

    private static void Draw(
        MeshGeometry3D mesh,
        int[] argb,
        float[] zbuf,
        int w,
        int h,
        double minX,
        double maxX,
        double minZ,
        double maxZ,
        bool crownAtBottom,
        int rgb,
        Vector3D light)
    {
        var idx = mesh.TriangleIndices;
        var pos = mesh.Positions;
        var n = idx.Count / 3;
        if (n == 0)
            return;
        var pad = 0.10;
        var xRange = Math.Max(1e-6, maxX - minX);
        var zRange = Math.Max(1e-6, maxZ - minZ);
        var ox = minX - xRange * pad;
        var oz = minZ - zRange * pad;
        var sx = (w - 1) / (xRange * (1 + pad * 2));
        var sz = (h - 1) / (zRange * (1 + pad * 2));
        var cr = (rgb >> 16) & 255;
        var cg = (rgb >> 8) & 255;
        var cb = rgb & 255;

        int MapX(double x) => (int)Math.Round((x - ox) * sx);
        int MapY(double z)
        {
            var u = (z - oz) * sz;
            return crownAtBottom ? (int)Math.Round(u) : (h - 1) - (int)Math.Round(u);
        }

        for (var t = 0; t < n; t++)
        {
            var a = pos[idx[t * 3]];
            var b = pos[idx[t * 3 + 1]];
            var c = pos[idx[t * 3 + 2]];
            var nrm = Vector3D.CrossProduct(b - a, c - a);
            if (nrm.LengthSquared < 1e-16)
                continue;
            nrm.Normalize();
            var ndot = Math.Max(0, Vector3D.DotProduct(nrm, light));
            var shade = 0.34 + 0.66 * ndot;
            var packed = unchecked((int)0xFF000000)
                         | ((Clamp((int)(cr * shade)) << 16)
                            | (Clamp((int)(cg * shade)) << 8)
                            | Clamp((int)(cb * shade)));

            var ax = MapX(a.X); var ay = MapY(a.Z); var az = (float)a.Y;
            var bx = MapX(b.X); var by = MapY(b.Z); var bz = (float)b.Y;
            var cx = MapX(c.X); var cy = MapY(c.Z); var cz = (float)c.Y;
            Fill(argb, zbuf, w, h, ax, ay, az, bx, by, bz, cx, cy, cz, packed);
        }
    }

    private static void Fill(
        int[] argb, float[] zbuf, int w, int h,
        int ax, int ay, float az,
        int bx, int by, float bz,
        int cx, int cy, float cz,
        int color)
    {
        var minX = Math.Max(0, Math.Min(ax, Math.Min(bx, cx)));
        var maxX = Math.Min(w - 1, Math.Max(ax, Math.Max(bx, cx)));
        var minY = Math.Max(0, Math.Min(ay, Math.Min(by, cy)));
        var maxY = Math.Min(h - 1, Math.Max(ay, Math.Max(by, cy)));
        var area = (double)(bx - ax) * (cy - ay) - (double)(by - ay) * (cx - ax);
        if (Math.Abs(area) < 0.5)
            return;
        var inv = 1.0 / area;
        for (var y = minY; y <= maxY; y++)
        {
            var row = y * w;
            for (var x = minX; x <= maxX; x++)
            {
                var w0 = ((bx - x) * (cy - y) - (by - y) * (cx - x)) * inv;
                var w1 = ((cx - x) * (ay - y) - (cy - y) * (ax - x)) * inv;
                var w2 = 1 - w0 - w1;
                if (w0 < -0.01 || w1 < -0.01 || w2 < -0.01)
                    continue;
                var depth = (float)(w0 * az + w1 * bz + w2 * cz);
                var i = row + x;
                if (depth < zbuf[i])
                    continue;
                zbuf[i] = depth;
                argb[i] = color;
            }
        }
    }

    private static void Outline(int[] argb, float[] zbuf, int w, int h)
    {
        var copy = (int[])argb.Clone();
        const int edge = unchecked((int)0xFF8A7A62);
        for (var y = 0; y < h; y++)
        {
            var row = y * w;
            for (var x = 0; x < w; x++)
            {
                if (zbuf[row + x] == float.MinValue)
                    continue;
                if (x == 0 || x == w - 1 || y == 0 || y == h - 1
                    || zbuf[row + x - 1] == float.MinValue
                    || zbuf[row + x + 1] == float.MinValue
                    || (y > 0 && zbuf[row - w + x] == float.MinValue)
                    || (y < h - 1 && zbuf[row + w + x] == float.MinValue))
                    copy[row + x] = edge;
            }
        }
        Array.Copy(copy, argb, argb.Length);
    }

    private static int Clamp(int v) => v < 0 ? 0 : v > 255 ? 255 : v;

    private static BitmapSource Empty()
    {
        var bmp = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Bgra32, null);
        bmp.Freeze();
        return bmp;
    }

    private static void SavePng(BitmapSource bmp, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        using var fs = File.Create(path);
        encoder.Save(fs);
    }

    private static Stream? OpenMesh(string fileName)
    {
        var path = FindSource(fileName);
        if (path is not null)
            return File.OpenRead(path);
        try
        {
            return Application.GetResourceStream(
                new Uri(
                    "pack://application:,,,/MyOrganizer.Wpf;component/Assets/Teeth/Source/" + fileName,
                    UriKind.Absolute))?.Stream;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static string? FindSource(string fileName)
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var a = Path.Combine(dir, "Assets", "Teeth", "Source", fileName);
            var b = Path.Combine(dir, "MyOrganizer.Wpf", "Assets", "Teeth", "Source", fileName);
            if (File.Exists(a)) return a;
            if (File.Exists(b)) return b;
            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }
        return null;
    }

    internal static string? OutputDirectory()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var a = Path.Combine(dir, "Assets", "Teeth");
            var b = Path.Combine(dir, "MyOrganizer.Wpf", "Assets", "Teeth");
            if (Directory.Exists(a)) return Path.Combine(a, "Odontogram");
            if (Directory.Exists(b)) return Path.Combine(b, "Odontogram");
            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }
        return null;
    }

    internal static string? FindPackedOrFile(string fdi)
    {
        var dir = OutputDirectory();
        if (dir is null) return null;
        var path = Path.Combine(dir, FileName(fdi));
        return File.Exists(path) ? path : null;
    }
}
