using System.Globalization;
using System.Windows.Input;
using MyOrganizer.Wpf.Data.Entities;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class EditClientDialogViewModel : DialogViewModel
{
    private string _firstName = "";
    private string _lastName = "";
    private string _middleName = "";
    private string _phone = "0";
    private string _price = "0";
    private string _debt = "0";
    private DateTime? _dateJoin = DateTime.Now;
    private bool _scheduleDouble;
    private DateTime? _dateDouble;
    private string? _firstNameError;
    private string? _lastNameError;
    private string? _middleNameError;

    public EditClientDialogViewModel(Client? client = null)
    {
        IsNew = client is null || client.Id <= 0;
        Title = IsNew ? "AddPatient".T() : "EditPatient".T();
        SaveCommand = new RelayCommand(Save, () => true);
        CancelCommand = new RelayCommand(() => Close(false));
        if (client is not null)
            Load(client);
    }

    public bool IsNew { get; }
    public int ClientId { get; private set; }
    public string Title { get; }
    public Client? Result { get; private set; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public string FirstName
    {
        get => _firstName;
        set
        {
            if (SetProperty(ref _firstName, value))
                FirstNameError = null;
        }
    }

    public string LastName
    {
        get => _lastName;
        set
        {
            if (SetProperty(ref _lastName, value))
                LastNameError = null;
        }
    }

    public string MiddleName
    {
        get => _middleName;
        set
        {
            if (SetProperty(ref _middleName, value))
                MiddleNameError = null;
        }
    }

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }

    public string Debt
    {
        get => _debt;
        set => SetProperty(ref _debt, value);
    }

    public DateTime? DateJoin
    {
        get => _dateJoin;
        set => SetProperty(ref _dateJoin, value);
    }

    public bool ScheduleDouble
    {
        get => _scheduleDouble;
        set => SetProperty(ref _scheduleDouble, value);
    }

    public DateTime? DateDouble
    {
        get => _dateDouble;
        set => SetProperty(ref _dateDouble, value);
    }

    public string? FirstNameError
    {
        get => _firstNameError;
        private set
        {
            if (SetProperty(ref _firstNameError, value))
                OnPropertyChanged(nameof(HasFirstNameError));
        }
    }

    public string? LastNameError
    {
        get => _lastNameError;
        private set
        {
            if (SetProperty(ref _lastNameError, value))
                OnPropertyChanged(nameof(HasLastNameError));
        }
    }

    public string? MiddleNameError
    {
        get => _middleNameError;
        private set
        {
            if (SetProperty(ref _middleNameError, value))
                OnPropertyChanged(nameof(HasMiddleNameError));
        }
    }

    public bool HasFirstNameError => !string.IsNullOrEmpty(FirstNameError);
    public bool HasLastNameError => !string.IsNullOrEmpty(LastNameError);
    public bool HasMiddleNameError => !string.IsNullOrEmpty(MiddleNameError);

    public Client ToClient()
    {
        var client = new Client
        {
            Id = ClientId,
            FirstName = FirstName.Trim(),
            LastName = LastName.Trim(),
            MidlName = MiddleName.Trim(),
            PhoneNumber = Phone.Trim(),
            Price = decimal.TryParse(Price, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0,
            Debet = decimal.TryParse(Debt, NumberStyles.Any, CultureInfo.InvariantCulture, out var debt) ? debt : 0,
            DateJoin = DateJoin ?? DateTime.Now
        };

        if (ScheduleDouble && DateDouble.HasValue)
        {
            client.DateDobleJoin = DateDouble.Value;
            client.DateJoinString = client.DateDobleJoin.Value.ToString("dd-MM-yyyy HH:mm:ss");
        }
        else
        {
            client.DateDobleJoin = null;
            client.DateJoinString = null;
        }

        return client;
    }

    public bool Validate()
    {
        var required = "FieldRequired".T();
        FirstNameError = string.IsNullOrWhiteSpace(FirstName) ? required : null;
        LastNameError = string.IsNullOrWhiteSpace(LastName) ? required : null;
        MiddleNameError = string.IsNullOrWhiteSpace(MiddleName) ? required : null;
        return !HasFirstNameError && !HasLastNameError && !HasMiddleNameError;
    }

    private void Load(Client client)
    {
        ClientId = client.Id;
        FirstName = client.FirstName ?? "";
        LastName = client.LastName ?? "";
        MiddleName = client.MidlName ?? "";
        Phone = string.IsNullOrWhiteSpace(client.PhoneNumber) ? "0" : client.PhoneNumber;
        Price = client.Price?.ToString(CultureInfo.InvariantCulture) ?? "0";
        Debt = client.Debet?.ToString(CultureInfo.InvariantCulture) ?? "0";
        DateJoin = client.DateJoin == default ? DateTime.Now : client.DateJoin;
        if (client.DateDobleJoin is { } doubleVisit && doubleVisit > DateTime.MinValue)
        {
            ScheduleDouble = true;
            DateDouble = doubleVisit;
        }
    }

    private void Save()
    {
        if (!Validate())
            return;
        Result = ToClient();
        Close(true);
    }
}
