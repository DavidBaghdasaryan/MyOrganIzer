using System.Windows.Input;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.Navigation;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class PlaceholderViewModel : ObservableObject
{
    private readonly ILegacyWindowService _legacy;

    public PlaceholderViewModel(AppSection section, ILegacyWindowService legacy)
    {
        Section = section;
        _legacy = legacy;
        OpenLegacyCommand = new RelayCommand(() => _legacy.Open(section), () => CanOpenLegacy);
    }

    public AppSection Section { get; }
    public ICommand OpenLegacyCommand { get; }

    public string Title => TitleKey.T();
    public string Message => "ComingSoon".T();
    public string ActionLabel => ActionKey.T();
    public bool CanOpenLegacy => Section is not AppSection.Dashboard;

    public void Refresh()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(ActionLabel));
    }

    private string TitleKey => Section switch
    {
        AppSection.Clients => "Clients",
        AppSection.Procedures => "Procedures",
        AppSection.Suppliers => "Suppliers",
        AppSection.Catalog => "ProductsAndServices",
        AppSection.Expenses => "Expenses",
        AppSection.Technicians => "Technicians",
        AppSection.Settings => "Settings",
        _ => "Dashboard"
    };

    private string ActionKey => Section == AppSection.Settings ? "SetPrices" : "OpenExistingWindow";
}
