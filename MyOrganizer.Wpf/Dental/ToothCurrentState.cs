using MyOrganizer.Wpf.Controls;

namespace MyOrganizer.Wpf.Dental;

public enum SurfaceClinicalState
{
    Healthy,
    SurfaceCaries,
    MediumCaries,
    DeepCaries,
    Filling,
    Restoration
}

public enum EndodonticClinicalState
{
    None,
    Treated
}

public enum WholeToothClinicalState
{
    Normal,
    Crown,
    Implant,
    Missing
}

/// <summary>
/// What the odontogram should show now. Distinct from treatment history
/// (<see cref="Entities.ToothWork"/> rows), which is never overwritten by Apply.
/// </summary>
public sealed class ToothCurrentState
{
    public required string ToothFdi { get; init; }
    public required IReadOnlyDictionary<ToothSurfaceType, SurfaceClinicalState> Surfaces { get; init; }
    public EndodonticClinicalState Endodontic { get; init; }
    public WholeToothClinicalState WholeTooth { get; init; }

    public SurfaceClinicalState Surface(ToothSurfaceType surface) =>
        Surfaces.TryGetValue(surface, out var state) ? state : SurfaceClinicalState.Healthy;

    public static ToothCurrentState Healthy(string toothFdi) => new()
    {
        ToothFdi = toothFdi,
        Surfaces = ToothCurrentStateCalculator.HealthySurfaces(),
        Endodontic = EndodonticClinicalState.None,
        WholeTooth = WholeToothClinicalState.Normal
    };
}
