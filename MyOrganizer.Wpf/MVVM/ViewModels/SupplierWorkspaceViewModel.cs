using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MyOrganizer.Wpf.Entities;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.Navigation;
using MyOrganizer.Wpf.Services;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed record OfferingRow
{
    public required int CatalogItemId { get; init; }
    public required OfferingKind Kind { get; init; }
    public required string Name { get; init; }
    public required decimal SupplierPrice { get; init; }
    public required string PriceDisplay { get; init; }
    public required string UnitDisplay { get; init; }
}

public sealed record SupplierExpenseRow
{
    public required int Id { get; init; }
    public required string DateDisplay { get; init; }
    public required string Reference { get; init; }
    public required string TotalDisplay { get; init; }
}

public sealed class SupplierWorkspaceViewModel : ObservableObject, INavigationAware
{
    private readonly ISupplierService _suppliers;
    private readonly ICatalogService _catalog;
    private readonly IUnitOfMeasureService _units;
    private readonly IExpenseService _expenses;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;
    private readonly IServiceProvider _services;
    private Supplier? _supplier;
    private OfferingRow? _selectedProduct;
    private OfferingRow? _selectedService;
    private SupplierExpenseRow? _selectedExpense;
    private bool _isBusy = true;
    private bool _notFound;
    private bool _opened;

    public SupplierWorkspaceViewModel(
        ISupplierService suppliers,
        ICatalogService catalog,
        IUnitOfMeasureService units,
        IExpenseService expenses,
        IDialogService dialogs,
        INavigationService navigation,
        IServiceProvider services)
    {
        _suppliers = suppliers;
        _catalog = catalog;
        _units = units;
        _expenses = expenses;
        _dialogs = dialogs;
        _navigation = navigation;
        _services = services;
        BackCommand = new RelayCommand(_navigation.GoBack);
        EditCommand = new AsyncRelayCommand(EditAsync);
        AddProductCommand = new AsyncRelayCommand(() => AddAssociationAsync(OfferingKind.Product));
        AddServiceCommand = new AsyncRelayCommand(() => AddAssociationAsync(OfferingKind.Service));
        EditProductCommand = new AsyncRelayCommand(() => EditAssociationAsync(SelectedProduct));
        EditServiceCommand = new AsyncRelayCommand(() => EditAssociationAsync(SelectedService));
        DeleteProductCommand = new AsyncRelayCommand(p => DeleteAssociationAsync(p as OfferingRow ?? SelectedProduct));
        DeleteServiceCommand = new AsyncRelayCommand(p => DeleteAssociationAsync(p as OfferingRow ?? SelectedService));
        AddExpenseCommand = new AsyncRelayCommand(AddExpenseAsync);
        OpenExpenseCommand = new AsyncRelayCommand(OpenExpenseAsync);
    }

    public int SupplierId { get; private set; }
    public ObservableCollection<OfferingRow> Products { get; } = [];
    public ObservableCollection<OfferingRow> Services { get; } = [];
    public ObservableCollection<SupplierExpenseRow> Expenses { get; } = [];

    public ICommand BackCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand AddProductCommand { get; }
    public ICommand AddServiceCommand { get; }
    public ICommand EditProductCommand { get; }
    public ICommand EditServiceCommand { get; }
    public ICommand DeleteProductCommand { get; }
    public ICommand DeleteServiceCommand { get; }
    public ICommand AddExpenseCommand { get; }
    public ICommand OpenExpenseCommand { get; }

    public string Title => _supplier?.Name ?? "Suppliers".T();
    public string BackLabel => "Suppliers".T();
    public string Email => _supplier?.Email ?? "";
    public string Phone => _supplier?.Phone ?? "";
    public string Notes => _supplier?.Notes ?? "";
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    public bool ShowProductsEmpty => !IsBusy && Products.Count == 0;
    public bool ShowServicesEmpty => !IsBusy && Services.Count == 0;
    public bool ShowExpensesEmpty => !IsBusy && Expenses.Count == 0;
    public bool ShowExpensesGrid => !IsBusy && Expenses.Count > 0;

