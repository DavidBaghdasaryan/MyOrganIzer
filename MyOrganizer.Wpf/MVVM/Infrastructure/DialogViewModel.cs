namespace MyOrganizer.Wpf.MVVM.Infrastructure;

public interface IDialogRequestClose
{
    event EventHandler<bool?>? CloseRequested;
}

public abstract class DialogViewModel : ObservableObject, IDialogRequestClose
{
    public event EventHandler<bool?>? CloseRequested;

    protected void Close(bool? result) => CloseRequested?.Invoke(this, result);
}
