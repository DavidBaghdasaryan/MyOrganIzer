using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Data.Entities;
using MyOrganizer.Wpf.Entities;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.MVVM.UI;
using MyOrganizer.Wpf.Navigation;
using MyOrganizer.Wpf.Services;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed record CatalogChoice(int? Id, string Name, decimal Price, int? UnitId);

public sealed record LookupOption(int? Id, string Name);

public sealed class ExpenseLineItemViewModel : ObservableObject
{
    private readonly ExpenseEditorViewModel _owner;
    private OfferingKind _kind = OfferingKind.Product;
    private CatalogChoice? _selectedChoice;
    private string _description = "";
    private string _quantity = "1";
    private UnitOfMeasure? _unit;
    private string _unitPrice = "0";
    private bool _showCaseLink;
    private LookupOption? _selectedClient;
    private string _toothFdi = "";
    private LookupOption? _selectedProcedure;
    private bool _suppress;

    public ExpenseLineItemViewModel(ExpenseEditorViewModel owner)
    {
        _owner = owner;
        RemoveCommand = new RelayCommand(() => _owner.RemoveLine(this));
        ToggleCaseLinkCommand = new RelayCommand(() => ShowCaseLink = !ShowCaseLink);
    }

    public ICommand RemoveCommand { get; }
    public ICommand ToggleCaseLinkCommand { get; }

    public OfferingKind Kind
    {
        get => _kind;
        set
        {
            if (!SetProperty(ref _kind, value))
                return;
            OnPropertyChanged(nameof(ShowCatalogPicker));
            OnPropertyChanged(nameof(ShowManualDescription));
            OnPropertyChanged(nameof(Choices));
            if (!_suppress)
            {
                SelectedChoice = Choices.FirstOrDefault();
                _owner.RecalculateTotal();
            }
        }
    }

    public bool ShowCatalogPicker => _owner.HasSupplier && Kind is OfferingKind.Product or OfferingKind.Service;
    public bool ShowManualDescription => !ShowCatalogPicker || SelectedChoice?.Id is null;

    public IEnumerable<CatalogChoice> Choices
    {
        get
        {
            if (ShowCatalogPicker)
            {
                foreach (var choice in _owner.ChoicesFor(Kind))
                    yield return choice;
            }

            yield return _owner.ManualChoice;
        }
    }

