using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.Data;
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
    private List<string> _procedures = [];
    private bool _isBusy;
    private string? _error;
    private string _selectedTooth = "";
    private string _selectedSurfaces = "";
    private bool _hasSelection;
    private string? _statusMessage;
    private DispatcherTimer? _statusTimer;

    public DentalChartViewModel(IToothWorkRepository repo, AppDbContext db, IDialogService dialogs)
    {
        _repo = repo;
        _db = db;
        _dialogs = dialogs;
        RetryCommand = new AsyncRelayCommand(() => InitializeAsync(ClientId));
        ClearSelectionStatus();
    }

    public int ClientId { get; private set; }
    public IReadOnlyList<string> Procedures => _procedures;
    public IReadOnlyDictionary<string, List<ToothMark>> Marks { get; private set; } =
        new Dictionary<string, List<ToothMark>>(StringComparer.Ordinal);

    public ICommand RetryCommand { get; }

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
        HasSelection = true;
        HasSurfaceSelection = !wholeTooth && surfaces.Count > 0;
        OnPropertyChanged(nameof(HasSurfaceSelection));
        SelectedTooth = toothNumber;
        if (wholeTooth || surfaces.Count == 0)
        {
            SelectedSurfaces = "WholeTooth".T();
            return;
        }

        var kind = ToothFdi.Kind(toothNumber);
        SelectedSurfaces = string.Join("  ·  ",
            surfaces.Select(s => ToothControl.SurfaceDisplayName(s, kind).T()));
    }

    public void ClearSelectionStatus()
    {
        HasSelection = false;
        HasSurfaceSelection = false;
        OnPropertyChanged(nameof(HasSurfaceSelection));
        SelectedTooth = "";
        SelectedSurfaces = "";
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
            OnPropertyChanged(nameof(Marks));
            return;
        }

        var works = await _repo.GetByClientAsync(ClientId);
        var map = new Dictionary<string, List<ToothMark>>(StringComparer.Ordinal);
        foreach (var group in works.GroupBy(w => w.ToothFdi))
            map[group.Key] = group.Select(ToMark).ToList();

        Marks = map;
        OnPropertyChanged(nameof(Marks));
    }

    private ToothMark ToMark(ToothWork work) => new()
    {
        Surface = Enum.TryParse<ToothSurfaceType>(work.Surface, ignoreCase: true, out var surface) ? surface : null,
        Procedure = work.ProcedureName,
        Code = ProcShort.TryGetValue(work.ProcedureName, out var code)
            ? code
            : work.ProcedureName[..Math.Min(2, work.ProcedureName.Length)].ToUpperInvariant(),
        Brush = ProcBrush.TryGetValue(work.ProcedureName, out var brush) ? brush : Brushes.SlateGray
    };

    private async Task LoadProceduresAsync()
    {
        var names = await _db.Procedures
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Id)
            .Select(p => p.Name)
            .ToListAsync();
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

    private static readonly Dictionary<string, string> ProcShort = new(StringComparer.Ordinal)
    {
        ["Removable Partial Denture (Metal Framework)"] = "BY",
        ["Full Denture"] = "PR",
        ["Implant with Zirconia Crown"] = "IZ",
        ["Implant with Metal-Ceramic Crown"] = "IM",
        ["Zirconia or E-max Crown"] = "ZR",
        ["Metal-Ceramic Crown"] = "MK",
        ["Composite or Inlay Restoration"] = "RS",
        ["Filling (Composite / Amalgam)"] = "PL",
        ["Work Shift / Appointment Slot"] = "SH",
        ["Endodontic Treatment (Root Canal)"] = "EN"
    };

    private static readonly Dictionary<string, Brush> ProcBrush = new(StringComparer.Ordinal)
    {
        ["Removable Partial Denture (Metal Framework)"] = Brush(0x39, 0x8E, 0xB5),
        ["Full Denture"] = Brush(0x6A, 0x1B, 0x9A),
        ["Implant with Zirconia Crown"] = Brush(0x00, 0x8B, 0x8B),
        ["Implant with Metal-Ceramic Crown"] = Brush(0x00, 0x64, 0x95),
        ["Zirconia or E-max Crown"] = Brush(0x2E, 0x7D, 0x32),
        ["Metal-Ceramic Crown"] = Brush(0xF9, 0xA8, 0x25),
        ["Composite or Inlay Restoration"] = Brush(0xEF, 0x6C, 0x00),
        ["Filling (Composite / Amalgam)"] = Brush(0xD8, 0x3F, 0x31),
        ["Work Shift / Appointment Slot"] = Brush(0x45, 0x55, 0x57),
        ["Endodontic Treatment (Root Canal)"] = Brush(0x15, 0x75, 0x9A),
    };

    private static SolidColorBrush Brush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
