using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.Entities;

namespace MyOrganizer.Wpf.Dental;

/// <summary>
/// Projects persisted <see cref="ToothWork"/> rows onto the approved Lab
/// odontogram presentation. Does not persist Lab demo patients.
/// Rows with <see cref="ToothWork.ProcedureId"/> restore that Lab identity.
/// Legacy rows still fold by catalog/tier/price. Endodontic rows without
/// <see cref="ToothWork.RootCanalIds"/> keep the catalog canals for that FDI.
/// </summary>
internal static class ToothWorkOdontogramProjection
{
    public static ToothOdontogramState ForTooth(
        string fdi,
        IEnumerable<ToothWork> works,
        IReadOnlyDictionary<string, int>? nameToId)
    {
        var clinical = new ToothLabClinicalState(fdi);
        FillClinical(clinical, works, nameToId);
        return ToothOdontogramState.From(fdi, clinical.Procedures);
    }

    public static void FillClinical(
        ToothLabClinicalState clinical,
        IEnumerable<ToothWork> works,
        IReadOnlyDictionary<string, int>? nameToId)
    {
        var fdi = clinical.ToothNumber;
        foreach (var work in works)
        {
            if (!string.Equals(work.ToothFdi, fdi, StringComparison.Ordinal))
                continue;
            if (!TryMap(work, fdi, nameToId, out var type, out var surfaces, out var canals))
                continue;
            if (work.ProcedureId is Guid pid && pid != Guid.Empty)
            {
                var restored = clinical.Restore(pid, type, surfaces, canals);
                restored?.SetBilling(work.ProcedureName ?? "", work.Tier, work.Price);
                continue;
            }
            Attach(clinical, type, surfaces, work, canals);
        }
    }

    /// <summary>
    /// One Lab procedure can persist as several ToothWork rows (one surface each).
    /// Reload folds same catalog/tier/price rows back into that one record.
    /// </summary>
    private static bool Attach(
        ToothLabClinicalState clinical,
        DentalProcedureType type,
        IEnumerable<ToothSurfaceType> surfaces,
        ToothWork work,
        IEnumerable<string>? canals)
    {
        var existing = clinical.Procedures.FirstOrDefault(p =>
            p.ProcedureType == type &&
            string.Equals(p.CatalogName, work.ProcedureName, StringComparison.Ordinal) &&
            string.Equals(p.Tier ?? "", work.Tier ?? "", StringComparison.Ordinal) &&
            p.Price == work.Price);
        if (existing is null)
        {
            clinical.TryCreate(type, surfaces, canals)
                ?.SetBilling(work.ProcedureName ?? "", work.Tier, work.Price);
            return true;
        }
        if (type == DentalProcedureType.Filling)
            clinical.TryUpdateSurfaces(existing.Id, existing.Surfaces.Concat(surfaces));
        else if (type == DentalProcedureType.Endodontic && canals is not null)
            clinical.TryUpdateRootCanals(existing.Id, existing.RootCanalIds.Concat(canals));
        return false;
    }

    internal const string ExtractionProcedureName = "Extraction / Missing";

    private static bool TryMap(
        ToothWork work,
        string fdi,
        IReadOnlyDictionary<string, int>? nameToId,
        out DentalProcedureType type,
        out IEnumerable<ToothSurfaceType> surfaces,
        out IEnumerable<string>? canals)
    {
        surfaces = [];
        canals = null;
        type = DentalProcedureType.Filling;
        if (IsExtraction(work.ProcedureName))
        {
            type = DentalProcedureType.Extraction;
            return true;
        }
        var kind = ProcedureVisualMap.Resolve(work.ProcedureName, nameToId);
        switch (kind)
        {
            case ToothClinicalKind.Filling or ToothClinicalKind.Restoration:
                type = DentalProcedureType.Filling;
                surfaces = SurfacesOf(work);
                return true;
            case ToothClinicalKind.Implant:
                type = DentalProcedureType.Implant;
                return true;
            case ToothClinicalKind.Endodontic:
                type = DentalProcedureType.Endodontic;
                canals = CanalsOf(work, fdi);
                return true;
            case ToothClinicalKind.Crown:
            case ToothClinicalKind.PartialDenture:
            case ToothClinicalKind.FullDenture:
                type = IsDenture(work.ProcedureName, nameToId)
                    ? DentalProcedureType.Denture
                    : DentalProcedureType.Crown;
                return true;
            default:
                return false;
        }
    }

    private static bool IsDenture(string? name, IReadOnlyDictionary<string, int>? nameToId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
        if (nameToId is not null && nameToId.TryGetValue(name, out var id))
            return id is ProcedureVisualMap.PartialDentureId or ProcedureVisualMap.FullDentureId;
        return name.Contains("Denture", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExtraction(string? name) =>
        !string.IsNullOrWhiteSpace(name) &&
        name.Contains("Extraction", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<ToothSurfaceType> SurfacesOf(ToothWork work)
    {
        if (LabSurfaces.TryParse(work.Surface, out var surface))
            return [surface];
        return LabSurfaces.All;
    }

    private static IEnumerable<string> CanalsOf(ToothWork work, string fdi)
    {
        var stored = SplitCanals(work.RootCanalIds);
        if (stored.Count > 0)
            return stored;
        return ToothRootCanalCatalog.ForFdi(fdi).Select(c => c.Id);
    }

    private static List<string> SplitCanals(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
