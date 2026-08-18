using System.Windows;
using System.Windows.Controls;
using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.MVVM.ViewModels;

namespace MyOrganizer.Wpf.MVVM.Views.DentalChart;

public partial class ToothLabView : UserControl
{
    private ToothLabViewModel? _vm;

    public ToothLabView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        BindViewModel(e.NewValue as ToothLabViewModel);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        MeshView.InteractionChanged += OnInteraction;
        BindViewModel(DataContext as ToothLabViewModel);
        ApplySurfaceDebug();
    }

    private void BindViewModel(ToothLabViewModel? vm)
    {
        if (ReferenceEquals(_vm, vm))
        {
            if (_vm is not null && MeshView is not null)
                PushClinical(_vm);
            return;
        }
        if (_vm is not null)
            _vm.ClinicalChanged -= OnClinicalChanged;
        _vm = vm;
        if (_vm is not null)
        {
            _vm.ClinicalChanged += OnClinicalChanged;
            if (MeshView is not null)
                PushClinical(_vm);
        }
    }

    private void OnClinicalChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
            PushClinical(_vm);
    }

    private void OnInteraction(object? sender, ToothLabHitEventArgs e)
    {
        _vm?.SetInteraction(e.Hover, e.Selected);
    }

    private void PushClinical(ToothLabViewModel vm) =>
        MeshView.SetFillingSurfaces(vm.FillingSurfaceNames);

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
