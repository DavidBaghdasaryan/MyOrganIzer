using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MyOrganizer.Wpf.MVVM.ViewModels;

namespace MyOrganizer.Wpf.MVVM.Views.Suppliers;

public partial class SupplierWorkspaceView : UserControl
{
    public SupplierWorkspaceView() => InitializeComponent();

    private SupplierWorkspaceViewModel? Vm => DataContext as SupplierWorkspaceViewModel;

    private void ProductsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<Button>(e.OriginalSource as DependencyObject) is not null)
            return;
        Vm?.EditProductCommand.Execute(null);
    }

    private void ServicesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<Button>(e.OriginalSource as DependencyObject) is not null)
            return;
        Vm?.EditServiceCommand.Execute(null);
    }

    private void ExpensesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<Button>(e.OriginalSource as DependencyObject) is not null)
            return;
        Vm?.OpenExpenseCommand.Execute(null);
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
}
