using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Loads the frozen per-tooth anatomical map (triangle → clinical surface).
/// Overlay materials, hover, Filling, and orbit stay in the shared viewer;
/// only triangle ownership differs per tooth.
/// </summary>
internal static class ToothSurfaceMapStore
{
    public static ClinicalSurfaceMap? TryLoad(string fdi, MeshGeometry3D crown) =>
        fdi switch
        {
            "16" => Fdi16SurfaceMapStore.TryLoad(crown),
            "26" => Fdi26SurfaceMapStore.TryLoad(crown),
            "36" => Fdi36SurfaceMapStore.TryLoad(crown),
            "46" => Fdi46SurfaceMapStore.TryLoad(crown),
            _ => null
        };
}
