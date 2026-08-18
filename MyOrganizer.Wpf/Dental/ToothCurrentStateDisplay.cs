using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.Extensions;

namespace MyOrganizer.Wpf.Dental;

public static class ToothCurrentStateDisplay
{
    public static string SurfaceLocKey(SurfaceClinicalState state) => state switch
    {
        SurfaceClinicalState.Filling => "ConditionFilling",
        SurfaceClinicalState.Restoration => "ConditionRestoration",
        SurfaceClinicalState.SurfaceCaries => "SurfaceCaries",
        SurfaceClinicalState.MediumCaries => "MediumCaries",
        SurfaceClinicalState.DeepCaries => "DeepCaries",
        _ => "Healthy"
    };

    public static string EndodonticLocKey(EndodonticClinicalState state) =>
        state is EndodonticClinicalState.Treated ? "ConditionEndo" : "EndodonticNone";

    public static string WholeToothLocKey(WholeToothClinicalState state) => state switch
    {
        WholeToothClinicalState.Crown => "ConditionCrown",
        WholeToothClinicalState.Implant => "ConditionImplant",
        WholeToothClinicalState.Missing => "ConditionMissing",
        _ => "WholeToothNormal"
    };

    public static string SurfaceName(ToothSurfaceType surface, string toothFdi) =>
        ToothControl.SurfaceDisplayName(surface, toothFdi).T();

    public static string SurfaceValue(SurfaceClinicalState state) => SurfaceLocKey(state).T();

    public static string EndodonticValue(EndodonticClinicalState state) => EndodonticLocKey(state).T();

    public static string WholeToothValue(WholeToothClinicalState state) => WholeToothLocKey(state).T();
}
