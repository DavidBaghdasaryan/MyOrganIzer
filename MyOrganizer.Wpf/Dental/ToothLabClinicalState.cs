using MyOrganizer.Wpf.Controls;

namespace MyOrganizer.Wpf.Dental;

/// <summary>
/// Lab-only procedure catalog. Filling is the only type in this prototype.
/// </summary>
public enum DentalProcedureType
{
    Filling
}

/// <summary>
/// One clinical procedure record. Identity is independent of visualization.
/// A Filling may own any subset of the five crown surfaces.
/// Palatal is stored as <see cref="ToothSurfaceType.Lingual"/>.
/// </summary>
public sealed class DentalProcedure
{
    private readonly HashSet<ToothSurfaceType> _surfaces;

    internal DentalProcedure(
        Guid id,
        int displayNumber,
        string toothNumber,
        DentalProcedureType procedureType,
        IEnumerable<ToothSurfaceType> surfaces)
    {
        Id = id;
        DisplayNumber = displayNumber;
        ToothNumber = toothNumber;
        ProcedureType = procedureType;
        _surfaces = LabSurfaces.Normalize(surfaces);
    }

    public Guid Id { get; }
    public int DisplayNumber { get; }
    public string ToothNumber { get; }
    public DentalProcedureType ProcedureType { get; }
    public IReadOnlyCollection<ToothSurfaceType> Surfaces => _surfaces;

    internal bool ReplaceSurfaces(IEnumerable<ToothSurfaceType> surfaces)
    {
        var next = LabSurfaces.Normalize(surfaces);
        if (next.Count == 0 || next.SetEquals(_surfaces))
            return false;
        _surfaces.Clear();
        foreach (var surface in next)
            _surfaces.Add(surface);
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
    /// </summary>
    public DentalProcedure? TryCreate(DentalProcedureType type, IEnumerable<ToothSurfaceType> surfaces)
    {
        var set = LabSurfaces.Normalize(surfaces);
        if (set.Count == 0)
            return null;
        var procedure = new DentalProcedure(
            Guid.NewGuid(),
            _nextDisplayNumber++,
            ToothNumber,
            type,
            set);
        _procedures.Add(procedure);
        return procedure;
    }

    public bool TryUpdateSurfaces(Guid id, IEnumerable<ToothSurfaceType> surfaces)
    {
        var procedure = Find(id);
        return procedure is not null && procedure.ReplaceSurfaces(surfaces);
    }

    /// <summary>
    /// Current visualization: union of Filling surfaces across all records.
    /// Names are 3D labels (Palatal, not Lingual).
    /// </summary>
    public IReadOnlyList<string> FillingSurfaceNames()
    {
        var set = new HashSet<ToothSurfaceType>();
        foreach (var procedure in _procedures)
        {
            if (procedure.ProcedureType != DentalProcedureType.Filling)
                continue;
            foreach (var surface in procedure.Surfaces)
                set.Add(surface);
        }
        return LabSurfaces.DisplayNames(set);
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

    public static string DisplayName(ToothSurfaceType surface) =>
        surface == ToothSurfaceType.Lingual ? "Palatal" : surface.ToString();

    public static string Join(IEnumerable<ToothSurfaceType> surfaces)
    {
        var set = surfaces as ISet<ToothSurfaceType> ?? surfaces.ToHashSet();
        return string.Join(", ", All.Where(set.Contains).Select(DisplayName));
    }

    public static IReadOnlyList<string> DisplayNames(IEnumerable<ToothSurfaceType> surfaces)
    {
        var set = surfaces as ISet<ToothSurfaceType> ?? surfaces.ToHashSet();
        return All.Where(set.Contains).Select(DisplayName).ToList();
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
