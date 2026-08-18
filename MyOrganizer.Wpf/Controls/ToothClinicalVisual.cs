using System.Windows.Media;

namespace MyOrganizer.Wpf.Controls;

public enum ToothClinicalKind
{
    Healthy,
    Filling,
    Restoration,
    Crown,
    Implant,
    Endodontic,
    PartialDenture,
    FullDenture,
    Other
}

public sealed class ToothClinicalStyle
{
    public required ToothClinicalKind Kind { get; init; }
    public required string Code { get; init; }
    public required string LocKey { get; init; }
    public required Color Color { get; init; }
    public required bool WholeTooth { get; init; }
    public required Brush Fill { get; init; }
}

/// <summary>
/// Presentation styles for mapped clinical kinds. Colors follow the approved odontogram palette.
/// Classification is by catalog procedure ID via <see cref="ProcedureVisualMap"/>, not display names.
/// </summary>
public static class ToothClinicalVisual
{
    public static readonly ToothClinicalStyle Healthy = Make(ToothClinicalKind.Healthy, "", "Healthy", 0xF4, 0xE9, 0xD4, false);
    public static readonly ToothClinicalStyle Filling = Make(ToothClinicalKind.Filling, "PL", "ConditionFilling", 0xC5, 0xCB, 0xD1, false);
    public static readonly ToothClinicalStyle Restoration = Make(ToothClinicalKind.Restoration, "RS", "ConditionRestoration", 0xC5, 0xCB, 0xD1, false);
    public static readonly ToothClinicalStyle Crown = Make(ToothClinicalKind.Crown, "CR", "ConditionCrown", 0xD5, 0xDA, 0xE0, true);
    public static readonly ToothClinicalStyle Implant = Make(ToothClinicalKind.Implant, "IM", "ConditionImplant", 0x6B, 0x72, 0x80, true);
    public static readonly ToothClinicalStyle Endodontic = Make(ToothClinicalKind.Endodontic, "EN", "ConditionEndo", 0xC6, 0x28, 0x28, true);
    public static readonly ToothClinicalStyle PartialDenture = Make(ToothClinicalKind.PartialDenture, "BY", "ConditionPartialDenture", 0xD5, 0xDA, 0xE0, true);
    public static readonly ToothClinicalStyle FullDenture = Make(ToothClinicalKind.FullDenture, "PR", "ConditionFullDenture", 0xD5, 0xDA, 0xE0, true);
    public static readonly ToothClinicalStyle Other = Make(ToothClinicalKind.Other, "", "ConditionOther", 0x5B, 0x65, 0x6B, true);

    public static IReadOnlyList<ToothClinicalStyle> Legend { get; } =
        [Filling, Crown, Implant, Endodontic];

    public static ToothClinicalStyle ForKind(ToothClinicalKind kind) => kind switch
    {
        ToothClinicalKind.Filling => Filling,
        ToothClinicalKind.Restoration => Restoration,
        ToothClinicalKind.Crown or ToothClinicalKind.PartialDenture or ToothClinicalKind.FullDenture => Crown,
        ToothClinicalKind.Implant => Implant,
        ToothClinicalKind.Endodontic => Endodontic,
        ToothClinicalKind.Other => Other,
        _ => Healthy
    };

    public static ToothClinicalStyle Classify(string procedure, IReadOnlyDictionary<string, int>? nameToId = null) =>
        ForKind(ProcedureVisualMap.Resolve(procedure, nameToId));

    public static bool PaintsChart(ToothClinicalKind kind) =>
        kind is not ToothClinicalKind.Healthy and not ToothClinicalKind.Other;

    public static Brush BrushFor(ToothClinicalKind kind) => ForKind(kind).Fill;

    public static string CodeFor(ToothClinicalKind kind)
    {
        var style = ForKind(kind);
        return style.Kind is ToothClinicalKind.Healthy or ToothClinicalKind.Other ? "" : style.Code;
    }

    private static ToothClinicalStyle Make(ToothClinicalKind kind, string code, string loc, byte r, byte g, byte b, bool whole)
    {
        var fill = new SolidColorBrush(Color.FromRgb(r, g, b));
        fill.Freeze();
        return new ToothClinicalStyle
        {
            Kind = kind,
            Code = code,
            LocKey = loc,
            Color = Color.FromRgb(r, g, b),
            WholeTooth = whole,
            Fill = fill
        };
    }
}
