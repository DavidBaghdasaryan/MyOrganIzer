using System.Windows.Media;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Visual payload for one occlusal surface. Procedure keys are looked up in
/// <see cref="ToothSurfaceAppearance"/>; <see cref="Color"/> overrides the map.
/// </summary>
public sealed class ToothSurfaceVisual
{
    public string ProcedureKey { get; init; } = ToothSurfaceAppearance.Healthy;
    public Color? Color { get; init; }
}

/// <summary>
/// Procedure-key → frozen brushes. The tooth renderer does not know catalog IDs.
/// Add a new procedure by adding a key here; geometry stays unchanged.
/// </summary>
public static class ToothSurfaceAppearance
{
    public const string Healthy = "healthy";
    public const string Filling = "filling";
    public const string Caries = "caries";
    public const string Crown = "crown";
    public const string Implant = "implant";
    public const string Endo = "endo";
    public const string Extraction = "extraction";
    public const string Missing = "missing";
    public const string Temporary = "temporary";
    public const string Bridge = "bridge";
    public const string Custom = "custom";

    public static IReadOnlyList<string> Keys { get; } =
    [
        Healthy, Filling, Caries, Crown, Implant, Endo,
        Extraction, Missing, Temporary, Bridge, Custom
    ];

    public static bool IsHealthy(string? key) =>
        string.IsNullOrWhiteSpace(key) ||
        string.Equals(key, Healthy, StringComparison.OrdinalIgnoreCase);

    public static Brush FillFor(ToothSurfaceVisual? visual)
    {
        if (visual?.Color is { } color)
            return Solid(color.A == 0 ? Color.FromArgb(0xE0, color.R, color.G, color.B) : color);

        return FillFor(visual?.ProcedureKey);
    }

    public static Brush FillFor(string? key)
    {
        if (IsHealthy(key))
            return Brushes.Transparent;

        return key?.ToLowerInvariant() switch
        {
            Filling => Solid(0xD9, 0xC5, 0xCB, 0xD1),
            Caries => Solid(0xD9, 0x8D, 0x6E, 0x4A),
            Crown => Solid(0xD9, 0xD5, 0xDA, 0xE0),
            Implant => Solid(0xD9, 0x6B, 0x72, 0x80),
            Endo => Solid(0xD0, 0xC6, 0x28, 0x28),
            Extraction => Solid(0xB8, 0xE5, 0xE7, 0xEB),
            Missing => Solid(0x88, 0xF3, 0xF4, 0xF6),
            Temporary => Solid(0xD9, 0xF6, 0xE7, 0xC1),
            Bridge => Solid(0xD9, 0xB0, 0xB8, 0xC4),
            Custom => Solid(0xD9, 0x93, 0xC5, 0xFD),
            _ => Brushes.Transparent
        };
    }

    public static string DisplayName(string key) => key switch
    {
        Healthy => "Healthy",
        Filling => "Filling",
        Caries => "Caries",
        Crown => "Crown",
        Implant => "Implant",
        Endo => "Endodontic",
        Extraction => "Extraction",
        Missing => "Missing",
        Temporary => "Temporary",
        Bridge => "Bridge",
        Custom => "Custom",
        _ => key
    };

    private static SolidColorBrush Solid(byte a, byte r, byte g, byte b) =>
        Solid(Color.FromArgb(a, r, g, b));

    private static SolidColorBrush Solid(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