    public OfferingRow? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    public OfferingRow? SelectedService
    {
        get => _selectedService;
        set => SetProperty(ref _selectedService, value);
    }

    public SupplierExpenseRow? SelectedExpense
    {
        get => _selectedExpense;
        set => SetProperty(ref _selectedExpense, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                OnPropertyChanged(nameof(ShowContent));
        }
    }

    public bool NotFound
    {
        get => _notFound;
        private set
        {
            if (SetProperty(ref _notFound, value))
                OnPropertyChanged(nameof(ShowContent));
        }
    }

    public bool ShowContent => !NotFound && _supplier is not null;

    public async Task LoadAsync(int supplierId)
    {
        SupplierId = supplierId;
        var hideUntilReady = _supplier is null;
        if (hideUntilReady)
            IsBusy = true;
        NotFound = false;
        try
        {
            _supplier = await _suppliers.GetByIdAsync(supplierId);
            if (_supplier is null || !_supplier.IsActive)
            {
                NotFound = true;
                return;
            }

            RaiseHeader();
            BindOfferings();
            await BindExpensesAsync();
        }
        catch
        {
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(ShowContent));
            OnPropertyChanged(nameof(ShowProductsEmpty));
            OnPropertyChanged(nameof(ShowServicesEmpty));
            OnPropertyChanged(nameof(ShowExpensesEmpty));
            OnPropertyChanged(nameof(ShowExpensesGrid));
        }
    }

    public void OnNavigatedTo()
    {
        if (!_opened)
        {
            _opened = true;
            return;
        }

        if (SupplierId > 0)
            _ = LoadAsync(SupplierId);
    }

    public void Refresh()
    {
        RaiseHeader();
        BindOfferings();
        OnPropertyChanged(nameof(ShowProductsEmpty));
        OnPropertyChanged(nameof(ShowServicesEmpty));
        OnPropertyChanged(nameof(ShowExpensesEmpty));
        OnPropertyChanged(nameof(ShowExpensesGrid));
    }

    private void BindOfferings()
    {
        var selectedProduct = SelectedProduct?.CatalogItemId;
        var selectedService = SelectedService?.CatalogItemId;
        Products.Clear();
        Services.Clear();
        foreach (var offering in (_supplier?.Offerings ?? [])
                     .Where(o => o.IsActive && o.CatalogItem is { IsActive: true })
                     .OrderBy(o => o.CatalogItem.Name))
        {
            var row = ToOffering(offering);
            if (offering.CatalogItem.Kind == OfferingKind.Product)
                Products.Add(row);
            else if (offering.CatalogItem.Kind == OfferingKind.Service)
                Services.Add(row);
        }

        SelectedProduct = selectedProduct is int pid
            ? Products.FirstOrDefault(p => p.CatalogItemId == pid)
            : Products.FirstOrDefault();
        SelectedService = selectedService is int sid
            ? Services.FirstOrDefault(s => s.CatalogItemId == sid)
            : Services.FirstOrDefault();
        OnPropertyChanged(nameof(ShowProductsEmpty));
        OnPropertyChanged(nameof(ShowServicesEmpty));
    }

    private async Task BindExpensesAsync()
    {
        var expenses = await _expenses.GetAllAsync(SupplierId);
        Expenses.Clear();
        foreach (var expense in expenses)
            Expenses.Add(ToExpense(expense));
        OnPropertyChanged(nameof(ShowExpensesEmpty));
        OnPropertyChanged(nameof(ShowExpensesGrid));
    }

    private async Task EditAsync()
    {
        if (_supplier is null)
            return;

        var editor = new EditSupplierDialogViewModel(_supplier);
        if (await _dialogs.ShowAsync(editor) != true || editor.Result is null)
            return;

        await _suppliers.UpdateAsync(
            _supplier.Id, editor.Result.Name, editor.Result.Email, editor.Result.Phone, editor.Result.Notes);
        await LoadAsync(SupplierId);
    }

    private async Task AddAssociationAsync(OfferingKind kind)
    {
        if (_supplier is null)
            return;

        var items = await _catalog.GetAllAsync(kind);
        var editor = new AssociateCatalogItemDialogViewModel(kind, items);
        if (await _dialogs.ShowAsync(editor) != true)
            return;

        if (editor.CreateNewRequested)
        {
            await CreateCatalogItemAsync(kind);
            return;
        }

        if (editor.Result is null)
            return;

        await _suppliers.UpsertAssociationAsync(_supplier.Id, editor.Result.CatalogItemId, editor.Result.Price);
        await LoadAsync(SupplierId);
    }

    private async Task CreateCatalogItemAsync(OfferingKind kind)
    {
        var suppliers = await _suppliers.GetAllAsync();
        var units = await _units.GetAllAsync();
        var editor = new EditCatalogItemDialogViewModel(suppliers, units, preselectedSupplierId: SupplierId, initialKind: kind);
        if (await _dialogs.ShowAsync(editor) != true || editor.Result is null)
            return;

        await _catalog.SaveAsync(0, editor.Result.Name, editor.Result.Kind, editor.Result.UnitOfMeasureId, editor.Result.Notes, editor.Result.Suppliers);
        await LoadAsync(SupplierId);
    }

    private async Task EditAssociationAsync(OfferingRow? row)
    {
        if (_supplier is null || row is null)
            return;

        var existing = _supplier.Offerings.FirstOrDefault(o => o.CatalogItemId == row.CatalogItemId);
        var items = await _catalog.GetAllAsync(row.Kind);
        var editor = new AssociateCatalogItemDialogViewModel(row.Kind, items, existing);
        if (await _dialogs.ShowAsync(editor) != true || editor.Result is null)
            return;

        if (editor.Result.CatalogItemId != row.CatalogItemId)
            await _suppliers.DeactivateAssociationAsync(_supplier.Id, row.CatalogItemId);

        await _suppliers.UpsertAssociationAsync(_supplier.Id, editor.Result.CatalogItemId, editor.Result.Price);
        await LoadAsync(SupplierId);
    }

    private async Task DeleteAssociationAsync(OfferingRow? row)
    {
        if (_supplier is null || row is null)
            return;

        var confirm = await _dialogs.ShowAsync(new ConfirmDialogViewModel(
            "DeleteOffering".T(),
            string.Format(CultureInfo.CurrentCulture, "DeleteOfferingMessage".T(), row.Name),
            "Delete".T(),
            danger: true));
        if (confirm != true)
            return;

        await _suppliers.DeactivateAssociationAsync(_supplier.Id, row.CatalogItemId);
        await LoadAsync(SupplierId);
    }

    private async Task AddExpenseAsync()
    {
        var editor = _services.GetRequiredService<ExpenseEditorViewModel>();
        await editor.LoadNewAsync(SupplierId);
        _navigation.NavigateTo(editor);
    }

    private async Task OpenExpenseAsync()
    {
        if (SelectedExpense is null)
            return;

        var editor = _services.GetRequiredService<ExpenseEditorViewModel>();
        await editor.LoadExistingAsync(SelectedExpense.Id);
        _navigation.NavigateTo(editor);
    }

    private void RaiseHeader()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(BackLabel));
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(Phone));
        OnPropertyChanged(nameof(Notes));
        OnPropertyChanged(nameof(HasNotes));
        OnPropertyChanged(nameof(ShowContent));
    }

    private static OfferingRow ToOffering(SupplierOffering offering) => new()
    {
        CatalogItemId = offering.CatalogItemId,
        Kind = offering.CatalogItem.Kind,
        Name = offering.CatalogItem.Name,
        SupplierPrice = offering.SupplierPrice,
        PriceDisplay = MoneyFormat.Display(offering.SupplierPrice),
        UnitDisplay = offering.CatalogItem.UnitOfMeasure?.Name ?? ""
    };

    private static SupplierExpenseRow ToExpense(Expense expense) => new()
    {
        Id = expense.Id,
        DateDisplay = expense.Date.ToString("d", CultureInfo.CurrentCulture),
        Reference = expense.Reference,
        TotalDisplay = MoneyFormat.Display(expense.TotalAmount)
    };
}
