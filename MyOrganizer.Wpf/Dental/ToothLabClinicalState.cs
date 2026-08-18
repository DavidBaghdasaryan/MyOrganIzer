using MyOrganizer.Wpf.Controls;

namespace MyOrganizer.Wpf.Dental;

/// <summary>
/// Lab-only clinical procedures. Independent of rendering and of production persistence.
/// </summary>
public enum DentalProcedureType
{
    None,
    Filling
}

/// <summary>
/// Per-surface clinical state for one tooth. Does not know meshes, materials, or input.
/// Palatal is stored as <see cref="ToothSurfaceType.Lingual"/>.
/// </summary>
public sealed class ToothLabClinicalState
{
    private readonly Dictionary<ToothSurfaceType, DentalProcedureType> _surfaces = new()
    {
        [ToothSurfaceType.Occlusal] = DentalProcedureType.None,
        [ToothSurfaceType.Buccal] = DentalProcedureType.None,
        [ToothSurfaceType.Lingual] = DentalProcedureType.None,
        [ToothSurfaceType.Mesial] = DentalProcedureType.None,
        [ToothSurfaceType.Distal] = DentalProcedureType.None
    };

    public ToothLabClinicalState(string toothNumber) => ToothNumber = toothNumber;

    public string ToothNumber { get; }

    public DentalProcedureType Get(ToothSurfaceType surface) =>
        _surfaces.TryGetValue(surface, out var value) ? value : DentalProcedureType.None;

    public bool Set(ToothSurfaceType surface, DentalProcedureType procedure)
    {
        if (!_surfaces.ContainsKey(surface))
            return false;
        if (_surfaces[surface] == procedure)
            return false;
        _surfaces[surface] = procedure;
        return true;
    }

    public IReadOnlyList<string> FillingSurfaceNames()
    {
        var names = new List<string>();
        foreach (var kv in _surfaces)
        {
            if (kv.Value != DentalProcedureType.Filling)
                continue;
            names.Add(kv.Key == ToothSurfaceType.Lingual ? "Palatal" : kv.Key.ToString());
        }
        names.Sort(StringComparer.Ordinal);
        return names;
    }
}
