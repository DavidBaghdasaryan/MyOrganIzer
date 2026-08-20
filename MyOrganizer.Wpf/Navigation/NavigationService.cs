using Microsoft.Extensions.DependencyInjection;
using MyOrganizer.Wpf.MVVM.ViewModels;

namespace MyOrganizer.Wpf.Navigation;

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;
    private readonly ILegacyWindowService _legacy;
    private readonly IDialogService _dialogs;
    private readonly Stack<object> _back = new();
    private object? _current;
    private AppSection _section = AppSection.Dashboard;

    public NavigationService(IServiceProvider services, ILegacyWindowService legacy, IDialogService dialogs)
    {
        _services = services;
        _legacy = legacy;
        _dialogs = dialogs;
    }

    public object? Current => _current;
    public AppSection CurrentSection => _section;
    public bool CanGoBack => _back.Count > 0;

    public event EventHandler? CurrentChanged;

    public void Navigate(AppSection section)
    {
        _dialogs.Dismiss();

        if (section == _section && _current is not null && _back.Count == 0)
            return;

        if (section == _section && _back.Count > 0)
        {
            object restored = _current!;
            while (_back.Count > 0)
                restored = _back.Pop();
            SetCurrent(restored);
            return;
        }

        _back.Clear();
        _section = section;
        SetCurrent(CreateRoot(section));
    }

    public void NavigateTo(object viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        if (_current is not null)
            _back.Push(_current);
        SetCurrent(viewModel);
    }

    public void NavigateToClient(int clientId, ClientWorkspaceTab tab = ClientWorkspaceTab.Overview)
    {
        if (_current is ClientWorkspaceViewModel open && open.ClientId == clientId)
        {
            open.SelectedTab = tab;
            return;
        }

        var workspace = _services.GetRequiredService<ClientWorkspaceViewModel>();
        _ = workspace.LoadAsync(clientId, tab);
        NavigateTo(workspace);
    }

    public void GoBack()
    {
        if (_back.Count == 0)
        {
            Navigate(AppSection.Dashboard);
            return;
        }

        SetCurrent(_back.Pop());
    }

    private object CreateRoot(AppSection section) => section switch
    {
        AppSection.Dashboard => _services.GetRequiredService<DashboardViewModel>(),
        AppSection.Clients => _services.GetRequiredService<ClientsViewModel>(),
        AppSection.Procedures => _services.GetRequiredService<ProceduresViewModel>(),
        AppSection.Suppliers => _services.GetRequiredService<SuppliersViewModel>(),
        AppSection.Catalog => _services.GetRequiredService<CatalogItemsViewModel>(),
        AppSection.Expenses => _services.GetRequiredService<ExpensesViewModel>(),
        AppSection.Technicians => _services.GetRequiredService<TechniciansViewModel>(),
        AppSection.ToothLab => _services.GetRequiredService<ToothLabViewModel>(),
        AppSection.Settings => _services.GetRequiredService<PricesViewModel>(),
        _ => new PlaceholderViewModel(section, _legacy)
    };

    private void SetCurrent(object viewModel)
    {
        _current = viewModel;
        CurrentChanged?.Invoke(this, EventArgs.Empty);
        if (viewModel is INavigationAware aware)
            aware.OnNavigatedTo();
    }
}
