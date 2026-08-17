using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Data.Entities;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.MVVM.UI;
using MyOrganizer.Wpf.Navigation;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public enum ClientWorkspaceTab
{
    Overview,
    DentalChart
}

public sealed class ClientWorkspaceViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private readonly INavigationService _navigation;
    private readonly IServiceProvider _services;
    private readonly IDialogService _dialogs;
    private ClientWorkspaceTab _tab = ClientWorkspaceTab.Overview;
    private ClientRow? _row;
    private DentalChartViewModel? _chart;
    private bool _isBusy = true;
    private bool _notFound;
    private string? _error;

    public ClientWorkspaceViewModel(
        AppDbContext db,
        INavigationService navigation,
        IServiceProvider services,
        IDialogService dialogs)
    {
        _db = db;
        _navigation = navigation;
        _services = services;
        _dialogs = dialogs;
        BackCommand = new RelayCommand(_navigation.GoBack);
        SelectTabCommand = new RelayCommand(p =>
        {
            if (p is ClientWorkspaceTab tab)
                SelectedTab = tab;
        });
        EditCommand = new AsyncRelayCommand(EditAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync);
        DentalChartCommand = new RelayCommand(() => SelectedTab = ClientWorkspaceTab.DentalChart);
    }

    public int ClientId { get; private set; }
    public ClientRow? Row
    {
        get => _row;
        private set
        {
            if (SetProperty(ref _row, value))
                OnPropertyChanged(nameof(Title));
        }
    }

    public DentalChartViewModel? Chart
    {
        get => _chart;
        private set => SetProperty(ref _chart, value);
    }

    public string Title => Row?.FullName ?? "Clients".T();
    public string BackLabel => "Clients".T();

    public ClientWorkspaceTab SelectedTab
    {
        get => _tab;
        set
        {
            if (!SetProperty(ref _tab, value))
            {
                if (value == ClientWorkspaceTab.DentalChart)
                    EnsureChart();
                return;
            }

            OnPropertyChanged(nameof(IsOverview));
            OnPropertyChanged(nameof(IsDentalChart));
            if (value == ClientWorkspaceTab.DentalChart)
                EnsureChart();
        }
    }

    public bool IsOverview => SelectedTab == ClientWorkspaceTab.Overview;
    public bool IsDentalChart => SelectedTab == ClientWorkspaceTab.DentalChart;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                OnPropertyChanged(nameof(ShowContent));
        }
    }

    public bool NotFound
    {
        get => _notFound;
        private set
        {
            if (SetProperty(ref _notFound, value))
                OnPropertyChanged(nameof(ShowContent));
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

    public bool HasError => !string.IsNullOrEmpty(Error);
    public bool ShowContent => !IsBusy && !NotFound && Row is not null;

    public ICommand BackCommand { get; }
    public ICommand SelectTabCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand DentalChartCommand { get; }

    public async Task LoadAsync(int clientId, ClientWorkspaceTab tab = ClientWorkspaceTab.Overview)
    {
        ClientId = clientId;
        if (_tab != tab)
        {
            _tab = tab;
            OnPropertyChanged(nameof(SelectedTab));
            OnPropertyChanged(nameof(IsOverview));
            OnPropertyChanged(nameof(IsDentalChart));
        }

        IsBusy = true;
        NotFound = false;
        Error = null;
        try
        {
            var client = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clientId);
            if (client is null)
            {
                Row = null;
                NotFound = true;
                return;
            }

            Row = new ClientRow(client);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            ModernDialog.Show(ex.Message, "Error".T(), MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }

        if (ShowContent && SelectedTab == ClientWorkspaceTab.DentalChart)
            EnsureChart();
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(BackLabel));
    }

    private async Task EditAsync()
    {
        if (Row is null)
            return;

        var entity = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == ClientId);
        if (entity is null)
        {
            NotFound = true;
            Row = null;
            return;
        }

        var editor = new EditClientDialogViewModel(entity);
        if (await _dialogs.ShowAsync(editor) != true || editor.Result is null)
            return;

        await PersistClientAsync(editor.Result);
        await LoadAsync(ClientId, SelectedTab);
        if (Chart is not null)
            await Chart.InitializeAsync(ClientId);
    }

    private async Task DeleteAsync()
    {
        if (Row is null)
            return;

        var confirm = await _dialogs.ShowAsync(new ConfirmDialogViewModel(
            "Confirm".T(),
            "Deletelient.".T(),
            "Delete".T(),
            danger: true));
        if (confirm != true)
            return;

        var entity = await _db.Clients.FirstOrDefaultAsync(x => x.Id == ClientId);
        if (entity is null)
        {
            _navigation.GoBack();
            return;
        }

        _db.Clients.Remove(entity);
        await _db.SaveChangesAsync();
        _navigation.GoBack();
    }

    private void EnsureChart()
    {
        if (Chart is not null || ClientId <= 0)
            return;

        Chart = _services.GetRequiredService<DentalChartViewModel>();
        _ = Chart.InitializeAsync(ClientId);
    }

    private async Task PersistClientAsync(Client client)
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

        await _db.SaveChangesAsync();
    }
}