    public CatalogChoice? SelectedChoice
    {
        get => _selectedChoice;
        set
        {
            if (!SetProperty(ref _selectedChoice, value) || _suppress)
            {
                OnPropertyChanged(nameof(ShowManualDescription));
                return;
            }

            OnPropertyChanged(nameof(ShowManualDescription));
            if (value?.Id is int)
            {
                Description = value.Name;
                UnitPrice = value.Price.ToString("0.##", CultureInfo.InvariantCulture);
                Unit = _owner.Units.FirstOrDefault(u => u.Id == value.UnitId) ?? _owner.NoneUnit;
            }
            _owner.RecalculateTotal();
        }
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
                _owner.RecalculateTotal();
        }
    }

    public UnitOfMeasure? Unit
    {
        get => _unit;
        set => SetProperty(ref _unit, value);
    }

    public string UnitPrice
    {
        get => _unitPrice;
        set
        {
            if (SetProperty(ref _unitPrice, value))
                _owner.RecalculateTotal();
        }
    }

    public bool ShowCaseLink
    {
        get => _showCaseLink;
        set => SetProperty(ref _showCaseLink, value);
    }

    public LookupOption? SelectedClient
    {
        get => _selectedClient;
        set => SetProperty(ref _selectedClient, value);
    }

    public string ToothFdi
    {
        get => _toothFdi;
        set => SetProperty(ref _toothFdi, value);
    }

    public LookupOption? SelectedProcedure
    {
        get => _selectedProcedure;
        set => SetProperty(ref _selectedProcedure, value);
    }

    public decimal LineTotal => decimal.Round(ParseQuantity() * ParsePrice(), 2);
    public string LineTotalDisplay => MoneyFormat.Display(LineTotal);

    public void RefreshChoices()
    {
        OnPropertyChanged(nameof(ShowCatalogPicker));
        OnPropertyChanged(nameof(Choices));
        if (SelectedChoice?.Id is int id && Choices.All(c => c.Id != id))
            SelectedChoice = Choices.FirstOrDefault();
        OnPropertyChanged(nameof(ShowManualDescription));
    }

    public void RaiseTotals()
    {
        OnPropertyChanged(nameof(LineTotal));
        OnPropertyChanged(nameof(LineTotalDisplay));
    }

    public decimal ParseQuantity()
    {
        if (decimal.TryParse(Quantity, NumberStyles.Any, CultureInfo.CurrentCulture, out var value) ||
            decimal.TryParse(Quantity, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            return value <= 0 ? 1 : value;
        return 1;
    }

    public decimal ParsePrice()
    {
        if (decimal.TryParse(UnitPrice, NumberStyles.Any, CultureInfo.CurrentCulture, out var value) ||
            decimal.TryParse(UnitPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            return value < 0 ? 0 : value;
        return 0;
    }

    public void BindExisting(ExpenseLine line)
    {
        _suppress = true;
        Kind = line.Kind;
        Description = line.Description;
        Quantity = line.Quantity.ToString("0.###", CultureInfo.InvariantCulture);
        UnitPrice = line.UnitPrice.ToString("0.##", CultureInfo.InvariantCulture);
        Unit = _owner.Units.FirstOrDefault(u => u.Id == line.UnitOfMeasureId) ?? _owner.NoneUnit;
        SelectedClient = _owner.Clients.FirstOrDefault(c => c.Id == line.ClientId) ?? _owner.Clients.FirstOrDefault();
        SelectedProcedure = _owner.Procedures.FirstOrDefault(p => p.Id == line.CatalogProcedureId) ?? _owner.Procedures.FirstOrDefault();
        ToothFdi = line.ToothFdi ?? "";
        ShowCaseLink = line.ClientId is not null || line.CatalogProcedureId is not null || !string.IsNullOrWhiteSpace(line.ToothFdi);
        SelectedChoice = line.CatalogItemId is int id
            ? Choices.FirstOrDefault(c => c.Id == id) ?? _owner.ManualChoice
            : _owner.ManualChoice;
        _suppress = false;
        RaiseTotals();
        RefreshChoices();
    }
}

public sealed class ExpenseEditorViewModel : ObservableObject
{
    private readonly IExpenseService _expenses;
    private readonly ISupplierService _suppliers;
    private readonly IProcedureService _procedures;
    private readonly IUnitOfMeasureService _units;
    private readonly AppDbContext _db;
    private readonly INavigationService _navigation;
    private int _expenseId;
    private bool _suppressSupplierLoad;
    private Supplier? _selectedSupplier;
    private DateTime? _date = DateTime.Today;
    private string _reference = "";
    private string _notes = "";
    private string _totalDisplay = MoneyFormat.Display(0);
    private bool _isBusy = true;
    private string? _error;

    public ExpenseEditorViewModel(
        IExpenseService expenses,
        ISupplierService suppliers,
        IProcedureService procedures,
        IUnitOfMeasureService units,
        AppDbContext db,
        INavigationService navigation)
    {
        _expenses = expenses;
        _suppliers = suppliers;
        _procedures = procedures;
        _units = units;
        _db = db;
        _navigation = navigation;
        ManualChoice = new CatalogChoice(null, "ManualEntry".T(), 0, null);
        BackCommand = new RelayCommand(_navigation.GoBack);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        AddLineCommand = new RelayCommand(AddLine);
        CancelCommand = new RelayCommand(_navigation.GoBack);
    }

    public CatalogChoice ManualChoice { get; }
    public UnitOfMeasure NoneUnit { get; } = new() { Id = 0, Name = "OptionalNone".T() };
    public ObservableCollection<Supplier> SupplierChoices { get; } = [];
    public ObservableCollection<SupplierOffering> Associations { get; } = [];
    public ObservableCollection<ExpenseLineItemViewModel> Lines { get; } = [];
    public ObservableCollection<LookupOption> Clients { get; } = [];
    public ObservableCollection<LookupOption> Procedures { get; } = [];
    public ObservableCollection<UnitOfMeasure> Units { get; } = [];
    public IReadOnlyList<KindOption> KindOptions { get; } =
    [
        new(OfferingKind.Product, "Product".T()),
        new(OfferingKind.Service, "Service".T()),
        new(OfferingKind.Other, "Other".T())
    ];

    public ICommand BackCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand CancelCommand { get; }

    public string Title => _expenseId > 0 ? "EditExpense".T() : "AddExpense".T();
    public string BackLabel => "Expenses".T();

    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (!SetProperty(ref _selectedSupplier, value))
                return;
            OnPropertyChanged(nameof(HasSupplier));
            if (!_suppressSupplierLoad)
                _ = LoadAssociationsAsync();
        }
    }

    public DateTime? Date
    {
        get => _date;
        set => SetProperty(ref _date, value);
    }

    public string Reference
    {
        get => _reference;
        set => SetProperty(ref _reference, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string TotalDisplay
    {
        get => _totalDisplay;
        private set => SetProperty(ref _totalDisplay, value);
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

    public bool HasError => !string.IsNullOrWhiteSpace(Error);
    public bool ShowContent => !IsBusy;
    public bool HasSupplier => SelectedSupplier is { Id: > 0 };

    public IEnumerable<CatalogChoice> ChoicesFor(OfferingKind kind) =>
        Associations
            .Where(a => a.CatalogItem.Kind == kind)
            .Select(a => new CatalogChoice(
                a.CatalogItemId,
                a.CatalogItem.Name,
                a.SupplierPrice,
                a.CatalogItem.UnitOfMeasureId));

    public async Task LoadNewAsync(int? supplierId = null)
    {
        _expenseId = 0;
        await LoadLookupsAsync();
        if (supplierId is int id)
            await SelectSupplierAsync(SupplierChoices.FirstOrDefault(s => s.Id == id) ?? NoneSupplier());
        else
            await SelectSupplierAsync(NoneSupplier());
        if (Lines.Count == 0)
            AddLine();
        RecalculateTotal();
        OnPropertyChanged(nameof(Title));
    }

    public async Task LoadExistingAsync(int expenseId)
    {
        _expenseId = expenseId;
        await LoadLookupsAsync();
        var expense = await _expenses.GetByIdAsync(expenseId);
        if (expense is null)
        {
            Error = "DeleteExpenseMessage".T();
            IsBusy = false;
            return;
        }

        Date = expense.Date;
        Reference = expense.Reference;
        Notes = expense.Notes;
        await SelectSupplierAsync(SupplierChoices.FirstOrDefault(s => s.Id == expense.SupplierId) ?? NoneSupplier());
        Lines.Clear();
        foreach (var line in expense.Lines)
        {
            var item = new ExpenseLineItemViewModel(this);
            item.BindExisting(line);
            Lines.Add(item);
        }

        if (Lines.Count == 0)
            AddLine();
        RecalculateTotal();
        OnPropertyChanged(nameof(Title));
        IsBusy = false;
    }

    public void RemoveLine(ExpenseLineItemViewModel line)
    {
        Lines.Remove(line);
        if (Lines.Count == 0)
            AddLine();
        RecalculateTotal();
    }

    public void RecalculateTotal()
    {
        foreach (var line in Lines)
            line.RaiseTotals();
        TotalDisplay = MoneyFormat.Display(Lines.Sum(l => l.LineTotal));
    }

    private async Task SelectSupplierAsync(Supplier? supplier)
    {
        _suppressSupplierLoad = true;
        SelectedSupplier = supplier;
        _suppressSupplierLoad = false;
        await LoadAssociationsAsync();
    }

    private async Task LoadLookupsAsync()
    {
        IsBusy = true;
        Error = null;
        try
        {
            SupplierChoices.Clear();
            SupplierChoices.Add(NoneSupplier());
            foreach (var supplier in await _suppliers.GetAllAsync())
                SupplierChoices.Add(supplier);

            var none = new LookupOption(null, "OptionalNone".T());
            Clients.Clear();
            Clients.Add(none);
            var clients = await _db.Clients.AsNoTracking()
                .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
                .ToListAsync();
            foreach (var client in clients)
                Clients.Add(new LookupOption(client.Id, ClientName(client)));

            Procedures.Clear();
            Procedures.Add(none);
            foreach (var procedure in await _procedures.GetAllAsync())
                Procedures.Add(new LookupOption(procedure.Id, procedure.Name));

            Units.Clear();
            NoneUnit.Name = "OptionalNone".T();
            Units.Add(NoneUnit);
            foreach (var unit in await _units.GetAllAsync())
                Units.Add(unit);
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(ShowContent));
        }
    }

    private async Task LoadAssociationsAsync()
    {
        Associations.Clear();
        if (SelectedSupplier is { Id: > 0 } supplier)
        {
            foreach (var association in await _suppliers.GetAssociationsAsync(supplier.Id))
                Associations.Add(association);
        }

        foreach (var line in Lines)
            line.RefreshChoices();
    }

    private void AddLine()
    {
        var item = new ExpenseLineItemViewModel(this)
        {
            SelectedClient = Clients.FirstOrDefault(),
            SelectedProcedure = Procedures.FirstOrDefault(),
            Unit = NoneUnit,
            SelectedChoice = ManualChoice
        };
        item.SelectedChoice = item.Choices.FirstOrDefault() ?? ManualChoice;
        Lines.Add(item);
        RecalculateTotal();
    }

    private async Task SaveAsync()
    {
        Error = null;
        var drafts = new List<ExpenseLineDraft>();
        foreach (var line in Lines)
        {
            var description = line.ShowManualDescription ? line.Description : line.SelectedChoice?.Name ?? line.Description;
            if (string.IsNullOrWhiteSpace(description))
            {
                Error = "NeedLine".T();
                return;
            }

            drafts.Add(new ExpenseLineDraft
            {
                CatalogItemId = line.ShowCatalogPicker ? line.SelectedChoice?.Id : null,
                Kind = line.Kind,
                Description = description.Trim(),
                Quantity = line.ParseQuantity(),
                UnitOfMeasureId = line.Unit?.Id is > 0 ? line.Unit.Id : null,
                UnitPrice = line.ParsePrice(),
                ClientId = line.SelectedClient?.Id,
                ToothFdi = line.ToothFdi,
                CatalogProcedureId = line.SelectedProcedure?.Id
            });
        }

        if (drafts.Count == 0)
        {
            Error = "NeedLine".T();
            return;
        }

        try
        {
            await _expenses.SaveAsync(new ExpenseDraft
            {
                Id = _expenseId,
                SupplierId = SelectedSupplier is { Id: > 0 } ? SelectedSupplier.Id : null,
                Date = Date ?? DateTime.Today,
                Reference = Reference,
                Notes = Notes,
                Lines = drafts
            });
            _navigation.GoBack();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            ModernDialog.Show(ex.Message, "Error".T());
        }
    }

    private Supplier NoneSupplier() =>
        SupplierChoices.FirstOrDefault(s => s.Id == 0)
        ?? new Supplier { Id = 0, Name = "OptionalSupplier".T() };

    private static string ClientName(Client client) =>
        string.Join(" ", new[] { client.LastName, client.FirstName, client.MidlName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
}
