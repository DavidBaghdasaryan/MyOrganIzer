using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.ViewModels;

namespace MyOrganizer.Wpf.MVVM.Views.Suppliers;

public partial class SuppliersView : UserControl
{
    public SuppliersView() => InitializeComponent();

    private SuppliersViewModel? Vm => DataContext as SuppliersViewModel;

    private void SuppliersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<Button>(e.OriginalSource as DependencyObject) is not null)
            return;
        if (Vm?.SelectedRow is { } row)
            Vm.OpenCommand.Execute(row);
    }

    private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T match)
                return match;
            child = VisualTreeHelper.GetParent(child);
        }
        return null;
    }

    private void RowMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || Vm is null)
            return;
        if (button.DataContext is not SupplierRow row)
            return;

        var menu = new ContextMenu
        {
            PlacementTarget = button,
            Placement = PlacementMode.Bottom,
            Style = TryFindResource("ModernContextMenu") as Style
        };
        menu.Items.Add(Menu("SupplierDetails".T(), Vm.OpenCommand, row));
        menu.Items.Add(Menu("Edit".T(), Vm.EditCommand, row));
        menu.Items.Add(new Separator());
        menu.Items.Add(Menu("Delete".T(), Vm.DeleteCommand, row));
        menu.IsOpen = true;
    }

    private static MenuItem Menu(string header, ICommand command, object parameter) =>
        new()
        {
            Header = header,
            Command = command,
            CommandParameter = parameter
        };
}
