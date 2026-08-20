using System.Windows.Input;
using MyOrganizer.Wpf.Entities;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class SupplierEditResult
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required string Notes { get; init; }
}

public sealed class EditSupplierDialogViewModel : DialogViewModel
{
    private string _name = "";
    private string _email = "";
    private string _phone = "";
    private string _notes = "";
    private string? _nameError;

    public EditSupplierDialogViewModel(Supplier? supplier = null)
    {
        IsNew = supplier is null;
        Title = IsNew ? "AddSupplier".T() : "EditSupplier".T();
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => Close(false));
        if (supplier is not null)
        {
            Name = supplier.Name;
            Email = supplier.Email;
            Phone = supplier.Phone;
            Notes = supplier.Notes;
        }
    }

    public bool IsNew { get; }
    public string Title { get; }
    public SupplierEditResult? Result { get; private set; }
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

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
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

    private void Save()
    {
        NameError = string.IsNullOrWhiteSpace(Name) ? "FieldRequired".T() : null;
        if (HasNameError)
            return;

        Result = new SupplierEditResult
        {
            Name = Name.Trim(),
            Email = Email.Trim(),
            Phone = Phone.Trim(),
            Notes = Notes.Trim()
        };
        Close(true);
    }
}
