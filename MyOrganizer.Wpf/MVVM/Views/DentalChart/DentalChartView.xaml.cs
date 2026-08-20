using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.MVVM.ViewModels;

namespace MyOrganizer.Wpf.MVVM.Views.DentalChart;

public partial class DentalChartView : UserControl
{
    private const double SideBySideWide = 1180;
    private const double SideBySideMin = 980;

    private DentalChartViewModel? _vm;
    private bool _presentQueued;

    public DentalChartView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (MeshView is not null)
            MeshView.InteractionChanged += OnInteraction;
        HookVm(DataContext as DentalChartViewModel);
        ArrangeInspector(ActualWidth);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (MeshView is not null)
            MeshView.InteractionChanged -= OnInteraction;
        UnhookVm();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) =>
        HookVm(e.NewValue as DentalChartViewModel);

    private void OnChartSizeChanged(object sender, SizeChangedEventArgs e) =>
        ArrangeInspector(e.NewSize.Width);

    private void ArrangeInspector(double width)
    {
        if (InspectorCol is null || InspectorRow is null || InspectorHost is null)
            return;

        if (width >= SideBySideMin)
        {
            InspectorCol.Width = new GridLength(width >= SideBySideWide ? 300 : 240);
            InspectorRow.Height = new GridLength(0);
            Grid.SetRow(InspectorHost, 0);
            Grid.SetColumn(InspectorHost, 1);
            InspectorHost.Margin = new Thickness(0);
            InspectorHost.MinHeight = 0;
        }
        else
        {
            InspectorCol.Width = new GridLength(0);
            InspectorRow.Height = GridLength.Auto;
            Grid.SetRow(InspectorHost, 1);
            Grid.SetColumn(InspectorHost, 0);
            InspectorHost.Margin = new Thickness(0, 10, 0, 0);
            InspectorHost.MinHeight = 168;
        }
    }

    private void HookVm(DentalChartViewModel? vm)
    {
        if (ReferenceEquals(_vm, vm))
        {
            QueuePresentation();
            return;
        }
        UnhookVm();
        _vm = vm;
        if (_vm is null)
            return;
        _vm.ClinicalChanged += OnClinicalChanged;
        _vm.PendingSelectionChanged += OnPendingSelectionChanged;
        _vm.PropertyChanged += OnVmPropertyChanged;
        QueuePresentation();
    }

    private void UnhookVm()
    {
        if (_vm is null)
            return;
        _vm.ClinicalChanged -= OnClinicalChanged;
        _vm.PendingSelectionChanged -= OnPendingSelectionChanged;
        _vm.PropertyChanged -= OnVmPropertyChanged;
        _vm = null;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DentalChartViewModel.ToothNumber)
            or nameof(DentalChartViewModel.ShowDetailedViewer)
            or nameof(DentalChartViewModel.ShowInspector)
            or null)
            QueuePresentation();
    }

    private void QueuePresentation()
    {
        if (_presentQueued)
            return;
        _presentQueued = true;
        Dispatcher.BeginInvoke(() =>
        {
            _presentQueued = false;
            ApplyToothPresentation();
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void ApplyToothPresentation()
    {
        if (_vm is null || MeshView is null)
            return;
        var push = _vm.ShowDetailedViewer && _vm.ShowClinicalTools;
        if (_vm.ShowDetailedViewer)
            MeshView.LoadRegisteredAsset(_vm.ToothNumber);
        else
            MeshView.ClearViewport();
        if (push)
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
        MeshView.SetSurfaceDebug(false, "All");
    }

    private void OnClinicalChanged(object? sender, EventArgs e) => QueuePresentation();

    private void OnPendingSelectionChanged(object? sender, EventArgs e)
    {
        if (_vm is not null && _vm.ShowDetailedViewer)
            PushSelection(_vm);
    }

    private void OnInteraction(object? sender, ToothLabHitEventArgs e) =>
        _vm?.SetInteraction(e.Hover, e.SelectedSurfaces);

    private void PushClinical(DentalChartViewModel vm)
    {
        MeshView.SetFillingSurfaces(vm.FillingSurfaceNames);
        MeshView.SetRootCanals(vm.TreatedRootCanalIds);
    }

    private void PushSelection(DentalChartViewModel vm) =>
        MeshView.SetSelectedSurfaces(vm.PendingSurfaceNames);

    private void OnOcclusal(object sender, RoutedEventArgs e) => MeshView.ResetToOcclusal();
    private void OnBuccal(object sender, RoutedEventArgs e) => MeshView.ShowBuccal();
    private void OnPalatal(object sender, RoutedEventArgs e) => MeshView.ShowPalatal();
    private void OnMesial(object sender, RoutedEventArgs e) => MeshView.ShowMesial();
    private void OnDistal(object sender, RoutedEventArgs e) => MeshView.ShowDistal();

}
