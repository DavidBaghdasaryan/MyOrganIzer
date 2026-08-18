namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Maps seeded catalog procedure IDs to chart visuals.
/// ToothWork stores names; callers resolve name → Id from the Procedures table,
/// then use this map. Does not match clinical phrases such as "Deep caries".
/// </summary>
public static class ProcedureVisualMap
{
    public const int PartialDentureId = 1;
    public const int FullDentureId = 2;
    public const int ImplantZirconiaId = 3;
    public const int ImplantMetalCeramicId = 4;
    public const int CrownZirconiaId = 5;
    public const int CrownMetalCeramicId = 6;
    public const int RestorationId = 7;
    public const int FillingId = 8;
    public const int AppointmentId = 9;
    public const int EndodonticId = 10;

    private static readonly Dictionary<int, string[]> SeedNames = new()
    {
        [PartialDentureId] = ["Removable Partial Denture (Metal Framework)"],
        [FullDentureId] = ["Full Denture"],
        [ImplantZirconiaId] = ["Implant with Zirconia Crown"],
        [ImplantMetalCeramicId] = ["Implant with Metal-Ceramic Crown"],
        [CrownZirconiaId] = ["Zirconia or E-max Crown"],
        [CrownMetalCeramicId] = ["Metal-Ceramic Crown"],
        [RestorationId] = ["Composite or Inlay Restoration"],
        [FillingId] = ["Filling (Composite / Amalgam)"],
        [AppointmentId] = ["Work Shift / Appointment Slot"],
        [EndodonticId] = ["Endodontic Treatment (Root Canal)"]
    };

    public static ToothClinicalKind KindForId(int procedureId) => procedureId switch
    {
        PartialDentureId or FullDentureId or CrownZirconiaId or CrownMetalCeramicId
            => ToothClinicalKind.Crown,
        ImplantZirconiaId or ImplantMetalCeramicId
            => ToothClinicalKind.Implant,
        RestorationId => ToothClinicalKind.Restoration,
        FillingId => ToothClinicalKind.Filling,
        EndodonticId => ToothClinicalKind.Endodontic,
        AppointmentId => ToothClinicalKind.Healthy,
        _ => ToothClinicalKind.Other
    };

    public static ToothClinicalKind Resolve(string procedureName, IReadOnlyDictionary<string, int>? nameToId = null)
    {
        if (string.IsNullOrWhiteSpace(procedureName))
            return ToothClinicalKind.Healthy;
        if (nameToId is not null && nameToId.TryGetValue(procedureName, out var id))
            return KindForId(id);
        foreach (var (seedId, names) in SeedNames)
        {
            if (names.Any(n => string.Equals(n, procedureName, StringComparison.Ordinal)))
                return KindForId(seedId);
        }
        return ToothClinicalKind.Other;
    }

    public static bool IsSurfaceState(ToothClinicalKind kind) =>
        kind is ToothClinicalKind.Filling or ToothClinicalKind.Restoration;

    public static bool IsWholeToothState(ToothClinicalKind kind) =>
        kind is ToothClinicalKind.Crown or ToothClinicalKind.Implant or ToothClinicalKind.Endodontic;
}
