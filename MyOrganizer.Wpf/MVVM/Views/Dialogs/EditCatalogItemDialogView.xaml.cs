using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyOrganizer.Wpf.MVVM.Views.Dialogs;

public partial class EditCatalogItemDialogView : UserControl
{
    public EditCatalogItemDialogView() => InitializeComponent();

    private void View_Loaded(object sender, RoutedEventArgs e) => Keyboard.Focus(NameBox);
}
