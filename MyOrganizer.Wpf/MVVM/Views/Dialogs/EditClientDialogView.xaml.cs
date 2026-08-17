using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyOrganizer.Wpf.MVVM.Views.Dialogs;

public partial class EditClientDialogView : UserControl
{
    public EditClientDialogView() => InitializeComponent();

    private void View_Loaded(object sender, RoutedEventArgs e) =>
        Keyboard.Focus(FirstNameBox);
}
