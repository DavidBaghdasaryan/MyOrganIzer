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

public sealed record SupplierRow
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required string Notes { get; init; }
}

public sealed class SuppliersViewModel : ObservableObject, INavigationAware
{
    private readonly ISupplierService _suppliers;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;
    private readonly IServiceProvider _services;
    private readonly DispatcherTimer _searchTimer;
    private readonly List<SupplierRow> _all = [];
    private SupplierRow? _selectedRow;
    private string _searchText = "";
    private bool _isBusy;
    private bool _navigated;

    public SuppliersViewModel(
        ISupplierService suppliers,
        IDialogService dialogs,
        INavigationService navigation,
        IServiceProvider services)
    {
        _suppliers = suppliers;
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
        OpenCommand = new AsyncRelayCommand(p => OpenAsync(Row(p)));
        ClearSearchCommand = new RelayCommand(() => SearchText = "");

        _ = LoadAsync(showBusy: true);
    }

    public ObservableCollection<SupplierRow> Items { get; } = [];
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand ClearSearchCommand { get; }

    public SupplierRow? SelectedRow
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

    public void Refresh() => ApplyFilter();

    public void OnNavigatedTo()
    {
        if (!_navigated)
        {
            _navigated = true;
            return;
        }

        _ = LoadAsync();
    }

    private SupplierRow? Row(object? parameter) => parameter as SupplierRow ?? SelectedRow;

    private async Task LoadAsync(bool showBusy = false)
    {
        if (showBusy)
            IsBusy = true;

        try
        {
            var entities = await _suppliers.GetAllAsync();
            _all.Clear();
            foreach (var supplier in entities)
                _all.Add(ToRow(supplier));
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
                row.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                row.Email.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                row.Phone.Contains(query, StringComparison.OrdinalIgnoreCase))
                Items.Add(row);
        }

        SelectedRow = selectedId is int id
            ? Items.FirstOrDefault(r => r.Id == id)
            : Items.FirstOrDefault();
        RaiseEmptyState();
    }

    private async Task AddAsync()
    {
        var editor = new EditSupplierDialogViewModel();
        if (await _dialogs.ShowAsync(editor) != true || editor.Result is null)
            return;

        var created = await _suppliers.AddAsync(
            editor.Result.Name, editor.Result.Email, editor.Result.Phone, editor.Result.Notes);
        await LoadAsync();
        SelectedRow = Items.FirstOrDefault(r => r.Id == created.Id);
    }

    private async Task EditAsync(SupplierRow? row)
    {
        if (row is null)
            return;

        var entity = await _suppliers.GetByIdAsync(row.Id);
        if (entity is null)
            return;

        var editor = new EditSupplierDialogViewModel(entity);
        if (await _dialogs.ShowAsync(editor) != true || editor.Result is null)
            return;

        await _suppliers.UpdateAsync(
            row.Id, editor.Result.Name, editor.Result.Email, editor.Result.Phone, editor.Result.Notes);
        await LoadAsync();
        SelectedRow = Items.FirstOrDefault(r => r.Id == row.Id);
    }

    private async Task DeleteAsync(SupplierRow? row)
    {
        if (row is null)
            return;

        var confirm = await _dialogs.ShowAsync(new ConfirmDialogViewModel(
            "DeleteSupplier".T(),
            string.Format(CultureInfo.CurrentCulture, "DeleteSupplierMessage".T(), row.Name),
            "Delete".T(),
            danger: true));
        if (confirm != true)
            return;

        await _suppliers.DeactivateAsync(row.Id);
        await LoadAsync();
    }

    private async Task OpenAsync(SupplierRow? row)
    {
        if (row is null)
            return;

        var workspace = _services.GetRequiredService<SupplierWorkspaceViewModel>();
        await workspace.LoadAsync(row.Id);
        _navigation.NavigateTo(workspace);
    }

    private static SupplierRow ToRow(Supplier supplier) => new()
    {
        Id = supplier.Id,
        Name = supplier.Name,
        Email = supplier.Email,
        Phone = supplier.Phone,
        Notes = supplier.Notes
    };

    private void RaiseEmptyState()
    {
        OnPropertyChanged(nameof(ShowGrid));
        OnPropertyChanged(nameof(ShowEmptyDatabase));
        OnPropertyChanged(nameof(ShowEmptyFilter));
        OnPropertyChanged(nameof(HasActiveFilter));
    }
}
