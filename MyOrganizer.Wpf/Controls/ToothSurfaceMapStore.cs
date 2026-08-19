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
            "11" => Fdi11SurfaceMapStore.TryLoad(crown),
            "12" => Fdi12SurfaceMapStore.TryLoad(crown),
            "21" => Fdi21SurfaceMapStore.TryLoad(crown),
            "22" => Fdi22SurfaceMapStore.TryLoad(crown),
            "31" => Fdi31SurfaceMapStore.TryLoad(crown),
            "41" => Fdi41SurfaceMapStore.TryLoad(crown),
            "32" => Fdi32SurfaceMapStore.TryLoad(crown),
            "42" => Fdi42SurfaceMapStore.TryLoad(crown),
            "17" => Fdi17SurfaceMapStore.TryLoad(crown),
            "27" => Fdi27SurfaceMapStore.TryLoad(crown),
            "37" => Fdi37SurfaceMapStore.TryLoad(crown),
            "47" => Fdi47SurfaceMapStore.TryLoad(crown),
            "18" => Fdi18SurfaceMapStore.TryLoad(crown),
            "28" => Fdi28SurfaceMapStore.TryLoad(crown),
            "38" => Fdi38SurfaceMapStore.TryLoad(crown),
            "48" => Fdi48SurfaceMapStore.TryLoad(crown),
            "13" => Fdi13SurfaceMapStore.TryLoad(crown),
            "23" => Fdi23SurfaceMapStore.TryLoad(crown),
            "33" => Fdi33SurfaceMapStore.TryLoad(crown),
            "43" => Fdi43SurfaceMapStore.TryLoad(crown),
            "14" => Fdi14SurfaceMapStore.TryLoad(crown),
            "15" => Fdi15SurfaceMapStore.TryLoad(crown),
            "25" => Fdi25SurfaceMapStore.TryLoad(crown),
            "24" => Fdi24SurfaceMapStore.TryLoad(crown),
            "34" => Fdi34SurfaceMapStore.TryLoad(crown),
            "44" => Fdi44SurfaceMapStore.TryLoad(crown),
            "35" => Fdi35SurfaceMapStore.TryLoad(crown),
            "45" => Fdi45SurfaceMapStore.TryLoad(crown),
            "16" => Fdi16SurfaceMapStore.TryLoad(crown),
            "26" => Fdi26SurfaceMapStore.TryLoad(crown),
            "36" => Fdi36SurfaceMapStore.TryLoad(crown),
            "46" => Fdi46SurfaceMapStore.TryLoad(crown),
            _ => null
        };
}
