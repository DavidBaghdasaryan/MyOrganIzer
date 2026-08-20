using System.ComponentModel;
using System.Linq;
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
            {
                PushClinical(_vm);
                PushSelection(_vm);
            }
            return;
        }
        if (_vm is not null)
        {
            _vm.ClinicalChanged -= OnClinicalChanged;
            _vm.PendingSelectionChanged -= OnPendingSelectionChanged;
            _vm.PropertyChanged -= OnVmPropertyChanged;
        }
        _vm = vm;
        if (_vm is not null)
        {
            _vm.ClinicalChanged += OnClinicalChanged;
            _vm.PendingSelectionChanged += OnPendingSelectionChanged;
            _vm.PropertyChanged += OnVmPropertyChanged;
            if (MeshView is not null)
            {
                PushClinical(_vm);
                PushSelection(_vm);
            }
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ToothLabViewModel.ToothNumber)
            or nameof(ToothLabViewModel.ShowInspector)
            or nameof(ToothLabViewModel.ShowDetailedViewer)
            or null)
            Dispatcher.BeginInvoke(ApplyToothPresentation, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void ApplyToothPresentation()
    {
        if (_vm is null || MeshView is null)
            return;
        if (_vm.ShowDetailedViewer)
            MeshView.LoadRegisteredAsset(_vm.ToothNumber);
        else
            MeshView.ClearViewport();
        SyncInspectComboLabel();
        if (_vm.ShowDetailedViewer && _vm.ShowClinicalTools)
        {
            PushClinical(_vm);
            PushSelection(_vm);
        }
        else
        {
            MeshView.SetFillingSurfaces([]);
            MeshView.SetSelectedSurfaces([]);
            MeshView.SetRootCanals([]);
        }
        if (_vm.ShowDetailedViewer && _vm.ShowSegTools)
            ApplySurfaceDebug();
        else
            MeshView.SetSurfaceDebug(false, "All");
    }

    private void SyncInspectComboLabel()
    {
        if (_vm is null || InspectCombo is null) return;
        foreach (var item in InspectCombo.Items.OfType<ComboBoxItem>())
        {
            var tag = item.Tag?.ToString() ?? "";
            if (tag is "Palatal" or "Lingual")
                item.Content = _vm.InnerCameraLabel;
        }
    }


    private void OnClinicalChanged(object? sender, EventArgs e)
    {
        if (_vm is null)
            return;
        ApplyToothPresentation();
    }

    private void OnPendingSelectionChanged(object? sender, EventArgs e)
    {
        if (_vm is not null && _vm.ShowDetailedViewer)
            PushSelection(_vm);
    }

    private void OnInteraction(object? sender, ToothLabHitEventArgs e)
    {
        _vm?.SetInteraction(e.Hover, e.SelectedSurfaces);
    }

    private void PushClinical(ToothLabViewModel vm)
    {
        MeshView.SetFillingSurfaces(vm.FillingSurfaceNames);
        MeshView.SetRootCanals(vm.TreatedRootCanalIds);
    }

    private void PushSelection(ToothLabViewModel vm) =>
        MeshView.SetSelectedSurfaces(vm.PendingSurfaceNames);



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
        var inspect = (InspectCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString()
                      ?? (InspectCombo.SelectedItem as ComboBoxItem)?.Content?.ToString()
                      ?? "All";
        MeshView.SetSurfaceDebug(ShowSegCheck.IsChecked == true, inspect);
    }
}
