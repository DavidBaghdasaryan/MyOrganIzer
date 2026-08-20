using MyOrganizer.Wpf.Controls;

namespace MyOrganizer.Wpf.Dental;

/// <summary>
/// Spatial role used to place a canal on the odontogram and 3D root mesh.
/// Anatomical names stay in <see cref="ToothRootCanalDefinition.DisplayName"/>.
/// </summary>
public enum CanalSpatial
{
    Mesial,
    Distal,
    Buccal,
    Palatal,
    Lingual,
    Mesiobuccal,
    Distobuccal,
    Central
}

/// <summary>
/// One selectable root/canal on a tooth. <see cref="Id"/> is the stored value;
/// <see cref="DisplayName"/> is the anatomical label shown in UI.
/// </summary>
public readonly record struct ToothRootCanalDefinition(
    string Id,
    string DisplayName,
    CanalSpatial Spatial);

/// <summary>
/// Single source of truth for available roots/canals by FDI.
/// Procedure UI, odontogram, and 3D overlays all read from here.
/// FDI 36 (mandibular left first molar) remains the approved Mesial/Distal reference.
/// </summary>
public static class ToothRootCanalCatalog
{
    public const string Mesial = "mesial";
    public const string Distal = "distal";
    public const string Mesiobuccal = "mesiobuccal";
    public const string Distobuccal = "distobuccal";
    public const string Palatal = "palatal";
    public const string Buccal = "buccal";
    public const string Lingual = "lingual";
    public const string Central = "central";

    private static readonly ToothRootCanalDefinition[] Empty = [];

    private static readonly ToothRootCanalDefinition[] MandibularMolar =
    [
        new(Mesial, "Mesial", CanalSpatial.Mesial),
        new(Distal, "Distal", CanalSpatial.Distal)
    ];

    private static readonly ToothRootCanalDefinition[] MaxillaryMolar =
    [
        new(Mesiobuccal, "Mesiobuccal", CanalSpatial.Mesiobuccal),
        new(Distobuccal, "Distobuccal", CanalSpatial.Distobuccal),
        new(Palatal, "Palatal", CanalSpatial.Palatal)
    ];

    private static readonly ToothRootCanalDefinition[] MaxillaryPremolar =
    [
        new(Buccal, "Buccal", CanalSpatial.Buccal),
        new(Palatal, "Palatal", CanalSpatial.Palatal)
    ];

    private static readonly ToothRootCanalDefinition[] MandibularTwoCanal =
    [
        new(Buccal, "Buccal", CanalSpatial.Buccal),
        new(Lingual, "Lingual", CanalSpatial.Lingual)
    ];

    private static readonly ToothRootCanalDefinition[] SingleCanal =
    [
        new(Central, "Central", CanalSpatial.Central)
    ];

    public static IReadOnlyList<ToothRootCanalDefinition> ForFdi(string? fdi)
    {
        fdi = ToothAssetRegistry.Normalize(fdi ?? "");
        if (!ToothFdi.TryParse(fdi, out var n))
            return Empty;

        var pos = n % 10;
        var upper = ToothFdi.IsUpper(fdi);
        return pos switch
        {
            6 or 7 or 8 when upper => MaxillaryMolar,
            6 or 7 or 8 => MandibularMolar,
            4 or 5 when upper => MaxillaryPremolar,
            4 => MandibularTwoCanal,
            1 or 2 when !upper => MandibularTwoCanal,
            1 or 2 or 3 or 5 => SingleCanal,
            _ => Empty
        };
    }

    public static bool HasChoices(string? fdi) => ForFdi(fdi).Count > 0;

    public static string DisplayName(string? fdi, string id)
    {
        foreach (var canal in ForFdi(fdi))
        {
            if (string.Equals(canal.Id, id, StringComparison.OrdinalIgnoreCase))
                return canal.DisplayName;
        }
        return id;
    }

    public static string Join(string? fdi, IEnumerable<string> ids)
    {
        var set = Normalize(fdi, ids);
        if (set.Count == 0)
            return "";
        return string.Join(", ", ForFdi(fdi).Where(c => set.Contains(c.Id)).Select(c => c.DisplayName));
    }

    public static HashSet<string> Normalize(string? fdi, IEnumerable<string>? ids)
    {
        var allowed = ForFdi(fdi).Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (ids is null)
            return set;
        foreach (var id in ids)
        {
            if (string.IsNullOrWhiteSpace(id))
                continue;
            var key = id.Trim();
            if (!allowed.Contains(key))
                continue;
            foreach (var canal in ForFdi(fdi))
            {
                if (string.Equals(canal.Id, key, StringComparison.OrdinalIgnoreCase))
                    set.Add(canal.Id);
            }
        }
        return set;
    }

    /// <summary>
    /// +1 when loaded mesh +X is mesial (approved FDI 36).
    /// Use the actual loader <paramref name="meshMirrored"/> flag, not the registry
    /// MirrorX intent — FDI 16 keeps +X mesial even when the registry asks to mirror.
    /// </summary>
    public static int MeshMesialSign(string? fdi, bool meshMirrored = false)
    {
        _ = fdi;
        return meshMirrored ? -1 : 1;
    }
}
