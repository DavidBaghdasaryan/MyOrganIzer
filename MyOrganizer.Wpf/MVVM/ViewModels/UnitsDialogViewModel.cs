using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using MyOrganizer.Wpf.Entities;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.MVVM.UI;
using MyOrganizer.Wpf.Services;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class UnitsDialogViewModel : DialogViewModel
{
    private readonly IUnitOfMeasureService _units;
    private UnitOfMeasure? _selected;
    private bool _isEditing;
    private int _editId;
    private string _editName = "";
    private UnitOfMeasure? _editBase;
    private string _editFactor = "";
    private string? _nameError;

    public UnitsDialogViewModel(IUnitOfMeasureService units)
    {
        _units = units;
        Title = "Units".T();
        CloseCommand = new RelayCommand(() => Close(true));
        AddCommand = new RelayCommand(StartAdd);
        EditCommand = new RelayCommand(StartEdit, () => Selected is not null && !IsEditing);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => Selected is not null && !IsEditing);
        SaveEditCommand = new AsyncRelayCommand(SaveEditAsync);
        CancelEditCommand = new RelayCommand(CancelEdit);
        _ = LoadAsync();
    }

    public string Title { get; }
    public ObservableCollection<UnitOfMeasure> Items { get; } = [];
    public ICommand CloseCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SaveEditCommand { get; }
    public ICommand CancelEditCommand { get; }

    public UnitOfMeasure? Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value))
            {
                ((RelayCommand)EditCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)DeleteCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsEditing
    {
        get => _isEditing;
        private set
        {
            if (SetProperty(ref _isEditing, value))
            {
                OnPropertyChanged(nameof(ShowList));
                ((RelayCommand)EditCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)DeleteCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool ShowList => !IsEditing;

    public string EditName
    {
        get => _editName;
        set
        {
            if (SetProperty(ref _editName, value))
                NameError = null;
        }
    }

    public UnitOfMeasure? EditBase
    {
        get => _editBase;
        set => SetProperty(ref _editBase, value);
    }

    public string EditFactor
    {
        get => _editFactor;
        set => SetProperty(ref _editFactor, value);
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
    public IEnumerable<UnitOfMeasure> BaseChoices => Items.Where(u => u.Id != _editId);

    private async Task LoadAsync()
    {
        var selectedId = Selected?.Id;
        Items.Clear();
        foreach (var unit in await _units.GetAllAsync())
            Items.Add(unit);
        Selected = selectedId is int id ? Items.FirstOrDefault(u => u.Id == id) : Items.FirstOrDefault();
        OnPropertyChanged(nameof(BaseChoices));
    }

    private void StartAdd()
    {
        _editId = 0;
        EditName = "";
        EditBase = null;
        EditFactor = "";
        NameError = null;
        IsEditing = true;
        OnPropertyChanged(nameof(BaseChoices));
    }

    private void StartEdit()
    {
        if (Selected is null)
            return;
        _editId = Selected.Id;
        EditName = Selected.Name;
        EditBase = Items.FirstOrDefault(u => u.Id == Selected.BaseUnitId);
        EditFactor = Selected.ConversionFactor?.ToString("0.####", CultureInfo.InvariantCulture) ?? "";
        NameError = null;
        IsEditing = true;
        OnPropertyChanged(nameof(BaseChoices));
    }

    private void CancelEdit() => IsEditing = false;

    private async Task SaveEditAsync()
    {
        NameError = string.IsNullOrWhiteSpace(EditName) ? "FieldRequired".T() : null;
        if (HasNameError)
            return;

        decimal? factor = null;
        if (EditBase is not null &&
            (decimal.TryParse(EditFactor, NumberStyles.Any, CultureInfo.CurrentCulture, out var value) ||
             decimal.TryParse(EditFactor, NumberStyles.Any, CultureInfo.InvariantCulture, out value)))
            factor = value;

        await _units.SaveAsync(_editId, EditName.Trim(), EditBase?.Id, factor);
        IsEditing = false;
        await LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (Selected is null)
            return;
        var result = ModernDialog.Show(
            string.Format(CultureInfo.CurrentCulture, "DeleteUnitMessage".T(), Selected.Name),
            "DeleteUnit".T(),
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.OK)
            return;
        await _units.DeactivateAsync(Selected.Id);
        await LoadAsync();
    }
}
