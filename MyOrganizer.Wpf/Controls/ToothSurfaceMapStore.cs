using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Loads the frozen per-tooth anatomical map. Overlay, hit-testing, and
/// future procedure wiring stay shared; only the triangle assignments differ.
/// </summary>
internal static class ToothSurfaceMapStore
{
    public static ClinicalSurfaceMap? TryLoad(string fdi, MeshGeometry3D crown) =>
        fdi switch
        {
            "16" => Fdi16SurfaceMapStore.TryLoad(crown),
            "36" => Fdi36SurfaceMapStore.TryLoad(crown),
            _ => null
        };
}
