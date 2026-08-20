using System.Collections.ObjectModel;
using System.Windows.Input;
using MyOrganizer.Wpf.Config;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.Navigation;
using MyOrganizer.Wpf.Services;
using MyOrganizer.Wpf.Services.DB_LocalizationService;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class ShellViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly IDbLocalizationService _loc;
    private readonly EventHandler _dialogChanged;
    private object? _currentViewModel;
    private string _selectedLanguage = AppSettings.CurrentLang;
    private string _pageTitle = "";
    private int _reminderCount;
    private IReadOnlyList<ReminderItem> _reminderItems = [];
    private string _reminderText = "";

    public ShellViewModel(
        INavigationService navigation,
        IDialogService dialogs,
        IDbLocalizationService loc,
        IReminderService reminders)
    {
        _navigation = navigation;
        _dialogs = dialogs;
        _loc = loc;

        NavItems =
        [
            new NavItemViewModel(AppSection.Dashboard, "Dashboard", "\uE80F"),
            new NavItemViewModel(AppSection.Clients, "Clients", "\uE716"),
            new NavItemViewModel(AppSection.Procedures, "Procedures", "\uE8F1"),
            new NavItemViewModel(AppSection.Suppliers, "Suppliers", "\uE77B"),
            new NavItemViewModel(AppSection.Catalog, "ProductsAndServices", "\uE71D"),
            new NavItemViewModel(AppSection.Expenses, "Expenses", "\uE8C7"),
            new NavItemViewModel(AppSection.ToothLab, "ToothLab", "\uE9F9"),
            new NavItemViewModel(AppSection.Settings, "Settings", "\uE713")
        ];

        NavigateCommand = new RelayCommand(p =>
        {
            if (p is AppSection section)
                _navigation.Navigate(section);
        });
        ShowRemindersCommand = new RelayCommand(ShowReminders, () => HasReminders);

        _dialogChanged = (_, _) =>
        {
            OnPropertyChanged(nameof(DialogContent));
            OnPropertyChanged(nameof(IsDialogOpen));
        };

        _navigation.CurrentChanged += OnCurrentChanged;
        _dialogs.Changed += _dialogChanged;
        AppSettings.LanguageChanged += OnLanguageChanged;

        _navigation.Navigate(AppSection.Dashboard);

        _ = LoadRemindersAsync(reminders);
    }

    public ObservableCollection<NavItemViewModel> NavItems { get; }
    public ICommand NavigateCommand { get; }
    public ICommand ShowRemindersCommand { get; }

    public object? CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetProperty(ref _currentViewModel, value);
    }

    public object? DialogContent => _dialogs.Current;
    public bool IsDialogOpen => _dialogs.IsOpen;

    public string PageTitle
    {
        get => _pageTitle;
        private set => SetProperty(ref _pageTitle, value);
    }

    public string Brand => "AppTitle".T();

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || !SetProperty(ref _selectedLanguage, value))
                return;
            AppSettings.SetLanguage(value);
            _ = WarmLanguageAsync(value);
        }
    }

    public int ReminderCount
    {
        get => _reminderCount;
        private set
        {
            if (SetProperty(ref _reminderCount, value))
            {
                OnPropertyChanged(nameof(HasReminders));
                ((RelayCommand)ShowRemindersCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasReminders => ReminderCount > 0;

    public void Detach()
    {
        _navigation.CurrentChanged -= OnCurrentChanged;
        _dialogs.Changed -= _dialogChanged;
        AppSettings.LanguageChanged -= OnLanguageChanged;
    }

    private void OnCurrentChanged(object? sender, EventArgs e) => SyncFromNavigation();

    private void SyncFromNavigation()
    {
        CurrentViewModel = _navigation.Current;
        foreach (var item in NavItems)
            item.IsSelected = item.Section == _navigation.CurrentSection;
        PageTitle = TitleKey(_navigation.CurrentSection).T();
        if (CurrentViewModel is DashboardViewModel dashboard)
            dashboard.SetAppointments(_reminderItems);
    }

    private async Task WarmLanguageAsync(string lang)
    {
        try
        {
            await _loc.WarmUpAsync(lang);
        }
        catch
        {
            // Strings fall back to keys if the cache is cold.
        }
    }

    private void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Brand));
        foreach (var item in NavItems)
            item.Refresh();
        PageTitle = TitleKey(_navigation.CurrentSection).T();
        if (CurrentViewModel is DashboardViewModel dashboard)
            dashboard.Refresh();
        if (CurrentViewModel is PlaceholderViewModel placeholder)
            placeholder.Refresh();
        if (CurrentViewModel is ClientsViewModel clients)
            clients.Refresh();
        if (CurrentViewModel is ClientWorkspaceViewModel workspace)
            workspace.Refresh();
        if (CurrentViewModel is ProceduresViewModel procedures)
            procedures.Refresh();
        if (CurrentViewModel is PricesViewModel prices)
            prices.Refresh();
        if (CurrentViewModel is SuppliersViewModel suppliers)
            suppliers.Refresh();
        if (CurrentViewModel is SupplierWorkspaceViewModel supplierWorkspace)
            supplierWorkspace.Refresh();
        if (CurrentViewModel is CatalogItemsViewModel catalog)
            catalog.Refresh();
        if (CurrentViewModel is ExpensesViewModel expenses)
            expenses.Refresh();
        if (CurrentViewModel is TechniciansViewModel technicians)
            technicians.Refresh();
    }

    private async Task LoadRemindersAsync(IReminderService reminders)
    {
        try
        {
            var items = await reminders.LoadTodaysAsync();
            _reminderItems = items;
            ReminderCount = items.Count;
            var session = "session".T();
            _reminderText = string.Join(
                Environment.NewLine,
                items.OrderBy(i => i.When).Select(i => $"{i.FullName}  {i.When:HH:mm}  {session}"));
            if (CurrentViewModel is DashboardViewModel dashboard)
                dashboard.SetAppointments(items);
        }
        catch
        {
            ReminderCount = 0;
            _reminderText = "";
        }
    }

    private void ShowReminders()
    {
        if (!HasReminders)
            return;
        MVVM.UI.ModernDialog.Show(_reminderText, "Reminders".T());
    }

    private static string TitleKey(AppSection section) => section switch
    {
        AppSection.Clients => "Clients",
        AppSection.Procedures => "Procedures",
        AppSection.Suppliers => "Suppliers",
        AppSection.Catalog => "ProductsAndServices",
        AppSection.Expenses => "Expenses",
        AppSection.Technicians => "Technicians",
        AppSection.ToothLab => "ToothLab",
        AppSection.Settings => "SetPrices",
        _ => "Dashboard"
    };
}
