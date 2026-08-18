namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Explicit triangle corrections for the Dundee FDI 16 crown.
/// Applied after automatic classification and topology cleanup.
/// Keys are crown triangle indices in the approved runtime mesh.
/// Leave empty until a specific triangle is known to be clinically wrong.
/// </summary>
internal static class Fdi16ManualOverrides
{
    public static readonly IReadOnlyDictionary<int, ClinicalSurface> Triangles =
        new Dictionary<int, ClinicalSurface>();
}
