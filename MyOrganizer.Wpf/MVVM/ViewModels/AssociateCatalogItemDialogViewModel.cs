using System.Globalization;
using System.Windows.Input;
using MyOrganizer.Wpf.Entities;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class AssociationEditResult
{
    public required int CatalogItemId { get; init; }
    public required decimal Price { get; init; }
}

public sealed class AssociateCatalogItemDialogViewModel : DialogViewModel
{
    private CatalogItem? _selectedItem;
    private string _price = "0";
    private string? _itemError;

    public AssociateCatalogItemDialogViewModel(OfferingKind kind, IReadOnlyList<CatalogItem> items, SupplierOffering? existing = null)
    {
        Kind = kind;
        Title = kind == OfferingKind.Product ? "AssociateProduct".T() : "AssociateService".T();
        Items = items;
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => Close(false));
        CreateNewCommand = new RelayCommand(() =>
        {
            CreateNewRequested = true;
            Close(true);
        });
        if (existing is not null)
        {
            SelectedItem = items.FirstOrDefault(i => i.Id == existing.CatalogItemId);
            Price = existing.SupplierPrice.ToString("0.##", CultureInfo.InvariantCulture);
        }
        else
        {
            SelectedItem = items.FirstOrDefault();
        }
    }

    public OfferingKind Kind { get; }
    public string Title { get; }
    public IReadOnlyList<CatalogItem> Items { get; }
    public AssociationEditResult? Result { get; private set; }
    public bool CreateNewRequested { get; private set; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand CreateNewCommand { get; }

    public CatalogItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
                ItemError = null;
        }
    }

    public string Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }

    public string? ItemError
    {
        get => _itemError;
        private set
        {
            if (SetProperty(ref _itemError, value))
                OnPropertyChanged(nameof(HasItemError));
        }
    }

    public bool HasItemError => !string.IsNullOrEmpty(ItemError);

    private void Save()
    {
        if (SelectedItem is null)
        {
            ItemError = "FieldRequired".T();
            return;
        }

        decimal amount = 0;
        _ = decimal.TryParse(Price, NumberStyles.Any, CultureInfo.CurrentCulture, out amount)
            || decimal.TryParse(Price, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);

        Result = new AssociationEditResult
        {
            CatalogItemId = SelectedItem.Id,
            Price = amount < 0 ? 0 : decimal.Round(amount, 2)
        };
        Close(true);
    }
}
