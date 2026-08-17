namespace MyOrganizer.Wpf.Services;

/// <summary>
/// Catalog default tiers used only to seed missing ProcedurePrices rows.
/// Runtime UI reads ProcedurePrices — not this table.
/// </summary>
public static class ProcedureCatalogPrices
{
    public static readonly IReadOnlyDictionary<string, (decimal Tier1, decimal Tier2, decimal Tier3)> Defaults =
        new Dictionary<string, (decimal, decimal, decimal)>(StringComparer.Ordinal)
        {
            ["Removable Partial Denture (Metal Framework)"] = (250000, 230000, 200000),
            ["Full Denture"] = (70000, 65000, 60000),
            ["Implant with Zirconia Crown"] = (90000, 85000, 80000),
            ["Implant with Metal-Ceramic Crown"] = (70000, 65000, 60000),
            ["Zirconia or E-max Crown"] = (80000, 78000, 75000),
            ["Metal-Ceramic Crown"] = (35000, 30000, 25000),
            ["Composite or Inlay Restoration"] = (20000, 18000, 15000),
            ["Filling (Composite / Amalgam)"] = (15000, 13000, 10000),
            ["Work Shift / Appointment Slot"] = (5000, 4000, 3000),
            ["Endodontic Treatment (Root Canal)"] = (7000, 6000, 5000)
        };
}
