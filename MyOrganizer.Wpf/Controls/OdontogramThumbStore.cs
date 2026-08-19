using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Frozen static previews for odontogram cells. Prefers packed PNGs generated
/// from the approved meshes; falls back to an in-memory raster of the same mesh.
/// </summary>
internal static class OdontogramThumbStore
{
    private static readonly Dictionary<string, ImageSource> Cache = new(StringComparer.Ordinal);
    private static bool _warmed;

    public static ImageSource? Get(string fdi)
    {
        fdi = ToothAssetRegistry.Normalize(fdi);
        if (Cache.TryGetValue(fdi, out var hit))
            return hit;
        var img = LoadPacked(fdi) ?? LoadFromDisk(fdi);
        if (img is not null)
        {
            Cache[fdi] = img;
            return img;
        }
        if (!ToothAssetRegistry.TryGet(fdi, out var asset) || !asset.RuntimeImported)
            return null;
        img = OdontogramThumbRenderer.Render(asset);
        if (img is not null)
            Cache[fdi] = img;
        return img;
    }

    public static void Warm()
    {
        if (_warmed)
            return;
        _warmed = true;
        foreach (var asset in ToothAssetRegistry.All)
        {
            if (!asset.RuntimeImported)
                continue;
            var img = LoadPacked(asset.FdiNumber) ?? LoadFromDisk(asset.FdiNumber);
            if (img is not null)
                Cache[asset.FdiNumber] = img;
        }
    }

    private static ImageSource? LoadPacked(string fdi)
    {
        try
        {
            var uri = new Uri(OdontogramThumbRenderer.PackUri(fdi), UriKind.Absolute);
            var stream = Application.GetResourceStream(uri)?.Stream;
            if (stream is null)
                return null;
            using (stream)
                return FromStream(stream);
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource? LoadFromDisk(string fdi)
    {
        var path = OdontogramThumbRenderer.FindPackedOrFile(fdi);
        if (path is null)
            return null;
        using var fs = File.OpenRead(path);
        return FromStream(fs);
    }

    private static ImageSource FromStream(Stream stream)
    {
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.StreamSource = stream;
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }
}
