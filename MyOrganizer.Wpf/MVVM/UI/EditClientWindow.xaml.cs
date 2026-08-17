using System.Windows;
using MyOrganizer.Wpf.Data.Entities;
using MyOrganizer.Wpf.MVVM.ViewModels;

namespace MyOrganizer.Wpf.MVVM.UI;

public partial class EditClientWindow : Window
{
    private readonly EditClientDialogViewModel _editor;

    public EditClientWindow() : this(null)
    {
    }

    public EditClientWindow(Client? client)
    {
        _editor = new EditClientDialogViewModel(client);
        InitializeComponent();
        PartView.DataContext = _editor;
        _editor.CloseRequested += (_, result) =>
        {
            DialogResult = result == true;
        };
    }

    public Client Model => _editor.Result ?? _editor.ToClient();
}
