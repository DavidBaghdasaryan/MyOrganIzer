using MyOrganizer.Wpf.Controls;

namespace MyOrganizer.Wpf.Dental;

/// <summary>
/// One selectable root/canal on a tooth. <see cref="Id"/> is the stored value;
/// <see cref="DisplayName"/> is the anatomical label shown in UI.
/// </summary>
public readonly record struct ToothRootCanalDefinition(string Id, string DisplayName);

/// <summary>
/// Single source of truth for available roots/canals by FDI.
/// Procedure UI, odontogram, and 3D overlays all read from here.
/// First reference implementation: FDI 36 (mandibular left first molar).
/// </summary>
public static class ToothRootCanalCatalog
{
    public const string Mesial = "mesial";
    public const string Distal = "distal";

    private static readonly ToothRootCanalDefinition[] Empty = [];

    private static readonly ToothRootCanalDefinition[] MandibularFirstMolar36 =
    [
        new(Mesial, "Mesial"),
        new(Distal, "Distal")
    ];

    public static IReadOnlyList<ToothRootCanalDefinition> ForFdi(string? fdi)
    {
        fdi = ToothAssetRegistry.Normalize(fdi ?? "");
        return fdi == "36" ? MandibularFirstMolar36 : Empty;
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
}
