using MyOrganizer.Wpf.MVVM.ViewModels;

namespace MyOrganizer.Wpf.Navigation;

public interface INavigationService
{
    object? Current { get; }
    AppSection CurrentSection { get; }
    bool CanGoBack { get; }

    event EventHandler? CurrentChanged;

    void Navigate(AppSection section);
    void NavigateTo(object viewModel);
    void NavigateToClient(int clientId, ClientWorkspaceTab tab = ClientWorkspaceTab.Overview);
    void GoBack();
}
