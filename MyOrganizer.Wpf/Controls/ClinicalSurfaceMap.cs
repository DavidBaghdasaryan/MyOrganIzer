using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Clinical crown surfaces in tooth-local FDI 16 space.
/// Roots are never included. Triangle indices refer to the approved crown mesh.
/// </summary>
internal enum ClinicalSurface
{
    Occlusal = 0,
    Buccal = 1,
    Palatal = 2,
    Mesial = 3,
    Distal = 4
}

/// <summary>
/// Derived surface map. The approved crown MeshGeometry3D remains the source of truth.
/// Pipeline: automatic classification → topology cleanup → <see cref="Overrides"/> → final map.
/// Counts always reflect SurfaceOf (automatic label plus any override).
/// </summary>
internal sealed class ClinicalSurfaceMap
{
    public required MeshGeometry3D SourceCrown { get; init; }
    public required ClinicalSurface[] TriangleSurface { get; init; }
    public Dictionary<int, ClinicalSurface> Overrides { get; } = new();
    public Vector3D OcclusalDirection { get; init; }
    public int[] Counts { get; init; } = new int[5];

    public ClinicalSurface SurfaceOf(int triangle) =>
        Overrides.TryGetValue(triangle, out var over) ? over : TriangleSurface[triangle];

    public List<int> Triangles(ClinicalSurface surface)
    {
        var list = new List<int>();
        for (var i = 0; i < TriangleSurface.Length; i++)
        {
            if (SurfaceOf(i) == surface)
                list.Add(i);
        }
        return list;
    }

    /// <summary>
    /// Triangle-index groups for a future deterministic FDI16SurfaceMap asset.
    /// </summary>
    public Dictionary<ClinicalSurface, int[]> ToIndexAsset()
    {
        var map = new Dictionary<ClinicalSurface, int[]>();
        for (var s = 0; s < 5; s++)
        {
            var surface = (ClinicalSurface)s;
            map[surface] = Triangles(surface).ToArray();
        }
        return map;
    }
}
