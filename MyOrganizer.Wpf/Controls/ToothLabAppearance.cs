using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Tooth-lab enamel/cementum materials. FDI 16 keeps the approved look.
/// Mandibular first molars (36/46) share the closer crown/root/CEJ pair.
/// Overlay colors stay in ToothSurfaceColorConvention. Lighting stays shared.
/// </summary>
internal static class ToothLabAppearance
{
    public static void Apply(string fdi, GeometryModel3D crown, GeometryModel3D root, GeometryModel3D cervical)
    {
        if (fdi is "36" or "46" or "37" or "47" or "38" or "48")
            ApplyFdi36(crown, root, cervical);
        else
            ApplyApprovedFdi16(crown, root);
    }

    private static void ApplyApprovedFdi16(GeometryModel3D crown, GeometryModel3D root)
    {
        crown.Material = Enamel(
            Color.FromRgb(0xF8, 0xF6, 0xF1),
            Color.FromArgb(0x06, 0xF7, 0xF5, 0xF0),
            Color.FromArgb(0x18, 0xFF, 0xF8, 0xF2),
            88,
            Colors.White);
        crown.BackMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0xE8, 0xE4, 0xDE)));
        root.Material = Cementum(
            Color.FromRgb(0xE2, 0xD4, 0xB2),
            Color.FromArgb(0x08, 0xE8, 0xD8, 0xB8),
            Color.FromArgb(0x10, 0xE8, 0xD4, 0xB0),
            48,
            Colors.White);
        root.BackMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0xD0, 0xC4, 0xA8)));
    }

    private static void ApplyFdi36(GeometryModel3D crown, GeometryModel3D root, GeometryModel3D cervical)
    {
        crown.Material = Enamel(
            Color.FromRgb(0xF3, 0xEF, 0xE6),
            Color.FromArgb(0x08, 0xF2, 0xEE, 0xE4),
            Color.FromArgb(0x14, 0xF6, 0xF1, 0xE6),
            72,
            Color.FromRgb(0xFF, 0xFC, 0xF6));
        crown.BackMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0xE6, 0xE1, 0xD6)));
        cervical.Material = Enamel(
            Color.FromRgb(0xED, 0xE6, 0xD7),
            Color.FromArgb(0x09, 0xEE, 0xE6, 0xD6),
            Color.FromArgb(0x12, 0xF0, 0xE8, 0xD8),
            64,
            Color.FromRgb(0xFF, 0xFC, 0xF6));
        cervical.BackMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0xDE, 0xD6, 0xC8)));
        root.Material = Cementum(
            Color.FromRgb(0xE7, 0xDB, 0xC4),
            Color.FromArgb(0x0A, 0xE6, 0xDA, 0xC2),
            Color.FromArgb(0x12, 0xE4, 0xD6, 0xBC),
            56,
            Color.FromRgb(0xFF, 0xF8, 0xF0));
        root.BackMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0xD6, 0xCA, 0xB2)));
    }

    private static Material Enamel(Color diffuse, Color emissive, Color specular, double power, Color ambient)
    {
        var group = new MaterialGroup();
        group.Children.Add(new DiffuseMaterial(new SolidColorBrush(diffuse)) { AmbientColor = ambient });
        group.Children.Add(new EmissiveMaterial(new SolidColorBrush(emissive)));
        group.Children.Add(new SpecularMaterial(new SolidColorBrush(specular), power));
        group.Freeze();
        return group;
    }

    private static Material Cementum(Color diffuse, Color emissive, Color specular, double power, Color ambient) =>
        Enamel(diffuse, emissive, specular, power, ambient);

    public static string CrownDiffuseHex(string fdi) =>
        fdi is "36" or "46" or "37" or "47" or "38" or "48" ? "#F3EFE6" : "#F8F6F1";

    public static string RootDiffuseHex(string fdi) =>
        fdi is "36" or "46" or "37" or "47" or "38" or "48" ? "#E7DBC4" : "#E2D4B2";
}
