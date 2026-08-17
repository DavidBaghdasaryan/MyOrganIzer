using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.ViewModels;

namespace MyOrganizer.Wpf.MVVM.Views.Clients;

public partial class ClientWorkspaceView : UserControl
{
    public ClientWorkspaceView() => InitializeComponent();

    private ClientWorkspaceViewModel? Vm => DataContext as ClientWorkspaceViewModel;

    private void Workspace_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;
        if (e.OriginalSource is TextBox or PasswordBox or ComboBox)
            return;
        Vm?.BackCommand.Execute(null);
        e.Handled = true;
    }

    private void More_Click(object sender, RoutedEventArgs e)
    {
        if (Vm is null || sender is not Button button)
            return;

        var menu = new ContextMenu
        {
            PlacementTarget = button,
            Placement = PlacementMode.Bottom,
            Style = TryFindResource("ModernContextMenu") as Style
        };
        if (Vm.IsOverview)
        {
            menu.Items.Add(new MenuItem
            {
                Header = "ToothChart".T(),
                Command = Vm.DentalChartCommand
            });
        }
        else
        {
            menu.Items.Add(new MenuItem
            {
                Header = "Edit".T(),
                Command = Vm.EditCommand
            });
        }
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header = "Delete".T(),
            Command = Vm.DeleteCommand
        });
        menu.IsOpen = true;
    }
}
