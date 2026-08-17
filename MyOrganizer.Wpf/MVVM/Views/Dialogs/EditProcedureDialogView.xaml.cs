using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyOrganizer.Wpf.MVVM.Views.Dialogs;

public partial class EditProcedureDialogView : UserControl
{
    public EditProcedureDialogView() => InitializeComponent();

    private void View_Loaded(object sender, RoutedEventArgs e) =>
        Keyboard.Focus(NameBox);
}
