using System.Globalization;
using System.Windows.Input;
using MyOrganizer.Wpf.Entities;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class TechnicianEditResult
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required int Price { get; init; }
    public required DateTime Date { get; init; }
}

public sealed class EditTechnicianDialogViewModel : DialogViewModel
{
    private string _name = "";
    private string? _type;
    private string _price = "0";
    private DateTime? _date = DateTime.Today;
    private string? _materialError;
    private string? _priceError;

    public EditTechnicianDialogViewModel(IReadOnlyList<string> materials, Technic? row = null)
    {
        IsNew = row is null;
        Title = IsNew ? "AddTechnician".T() : "EditTechnician".T();
        Materials = IncludeCurrent(materials, row?.Type);
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => Close(false));
        if (row is not null)
        {
            Name = row.Name ?? "";
            Type = string.IsNullOrWhiteSpace(row.Type) ? null : row.Type;
            Price = row.Price.ToString(CultureInfo.InvariantCulture);
            Date = row.Date == default ? DateTime.Today : row.Date;
        }
    }

    public bool IsNew { get; }
    public string Title { get; }
    public IReadOnlyList<string> Materials { get; }
    public TechnicianEditResult? Result { get; private set; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string? Type
    {
        get => _type;
        set
        {
            if (SetProperty(ref _type, value))
                MaterialError = null;
        }
    }

    public string Price
    {
        get => _price;
        set
        {
            if (SetProperty(ref _price, value))
                PriceError = null;
        }
    }

    public DateTime? Date
    {
        get => _date;
        set => SetProperty(ref _date, value);
    }

    public string? MaterialError
    {
        get => _materialError;
        private set
        {
            if (SetProperty(ref _materialError, value))
                OnPropertyChanged(nameof(HasMaterialError));
        }
    }

    public string? PriceError
    {
        get => _priceError;
        private set
        {
            if (SetProperty(ref _priceError, value))
                OnPropertyChanged(nameof(HasPriceError));
        }
    }

    public bool HasMaterialError => !string.IsNullOrEmpty(MaterialError);
    public bool HasPriceError => !string.IsNullOrEmpty(PriceError);

    public bool Validate()
    {
        MaterialError = string.IsNullOrWhiteSpace(Type) ? "Materialnotspecified".T() : null;
        if (!TryParsePrice(Price, out var amount) || amount < 0)
        {
            PriceError = "FieldRequired".T();
            return false;
        }

        PriceError = null;
        return !HasMaterialError;
    }

    private void Save()
    {
        if (!Validate())
            return;

        TryParsePrice(Price, out var amount);
        Result = new TechnicianEditResult
        {
            Name = Name.Trim(),
            Type = Type!.Trim(),
            Price = amount,
            Date = Date ?? DateTime.Today
        };
        Close(true);
    }

    private static IReadOnlyList<string> IncludeCurrent(IReadOnlyList<string> materials, string? current)
    {
        if (string.IsNullOrWhiteSpace(current) || materials.Contains(current, StringComparer.Ordinal))
            return materials;
        return [current, .. materials];
    }

    private static bool TryParsePrice(string text, out int value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0;
            return true;
        }

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value)
            || int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
