using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using MyOrganizer.Wpf.Entities;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.Services;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class CatalogItemEditResult
{
    public required string Name { get; init; }
    public required OfferingKind Kind { get; init; }
    public int? UnitOfMeasureId { get; init; }
    public required string Notes { get; init; }
    public required IReadOnlyList<CatalogSupplierLink> Suppliers { get; init; }
}

public sealed class SupplierLinkRow : ObservableObject
{
    private string _price = "0";

    public SupplierLinkRow(Supplier supplier, decimal price)
    {
        Supplier = supplier;
        SupplierId = supplier.Id;
        Name = supplier.Name;
        _price = price.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public Supplier Supplier { get; }
    public int SupplierId { get; }
    public string Name { get; }

    public string Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }
}

public sealed class EditCatalogItemDialogViewModel : DialogViewModel
{
    private string _name = "";
    private KindOption _kind;
    private UnitOfMeasure? _unit;
    private string _notes = "";
    private string? _nameError;
    private Supplier? _selectedSupplier;
    private string _price = "0";
    private readonly Supplier _noneSupplier = new() { Id = 0, Name = "OptionalSupplier".T() };

    public EditCatalogItemDialogViewModel(
        IReadOnlyList<Supplier> suppliers,
        IReadOnlyList<UnitOfMeasure> units,
        CatalogItem? item = null,
        int? preselectedSupplierId = null,
        OfferingKind? initialKind = null)
    {
        IsNew = item is null;
        Title = IsNew ? "AddCatalogItem".T() : "EditCatalogItem".T();
        KindOptions =
        [
            new KindOption(OfferingKind.Product, "Product".T()),
            new KindOption(OfferingKind.Service, "Service".T())
        ];
        Units = [new UnitOfMeasure { Id = 0, Name = "OptionalNone".T() }, .. units];
        _kind = KindOptions[0];
        _unit = Units[0];
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => Close(false));
        RemoveSupplierCommand = new RelayCommand(p => RemoveSupplier(p as SupplierLinkRow));

        SupplierChoices.Add(_noneSupplier);
        SupplierLinkRow? primary = null;
        foreach (var supplier in suppliers)
        {
            var existing = item?.SupplierOfferings.FirstOrDefault(o => o.SupplierId == supplier.Id && o.IsActive);
            var selected = existing is not null || supplier.Id == preselectedSupplierId;
            if (selected && primary is null)
            {
                primary = new SupplierLinkRow(supplier, existing?.SupplierPrice ?? 0);
                continue;
            }

            if (selected)
                ExtraLinks.Add(new SupplierLinkRow(supplier, existing?.SupplierPrice ?? 0));
            else
                SupplierChoices.Add(supplier);
        }

        _selectedSupplier = primary?.Supplier ?? _noneSupplier;
        _price = primary?.Price ?? "0";
        if (primary is not null)
            SupplierChoices.Insert(1, primary.Supplier);

        if (item is not null)
        {
            Name = item.Name;
            Kind = KindOptions.FirstOrDefault(k => k.Kind == item.Kind) ?? KindOptions[0];
            Unit = Units.FirstOrDefault(u => u.Id == item.UnitOfMeasureId) ?? Units[0];
            Notes = item.Notes;
        }
        else if (initialKind is { } kind)
        {
            Kind = KindOptions.FirstOrDefault(k => k.Kind == kind) ?? KindOptions[0];
        }
    }

    public bool IsNew { get; }
    public string Title { get; }
    public CatalogItemEditResult? Result { get; private set; }
    public IReadOnlyList<KindOption> KindOptions { get; }
    public IReadOnlyList<UnitOfMeasure> Units { get; }
    public ObservableCollection<Supplier> SupplierChoices { get; } = [];
    public ObservableCollection<SupplierLinkRow> ExtraLinks { get; } = [];
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand RemoveSupplierCommand { get; }
    public bool ShowExtraLinks => ExtraLinks.Count > 0;

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
                NameError = null;
        }
    }

    public KindOption Kind
    {
        get => _kind;
        set => SetProperty(ref _kind, value);
    }

    public UnitOfMeasure? Unit
    {
        get => _unit;
        set => SetProperty(ref _unit, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string? NameError
    {
        get => _nameError;
        private set
        {
            if (SetProperty(ref _nameError, value))
                OnPropertyChanged(nameof(HasNameError));
        }
    }

    public bool HasNameError => !string.IsNullOrEmpty(NameError);

    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set => SetProperty(ref _selectedSupplier, value);
    }

    public string Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }

    private void RemoveSupplier(SupplierLinkRow? row)
    {
        if (row is null)
            return;

        ExtraLinks.Remove(row);
        SupplierChoices.Add(row.Supplier);
        OnPropertyChanged(nameof(ShowExtraLinks));
    }

    private void Save()
    {
        NameError = string.IsNullOrWhiteSpace(Name) ? "FieldRequired".T() : null;
        if (HasNameError)
            return;

        var suppliers = ExtraLinks
            .Select(s => new CatalogSupplierLink
            {
                SupplierId = s.SupplierId,
                Price = ParsePrice(s.Price)
            })
            .ToList();
        if (SelectedSupplier is { Id: > 0 } supplier)
        {
            suppliers.RemoveAll(s => s.SupplierId == supplier.Id);
            suppliers.Add(new CatalogSupplierLink
            {
                SupplierId = supplier.Id,
                Price = ParsePrice(Price)
            });
        }

        Result = new CatalogItemEditResult
        {
            Name = Name.Trim(),
            Kind = Kind.Kind,
            UnitOfMeasureId = Unit?.Id is > 0 ? Unit.Id : null,
            Notes = Notes.Trim(),
            Suppliers = suppliers
        };
        Close(true);
    }

    private static decimal ParsePrice(string text)
    {
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var value) ||
            decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            return value < 0 ? 0 : value;
        return 0;
    }
}
