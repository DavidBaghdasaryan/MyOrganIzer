using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using MyOrganizer.Wpf.Entities;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.Navigation;
using MyOrganizer.Wpf.Services;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed record ExpenseRow
{
    public required int Id { get; init; }
    public required string SupplierName { get; init; }
    public required string DateDisplay { get; init; }
    public required string Reference { get; init; }
    public required int LineCount { get; init; }
    public required string LinesDisplay { get; init; }
    public required string TotalDisplay { get; init; }
    public required bool IsMigrated { get; init; }
}

public sealed class ExpensesViewModel : ObservableObject, INavigationAware
{
    private readonly IExpenseService _expenses;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;
    private readonly IServiceProvider _services;
    private readonly DispatcherTimer _searchTimer;
    private readonly List<ExpenseRow> _all = [];
    private ExpenseRow? _selectedRow;
    private string _searchText = "";
    private bool _isBusy;
    private bool _navigated;

    public ExpensesViewModel(
        IExpenseService expenses,
        IDialogService dialogs,
        INavigationService navigation,
        IServiceProvider services)
    {
        _expenses = expenses;
        _dialogs = dialogs;
        _navigation = navigation;
        _services = services;
        _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _searchTimer.Tick += (_, _) =>
        {
            _searchTimer.Stop();
            ApplyFilter();
        };

        AddCommand = new AsyncRelayCommand(AddAsync);
        EditCommand = new AsyncRelayCommand(p => EditAsync(Row(p)));
        DeleteCommand = new AsyncRelayCommand(p => DeleteAsync(Row(p)));
        ClearSearchCommand = new RelayCommand(() => SearchText = "");

        _ = LoadAsync(showBusy: true);
    }

    public ObservableCollection<ExpenseRow> Items { get; } = [];
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ClearSearchCommand { get; }

    public ExpenseRow? SelectedRow
    {
        get => _selectedRow;
        set => SetProperty(ref _selectedRow, value);
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

    public bool HasActiveFilter => !string.IsNullOrWhiteSpace(SearchText);

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

    public void Refresh()
    {
        for (var i = 0; i < _all.Count; i++)
            _all[i] = _all[i] with
            {
                DateDisplay = _all[i].DateDisplay,
                TotalDisplay = _all[i].TotalDisplay
            };
        ApplyFilter();
    }

    public void OnNavigatedTo()
    {
        if (!_navigated)
        {
            _navigated = true;
            return;
        }

        _ = LoadAsync();
    }

    private ExpenseRow? Row(object? parameter) => parameter as ExpenseRow ?? SelectedRow;

    private async Task LoadAsync(bool showBusy = false)
    {
        if (showBusy)
            IsBusy = true;

        try
        {
            var entities = await _expenses.GetAllAsync();
            _all.Clear();
            foreach (var expense in entities)
                _all.Add(ToRow(expense));
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
        var selectedId = SelectedRow?.Id;
        Items.Clear();
        foreach (var row in _all)
        {
            if (query.Length == 0 ||
                row.SupplierName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                row.Reference.Contains(query, StringComparison.OrdinalIgnoreCase))
                Items.Add(row);
        }

        SelectedRow = selectedId is int id
            ? Items.FirstOrDefault(r => r.Id == id)
            : Items.FirstOrDefault();
        RaiseEmptyState();
    }

    private async Task AddAsync()
    {
        var editor = _services.GetRequiredService<ExpenseEditorViewModel>();
        await editor.LoadNewAsync();
        _navigation.NavigateTo(editor);
    }

    private async Task EditAsync(ExpenseRow? row)
    {
        if (row is null)
            return;

        var editor = _services.GetRequiredService<ExpenseEditorViewModel>();
        await editor.LoadExistingAsync(row.Id);
        _navigation.NavigateTo(editor);
    }

    private async Task DeleteAsync(ExpenseRow? row)
    {
        if (row is null)
            return;

        var confirm = await _dialogs.ShowAsync(new ConfirmDialogViewModel(
            "DeleteExpense".T(),
            "DeleteExpenseMessage".T(),
            "Delete".T(),
            danger: true));
        if (confirm != true)
            return;

        await _expenses.DeleteAsync(row.Id);
        await LoadAsync();
    }

    private static ExpenseRow ToRow(Expense expense) => new()
    {
        Id = expense.Id,
        SupplierName = expense.Supplier?.Name ?? "",
        DateDisplay = expense.Date.ToString("d", CultureInfo.CurrentCulture),
        Reference = expense.Reference,
        LineCount = expense.Lines.Count,
        LinesDisplay = expense.Lines.Count.ToString(CultureInfo.CurrentCulture),
        TotalDisplay = MoneyFormat.Display(expense.TotalAmount),
        IsMigrated = expense.Reference.StartsWith(LegacyTechnicsCopy.ReferencePrefix, StringComparison.OrdinalIgnoreCase)
    };

    private void RaiseEmptyState()
    {
        OnPropertyChanged(nameof(ShowGrid));
        OnPropertyChanged(nameof(ShowEmptyDatabase));
        OnPropertyChanged(nameof(ShowEmptyFilter));
        OnPropertyChanged(nameof(HasActiveFilter));
    }
}
