namespace MyOrganizer.Wpf.Controls;

public enum ToothVisualType
{
    CentralIncisor,
    LateralIncisor,
    Canine,
    FirstPremolar,
    SecondPremolar,
    FirstMolar,
    SecondMolar,
    ThirdMolar
}

public static class ToothFdi
{
    public static ToothKind Kind(string fdi) => Visual(fdi) switch
    {
        ToothVisualType.CentralIncisor or ToothVisualType.LateralIncisor => ToothKind.Incisor,
        ToothVisualType.Canine => ToothKind.Canine,
        ToothVisualType.FirstPremolar or ToothVisualType.SecondPremolar => ToothKind.Premolar,
        _ => ToothKind.Molar
    };

    public static ToothVisualType Visual(string fdi)
    {
        if (!TryParse(fdi, out var n))
            return ToothVisualType.FirstMolar;

        return (n % 10) switch
        {
            1 => ToothVisualType.CentralIncisor,
            2 => ToothVisualType.LateralIncisor,
            3 => ToothVisualType.Canine,
            4 => ToothVisualType.FirstPremolar,
            5 => ToothVisualType.SecondPremolar,
            6 => ToothVisualType.FirstMolar,
            7 => ToothVisualType.SecondMolar,
            _ => ToothVisualType.ThirdMolar
        };
    }

    public static bool IsUpper(string fdi) =>
        TryParse(fdi, out var n) && n / 10 is 1 or 2;

    public static bool IsAnterior(string fdi) =>
        Kind(fdi) is ToothKind.Incisor or ToothKind.Canine;

    /// <summary>
    /// Chart is drawn facing the patient. Mesial is toward the midline.
    /// Quadrants 2 and 3 sit on the right side of the chart, so mesial is on the left.
    /// </summary>
    public static bool MesialOnLeft(string fdi) =>
        TryParse(fdi, out var n) && n / 10 is 2 or 3;

    public static double ColumnWeight(string fdi) => Visual(fdi) switch
    {
        ToothVisualType.ThirdMolar => 1.10,
        ToothVisualType.SecondMolar => 1.14,
        ToothVisualType.FirstMolar => 1.18,
        ToothVisualType.SecondPremolar => 1.02,
        ToothVisualType.FirstPremolar => 1.00,
        ToothVisualType.Canine => 0.96,
        ToothVisualType.LateralIncisor => 0.90,
        _ => 0.94
    };

    public static string VisualLocKey(string fdi) => Visual(fdi) switch
    {
        ToothVisualType.CentralIncisor => "CentralIncisor",
        ToothVisualType.LateralIncisor => "LateralIncisor",
        ToothVisualType.Canine => "Canine",
        ToothVisualType.FirstPremolar => "FirstPremolar",
        ToothVisualType.SecondPremolar => "SecondPremolar",
        ToothVisualType.FirstMolar => "FirstMolar",
        ToothVisualType.SecondMolar => "SecondMolar",
        _ => "ThirdMolar"
    };

    public static bool TryParse(string fdi, out int number) =>
        int.TryParse(fdi, out number) && number is >= 11 and <= 48;
}

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
