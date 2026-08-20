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

public sealed partial class DentalChartViewModel : ObservableObject
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
    private readonly PatientTreatmentChart _treatment = new();

    public DentalChartViewModel(IToothWorkRepository repo, AppDbContext db, IDialogService dialogs)
    {
        _repo = repo;
        _db = db;
        _dialogs = dialogs;
        InitClinicalEditor();
        RetryCommand = new AsyncRelayCommand(() => InitializeAsync(ClientId));
        SelectToothCommand = new RelayCommand(p => SelectTooth(p?.ToString() ?? ""));
        LegendItems = ToothClinicalVisual.Legend
            .Select(style => new ToothLegendItem(style.Fill, style.LocKey))
            .ToList();
        ChartRows =
        [
            new ChartJawRow("UPPER", ["18", "17", "16", "15", "14", "13", "12", "11"], ["21", "22", "23", "24", "25", "26", "27", "28"]),
            new ChartJawRow("LOWER", ["48", "47", "46", "45", "44", "43", "42", "41"], ["31", "32", "33", "34", "35", "36", "37", "38"])
        ];
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
    public ICommand SelectToothCommand { get; }
    public IReadOnlyList<ChartJawRow> ChartRows { get; }

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
            RebuildPriceTiers();
            await ReloadMarksAsync();
            // #region agent log
            Stage2Log("B", "chart-init",
                "{\"clientId\":" + ClientId +
                ",\"procedureCount\":" + _procedures.Count +
                ",\"sessionCount\":" + (_treatment.Current is null ? 0 : 1) +
                ",\"labPatients\":false}");
            Stage3Log("A", "chart-init",
                "{\"clientId\":" + ClientId +
                ",\"procedureCount\":" + _procedures.Count +
                ",\"labPatients\":false}");
            // #endregion
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
        {
            HighlightSelectedTooth(toothNumber);
            RebuildProcedureContext();
        }
        else if (CurrentProcedureContext is SurfaceProcedureContextViewModel surface)
            surface.NotifySurfacesDisplay(SelectedSurfaces);
    }

    public void SelectTooth(string fdi)
    {
        if (string.IsNullOrWhiteSpace(fdi))
            return;
        UpdateSelection(fdi, [], wholeTooth: true);
        ActivateClinicalTooth(fdi);
        // #region agent log
        var session = _treatment.Current;
        Stage2Log("D", "select-tooth",
            "{\"clientId\":" + ClientId +
            ",\"fdi\":\"" + fdi +
            "\",\"selected\":\"" + SelectedTooth +
            "\",\"hasSelection\":" + (HasSelection ? "true" : "false") + "}");
        Stage3Log("B", "select-tooth",
            "{\"clientId\":" + ClientId +
            ",\"fdi\":\"" + fdi +
            "\",\"sessionFdi\":\"" + (session?.Clinical.ToothNumber ?? "") +
            "\",\"procedureCount\":" + (session?.Clinical.Procedures.Count ?? 0) +
            ",\"pending\":" + (session?.Pending.Count ?? 0) +
            ",\"showDetailed\":" + (ShowDetailedViewer ? "true" : "false") +
            ",\"implant\":" + (IsImplantSelected ? "true" : "false") +
            ",\"labPatients\":false}");
        // #endregion
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
        HighlightSelectedTooth("");
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
            _treatment.ReloadFromWorks(0, [], _procedureIdByName);
            RefreshOdontogram();
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
        _treatment.ReloadFromWorks(ClientId, works, _procedureIdByName);
        RefreshOdontogram();
        if (HasSelection && !string.IsNullOrEmpty(SelectedTooth))
        {
            RebuildConditions(SelectedTooth);
            RestoreClinicalEditor();
            NotifyPresentation();
            ClinicalChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void RefreshOdontogram()
    {
        OdontogramThumbStore.Warm();
        foreach (var row in ChartRows)
        {
            foreach (var slot in row.Right.Concat(row.Left))
            {
                var state = _treatment.OdontogramFor(slot.Fdi);
                slot.ApplyPresentation(OdontogramThumbStore.Get(slot.Fdi), state);
                slot.SetSelected(slot.Fdi == SelectedTooth);
            }
        }
        // #region agent log
        Stage2Log("C", "odontogram-marks", IsolationJson());
        Stage3Log("C", "odontogram-from-sessions", _treatment.IsolationJson());
        // #endregion
    }

    private void HighlightSelectedTooth(string fdi)
    {
        foreach (var row in ChartRows)
        {
            foreach (var slot in row.Right.Concat(row.Left))
                slot.SetSelected(slot.Fdi == fdi);
        }
    }

    // #region agent log
    private string IsolationJson()
    {
        string Slot(string fdi)
        {
            var slot = ChartRows.SelectMany(row => row.Right.Concat(row.Left)).First(s => s.Fdi == fdi);
            var n = _treatment.SessionFor(fdi).Clinical.Procedures.Count;
            return "\"" + fdi + "\":{\"n\":" + n +
                   ",\"filling\":" + (slot.ShowFilling ? "true" : "false") +
                   ",\"implant\":" + (slot.ShowImplant ? "true" : "false") +
                   ",\"missing\":" + (slot.ShowMissing ? "true" : "false") +
                   ",\"endo\":" + (slot.ShowEndodontic ? "true" : "false") + "}";
        }

        var workCount = Marks.Values.Sum(list => list.Count);
        return "{\"clientId\":" + ClientId +
               ",\"workCount\":" + workCount +
               ",\"labPatients\":false" +
               ",\"fdi\":\"" + SelectedTooth + "\"," +
               Slot("16") + "," + Slot("24") + "," + Slot("34") + "," + Slot("46") + "}";
    }

    private static void Stage2Log(string hypothesisId, string message, string dataJson)
    {
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"stage2\",\"hypothesisId\":\"" + hypothesisId +
                   "\",\"location\":\"DentalChartViewModel.cs\",\"message\":\"" + message +
                   "\",\"data\":" + dataJson + ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { System.IO.File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { /* chart logging must not break the workflow */ }
    }

    private static void Stage3Log(string hypothesisId, string message, string dataJson)
    {
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"stage3\",\"hypothesisId\":\"" + hypothesisId +
                   "\",\"location\":\"DentalChartViewModel.cs\",\"message\":\"" + message +
                   "\",\"data\":" + dataJson + ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { System.IO.File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { /* chart logging must not break the workflow */ }
    }
    // #endregion

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
