using System.Windows;
using System.Windows.Controls;
using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.MVVM.ViewModels;

namespace MyOrganizer.Wpf.MVVM.Views.DentalChart;

public partial class ToothLabView : UserControl
{
    public ToothLabView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        MeshView.InteractionChanged += OnInteraction;
        ApplySurfaceDebug();
    }

    private void OnInteraction(object? sender, ToothLabHitEventArgs e)
    {
        if (DataContext is ToothLabViewModel vm)
            vm.SetInteraction(e.Hover, e.Selected);
    }

    private void OnOcclusal(object sender, RoutedEventArgs e) => MeshView.ResetToOcclusal();
    private void OnBuccal(object sender, RoutedEventArgs e) => MeshView.ShowBuccal();
    private void OnPalatal(object sender, RoutedEventArgs e) => MeshView.ShowPalatal();
    private void OnMesial(object sender, RoutedEventArgs e) => MeshView.ShowMesial();
    private void OnDistal(object sender, RoutedEventArgs e) => MeshView.ShowDistal();

    private void OnSegChanged(object sender, RoutedEventArgs e) => ApplySurfaceDebug();

    private void OnSegChanged(object sender, SelectionChangedEventArgs e) => ApplySurfaceDebug();

    private void ApplySurfaceDebug()
    {
        if (MeshView is null || ShowSegCheck is null || InspectCombo is null)
            return;
        var inspect = (InspectCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
        MeshView.SetSurfaceDebug(ShowSegCheck.IsChecked == true, inspect);
    }
}
