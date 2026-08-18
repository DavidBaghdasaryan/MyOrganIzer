using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.Entities;

namespace MyOrganizer.Wpf.Dental;

/// <summary>
/// Derives current odontogram state from chronological <see cref="ToothWork"/> history.
/// Apply appends history; this walk uses last-write-wins per channel (surface / endo / whole tooth).
/// </summary>
public static class ToothCurrentStateCalculator
{
    private static readonly ToothSurfaceType[] AllSurfaces = Enum.GetValues<ToothSurfaceType>();

    public static IReadOnlyDictionary<ToothSurfaceType, SurfaceClinicalState> HealthySurfaces()
    {
        var map = new Dictionary<ToothSurfaceType, SurfaceClinicalState>();
        foreach (var surface in AllSurfaces)
            map[surface] = SurfaceClinicalState.Healthy;
        return map;
    }

    public static IReadOnlyDictionary<string, ToothCurrentState> FromHistory(
        IEnumerable<ToothWork> works,
        IReadOnlyDictionary<string, int>? nameToId = null)
    {
        var builders = new Dictionary<string, Builder>(StringComparer.Ordinal);
        foreach (var work in works)
        {
            if (string.IsNullOrWhiteSpace(work.ToothFdi))
                continue;
            if (!builders.TryGetValue(work.ToothFdi, out var builder))
            {
                builder = new Builder(work.ToothFdi);
                builders[work.ToothFdi] = builder;
            }

            builder.Apply(work, nameToId);
        }

        return builders.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Build(),
            StringComparer.Ordinal);
    }

    public static ToothCurrentState ForTooth(
        string toothFdi,
        IReadOnlyDictionary<string, ToothCurrentState> states) =>
        states.TryGetValue(toothFdi, out var state) ? state : ToothCurrentState.Healthy(toothFdi);

    private sealed class Builder
    {
        private readonly string _fdi;
        private readonly Dictionary<ToothSurfaceType, SurfaceClinicalState> _surfaces = [];
        private EndodonticClinicalState _endo = EndodonticClinicalState.None;
        private WholeToothClinicalState _whole = WholeToothClinicalState.Normal;

        public Builder(string fdi)
        {
            _fdi = fdi;
            foreach (var surface in AllSurfaces)
                _surfaces[surface] = SurfaceClinicalState.Healthy;
        }

        public void Apply(ToothWork work, IReadOnlyDictionary<string, int>? nameToId)
        {
            var kind = ProcedureVisualMap.Resolve(work.ProcedureName, nameToId);
            var scope = ProcedureScopeMap.ForKind(kind);
            if (scope is null)
                return;

            switch (scope)
            {
                case DentalProcedureScope.Surface:
                    ApplySurface(kind, work.Surface);
                    break;
                case DentalProcedureScope.Endodontic:
                    _endo = EndodonticClinicalState.Treated;
                    break;
                case DentalProcedureScope.WholeTooth:
                    ApplyWhole(kind);
                    break;
            }
        }

        private void ApplySurface(ToothClinicalKind kind, string surfaceName)
        {
            var surfaceState = kind switch
            {
                ToothClinicalKind.Restoration => SurfaceClinicalState.Restoration,
                ToothClinicalKind.Filling => SurfaceClinicalState.Filling,
                _ => (SurfaceClinicalState?)null
            };
            if (surfaceState is null)
                return;

            if (Enum.TryParse<ToothSurfaceType>(surfaceName, ignoreCase: true, out var surface))
            {
                _surfaces[surface] = surfaceState.Value;
                return;
            }

            foreach (var all in AllSurfaces)
                _surfaces[all] = surfaceState.Value;
        }

        private void ApplyWhole(ToothClinicalKind kind)
        {
            _whole = kind switch
            {
                ToothClinicalKind.Implant => WholeToothClinicalState.Implant,
                ToothClinicalKind.Crown or ToothClinicalKind.PartialDenture or ToothClinicalKind.FullDenture
                    => WholeToothClinicalState.Crown,
                _ => _whole
            };

            if (_whole is WholeToothClinicalState.Implant)
                _endo = EndodonticClinicalState.None;
        }

        public ToothCurrentState Build() => new()
        {
            ToothFdi = _fdi,
            Surfaces = new Dictionary<ToothSurfaceType, SurfaceClinicalState>(_surfaces),
            Endodontic = _endo,
            WholeTooth = _whole
        };
    }
}
