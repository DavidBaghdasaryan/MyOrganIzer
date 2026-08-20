using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using MyOrganizer.Wpf.Entities;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.Navigation;
using MyOrganizer.Wpf.Services;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed record CatalogItemRow
{
    public required int Id { get; init; }
    public required OfferingKind Kind { get; init; }
    public required string KindDisplay { get; init; }
    public required string Name { get; init; }
    public required string UnitDisplay { get; init; }
    public required string SuppliersDisplay { get; init; }
}

public sealed class CatalogItemsViewModel : ObservableObject, INavigationAware
{
    private readonly ICatalogService _catalog;
    private readonly IUnitOfMeasureService _units;
    private readonly ISupplierService _suppliers;
    private readonly IDialogService _dialogs;
    private readonly DispatcherTimer _searchTimer;
    private readonly List<CatalogItemRow> _all = [];
    private CatalogItemRow? _selectedRow;
    private string _searchText = "";
    private KindOption? _kindFilter;
    private bool _isBusy;
    private bool _navigated;

    public CatalogItemsViewModel(
        ICatalogService catalog,
        IUnitOfMeasureService units,
        ISupplierService suppliers,
        IDialogService dialogs)
    {
        _catalog = catalog;
        _units = units;
        _suppliers = suppliers;
        _dialogs = dialogs;
        KindFilters =
        [
            new KindOption(OfferingKind.Product, "AllKinds".T()) { IsAll = true },
            new KindOption(OfferingKind.Product, "Product".T()),
            new KindOption(OfferingKind.Service, "Service".T())
        ];
        _kindFilter = KindFilters[0];
        _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _searchTimer.Tick += (_, _) =>
        {
            _searchTimer.Stop();
            ApplyFilter();
        };

        AddCommand = new AsyncRelayCommand(AddAsync);
        EditCommand = new AsyncRelayCommand(p => EditAsync(Row(p)));
        DeleteCommand = new AsyncRelayCommand(p => DeleteAsync(Row(p)));
        UnitsCommand = new AsyncRelayCommand(OpenUnitsAsync);
        ClearSearchCommand = new RelayCommand(() => SearchText = "");

        _ = LoadAsync(showBusy: true);
    }

    public ObservableCollection<CatalogItemRow> Items { get; } = [];
    public IReadOnlyList<KindOption> KindFilters { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand UnitsCommand { get; }
    public ICommand ClearSearchCommand { get; }

    public CatalogItemRow? SelectedRow
    {
        get => _selectedRow;
        set => SetProperty(ref _selectedRow, value);
    }

    public KindOption? KindFilter
    {
        get => _kindFilter;
        set
        {
            if (!SetProperty(ref _kindFilter, value))
                return;
            ApplyFilter();
        }
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

    public bool HasActiveFilter => !string.IsNullOrWhiteSpace(SearchText) || KindFilter is { IsAll: false };
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

    public void Refresh() => ApplyFilter();

    public void OnNavigatedTo()
    {
        if (!_navigated)
        {
            _navigated = true;
            return;
        }

        _ = LoadAsync();
    }

    private CatalogItemRow? Row(object? parameter) => parameter as CatalogItemRow ?? SelectedRow;

    private async Task LoadAsync(bool showBusy = false)
    {
        if (showBusy)
            IsBusy = true;
        try
        {
            var entities = await _catalog.GetAllAsync();
            _all.Clear();
            foreach (var item in entities)
                _all.Add(ToRow(item));
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
        var kind = KindFilter is { IsAll: false } filter ? filter.Kind : (OfferingKind?)null;
        var selectedId = SelectedRow?.Id;
        Items.Clear();
        foreach (var row in _all)
        {
            if (kind is { } k && row.Kind != k)
                continue;
            if (query.Length > 0 && !row.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                continue;
            Items.Add(row);
        }

        SelectedRow = selectedId is int id
            ? Items.FirstOrDefault(r => r.Id == id)
            : Items.FirstOrDefault();
        RaiseEmptyState();
    }

    private async Task AddAsync()
    {
        var suppliers = await _suppliers.GetAllAsync();
        var units = await _units.GetAllAsync();
        var editor = new EditCatalogItemDialogViewModel(suppliers, units);
        if (await _dialogs.ShowAsync(editor) != true || editor.Result is null)
            return;
        await _catalog.SaveAsync(0, editor.Result.Name, editor.Result.Kind, editor.Result.UnitOfMeasureId, editor.Result.Notes, editor.Result.Suppliers);
        await LoadAsync();
    }

    private async Task EditAsync(CatalogItemRow? row)
    {
        if (row is null)
            return;
        var entity = await _catalog.GetByIdAsync(row.Id);
        if (entity is null)
            return;
        var suppliers = await _suppliers.GetAllAsync();
        var units = await _units.GetAllAsync();
        var editor = new EditCatalogItemDialogViewModel(suppliers, units, entity);
        if (await _dialogs.ShowAsync(editor) != true || editor.Result is null)
            return;
        await _catalog.SaveAsync(row.Id, editor.Result.Name, editor.Result.Kind, editor.Result.UnitOfMeasureId, editor.Result.Notes, editor.Result.Suppliers);
        await LoadAsync();
        SelectedRow = Items.FirstOrDefault(r => r.Id == row.Id);
    }

    private async Task DeleteAsync(CatalogItemRow? row)
    {
        if (row is null)
            return;
        var confirm = await _dialogs.ShowAsync(new ConfirmDialogViewModel(
            "DeleteCatalogItem".T(),
            string.Format(CultureInfo.CurrentCulture, "DeleteCatalogItemMessage".T(), row.Name),
            "Delete".T(),
            danger: true));
        if (confirm != true)
            return;
        await _catalog.DeactivateAsync(row.Id);
        await LoadAsync();
    }

    private async Task OpenUnitsAsync()
    {
        await _dialogs.ShowAsync(new UnitsDialogViewModel(_units));
        await LoadAsync();
    }

    private static CatalogItemRow ToRow(CatalogItem item) => new()
    {
        Id = item.Id,
        Kind = item.Kind,
        KindDisplay = item.Kind == OfferingKind.Product ? "Product".T() : "Service".T(),
        Name = item.Name,
        UnitDisplay = item.UnitOfMeasure?.Name ?? "",
        SuppliersDisplay = string.Join(", ",
            item.SupplierOfferings.Where(o => o.IsActive && o.Supplier.IsActive).Select(o => o.Supplier.Name))
    };

    private void RaiseEmptyState()
    {
        OnPropertyChanged(nameof(ShowGrid));
        OnPropertyChanged(nameof(ShowEmptyDatabase));
        OnPropertyChanged(nameof(ShowEmptyFilter));
        OnPropertyChanged(nameof(HasActiveFilter));
    }
}

public sealed record KindOption(OfferingKind Kind, string Label)
{
    public bool IsAll { get; set; }
}
