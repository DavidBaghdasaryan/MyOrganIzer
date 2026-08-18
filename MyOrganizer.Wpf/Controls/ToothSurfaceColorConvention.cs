using System.Windows.Media;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Authoritative debug overlay colors from the approved FDI 16 segmentation.
/// Every tooth map reuses this mapping. Do not invent per-tooth palettes.
/// Index order matches <see cref="ClinicalSurface"/>.
/// </summary>
internal static class ToothSurfaceColorConvention
{
    public static readonly Color[] Overlay =
    [
        Color.FromArgb(0x7A, 0xE8, 0x5D, 0x4C),
        Color.FromArgb(0x7A, 0x3D, 0x7C, 0xFF),
        Color.FromArgb(0x7A, 0x2E, 0xBB, 0x6B),
        Color.FromArgb(0x7A, 0xF4, 0xD0, 0x3F),
        Color.FromArgb(0x7A, 0x9B, 0x59, 0xB6)
    ];
}
