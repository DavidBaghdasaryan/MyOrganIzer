using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.Entities;

namespace MyOrganizer.Wpf.Dental;

/// <summary>
/// Projects persisted <see cref="ToothWork"/> rows onto the approved Lab
/// odontogram presentation. Does not persist Lab demo patients or change
/// ToothWork storage. Endodontic rows have no canal-id column yet, so a treated
/// tooth uses the catalog canals for that FDI (Lab overlay needs at least one).
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
            if (IsExtraction(work.ProcedureName))
            {
                clinical.TryCreate(DentalProcedureType.Extraction, [])
                    ?.SetBilling(work.ProcedureName, work.Tier, work.Price);
                continue;
            }
            var kind = ProcedureVisualMap.Resolve(work.ProcedureName, nameToId);
            DentalProcedure? created = kind switch
            {
                ToothClinicalKind.Filling or ToothClinicalKind.Restoration =>
                    clinical.TryCreate(DentalProcedureType.Filling, SurfacesOf(work)),
                ToothClinicalKind.Implant =>
                    clinical.TryCreate(DentalProcedureType.Implant, []),
                ToothClinicalKind.Endodontic =>
                    clinical.TryCreate(
                        DentalProcedureType.Endodontic,
                        [],
                        ToothRootCanalCatalog.ForFdi(fdi).Select(c => c.Id)),
                _ => null
            };
            created?.SetBilling(work.ProcedureName, work.Tier, work.Price);
        }
    }

    internal const string ExtractionProcedureName = "Extraction / Missing";

    private static bool IsExtraction(string? name) =>
        !string.IsNullOrWhiteSpace(name) &&
        name.Contains("Extraction", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<ToothSurfaceType> SurfacesOf(ToothWork work)
    {
        if (LabSurfaces.TryParse(work.Surface, out var surface))
            return [surface];
        return LabSurfaces.All;
    }
}
