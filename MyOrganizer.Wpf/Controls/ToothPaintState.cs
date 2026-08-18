namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Current paint from chronological ToothWork. History remains in the marks list;
/// this is only what the odontogram draws.
/// </summary>
internal readonly record struct ToothPaintState(
    bool Implant,
    bool Endodontic,
    bool Crown,
    IReadOnlyDictionary<ToothSurfaceType, ToothClinicalKind> Surfaces)
{
    public static ToothPaintState FromMarks(IReadOnlyList<ToothMark> marks)
    {
        var implant = false;
        var endodontic = false;
        var crown = false;
        var surfaces = new Dictionary<ToothSurfaceType, ToothClinicalKind>();

        foreach (var mark in marks)
        {
            switch (mark.Kind)
            {
                case ToothClinicalKind.Implant:
                    implant = true;
                    break;
                case ToothClinicalKind.Endodontic:
                    endodontic = true;
                    break;
                case ToothClinicalKind.Crown:
                    crown = true;
                    break;
            }

            if (!ProcedureVisualMap.IsSurfaceState(mark.Kind))
                continue;

            if (mark.Surface is { } surface)
            {
                surfaces[surface] = mark.Kind;
                continue;
            }

            foreach (var all in Enum.GetValues<ToothSurfaceType>())
                surfaces[all] = mark.Kind;
        }

        if (implant)
            endodontic = false;

        return new ToothPaintState(implant, endodontic, crown, surfaces);
    }
}
