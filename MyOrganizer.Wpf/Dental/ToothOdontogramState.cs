using MyOrganizer.Wpf.Controls;

namespace MyOrganizer.Wpf.Dental;

/// <summary>
/// How a tooth is drawn in the odontogram. Missing and Implant replace the
/// natural crown; later whole-tooth kinds can be added here.
/// </summary>
public enum OdontogramPresentation
{
    Natural,
    Implant,
    Missing
}

/// <summary>
/// Compact overlays drawn on a remaining natural tooth. Add kinds here when
/// a new procedure should appear on the odontogram without replacing the tooth.
/// </summary>
public enum OdontogramOverlayKind
{
    Filling,
    Endodontic
}

/// <summary>
/// Presentation projection for one FDI slot. Derived from procedure records;
/// never stored as a second clinical source of truth.
/// </summary>
public sealed class ToothOdontogramState
{
    public static ToothOdontogramState Healthy(string fdi) => new(fdi, OdontogramPresentation.Natural, [], []);

    public ToothOdontogramState(
        string fdi,
        OdontogramPresentation presentation,
        IReadOnlyList<OdontogramOverlayKind> overlays,
        IReadOnlyList<ToothSurfaceType> fillingSurfaces,
        IReadOnlyList<string>? treatedRootCanalIds = null)
    {
        Fdi = fdi;
        Presentation = presentation;
        Overlays = overlays;
        FillingSurfaces = fillingSurfaces;
        TreatedRootCanalIds = treatedRootCanalIds ?? [];
    }

    public string Fdi { get; }
    public OdontogramPresentation Presentation { get; }
    public IReadOnlyList<OdontogramOverlayKind> Overlays { get; }
    public IReadOnlyList<ToothSurfaceType> FillingSurfaces { get; }
    public IReadOnlyList<string> TreatedRootCanalIds { get; }

    public bool ShowNaturalTooth => Presentation == OdontogramPresentation.Natural;
    public bool ShowImplant => Presentation == OdontogramPresentation.Implant;
    public bool ShowMissing => Presentation == OdontogramPresentation.Missing;
    public bool ShowFilling => ShowNaturalTooth && Overlays.Contains(OdontogramOverlayKind.Filling);
    public bool ShowEndodontic => ShowNaturalTooth && Overlays.Contains(OdontogramOverlayKind.Endodontic);

    /// <summary>
    /// Deterministic mapping from saved procedures. Implant wins over extraction.
    /// Filling and endodontic may both appear on a remaining tooth.
    /// </summary>
    public static ToothOdontogramState From(string fdi, IEnumerable<DentalProcedure> procedures)
    {
        var hasImplant = false;
        var hasExtraction = false;
        var hasEndodontic = false;
        var filling = new HashSet<ToothSurfaceType>();
        var canals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var procedure in procedures)
        {
            switch (procedure.ProcedureType)
            {
                case DentalProcedureType.Implant:
                    hasImplant = true;
                    break;
                case DentalProcedureType.Extraction:
                    hasExtraction = true;
                    break;
                case DentalProcedureType.Endodontic:
                    hasEndodontic = true;
                    foreach (var id in procedure.RootCanalIds)
                        canals.Add(id);
                    break;
                case DentalProcedureType.Filling:
                    foreach (var surface in procedure.Surfaces)
                        filling.Add(surface);
                    break;
                case DentalProcedureType.Crown:
                case DentalProcedureType.Denture:
                    break;
            }
        }

        var presentation = hasImplant
            ? OdontogramPresentation.Implant
            : hasExtraction
                ? OdontogramPresentation.Missing
                : OdontogramPresentation.Natural;

        var overlays = new List<OdontogramOverlayKind>();
        if (presentation == OdontogramPresentation.Natural)
        {
            if (filling.Count > 0)
                overlays.Add(OdontogramOverlayKind.Filling);
            if (hasEndodontic)
                overlays.Add(OdontogramOverlayKind.Endodontic);
        }

        var treated = ToothRootCanalCatalog.ForFdi(fdi).Where(c => canals.Contains(c.Id)).Select(c => c.Id).ToList();
        return new ToothOdontogramState(
            fdi,
            presentation,
            overlays,
            LabSurfaces.All.Where(filling.Contains).ToList(),
            treated);
    }
}

public static class DentalProcedureTypes
{
    public static readonly DentalProcedureType[] All =
    [
        DentalProcedureType.Filling,
        DentalProcedureType.Implant,
        DentalProcedureType.Endodontic,
        DentalProcedureType.Extraction,
        DentalProcedureType.Crown,
        DentalProcedureType.Denture
    ];

    public static bool RequiresSurfaces(DentalProcedureType type) =>
        type == DentalProcedureType.Filling;

    public static bool RequiresRootCanals(DentalProcedureType type, string fdi) =>
        type == DentalProcedureType.Endodontic && ToothRootCanalCatalog.HasChoices(fdi);

    public static string DisplayName(DentalProcedureType type) => type switch
    {
        DentalProcedureType.Filling => "Filling",
        DentalProcedureType.Implant => "Implant",
        DentalProcedureType.Endodontic => "Endodontic / Root Canal",
        DentalProcedureType.Extraction => "Extraction / Missing",
        DentalProcedureType.Crown => "Crown",
        DentalProcedureType.Denture => "Denture",
        _ => type.ToString()
    };
}
