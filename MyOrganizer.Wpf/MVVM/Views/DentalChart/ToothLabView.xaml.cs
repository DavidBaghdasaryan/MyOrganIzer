using System.Windows;
using System.Windows.Controls;

namespace MyOrganizer.Wpf.MVVM.Views.DentalChart;

public partial class ToothLabView : UserControl
{
    public ToothLabView()
    {
        InitializeComponent();
    }

    private void OnOcclusal(object sender, RoutedEventArgs e) => MeshView.ResetToOcclusal();
    private void OnBuccal(object sender, RoutedEventArgs e) => MeshView.ShowBuccal();
    private void OnPalatal(object sender, RoutedEventArgs e) => MeshView.ShowPalatal();
    private void OnMesial(object sender, RoutedEventArgs e) => MeshView.ShowMesial();
    private void OnDistal(object sender, RoutedEventArgs e) => MeshView.ShowDistal();
}
