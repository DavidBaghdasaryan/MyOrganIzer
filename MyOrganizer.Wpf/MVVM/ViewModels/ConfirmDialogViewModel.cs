using System.Windows.Input;
using MyOrganizer.Wpf.MVVM.Infrastructure;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class ConfirmDialogViewModel : DialogViewModel
{
    public ConfirmDialogViewModel(string title, string message, string confirmText, bool danger = false)
    {
        Title = title;
        Message = message;
        ConfirmText = confirmText;
        IsDanger = danger;
        ConfirmCommand = new RelayCommand(() => Close(true));
        CancelCommand = new RelayCommand(() => Close(false));
    }

    public string Title { get; }
    public string Message { get; }
    public string ConfirmText { get; }
    public bool IsDanger { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }
}
