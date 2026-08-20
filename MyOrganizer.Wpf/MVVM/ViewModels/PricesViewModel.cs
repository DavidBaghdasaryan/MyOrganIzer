using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using MyOrganizer.Wpf.Entities.Procedures;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.MVVM.UI;
using MyOrganizer.Wpf.Navigation;
using MyOrganizer.Wpf.Services;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class PriceRow : ObservableObject
{
    private readonly PricesViewModel _owner;
    private string _tier1;
    private string _tier2;
    private string _tier3;

    public PriceRow(PricesViewModel owner, int procedureId, string name, decimal tier1, decimal tier2, decimal tier3, string currency)
    {
        _owner = owner;
        ProcedureId = procedureId;
        Name = name;
        Currency = currency;
        _tier1 = Format(tier1);
        _tier2 = Format(tier2);
        _tier3 = Format(tier3);
    }

    public int ProcedureId { get; }
    public string Name { get; }
    public string Currency { get; }

    public string Tier1
    {
        get => _tier1;
        set
        {
            if (SetProperty(ref _tier1, value))
                _owner.MarkDirty();
        }
    }

    public string Tier2
    {
        get => _tier2;
        set
        {
            if (SetProperty(ref _tier2, value))
                _owner.MarkDirty();
        }
    }

    public string Tier3
    {
        get => _tier3;
        set
        {
            if (SetProperty(ref _tier3, value))
                _owner.MarkDirty();
        }
    }

    public bool TryRead(out decimal t1, out decimal t2, out decimal t3)
    {
        t1 = t2 = t3 = 0;
        return TryParse(Tier1, out t1) && TryParse(Tier2, out t2) && TryParse(Tier3, out t3)
            && t1 >= 0 && t2 >= 0 && t3 >= 0;
    }

    private static string Format(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);

    private static bool TryParse(string text, out decimal value)
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

public sealed class PricesViewModel : ObservableObject, INavigationAware
{
    private readonly IProcedureService _procedures;
    private readonly IDialogService _dialogs;
    private readonly DispatcherTimer _searchTimer;
    private readonly List<PriceRow> _all = [];
    private PriceRow? _selectedRow;
    private string _searchText = "";
    private bool _isBusy;
    private bool _isDirty;
    private string? _error;
    private bool _navigated;

    public PricesViewModel(IProcedureService procedures, IDialogService dialogs)
    {
        _procedures = procedures;
        _dialogs = dialogs;
        _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _searchTimer.Tick += (_, _) =>
        {
            _searchTimer.Stop();
            ApplyFilter();
        };

        AddCommand = new AsyncRelayCommand(AddAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => IsDirty && !IsBusy);
        DeleteCommand = new AsyncRelayCommand(p => DeleteAsync(Row(p)));
        ClearSearchCommand = new RelayCommand(() => SearchText = "");
        _ = LoadAsync(showBusy: true);
    }

    public ObservableCollection<PriceRow> Items { get; } = [];
    public ICommand AddCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ClearSearchCommand { get; }

    public PriceRow? SelectedRow
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
            {
                RaiseEmptyState();
                ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (SetProperty(ref _isDirty, value))
                ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }
    }

    public string? Error
    {
        get => _error;
        private set
        {
            if (SetProperty(ref _error, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(Error);
    public bool ShowGrid => !IsBusy && Items.Count > 0;
    public bool ShowEmptyDatabase => !IsBusy && _all.Count == 0;
    public bool ShowEmptyFilter => !IsBusy && _all.Count > 0 && Items.Count == 0;

    public void MarkDirty()
    {
        Error = null;
        IsDirty = true;
    }

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

    private PriceRow? Row(object? parameter) => parameter as PriceRow ?? SelectedRow;

    private async Task LoadAsync(bool showBusy = false)
    {
        if (showBusy)
            IsBusy = true;
        Error = null;
        try
        {
            var entities = await _procedures.GetAllWithPricesAsync();
            _all.Clear();
            foreach (var procedure in entities)
                _all.Add(ToRow(procedure));
            ApplyFilter();
            IsDirty = false;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilter()
    {
        var query = SearchText.Trim();
        var selectedId = SelectedRow?.ProcedureId;
        Items.Clear();
        foreach (var row in _all)
        {
            if (query.Length == 0 || row.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                Items.Add(row);
        }

        SelectedRow = selectedId is int id
            ? Items.FirstOrDefault(r => r.ProcedureId == id)
            : Items.FirstOrDefault();
        RaiseEmptyState();
    }

    private async Task AddAsync()
    {
        var editor = new EditProcedureDialogViewModel();
        if (await _dialogs.ShowAsync(editor) != true || editor.Result is null)
            return;

        var created = await _procedures.AddAsync(editor.Result.Name);
        await _procedures.UpsertPricesAsync(
        [
            (created.Id, editor.Result.Tier1, editor.Result.Tier2, editor.Result.Tier3, editor.Result.Currency)
        ]);
        await LoadAsync();
        SelectedRow = Items.FirstOrDefault(r => r.ProcedureId == created.Id);
    }

    private async Task SaveAsync()
    {
        Error = null;
        var drafts = new List<(int procedureId, decimal t1, decimal t2, decimal t3, string currency)>();
        foreach (var row in _all)
        {
            if (!row.TryRead(out var t1, out var t2, out var t3))
            {
                Error = "FieldRequired".T();
                return;
            }

            drafts.Add((row.ProcedureId, t1, t2, t3, row.Currency));
        }

        try
        {
            await _procedures.UpsertPricesAsync(drafts);
            IsDirty = false;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            ModernDialog.Show(ex.Message, "Error".T());
        }
    }

    private async Task DeleteAsync(PriceRow? row)
    {
        if (row is null)
            return;

        var confirm = await _dialogs.ShowAsync(new ConfirmDialogViewModel(
            "DeleteProcedure".T(),
            string.Format(CultureInfo.CurrentCulture, "DeleteProcedureMessage".T(), row.Name),
            "Delete".T(),
            danger: true));
        if (confirm != true)
            return;

        await _procedures.DeleteAsync(row.ProcedureId);
        await LoadAsync();
    }

    private PriceRow ToRow(Procedure procedure)
    {
        var price = procedure.Prices?.OrderByDescending(p => p.Id).FirstOrDefault();
        return new PriceRow(
            this,
            procedure.Id,
            procedure.Name,
            price?.Tier1 ?? 0,
            price?.Tier2 ?? 0,
            price?.Tier3 ?? 0,
            price?.Currency ?? "AMD");
    }

    private void RaiseEmptyState()
    {
        OnPropertyChanged(nameof(ShowGrid));
        OnPropertyChanged(nameof(ShowEmptyDatabase));
        OnPropertyChanged(nameof(ShowEmptyFilter));
        OnPropertyChanged(nameof(HasActiveFilter));
    }
}
