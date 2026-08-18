using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Media;
using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class ApplyProcedureDialogViewModel : DialogViewModel
{
    private readonly IReadOnlyList<string> _allProcedures;
    private readonly Func<string, int[]> _pricesFor;
    private readonly Func<string, string, int, IReadOnlyList<ToothSurfaceType>, Task> _apply;
    private string _search = "";
    private string? _selectedProcedure;
    private PriceTierOption? _selectedTier;
    private string? _error;
    private bool _isBusy;

    public ApplyProcedureDialogViewModel(
        string toothNumber,
        IReadOnlyList<ToothSurfaceType> surfaces,
        bool wholeTooth,
        IReadOnlyList<string> procedures,
        Func<string, int[]> pricesFor,
        Func<string, string, int, IReadOnlyList<ToothSurfaceType>, Task> apply)
    {
        ToothNumber = toothNumber;
        Surfaces = surfaces;
        IsWholeTooth = wholeTooth || surfaces.Count == 0;
        SurfacesDisplay = IsWholeTooth
            ? "WholeTooth".T()
            : string.Join("  ·  ", surfaces.Select(s =>
                ToothControl.SurfaceDisplayName(s, toothNumber).T()));
        _allProcedures = procedures;
        _pricesFor = pricesFor;
        _apply = apply;
        FilteredProcedures = new ObservableCollection<string>(_allProcedures);
        ApplyCommand = new AsyncRelayCommand(ApplyAsync, () => CanApply);
        CancelCommand = new RelayCommand(() => Close(false));
    }

    public string ToothNumber { get; }
    public IReadOnlyList<ToothSurfaceType> Surfaces { get; }
    public bool IsWholeTooth { get; }
    public string SurfacesDisplay { get; }
    public ObservableCollection<string> FilteredProcedures { get; }
    public ObservableCollection<PriceTierOption> Tiers { get; } = [];

    public ICommand ApplyCommand { get; }
    public ICommand CancelCommand { get; }

    public string SearchText
    {
        get => _search;
        set
        {
            if (!SetProperty(ref _search, value))
                return;
            ApplyFilter();
        }
    }

    public string? SelectedProcedure
    {
        get => _selectedProcedure;
        set
        {
            if (!SetProperty(ref _selectedProcedure, value))
                return;
            RebuildTiers();
            RaiseApplyCanExecute();
        }
    }

    public PriceTierOption? SelectedTier
    {
        get => _selectedTier;
        set
        {
            if (!SetProperty(ref _selectedTier, value))
                return;
            RaiseApplyCanExecute();
        }
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

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                RaiseApplyCanExecute();
        }
    }

    public bool CanApply =>
        !IsBusy && !string.IsNullOrWhiteSpace(SelectedProcedure) && SelectedTier is not null;

    private void ApplyFilter()
    {
        var query = SearchText.Trim();
        FilteredProcedures.Clear();
        foreach (var name in _allProcedures)
        {
            if (query.Length == 0 || name.Contains(query, StringComparison.OrdinalIgnoreCase))
                FilteredProcedures.Add(name);
        }

        if (SelectedProcedure is not null && !FilteredProcedures.Contains(SelectedProcedure))
            SelectedProcedure = null;
    }

    private void RebuildTiers()
    {
        Tiers.Clear();
        SelectedTier = null;
        if (string.IsNullOrWhiteSpace(SelectedProcedure))
            return;

        foreach (var tier in PriceTierOption.FromPrices(_pricesFor(SelectedProcedure)))
            Tiers.Add(tier);
        SelectedTier = Tiers[0];
        OnPropertyChanged(nameof(Tiers));
    }

    private async Task ApplyAsync()
    {
        if (!CanApply || SelectedProcedure is null || SelectedTier is null)
            return;

        IsBusy = true;
        Error = null;
        try
        {
            await _apply(SelectedProcedure, SelectedTier.Code, SelectedTier.Price, Surfaces);
            Close(true);
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

    private void RaiseApplyCanExecute() =>
        ((AsyncRelayCommand)ApplyCommand).RaiseCanExecuteChanged();
}

public sealed class PriceTierOption
{
    public required string Code { get; init; }
    public required int Price { get; init; }
    public required string Display { get; init; }
    public required Brush Marker { get; init; }

    public static IReadOnlyList<PriceTierOption> FromPrices(int[] prices)
    {
        var currency = "Currency".T();
        return
        [
            Create("A", prices.ElementAtOrDefault(0), currency, Color.FromRgb(0x22, 0xC5, 0x5E)),
            Create("B", prices.ElementAtOrDefault(1), currency, Color.FromRgb(0xD9, 0x77, 0x06)),
            Create("C", prices.ElementAtOrDefault(2), currency, Color.FromRgb(0xDC, 0x26, 0x26))
        ];
    }

    private static PriceTierOption Create(string code, int price, string currency, Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return new PriceTierOption
        {
            Code = code,
            Price = price,
            Display = $"{code}  —  {price.ToString("N0", CultureInfo.InvariantCulture)} {currency}",
            Marker = brush
        };
    }
}
