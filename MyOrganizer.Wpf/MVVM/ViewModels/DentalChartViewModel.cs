using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Dental;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.Navigation;
using MyOrganizer.Wpf.Repository;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed partial class DentalChartViewModel : ObservableObject
{
    private readonly IToothWorkRepository _repo;
    private readonly AppDbContext _db;
    private readonly Dictionary<string, int[]> _priceTable = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _procedureIdByName = new(StringComparer.Ordinal);
    private List<string> _procedures = [];
    private bool _isBusy;
    private string? _error;
    private string _selectedTooth = "";
    private bool _hasSelection;
    private string? _statusMessage;
    private string _selectedVisualType = "";
    private DispatcherTimer? _statusTimer;
    private readonly PatientTreatmentChart _treatment = new();

    public DentalChartViewModel(IToothWorkRepository repo, AppDbContext db, IDialogService dialogs)
    {
        _ = dialogs;
        _repo = repo;
        _db = db;
        InitClinicalEditor();
        RetryCommand = new AsyncRelayCommand(() => InitializeAsync(ClientId));
        SelectToothCommand = new RelayCommand(p => SelectTooth(p?.ToString() ?? ""));
        ChartRows =
        [
            new ChartJawRow("UPPER", ["18", "17", "16", "15", "14", "13", "12", "11"], ["21", "22", "23", "24", "25", "26", "27", "28"]),
            new ChartJawRow("LOWER", ["48", "47", "46", "45", "44", "43", "42", "41"], ["31", "32", "33", "34", "35", "36", "37", "38"])
        ];
        ClearSelectionStatus();
    }

    public int ClientId { get; private set; }
    public IReadOnlyList<string> Procedures => _procedures;

    public ICommand RetryCommand { get; }
    public ICommand SelectToothCommand { get; }
    public IReadOnlyList<ChartJawRow> ChartRows { get; }

    public string SelectedVisualType
    {
        get => _selectedVisualType;
        private set => SetProperty(ref _selectedVisualType, value);
    }

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

    public bool HasSelection
    {
        get => _hasSelection;
        private set => SetProperty(ref _hasSelection, value);
    }

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
            await ReloadClinicalAsync();
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
        _ = surfaces;
        _ = wholeTooth;
        var toothChanged = !string.Equals(SelectedTooth, toothNumber, StringComparison.Ordinal);
        HasSelection = true;
        SelectedTooth = toothNumber;
        SelectedVisualType = ToothFdi.VisualLocKey(toothNumber).T();
        if (toothChanged)
            HighlightSelectedTooth(toothNumber);
    }

    public void SelectTooth(string fdi)
    {
        if (string.IsNullOrWhiteSpace(fdi))
            return;
        UpdateSelection(fdi, [], wholeTooth: true);
        ActivateClinicalTooth(fdi);
    }

    public void ClearSelectionStatus()
    {
        HasSelection = false;
        SelectedTooth = "";
        SelectedVisualType = "";
        HighlightSelectedTooth("");
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

    public async Task ReloadClinicalAsync()
    {
        if (ClientId <= 0)
        {
            _treatment.ReloadFromWorks(0, [], _procedureIdByName);
            RefreshOdontogram();
            return;
        }

        var works = await _repo.GetByClientAsync(ClientId);
        _treatment.ReloadFromWorks(ClientId, works, _procedureIdByName);
        RefreshOdontogram();
        if (HasSelection && !string.IsNullOrEmpty(SelectedTooth))
        {
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
    }

    private void HighlightSelectedTooth(string fdi)
    {
        foreach (var row in ChartRows)
        {
            foreach (var slot in row.Right.Concat(row.Left))
                slot.SetSelected(slot.Fdi == fdi);
        }
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
