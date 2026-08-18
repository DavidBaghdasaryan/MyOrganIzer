namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Anatomical procedure scope from catalog IDs via <see cref="ProcedureVisualMap"/>.
/// Does not classify by display-name fragments.
/// Returns null when the catalog row is not a dental anatomical procedure
/// (appointment slot, custom user procedures).
/// </summary>
public static class ProcedureScopeMap
{
    public static DentalProcedureScope? ForKind(ToothClinicalKind kind) => kind switch
    {
        ToothClinicalKind.Filling or ToothClinicalKind.Restoration
            => DentalProcedureScope.Surface,
        ToothClinicalKind.Endodontic
            => DentalProcedureScope.Endodontic,
        ToothClinicalKind.Crown or ToothClinicalKind.Implant
            or ToothClinicalKind.PartialDenture or ToothClinicalKind.FullDenture
            => DentalProcedureScope.WholeTooth,
        _ => null
    };

    public static DentalProcedureScope? ForId(int procedureId) =>
        ForKind(ProcedureVisualMap.KindForId(procedureId));

    public static DentalProcedureScope? Resolve(string? procedureName, IReadOnlyDictionary<string, int>? nameToId = null)
    {
        if (string.IsNullOrWhiteSpace(procedureName))
            return null;
        return ForKind(ProcedureVisualMap.Resolve(procedureName, nameToId));
    }
}
