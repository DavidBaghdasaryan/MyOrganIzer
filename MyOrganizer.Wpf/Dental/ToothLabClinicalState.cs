using MyOrganizer.Wpf.Controls;

namespace MyOrganizer.Wpf.Dental;

/// <summary>
/// Lab procedure catalog. Filling is surface-scoped; Endodontic is root/canal-scoped
/// when the tooth has catalog entries. Implant and Extraction remain whole-tooth.
/// Odontogram presentation is derived from these records, not stored separately.
/// </summary>
public enum DentalProcedureType
{
    Filling,
    Implant,
    Endodontic,
    Extraction,
    Crown,
    Denture
}

/// <summary>
/// One clinical procedure record. Identity is independent of visualization.
/// A Filling may own any subset of the five crown surfaces.
/// Palatal is stored as <see cref="ToothSurfaceType.Lingual"/>.
/// An Endodontic record stores selected root/canal IDs from
/// <see cref="ToothRootCanalCatalog"/>; it never creates a Filling.
/// </summary>
public sealed class DentalProcedure
{
    private readonly HashSet<ToothSurfaceType> _surfaces;
    private readonly HashSet<string> _rootCanalIds;

    internal DentalProcedure(
        Guid id,
        int displayNumber,
        string toothNumber,
        DentalProcedureType procedureType,
        IEnumerable<ToothSurfaceType> surfaces,
        IEnumerable<string>? rootCanalIds = null)
    {
        Id = id;
        DisplayNumber = displayNumber;
        ToothNumber = toothNumber;
        ProcedureType = procedureType;
        _surfaces = procedureType == DentalProcedureType.Filling
            ? LabSurfaces.Normalize(surfaces)
            : [];
        _rootCanalIds = procedureType == DentalProcedureType.Endodontic
            ? ToothRootCanalCatalog.Normalize(toothNumber, rootCanalIds)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public Guid Id { get; }
    public int DisplayNumber { get; }
    public string ToothNumber { get; }
    public DentalProcedureType ProcedureType { get; }
    public IReadOnlyCollection<ToothSurfaceType> Surfaces => _surfaces;
    public IReadOnlyCollection<string> RootCanalIds => _rootCanalIds;
    public string CatalogName { get; private set; } = "";
    public string Tier { get; private set; } = "";
    public int Price { get; private set; }

    internal void SetBilling(string catalogName, string? tier, int price)
    {
        CatalogName = catalogName ?? "";
        Tier = tier ?? "";
        Price = price;
    }

    internal bool ReplaceSurfaces(IEnumerable<ToothSurfaceType> surfaces)
    {
        if (ProcedureType != DentalProcedureType.Filling)
            return false;
        var next = LabSurfaces.Normalize(surfaces);
        if (next.Count == 0 || next.SetEquals(_surfaces))
            return false;
        _surfaces.Clear();
        foreach (var surface in next)
            _surfaces.Add(surface);
        return true;
    }

    internal bool ReplaceRootCanals(IEnumerable<string> rootCanalIds)
    {
        if (ProcedureType != DentalProcedureType.Endodontic)
            return false;
        var next = ToothRootCanalCatalog.Normalize(ToothNumber, rootCanalIds);
        if (next.Count == 0 || next.SetEquals(_rootCanalIds))
            return false;
        _rootCanalIds.Clear();
        foreach (var id in next)
            _rootCanalIds.Add(id);
        return true;
    }
}

/// <summary>
/// In-memory Tooth Lab chart: first-class procedure records plus a derived
/// current-state projection for rendering. Never merges or splits records.
/// </summary>
public sealed class ToothLabClinicalState
{
    private readonly List<DentalProcedure> _procedures = [];
    private int _nextDisplayNumber = 1;

    public ToothLabClinicalState(string toothNumber) => ToothNumber = toothNumber;

    public string ToothNumber { get; }

    public IReadOnlyList<DentalProcedure> Procedures => _procedures;

    public DentalProcedure? Find(Guid id) =>
        _procedures.FirstOrDefault(p => p.Id == id);

    /// <summary>
    /// Always appends a new record. Same tooth and type never merge automatically.
    /// Filling requires at least one surface. Endodontic requires at least one
    /// catalog canal when the tooth has definitions. Never creates a Filling
    /// as a side effect of Root Canal.
    /// </summary>
    public DentalProcedure? TryCreate(
        DentalProcedureType type,
        IEnumerable<ToothSurfaceType> surfaces,
        IEnumerable<string>? rootCanalIds = null)
    {
        var set = type == DentalProcedureType.Filling ? LabSurfaces.Normalize(surfaces) : [];
        if (DentalProcedureTypes.RequiresSurfaces(type) && set.Count == 0)
            return null;
        var canals = type == DentalProcedureType.Endodontic
            ? ToothRootCanalCatalog.Normalize(ToothNumber, rootCanalIds)
            : [];
        if (DentalProcedureTypes.RequiresRootCanals(type, ToothNumber) && canals.Count == 0)
            return null;
        var procedure = new DentalProcedure(
            Guid.NewGuid(),
            _nextDisplayNumber++,
            ToothNumber,
            type,
            set,
            canals);
        _procedures.Add(procedure);
        return procedure;
    }

