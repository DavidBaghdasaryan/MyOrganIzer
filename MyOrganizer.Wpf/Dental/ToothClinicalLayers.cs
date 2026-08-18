using System.Windows.Media;
using MyOrganizer.Wpf.Controls;

namespace MyOrganizer.Wpf.Dental;

/// <summary>
/// Maps <see cref="ToothCurrentState"/> onto odontogram overlay layers.
/// Does not read treatment history.
/// </summary>
internal static class ToothClinicalLayers
{
    public static bool ShowMissing(ToothCurrentState state) =>
        state.WholeTooth is WholeToothClinicalState.Missing;

    public static bool ShowImplant(ToothCurrentState state) =>
        state.WholeTooth is WholeToothClinicalState.Implant && !ShowMissing(state);

    public static bool ShowCrown(ToothCurrentState state) =>
        state.WholeTooth is WholeToothClinicalState.Crown && !ShowMissing(state);

    public static bool ShowEndodontic(ToothCurrentState state) =>
        state.Endodontic is EndodonticClinicalState.Treated
        && state.WholeTooth is not WholeToothClinicalState.Implant
        && !ShowMissing(state);

    public static bool ShowSurfaceTreatments(ToothCurrentState state) =>
        !ShowMissing(state) && state.WholeTooth is WholeToothClinicalState.Normal;

    public static bool ShowFilling(ToothCurrentState state, ToothSurfaceType surface) =>
        ShowSurfaceTreatments(state)
        && state.Surface(surface) is SurfaceClinicalState.Filling or SurfaceClinicalState.Restoration;

    public static bool ShowCaries(ToothCurrentState state, ToothSurfaceType surface) =>
        ShowSurfaceTreatments(state) && CariesLevel(state.Surface(surface)) is not null;

    public static double? CariesLevel(SurfaceClinicalState surface) => surface switch
    {
        SurfaceClinicalState.SurfaceCaries => 0.34,
        SurfaceClinicalState.MediumCaries => 0.52,
        SurfaceClinicalState.DeepCaries => 0.72,
        _ => null
    };

    public static Brush CariesBrush(SurfaceClinicalState surface) => surface switch
    {
        SurfaceClinicalState.SurfaceCaries => ToothBrushes.CariesSurface,
        SurfaceClinicalState.MediumCaries => ToothBrushes.CariesMedium,
        SurfaceClinicalState.DeepCaries => ToothBrushes.CariesDeep,
        _ => ToothBrushes.CariesSurface
    };
}
