using System.Globalization;
using System.Windows.Input;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class ProcedureEditResult
{
    public required string Name { get; init; }
    public required decimal Tier1 { get; init; }
    public required decimal Tier2 { get; init; }
    public required decimal Tier3 { get; init; }
    public required string Currency { get; init; }
}

public sealed class EditProcedureDialogViewModel : DialogViewModel
{
    private string _name = "";
    private string _tier1 = "0";
    private string _tier2 = "0";
    private string _tier3 = "0";
    private string? _nameError;
    private string? _priceError;

    public EditProcedureDialogViewModel(ProcedureRow? row = null)
    {
        IsNew = row is null;
        Title = IsNew ? "AddProcedure".T() : "EditProcedure".T();
        Currency = row?.Currency ?? "AMD";
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => Close(false));
        if (row is not null)
        {
            Name = row.Name;
            Tier1 = Format(row.Tier1);
            Tier2 = Format(row.Tier2);
            Tier3 = Format(row.Tier3);
        }
    }

    public bool IsNew { get; }
    public string Title { get; }
    public string Currency { get; }
    public ProcedureEditResult? Result { get; private set; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
                NameError = null;
        }
    }

    public string Tier1
    {
        get => _tier1;
        set
        {
            if (SetProperty(ref _tier1, value))
                PriceError = null;
        }
    }

    public string Tier2
    {
        get => _tier2;
        set
        {
            if (SetProperty(ref _tier2, value))
                PriceError = null;
        }
    }

    public string Tier3
    {
        get => _tier3;
        set
        {
            if (SetProperty(ref _tier3, value))
                PriceError = null;
        }
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

    public string? PriceError
    {
        get => _priceError;
        private set
        {
            if (SetProperty(ref _priceError, value))
                OnPropertyChanged(nameof(HasPriceError));
        }
    }

    public bool HasNameError => !string.IsNullOrEmpty(NameError);
    public bool HasPriceError => !string.IsNullOrEmpty(PriceError);

    public bool Validate()
    {
        NameError = string.IsNullOrWhiteSpace(Name) ? "FieldRequired".T() : null;

        if (!TryParsePrice(Tier1, out var t1) ||
            !TryParsePrice(Tier2, out var t2) ||
            !TryParsePrice(Tier3, out var t3) ||
            t1 < 0 || t2 < 0 || t3 < 0)
        {
            PriceError = "FieldRequired".T();
            return false;
        }

        PriceError = null;
        return !HasNameError;
    }

    private void Save()
    {
        if (!Validate())
            return;

        TryParsePrice(Tier1, out var t1);
        TryParsePrice(Tier2, out var t2);
        TryParsePrice(Tier3, out var t3);
        Result = new ProcedureEditResult
        {
            Name = Name.Trim(),
            Tier1 = t1,
            Tier2 = t2,
            Tier3 = t3,
            Currency = Currency
        };
        Close(true);
    }

    private static string Format(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    private static bool TryParsePrice(string text, out decimal value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0;
            return true;
        }

        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value)
            || decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }
}
