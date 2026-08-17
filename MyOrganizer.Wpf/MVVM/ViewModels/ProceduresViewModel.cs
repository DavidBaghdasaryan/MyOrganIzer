using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using MyOrganizer.Wpf.Entities.Procedures;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.Navigation;
using MyOrganizer.Wpf.Services;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed record ProcedureRow
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required decimal Tier1 { get; init; }
    public required decimal Tier2 { get; init; }
    public required decimal Tier3 { get; init; }
    public required string Currency { get; init; }
    public required string PriceDisplay { get; init; }
    public required string Tier1Display { get; init; }
    public required string Tier2Display { get; init; }
    public required string Tier3Display { get; init; }
}

public sealed class ProceduresViewModel : ObservableObject, INavigationAware
{
    private readonly IProcedureService _procedures;
    private readonly IDialogService _dialogs;
    private readonly DispatcherTimer _searchTimer;
    private readonly List<ProcedureRow> _all = [];
    private ProcedureRow? _selectedRow;
    private string _searchText = "";
    private bool _isBusy;
    private bool _navigated;

    public ProceduresViewModel(IProcedureService procedures, IDialogService dialogs)
    {
        _procedures = procedures;
        _dialogs = dialogs;
        _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _searchTimer.Tick += (_, _) =>
        {
            _searchTimer.Stop();
            ApplyFilter();
        };

        AddCommand = new AsyncRelayCommand(AddAsync);
        EditCommand = new AsyncRelayCommand(p => EditAsync(Row(p)));
        DeleteCommand = new AsyncRelayCommand(p => DeleteAsync(Row(p)));
        ClearSearchCommand = new RelayCommand(() => SearchText = "");

        _ = LoadAsync(showBusy: true);
    }

    public ObservableCollection<ProcedureRow> Items { get; } = [];

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ClearSearchCommand { get; }

    public ProcedureRow? SelectedRow
    {
        get => _selectedRow;
        set => SetProperty(ref _selectedRow, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
                return;
            _searchTimer.Stop();
            _searchTimer.Start();
            OnPropertyChanged(nameof(HasActiveFilter));
        }
    }

    public bool HasActiveFilter => !string.IsNullOrWhiteSpace(SearchText);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                RaiseEmptyState();
        }
    }

    public bool ShowGrid => !IsBusy && Items.Count > 0;
    public bool ShowEmptyDatabase => !IsBusy && _all.Count == 0;
    public bool ShowEmptyFilter => !IsBusy && _all.Count > 0 && Items.Count == 0;

    public void Refresh()
    {
        for (var i = 0; i < _all.Count; i++)
            _all[i] = WithDisplay(_all[i]);
        ApplyFilter();
    }

    public void OnNavigatedTo()
    {
        if (!_navigated)
        {
            _navigated = true;
            return;
        }

        _ = LoadAsync();
    }

    private ProcedureRow? Row(object? parameter) => parameter as ProcedureRow ?? SelectedRow;

    private async Task LoadAsync(bool showBusy = false)
    {
        if (showBusy)
            IsBusy = true;

        try
        {
            var entities = await _procedures.GetAllWithPricesAsync();
            _all.Clear();
            foreach (var procedure in entities)
                _all.Add(ToRow(procedure));
            ApplyFilter();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilter()
    {
        var query = SearchText.Trim();
        var selectedId = SelectedRow?.Id;
        Items.Clear();
        foreach (var row in _all)
        {
            if (query.Length == 0 || row.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                Items.Add(row);
        }

        SelectedRow = selectedId is int id
            ? Items.FirstOrDefault(r => r.Id == id)
            : Items.FirstOrDefault();
        RaiseEmptyState();
    }

    private async Task AddAsync()
    {
        var editor = new EditProcedureDialogViewModel();
        if (await _dialogs.ShowAsync(editor) != true || editor.Result is null)
            return;

        var created = await _procedures.AddAsync(editor.Result.Name);
        await _procedures.UpsertPricesAsync(
        [
            (created.Id, editor.Result.Tier1, editor.Result.Tier2, editor.Result.Tier3, editor.Result.Currency)
        ]);
        await LoadAsync();
        SelectedRow = Items.FirstOrDefault(r => r.Id == created.Id);
    }

    private async Task EditAsync(ProcedureRow? row)
    {
        if (row is null)
            return;

        var editor = new EditProcedureDialogViewModel(row);
        if (await _dialogs.ShowAsync(editor) != true || editor.Result is null)
            return;

        await _procedures.UpdateAsync(row.Id, editor.Result.Name);
        await _procedures.UpsertPricesAsync(
        [
            (row.Id, editor.Result.Tier1, editor.Result.Tier2, editor.Result.Tier3, editor.Result.Currency)
        ]);
        await LoadAsync();
        SelectedRow = Items.FirstOrDefault(r => r.Id == row.Id);
    }

    private async Task DeleteAsync(ProcedureRow? row)
    {
        if (row is null)
            return;

        var confirm = await _dialogs.ShowAsync(new ConfirmDialogViewModel(
            "DeleteProcedure".T(),
            string.Format(CultureInfo.CurrentCulture, "DeleteProcedureMessage".T(), row.Name),
            "Delete".T(),
            danger: true));
        if (confirm != true)
            return;

        await _procedures.DeleteAsync(row.Id);
        await LoadAsync();
    }

    private static ProcedureRow ToRow(Procedure procedure)
    {
        var price = procedure.Prices?.OrderByDescending(p => p.Id).FirstOrDefault();
        var t1 = price?.Tier1 ?? 0;
        var t2 = price?.Tier2 ?? 0;
        var t3 = price?.Tier3 ?? 0;
        return new ProcedureRow
        {
            Id = procedure.Id,
            Name = procedure.Name,
            Tier1 = t1,
            Tier2 = t2,
            Tier3 = t3,
            Currency = price?.Currency ?? "AMD",
            PriceDisplay = FormatPrices(t1, t2, t3),
            Tier1Display = FormatTier(t1),
            Tier2Display = FormatTier(t2),
            Tier3Display = FormatTier(t3)
        };
    }

    private static ProcedureRow WithDisplay(ProcedureRow row) =>
        row with
        {
            PriceDisplay = FormatPrices(row.Tier1, row.Tier2, row.Tier3),
            Tier1Display = FormatTier(row.Tier1),
            Tier2Display = FormatTier(row.Tier2),
            Tier3Display = FormatTier(row.Tier3)
        };

    private static string FormatTier(decimal value) =>
        string.Format(CultureInfo.CurrentCulture, "{0:N0} {1}", value, "Currency".T());

    private static string FormatPrices(decimal t1, decimal t2, decimal t3) =>
        string.Join("  ·  ", FormatTier(t1), FormatTier(t2), FormatTier(t3));

    private void RaiseEmptyState()
    {
        OnPropertyChanged(nameof(ShowGrid));
        OnPropertyChanged(nameof(ShowEmptyDatabase));
        OnPropertyChanged(nameof(ShowEmptyFilter));
        OnPropertyChanged(nameof(HasActiveFilter));
    }
}
