namespace MyOrganizer.Wpf.Controls;

public enum ToothSurfaceType
{
    Mesial,
    Distal,
    Buccal,
    Lingual,
    Occlusal
}

public enum ToothKind
{
    Incisor,
    Canine,
    Premolar,
    Molar
}

public static class ToothFdi
{
    public static ToothKind Kind(string fdi)
    {
        if (!TryParse(fdi, out var n))
            return ToothKind.Molar;

        return (n % 10) switch
        {
            1 or 2 => ToothKind.Incisor,
            3 => ToothKind.Canine,
            4 or 5 => ToothKind.Premolar,
            _ => ToothKind.Molar
        };
    }

    public static bool IsUpper(string fdi) =>
        TryParse(fdi, out var n) && n / 10 is 1 or 2;

    /// <summary>
    /// Chart is drawn facing the patient. Mesial is toward the midline.
    /// Quadrants 2 and 3 sit on the right side of the chart, so mesial is on the left.
    /// </summary>
    public static bool MesialOnLeft(string fdi) =>
        TryParse(fdi, out var n) && n / 10 is 2 or 3;

    public static bool TryParse(string fdi, out int number) =>
        int.TryParse(fdi, out number) && number is >= 11 and <= 48;
}