    /// <summary>
    /// Rebuilds a record from storage using its saved identity. Lab create/edit
    /// still goes through <see cref="TryCreate"/> and never calls this.
    /// </summary>
    internal DentalProcedure? Restore(
        Guid id,
        DentalProcedureType type,
        IEnumerable<ToothSurfaceType> surfaces,
        IEnumerable<string>? rootCanalIds = null)
    {
        if (id == Guid.Empty)
            return TryCreate(type, surfaces, rootCanalIds);

        var existing = Find(id);
        if (existing is not null)
        {
            if (type == DentalProcedureType.Filling)
                TryUpdateSurfaces(id, existing.Surfaces.Concat(surfaces));
            else if (type == DentalProcedureType.Endodontic)
                TryUpdateRootCanals(id, existing.RootCanalIds.Concat(rootCanalIds ?? []));
            return existing;
        }

        var set = type == DentalProcedureType.Filling ? LabSurfaces.Normalize(surfaces) : [];
        if (DentalProcedureTypes.RequiresSurfaces(type) && set.Count == 0)
            return null;
        var canals = type == DentalProcedureType.Endodontic
            ? ToothRootCanalCatalog.Normalize(ToothNumber, rootCanalIds)
            : [];
        if (DentalProcedureTypes.RequiresRootCanals(type, ToothNumber) && canals.Count == 0)
            return null;
        var procedure = new DentalProcedure(
            id,
            _nextDisplayNumber++,
            ToothNumber,
            type,
            set,
            canals);
        _procedures.Add(procedure);
        return procedure;
    }

    public bool TryUpdateSurfaces(Guid id, IEnumerable<ToothSurfaceType> surfaces)
    {
        var procedure = Find(id);
        return procedure is not null && procedure.ReplaceSurfaces(surfaces);
    }

    public bool TryUpdateRootCanals(Guid id, IEnumerable<string> rootCanalIds)
    {
        var procedure = Find(id);
        return procedure is not null && procedure.ReplaceRootCanals(rootCanalIds);
    }

    public bool TryRemove(Guid id)
    {
        var n = _procedures.RemoveAll(p => p.Id == id);
        return n > 0;
    }

    /// <summary>
    /// Current visualization: union of Filling surfaces across all records.
    /// Names are tooth-aware 3D labels (Palatal on maxilla, Lingual on mandible).
    /// </summary>
    public IReadOnlyList<string> FillingSurfaceNames(string innerName = "Palatal")
    {
        var set = new HashSet<ToothSurfaceType>();
        foreach (var procedure in _procedures)
        {
            if (procedure.ProcedureType != DentalProcedureType.Filling)
                continue;
            foreach (var surface in procedure.Surfaces)
                set.Add(surface);
        }
        return LabSurfaces.DisplayNames(set, innerName);
    }

    /// <summary>
    /// Union of treated root/canal IDs across Endodontic records. Independent of Filling.
    /// </summary>
    public IReadOnlyList<string> TreatedRootCanalIds()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var procedure in _procedures)
        {
            if (procedure.ProcedureType != DentalProcedureType.Endodontic)
                continue;
            foreach (var id in procedure.RootCanalIds)
                set.Add(id);
        }
        return ToothRootCanalCatalog.ForFdi(ToothNumber).Where(c => set.Contains(c.Id)).Select(c => c.Id).ToList();
    }
}

public static class LabSurfaces
{
    public static readonly ToothSurfaceType[] All =
    [
        ToothSurfaceType.Occlusal,
        ToothSurfaceType.Buccal,
        ToothSurfaceType.Lingual,
        ToothSurfaceType.Mesial,
        ToothSurfaceType.Distal
    ];

    public static string DisplayName(ToothSurfaceType surface, string innerName = "Palatal") =>
        surface == ToothSurfaceType.Lingual ? innerName : surface.ToString();

    public static string Join(IEnumerable<ToothSurfaceType> surfaces, string innerName = "Palatal")
    {
        var set = surfaces as ISet<ToothSurfaceType> ?? surfaces.ToHashSet();
        return string.Join(", ", All.Where(set.Contains).Select(s => DisplayName(s, innerName)));
    }

    public static IReadOnlyList<string> DisplayNames(IEnumerable<ToothSurfaceType> surfaces, string innerName = "Palatal")
    {
        var set = surfaces as ISet<ToothSurfaceType> ?? surfaces.ToHashSet();
        return All.Where(set.Contains).Select(s => DisplayName(s, innerName)).ToList();
    }

    public static HashSet<ToothSurfaceType> Normalize(IEnumerable<ToothSurfaceType> surfaces)
    {
        var set = new HashSet<ToothSurfaceType>();
        foreach (var surface in surfaces)
        {
            if (All.Contains(surface))
                set.Add(surface);
        }
        return set;
    }

    public static bool TryParse(string? name, out ToothSurfaceType surface)
    {
        surface = ToothSurfaceType.Occlusal;
        if (string.IsNullOrWhiteSpace(name))
            return false;
        if (name.Equals("Palatal", StringComparison.OrdinalIgnoreCase))
        {
            surface = ToothSurfaceType.Lingual;
            return true;
        }
        return Enum.TryParse(name, true, out surface) && All.Contains(surface);
    }
}
