using System.Windows;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.MVVM.UI;

namespace MyOrganizer.Wpf.Navigation;

public sealed class DialogService : IDialogService
{
    private TaskCompletionSource<bool?>? _pending;
    private AppDialogWindow? _window;

    public object? Current { get; private set; }
    public bool IsOpen => Current is not null;

    public event EventHandler? Changed;

    public void Dismiss()
    {
        if (Current is null && _pending is null)
            return;
        Complete(false);
    }

    public async Task<bool?> ShowAsync(object viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        if (_pending is not null)
            Complete(false);

        Current = viewModel;
        _pending = new TaskCompletionSource<bool?>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (viewModel is IDialogRequestClose closable)
            closable.CloseRequested += OnCloseRequested;

        if (ShouldUseShell())
        {
            Changed?.Invoke(this, EventArgs.Empty);
            return await _pending.Task;
        }

        var owner = ActiveWindow();
        _window = new AppDialogWindow
        {
            Owner = owner,
            DataContext = viewModel,
            WindowStartupLocation = owner is null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner
        };
        _window.Closed += OnFallbackClosed;
        _window.Show();
        return await _pending.Task;
    }

    private void OnCloseRequested(object? sender, bool? result) => Complete(result);

    private void OnFallbackClosed(object? sender, EventArgs e) => Complete(false);

    private void Complete(bool? result)
    {
        if (Current is IDialogRequestClose closable)
            closable.CloseRequested -= OnCloseRequested;

        Current = null;
        Changed?.Invoke(this, EventArgs.Empty);

        if (_window is not null)
        {
            var window = _window;
            _window = null;
            window.Closed -= OnFallbackClosed;
            if (window.IsVisible)
                window.Close();
        }

        var pending = _pending;
        _pending = null;
        pending?.TrySetResult(result);
    }

    private static bool ShouldUseShell()
    {
        if (Application.Current?.MainWindow is not MainWindow)
            return false;

        var active = ActiveWindow();
        return active is null or MainWindow;
    }

    private static Window? ActiveWindow() =>
        Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        ?? Application.Current?.MainWindow;
}
