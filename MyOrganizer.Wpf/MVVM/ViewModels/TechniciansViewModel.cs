using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Entities;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.MVVM.UI;
using MyOrganizer.Wpf.Navigation;
using MyOrganizer.Wpf.Services;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed record TechnicianRow
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required int Price { get; init; }
    public required DateTime Date { get; init; }
    public required string PriceDisplay { get; init; }
    public required string DateDisplay { get; init; }
}

public sealed class TechniciansViewModel : ObservableObject, INavigationAware
{
    private readonly AppDbContext _db;
    private readonly IProcedureService _procedures;
    private readonly IDialogService _dialogs;
    private readonly DispatcherTimer _searchTimer;
    private readonly List<TechnicianRow> _all = [];
    private TechnicianRow? _selectedRow;
    private string _searchText = "";
    private string? _materialFilter;
    private DateTime? _month = DateTime.Today;
    private string _monthlyTotal = "";
    private bool _isBusy;
    private bool _navigated;

    public TechniciansViewModel(AppDbContext db, IProcedureService procedures, IDialogService dialogs)
    {
        _db = db;
        _procedures = procedures;
        _dialogs = dialogs;
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
        ViewAmountCommand = new AsyncRelayCommand(ViewAmountAsync);

        _ = LoadAsync(showBusy: true);
    }

    public ObservableCollection<TechnicianRow> Items { get; } = [];
    public ObservableCollection<string> Materials { get; } = [];

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand ViewAmountCommand { get; }

    public TechnicianRow? SelectedRow
    {
        get => _selectedRow;
        set
        {
            if (!SetProperty(ref _selectedRow, value) || value is null)
                return;
            if (string.IsNullOrWhiteSpace(MaterialFilter))
                MaterialFilter = value.Type;
        }
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

    public string? MaterialFilter
    {
        get => _materialFilter;
        set => SetProperty(ref _materialFilter, value);
    }

    public DateTime? Month
    {
        get => _month;
        set => SetProperty(ref _month, value ?? DateTime.Today);
    }

    public string MonthlyTotal
    {
        get => _monthlyTotal;
        private set => SetProperty(ref _monthlyTotal, value);
    }

    public bool HasActiveFilter => !string.IsNullOrWhiteSpace(SearchText);
    public bool ShowGrid => !IsBusy && Items.Count > 0;
    public bool ShowEmptyDatabase => !IsBusy && _all.Count == 0;
    public bool ShowEmptyFilter => !IsBusy && _all.Count > 0 && Items.Count == 0;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                RaiseEmptyState();
        }
    }

    public void Refresh()
    {
        for (var i = 0; i < _all.Count; i++)
            _all[i] = WithDisplay(_all[i]);
        ApplyFilter();
        OnPropertyChanged(nameof(MonthlyTotal));
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

    private TechnicianRow? Row(object? parameter) => parameter as TechnicianRow ?? SelectedRow;

    private async Task LoadAsync(bool showBusy = false)
    {
        if (showBusy)
            IsBusy = true;

        try
        {
            var materials = await _procedures.GetAllAsync();
            Materials.Clear();
            foreach (var name in materials.Select(p => p.Name))
                Materials.Add(name);

            var rows = await _db.Technics.AsNoTracking().OrderByDescending(t => t.Date).ThenBy(t => t.Name).ToListAsync();
            _all.Clear();
            foreach (var entity in rows)
                _all.Add(ToRow(entity));
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
            if (query.Length == 0
                || row.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || row.Type.Contains(query, StringComparison.OrdinalIgnoreCase))
                Items.Add(row);
        }

        SelectedRow = selectedId is int id
            ? Items.FirstOrDefault(r => r.Id == id)
            : Items.FirstOrDefault();
        RaiseEmptyState();
    }

    private async Task AddAsync()
    {
        var editor = new EditTechnicianDialogViewModel([.. Materials]);
        if (await _dialogs.ShowAsync(editor) != true || editor.Result is null)
            return;

        _db.Technics.Add(ToEntity(editor.Result));
        await _db.SaveChangesAsync();
        await LoadAsync();
    }

    private async Task EditAsync(TechnicianRow? row)
    {
        if (row is null)
            return;

        var entity = await _db.Technics.AsNoTracking().FirstOrDefaultAsync(x => x.Id == row.Id);
        if (entity is null)
        {
            await LoadAsync();
            return;
        }

        var editor = new EditTechnicianDialogViewModel([.. Materials], entity);
        if (await _dialogs.ShowAsync(editor) != true || editor.Result is null)
            return;

        var tracked = await _db.Technics.FirstOrDefaultAsync(x => x.Id == row.Id);
        if (tracked is null)
            return;

        tracked.Name = editor.Result.Name;
        tracked.Type = editor.Result.Type;
        tracked.Price = editor.Result.Price;
        tracked.Date = editor.Result.Date;
        await _db.SaveChangesAsync();
        await LoadAsync();
        SelectedRow = Items.FirstOrDefault(r => r.Id == row.Id);
    }

    private async Task DeleteAsync(TechnicianRow? row)
    {
        if (row is null)
            return;

        var label = string.IsNullOrWhiteSpace(row.Name) ? row.Type : row.Name;
        var confirm = await _dialogs.ShowAsync(new ConfirmDialogViewModel(
            "DeleteTechnician".T(),
            string.Format(CultureInfo.CurrentCulture, "DeleteTechnicianMessage".T(), label),
            "Delete".T(),
            danger: true));
        if (confirm != true)
            return;

        var entity = await _db.Technics.FirstOrDefaultAsync(x => x.Id == row.Id);
        if (entity is not null)
        {
            _db.Technics.Remove(entity);
            await _db.SaveChangesAsync();
        }

        await LoadAsync();
    }

    private async Task ViewAmountAsync()
    {
        var type = MaterialFilter;
        if (string.IsNullOrWhiteSpace(type))
        {
            type = SelectedRow?.Type;
            MaterialFilter = type;
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            MonthlyTotal = "";
            ModernDialog.Show("Materialnotspecified".T(), "ViewAmount".T());
            return;
        }

        var date = Month ?? DateTime.Today;
        var sum = await _db.Technics
            .Where(t => t.Type == type && t.Date.Year == date.Year && t.Date.Month == date.Month)
            .SumAsync(t => (int?)t.Price) ?? 0;
        MonthlyTotal = string.Format(CultureInfo.CurrentCulture, "{0:N0} {1}", sum, "Currency".T());
    }

    private static Technic ToEntity(TechnicianEditResult result) => new()
    {
        Name = result.Name,
        Type = result.Type,
        Price = result.Price,
        Date = result.Date
    };

    private static TechnicianRow ToRow(Technic entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name ?? "",
        Type = entity.Type ?? "",
        Price = entity.Price,
        Date = entity.Date,
        PriceDisplay = FormatPrice(entity.Price),
        DateDisplay = entity.Date.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture)
    };

    private static TechnicianRow WithDisplay(TechnicianRow row) =>
        row with
        {
            PriceDisplay = FormatPrice(row.Price),
            DateDisplay = row.Date.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture)
        };

    private static string FormatPrice(int price) =>
        string.Format(CultureInfo.CurrentCulture, "{0:N0} {1}", price, "Currency".T());

    private void RaiseEmptyState()
    {
        OnPropertyChanged(nameof(ShowGrid));
        OnPropertyChanged(nameof(ShowEmptyDatabase));
        OnPropertyChanged(nameof(ShowEmptyFilter));
        OnPropertyChanged(nameof(HasActiveFilter));
    }
}
