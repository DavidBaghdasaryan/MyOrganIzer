namespace MyOrganizer.Wpf.Navigation;

public interface IDialogService
{
    object? Current { get; }
    bool IsOpen { get; }

    event EventHandler? Changed;

    Task<bool?> ShowAsync(object viewModel);
    void Dismiss();
}
