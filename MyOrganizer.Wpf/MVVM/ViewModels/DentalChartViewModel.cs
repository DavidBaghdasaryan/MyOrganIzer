using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Dental;
using MyOrganizer.Wpf.Entities;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.Navigation;
using MyOrganizer.Wpf.Repository;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class DentalChartViewModel : ObservableObject
{
    private readonly IToothWorkRepository _repo;
    private readonly AppDbContext _db;
    private readonly IDialogService _dialogs;
    private readonly Dictionary<string, int[]> _priceTable = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _procedureIdByName = new(StringComparer.Ordinal);
    private List<string> _procedures = [];
    private bool _isBusy;
    private string? _error;
    private string _selectedTooth = "";
    private string _selectedSurfaces = "";
    private bool _hasSelection;
    private string? _statusMessage;
    private string _selectedVisualType = "";
    private string? _selectedProcedureName;
    private ProcedureContextViewModel _currentProcedureContext = new ToothSummaryContextViewModel();
    private DispatcherTimer? _statusTimer;

    public DentalChartViewModel(IToothWorkRepository repo, AppDbContext db, IDialogService dialogs)
    {
        _repo = repo;
        _db = db;
        _dialogs = dialogs;
        RetryCommand = new AsyncRelayCommand(() => InitializeAsync(ClientId));
        LegendItems = ToothClinicalVisual.Legend
            .Select(style => new ToothLegendItem(style.Fill, style.LocKey))
            .ToList();
        ClearSelectionStatus();
    }

    public int ClientId { get; private set; }
    public IReadOnlyList<string> Procedures => _procedures;
    public IReadOnlyDictionary<string, List<ToothMark>> Marks { get; private set; } =
        new Dictionary<string, List<ToothMark>>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, ToothCurrentState> CurrentStates { get; private set; } =
        new Dictionary<string, ToothCurrentState>(StringComparer.Ordinal);

    public ObservableCollection<ToothConditionItem> Conditions { get; } = [];
    public ObservableCollection<ToothCurrentStateLine> CurrentStateLines { get; } = [];
    public IReadOnlyList<ToothLegendItem> LegendItems { get; }

    public ICommand RetryCommand { get; }

    public string SelectedVisualType
    {
        get => _selectedVisualType;
        private set => SetProperty(ref _selectedVisualType, value);
    }

    public bool HasConditions => Conditions.Count > 0;
    public bool HasCurrentState => CurrentStateLines.Count > 0;

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string? Error
    {
        get => _error;
        private set
        {
            if (SetProperty(ref _error, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(Error);

    public string SelectedTooth
    {
        get => _selectedTooth;
        private set => SetProperty(ref _selectedTooth, value);
    }

    public string SelectedSurfaces
    {
        get => _selectedSurfaces;
        private set => SetProperty(ref _selectedSurfaces, value);
    }

    public bool HasSelection
    {
        get => _hasSelection;
        private set => SetProperty(ref _hasSelection, value);
    }

    public bool HasSurfaceSelection { get; private set; }

    public IReadOnlyList<ToothSurfaceType> InspectorSurfaces { get; private set; } = [];

    public string? SelectedProcedureName
    {
        get => _selectedProcedureName;
        set
        {
            if (!SetProperty(ref _selectedProcedureName, value))
                return;
            RebuildProcedureContext();
        }
    }

    public ProcedureContextViewModel CurrentProcedureContext
    {
        get => _currentProcedureContext;
        private set
        {
            if (!SetProperty(ref _currentProcedureContext, value))
                return;
            OnPropertyChanged(nameof(HasInlineApplyContext));
        }
    }

    public bool HasInlineApplyContext => CurrentProcedureContext is ProcedureApplyContextViewModel;

    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
                OnPropertyChanged(nameof(HasStatus));
        }
    }

    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    public async Task InitializeAsync(int clientId)
    {
        ClientId = clientId;
        IsBusy = true;
        Error = null;
        try
        {
            await LoadProceduresAsync();
            await LoadPriceTableAsync();
            await ReloadMarksAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public int[] PricesFor(string procedure) =>
        _priceTable.TryGetValue(procedure, out var prices) ? prices : [0, 0, 0];

    public void UpdateSelection(string toothNumber, IReadOnlyList<ToothSurfaceType> surfaces, bool wholeTooth)
    {
        var toothChanged = !string.Equals(SelectedTooth, toothNumber, StringComparison.Ordinal);
        HasSelection = true;
        HasSurfaceSelection = !wholeTooth && surfaces.Count > 0;
        OnPropertyChanged(nameof(HasSurfaceSelection));
        SelectedTooth = toothNumber;
        SelectedVisualType = ToothFdi.VisualLocKey(toothNumber).T();
        InspectorSurfaces = wholeTooth || surfaces.Count == 0 ? [] : surfaces.ToList();
        RebuildConditions(toothNumber);
        SelectedSurfaces = InspectorSurfaces.Count == 0
            ? "WholeTooth".T()
            : string.Join("  ·  ",
                InspectorSurfaces.Select(s => ToothControl.SurfaceDisplayName(s, toothNumber).T()));

        if (toothChanged)
            RebuildProcedureContext();
        else if (CurrentProcedureContext is SurfaceProcedureContextViewModel surface)
            surface.NotifySurfacesDisplay(SelectedSurfaces);
    }

    public void ClearSelectionStatus()
    {
        HasSelection = false;
        HasSurfaceSelection = false;
        OnPropertyChanged(nameof(HasSurfaceSelection));
        SelectedTooth = "";
        SelectedVisualType = "";
        SelectedSurfaces = "";
        InspectorSurfaces = [];
        _selectedProcedureName = null;
        OnPropertyChanged(nameof(SelectedProcedureName));
        CurrentProcedureContext = new ToothSummaryContextViewModel();
        Conditions.Clear();
        CurrentStateLines.Clear();
        OnPropertyChanged(nameof(HasConditions));
        OnPropertyChanged(nameof(HasCurrentState));
    }

    public async Task<bool> OpenApplyDialogAsync(
        string toothNumber,
        IReadOnlyList<ToothSurfaceType> surfaces,
        bool wholeTooth)
    {
        await LoadProceduresAsync();
        await LoadPriceTableAsync();

        var dialog = new ApplyProcedureDialogViewModel(
            toothNumber,
            surfaces,
            wholeTooth,
            Procedures,
            PricesFor,
            (procedure, tier, price, selected) =>
                ApplyProcedureAsync(toothNumber, procedure, tier, price, selected));

        var applied = await _dialogs.ShowAsync(dialog) == true;
        if (applied)
            ShowStatus("ProcedureApplied".T());
        return applied;
    }

    private void ShowStatus(string message)
    {
        StatusMessage = message;
        _statusTimer?.Stop();
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
        _statusTimer.Tick += (_, _) =>
        {
            _statusTimer.Stop();
            StatusMessage = null;
        };
        _statusTimer.Start();
    }

    private void RebuildProcedureContext()
    {
        if (!HasSelection || string.IsNullOrEmpty(SelectedTooth))
        {
            CurrentProcedureContext = new ToothSummaryContextViewModel();
            return;
        }

        var scope = ProcedureScopeMap.Resolve(SelectedProcedureName, _procedureIdByName);
        InspectorSurfaces = [];
        HasSurfaceSelection = false;
        SelectedSurfaces = "WholeTooth".T();
        OnPropertyChanged(nameof(HasSurfaceSelection));

        var procedure = SelectedProcedureName ?? "";
        var prices = PricesFor(procedure);
        CurrentProcedureContext = scope switch
        {
            DentalProcedureScope.Surface => new SurfaceProcedureContextViewModel(
                SelectedTooth,
                procedure,
                prices,
                SelectedSurfaces,
                OnContextSurfacesChanged,
                tier => ApplyFromContextAsync(InspectorSurfaces, tier),
                CancelProcedureSelection),
            DentalProcedureScope.Endodontic => new EndodonticProcedureContextViewModel(
                SelectedTooth,
                procedure,
                prices,
                tier => ApplyFromContextAsync([], tier),
                CancelProcedureSelection),
            DentalProcedureScope.WholeTooth => new WholeToothProcedureContextViewModel(
                SelectedTooth,
                procedure,
                prices,
                tier => ApplyFromContextAsync([], tier),
                CancelProcedureSelection),
            _ => new ToothSummaryContextViewModel(
                string.IsNullOrWhiteSpace(SelectedProcedureName) ? null : SelectedProcedureName)
        };
    }

    private void OnContextSurfacesChanged(IReadOnlyList<ToothSurfaceType> surfaces, bool wholeTooth)
    {
        if (string.IsNullOrEmpty(SelectedTooth))
            return;
        UpdateSelection(SelectedTooth, surfaces, wholeTooth);
    }

    private void CancelProcedureSelection() => SelectedProcedureName = null;

    private async Task ApplyFromContextAsync(IReadOnlyList<ToothSurfaceType> surfaces, PriceTierOption tier)
    {
        if (string.IsNullOrWhiteSpace(SelectedProcedureName) || string.IsNullOrEmpty(SelectedTooth))
            return;
        await ApplyProcedureAsync(SelectedTooth, SelectedProcedureName, tier.Code, tier.Price, surfaces);
        ShowStatus("ProcedureApplied".T());
    }

    public async Task ApplyProcedureAsync(string toothNumber, string procedure, string tier, int price,
        IReadOnlyList<ToothSurfaceType> surfaces)
    {
        if (ClientId <= 0)
            return;

        if (surfaces.Count == 0)
        {
            await _repo.AddAsync(ClientId, toothNumber, procedure, tier, price);
        }
        else
        {
            foreach (var surface in surfaces)
                await _repo.AddAsync(ClientId, toothNumber, procedure, tier, price, surface.ToString());
        }

        await ReloadMarksAsync();
    }

    public async Task ClearSurfacesAsync(string toothNumber, IReadOnlyList<string> surfaces)
    {
        if (ClientId <= 0)
            return;
        await _repo.ClearSurfacesAsync(ClientId, toothNumber, surfaces);
        await ReloadMarksAsync();
    }

    public async Task ClearToothAsync(string toothNumber)
    {
        if (ClientId <= 0)
            return;
        await _repo.ClearToothAsync(ClientId, toothNumber);
        await ReloadMarksAsync();
    }

    public async Task ReloadMarksAsync()
    {
        if (ClientId <= 0)
        {
            Marks = new Dictionary<string, List<ToothMark>>(StringComparer.Ordinal);
            CurrentStates = new Dictionary<string, ToothCurrentState>(StringComparer.Ordinal);
            OnPropertyChanged(nameof(Marks));
            OnPropertyChanged(nameof(CurrentStates));
            return;
        }

        var works = await _repo.GetByClientAsync(ClientId);
        var map = new Dictionary<string, List<ToothMark>>(StringComparer.Ordinal);
        foreach (var group in works.GroupBy(w => w.ToothFdi))
            map[group.Key] = group.Select(ToMark).ToList();

        Marks = map;
        CurrentStates = ToothCurrentStateCalculator.FromHistory(works, _procedureIdByName);
        OnPropertyChanged(nameof(Marks));
        OnPropertyChanged(nameof(CurrentStates));
        if (HasSelection && !string.IsNullOrEmpty(SelectedTooth))
            RebuildConditions(SelectedTooth);
    }

    private void RebuildConditions(string toothNumber)
    {
        Conditions.Clear();
        if (Marks.TryGetValue(toothNumber, out var marks))
        {
            foreach (var mark in marks)
            {
                var style = ToothClinicalVisual.ForKind(mark.Kind);
                var kind = style.Kind is ToothClinicalKind.Other or ToothClinicalKind.Healthy
                    ? mark.Procedure
                    : style.LocKey.T();
                var where = mark.Surface is null
                    ? "WholeTooth".T()
                    : ToothControl.SurfaceDisplayName(mark.Surface.Value, toothNumber).T();
                Conditions.Add(new ToothConditionItem(kind, where, mark.Procedure, mark.Brush));
            }
        }
        OnPropertyChanged(nameof(HasConditions));
        RebuildCurrentStateLines(toothNumber);
    }

    private void RebuildCurrentStateLines(string toothNumber)
    {
        CurrentStateLines.Clear();
        var state = ToothCurrentStateCalculator.ForTooth(toothNumber, CurrentStates);
        foreach (var surface in Enum.GetValues<ToothSurfaceType>())
        {
            CurrentStateLines.Add(new ToothCurrentStateLine(
                ToothCurrentStateDisplay.SurfaceName(surface, toothNumber),
                ToothCurrentStateDisplay.SurfaceValue(state.Surface(surface))));
        }

        CurrentStateLines.Add(new ToothCurrentStateLine(
            "ConditionEndo".T(),
            ToothCurrentStateDisplay.EndodonticValue(state.Endodontic)));
        CurrentStateLines.Add(new ToothCurrentStateLine(
            "WholeTooth".T(),
            ToothCurrentStateDisplay.WholeToothValue(state.WholeTooth)));
        OnPropertyChanged(nameof(HasCurrentState));
    }

    private ToothMark ToMark(ToothWork work)
    {
        var kind = ProcedureVisualMap.Resolve(work.ProcedureName, _procedureIdByName);
        var style = ToothClinicalVisual.ForKind(kind);
        return new ToothMark
        {
            Surface = Enum.TryParse<ToothSurfaceType>(work.Surface, ignoreCase: true, out var surface) ? surface : null,
            Procedure = work.ProcedureName,
            Kind = kind,
            Code = ToothClinicalVisual.CodeFor(kind),
            Brush = style.Fill
        };
    }

    private async Task LoadProceduresAsync()
    {
        var rows = await _db.Procedures
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Id)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync();

        _procedureIdByName.Clear();
        foreach (var row in rows)
            _procedureIdByName[row.Name] = row.Id;

        var names = rows.Select(r => r.Name).ToList();
        _procedures = names.Count > 0 ? names : [.. FallbackProcedures];
        OnPropertyChanged(nameof(Procedures));
    }

    private async Task LoadPriceTableAsync()
    {
        var latest = await _db.LoadLatestPricesAsync();
        var procNames = await _db.Procedures.AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync();
        var nameById = procNames.ToDictionary(x => x.Id, x => x.Name);

        _priceTable.Clear();
        foreach (var row in latest)
        {
            if (!nameById.TryGetValue(row.ProcedureId, out var name))
                continue;
            _priceTable[name] =
            [
                (int)Math.Round(row.Tier1, MidpointRounding.AwayFromZero),
                (int)Math.Round(row.Tier2, MidpointRounding.AwayFromZero),
                (int)Math.Round(row.Tier3, MidpointRounding.AwayFromZero),
            ];
        }
    }

    private static readonly string[] FallbackProcedures =
    [
        "Removable Partial Denture (Metal Framework)",
        "Full Denture",
        "Implant with Zirconia Crown",
        "Implant with Metal-Ceramic Crown",
        "Zirconia or E-max Crown",
        "Metal-Ceramic Crown",
        "Composite or Inlay Restoration",
        "Filling (Composite / Amalgam)",
        "Work Shift / Appointment Slot",
        "Endodontic Treatment (Root Canal)"
    ];
}

public sealed class ToothConditionItem
{
    public ToothConditionItem(string kind, string location, string procedure, Brush marker)
    {
        Kind = kind;
        Location = location;
        Procedure = procedure;
        Marker = marker;
    }

    public string Kind { get; }
    public string Location { get; }
    public string Procedure { get; }
    public Brush Marker { get; }
}

public sealed class ToothCurrentStateLine
{
    public ToothCurrentStateLine(string label, string value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }
    public string Value { get; }
}

public sealed class ToothLegendItem
{
    private readonly string _locKey;

    public ToothLegendItem(Brush marker, string locKey)
    {
        Marker = marker;
        _locKey = locKey;
    }

    public Brush Marker { get; }
    public string Name => _locKey.T();
}
