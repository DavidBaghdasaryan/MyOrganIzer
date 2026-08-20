using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Data.Entities;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.MVVM.UI;
using MyOrganizer.Wpf.Navigation;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class ClientsViewModel : ObservableObject, INavigationAware
{
    private const int DemoClientLimit = 10;

    private readonly AppDbContext _db;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private ClientRow? _selectedRow;
    private string _searchText = "";
    private ClientSearchField? _searchField;
    private DateTime? _month = DateTime.Today;
    private bool _filterByMonth;
    private bool _todayOnly;
    private bool _isBusy;
    private string _totalAmount = "";
    private int _unfilteredCount;
    private int _loadToken;
    private bool _navigated;

    public ClientsViewModel(AppDbContext db, INavigationService navigation, IDialogService dialogs)
    {
        _db = db;
        _navigation = navigation;
        _dialogs = dialogs;

        SearchFields =
        [
            new ClientSearchField("FirstName", "FirstName"),
            new ClientSearchField("LastName", "LastName"),
            new ClientSearchField("MidlName", "MiddlName"),
            new ClientSearchField("Phone", "Phone")
        ];
        _searchField = SearchFields[0];

        AddCommand = new AsyncRelayCommand(AddAsync);
        EditCommand = new AsyncRelayCommand(p => EditAsync(Row(p)));
        DeleteCommand = new AsyncRelayCommand(p => DeleteAsync(Row(p)));
        OpenCommand = new RelayCommand(p => Open(Row(p)));
        DentalChartCommand = new RelayCommand(p => OpenDentalChart(Row(p)));
        MonthIncomeCommand = new AsyncRelayCommand(LoadMonthIncomeAsync);
        MonthDebtCommand = new AsyncRelayCommand(LoadMonthDebtAsync);
        ClearFiltersCommand = new AsyncRelayCommand(ClearFiltersAsync);

        _ = LoadAsync(showBusy: true);
    }

    public ObservableCollection<ClientRow> Items { get; } = [];
    public IReadOnlyList<ClientSearchField> SearchFields { get; }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand DentalChartCommand { get; }
    public ICommand MonthIncomeCommand { get; }
    public ICommand MonthDebtCommand { get; }
    public ICommand ClearFiltersCommand { get; }

    public ClientRow? SelectedRow
    {
        get => _selectedRow;
        set
        {
            if (SetProperty(ref _selectedRow, value))
            {
                ((AsyncRelayCommand)EditCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)DeleteCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                _ = LoadAsync();
        }
    }

    public ClientSearchField? SearchField
    {
        get => _searchField;
        set
        {
            if (SetProperty(ref _searchField, value))
                _ = LoadAsync();
        }
    }

    public DateTime? Month
    {
        get => _month;
        set
        {
            if (SetProperty(ref _month, value ?? DateTime.Today) && FilterByMonth)
                _ = LoadAsync();
        }
    }

    public bool FilterByMonth
    {
        get => _filterByMonth;
        set
        {
            if (SetProperty(ref _filterByMonth, value))
                _ = LoadAsync();
        }
    }

    public bool TodayOnly
    {
        get => _todayOnly;
        set
        {
            if (SetProperty(ref _todayOnly, value))
                _ = LoadAsync();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                RaiseEmptyFlags();
        }
    }

    public string TotalAmount
    {
        get => _totalAmount;
        private set => SetProperty(ref _totalAmount, value);
    }

    public bool HasItems => Items.Count > 0;
    public bool ShowEmptyDatabase => !IsBusy && !HasActiveFilter && _unfilteredCount == 0;
    public bool ShowEmptyFilter => !IsBusy && HasActiveFilter && Items.Count == 0;
    public bool ShowGrid => !IsBusy && Items.Count > 0;
    public bool HasActiveFilter =>
        !string.IsNullOrWhiteSpace(SearchText) || FilterByMonth || TodayOnly;

    public void Refresh()
    {
        foreach (var field in SearchFields)
            field.Refresh();
        OnPropertyChanged(nameof(SearchFields));
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

    private ClientRow? Row(object? parameter) => parameter as ClientRow ?? SelectedRow;

    private async Task LoadAsync(bool showBusy = false)
    {
        var token = ++_loadToken;
        if (showBusy)
            IsBusy = true;

        try
        {
            var unfiltered = await _db.Clients.AsNoTracking().CountAsync();
            if (token != _loadToken)
                return;
            _unfilteredCount = unfiltered;

            IQueryable<Client> query = _db.Clients.AsNoTracking();
            var text = (SearchText ?? "").Trim();
            var prop = SearchField?.Property;

            if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrEmpty(prop))
            {
                query = prop switch
                {
                    "FirstName" => query.Where(c => c.FirstName != null && c.FirstName.Contains(text)),
                    "LastName" => query.Where(c => c.LastName != null && c.LastName.Contains(text)),
                    "MidlName" => query.Where(c => c.MidlName != null && c.MidlName.Contains(text)),
                    "Phone" => query.Where(c => c.PhoneNumber != null && c.PhoneNumber.Contains(text)),
                    _ => query
                };
            }

            if (FilterByMonth && Month is DateTime month)
                query = query.Where(c => c.DateJoin.Year == month.Year && c.DateJoin.Month == month.Month);

            if (TodayOnly)
            {
                var start = DateTime.Today;
                var end = start.AddDays(1);
                query = query.Where(c =>
                    (c.DateJoin >= start && c.DateJoin < end) ||
                    (c.DateDobleJoin != null && c.DateDobleJoin >= start && c.DateDobleJoin < end));
            }

            var list = await query
                .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
                .ToListAsync();
            if (token != _loadToken)
                return;

            var selectedId = SelectedRow?.Id;
            Items.Clear();
            foreach (var client in list)
                Items.Add(new ClientRow(client));
            SelectedRow = Items.FirstOrDefault(r => r.Id == selectedId) ?? Items.FirstOrDefault();
            RaiseEmptyFlags();
        }
        finally
        {
            if (showBusy && token == _loadToken)
                IsBusy = false;
        }
    }

    private void RaiseEmptyFlags()
    {
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(ShowEmptyDatabase));
        OnPropertyChanged(nameof(ShowEmptyFilter));
        OnPropertyChanged(nameof(ShowGrid));
        OnPropertyChanged(nameof(HasActiveFilter));
    }

    private async Task ClearFiltersAsync()
    {
        _searchText = "";
        _filterByMonth = false;
        _todayOnly = false;
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(FilterByMonth));
        OnPropertyChanged(nameof(TodayOnly));
        await LoadAsync();
    }

    private void Open(ClientRow? row)
    {
        if (row is null)
        {
            ModernDialog.Show("SelectClient".T(), "Info".T());
            return;
        }

        _navigation.NavigateToClient(row.Id);
    }

    private async Task AddAsync()
    {
        if (!await CheckClientLimitAsync())
            return;

        var editor = new EditClientDialogViewModel();
        if (await _dialogs.ShowAsync(editor) != true || editor.Result is null)
            return;

        await PersistClientAsync(editor.Result);
        await LoadAsync();
        SelectedRow = Items.FirstOrDefault(c => c.Id == editor.Result.Id);
    }

    private async Task EditAsync(ClientRow? row)
    {
        if (row is null)
        {
            ModernDialog.Show("SelectClient".T(), "Info".T());
            return;
        }

        var entity = await _db.Clients.AsNoTracking().FirstAsync(x => x.Id == row.Id);
        var editor = new EditClientDialogViewModel(entity);
        if (await _dialogs.ShowAsync(editor) != true || editor.Result is null)
            return;

        await PersistClientAsync(editor.Result);
        await LoadAsync();
        SelectedRow = Items.FirstOrDefault(c => c.Id == editor.Result.Id);
    }

    private async Task DeleteAsync(ClientRow? row)
    {
        if (row is null)
        {
            ModernDialog.Show("Selecttheclienttodelete".T(), "Info".T());
            return;
        }

        var confirm = await _dialogs.ShowAsync(new ConfirmDialogViewModel(
            "Confirm".T(),
            "Deletelient.".T(),
            "Delete".T(),
            danger: true));
        if (confirm != true)
            return;

        var entity = await _db.Clients.FirstAsync(x => x.Id == row.Id);
        _db.Clients.Remove(entity);
        await _db.SaveChangesAsync();
        Items.Remove(row);
        if (SelectedRow?.Id == row.Id)
            SelectedRow = Items.FirstOrDefault();
        _unfilteredCount = Math.Max(0, _unfilteredCount - 1);
        RaiseEmptyFlags();
    }

    private void OpenDentalChart(ClientRow? row)
    {
        if (row is null)
        {
            ModernDialog.Show("SelectClient".T(), "Info".T());
            return;
        }

        _navigation.NavigateToClient(row.Id, ClientWorkspaceTab.DentalChart);
    }

    private async Task PersistClientAsync(Client client)
    {
        if (client.Id == 0)
        {
            _db.Clients.Add(client);
        }
        else
        {
            var tracked = await _db.Clients.FirstOrDefaultAsync(x => x.Id == client.Id);
            if (tracked is null)
            {
                _db.Clients.Add(client);
            }
            else
            {
                tracked.FirstName = client.FirstName;
                tracked.LastName = client.LastName;
                tracked.MidlName = client.MidlName;
                tracked.PhoneNumber = client.PhoneNumber;
                tracked.Price = client.Price;
                tracked.Debet = client.Debet;
                tracked.DateJoin = client.DateJoin;
                tracked.DateDobleJoin = client.DateDobleJoin;
                tracked.DateJoinString = client.DateJoinString;
            }
        }

        await _db.SaveChangesAsync();
    }

    private async Task<bool> CheckClientLimitAsync()
    {
        var count = await _db.Clients.CountAsync();
        if (count >= DemoClientLimit)
        {
            ModernDialog.ShowWithLink(
                beforeText: $"Demo limit reached.\nYou can store up to {DemoClientLimit} clients.\n\nPlease ",
                linkText: "contact us by email",
                navigateUri: "mailto:myorganizer.dental@gmail.com?subject=Upgrade%20Request",
                afterText: " to unlock the full version.",
                caption: "Demo Limit",
                buttons: MessageBoxButton.OK,
                icon: MessageBoxImage.Information);
            return false;
        }

        if (count >= DemoClientLimit * 0.8)
        {
            ModernDialog.Show(
                "You’re nearing the demo limit (80%).\nUpgrade anytime to keep adding clients.",
                "Demo Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        return true;
    }

    private async Task LoadMonthIncomeAsync()
    {
        if (Month is not DateTime d)
            return;
        var sum = await _db.Clients
            .Where(c => c.DateJoin.Year == d.Year && c.DateJoin.Month == d.Month)
            .SumAsync(c => c.Price ?? 0m);
        TotalAmount = sum.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private async Task LoadMonthDebtAsync()
    {
        if (Month is not DateTime d)
            return;
        var sum = await _db.Clients
            .Where(c => c.DateJoin.Year == d.Year && c.DateJoin.Month == d.Month)
            .SumAsync(c => c.Debet ?? 0m);
        TotalAmount = sum.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public sealed class ClientSearchField(string property, string locKey) : ObservableObject
    {
        public string Property { get; } = property;
        public string Label => locKey.T();
        public void Refresh() => OnPropertyChanged(nameof(Label));
        public override string ToString() => Label;
    }
}
