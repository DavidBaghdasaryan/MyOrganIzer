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
        // #region agent log
        LogHost();
        // #endregion
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
            Grid.SetRow(InspectorHost, 1);
            Grid.SetColumn(InspectorHost, 1);
            InspectorHost.Margin = new Thickness(0);
            InspectorHost.MinHeight = 0;
        }
        else
        {
            InspectorCol.Width = new GridLength(0);
            InspectorRow.Height = GridLength.Auto;
            Grid.SetRow(InspectorHost, 2);
            Grid.SetColumn(InspectorHost, 0);
            InspectorHost.Margin = new Thickness(0, 10, 0, 0);
            InspectorHost.MinHeight = 168;
        }
    }

    private void HookVm(DentalChartViewModel? vm)
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
        UnhookVm();
        _vm = vm;
        if (_vm is null)
            return;
        _vm.ClinicalChanged += OnClinicalChanged;
        _vm.PendingSelectionChanged += OnPendingSelectionChanged;
        _vm.PropertyChanged += OnVmPropertyChanged;
        if (MeshView is not null)
        {
            PushClinical(_vm);
            PushSelection(_vm);
        }
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
        MeshView.SetSurfaceDebug(false, "All");
        // #region agent log
        LogMesh();
        // #endregion
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

    private void OnInteraction(object? sender, ToothLabHitEventArgs e) =>
        _vm?.SetInteraction(e.Hover, e.SelectedSurfaces);

    private void PushClinical(DentalChartViewModel vm)
    {
        MeshView.SetFillingSurfaces(vm.FillingSurfaceNames);
        MeshView.SetRootCanals(vm.TreatedRootCanalIds);
        // #region agent log
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"stage4\",\"hypothesisId\":\"C\",\"location\":\"DentalChartView.xaml.cs\",\"message\":\"filling-from-clinical\",\"data\":{\"clientId\":" +
                   vm.ClientId + ",\"fdi\":\"" + vm.ToothNumber +
                   "\",\"derived\":\"" + string.Join(",", vm.FillingSurfaceNames) +
                   "\",\"canals\":\"" + string.Join(",", vm.TreatedRootCanalIds) +
                   "\",\"procedureCount\":" + vm.Clinical.Procedures.Count +
                   ",\"labPatients\":false},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { System.IO.File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { }
        // #endregion
    }

    private void PushSelection(DentalChartViewModel vm) =>
        MeshView.SetSelectedSurfaces(vm.PendingSurfaceNames);

    private void OnOcclusal(object sender, RoutedEventArgs e) => MeshView.ResetToOcclusal();
    private void OnBuccal(object sender, RoutedEventArgs e) => MeshView.ShowBuccal();
    private void OnPalatal(object sender, RoutedEventArgs e) => MeshView.ShowPalatal();
    private void OnMesial(object sender, RoutedEventArgs e) => MeshView.ShowMesial();
    private void OnDistal(object sender, RoutedEventArgs e) => MeshView.ShowDistal();

    // #region agent log
    private void LogHost()
    {
        var odontogram = FindName("ProductionOdontogram") as OdontogramView;
        var mesh = FindName("MeshView") as ToothMeshView;
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"stage4\",\"hypothesisId\":\"A\",\"location\":\"DentalChartView.xaml.cs\",\"message\":\"production-lab-host\",\"data\":{\"odontogramPresent\":" +
                   (odontogram is not null ? "true" : "false") +
                   ",\"meshPresent\":" + (mesh is not null ? "true" : "false") +
                   ",\"meshVisible\":" + (mesh?.IsVisible == true ? "true" : "false") +
                   ",\"showDetailed\":" + (_vm?.ShowDetailedViewer == true ? "true" : "false") +
                   ",\"labSelectorPresent\":" + (FindName("LabPatientSelector") is not null ? "true" : "false") +
                   ",\"createButton\":" + ContainsText(this, "Create Procedure") +
                   ",\"clientId\":" + (_vm?.ClientId ?? 0) +
                   "},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { System.IO.File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { }
    }

    private void LogMesh()
    {
        if (_vm is null || MeshView is null)
            return;
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"stage4\",\"hypothesisId\":\"B\",\"location\":\"DentalChartView.xaml.cs\",\"message\":\"mesh-presentation\",\"data\":{\"clientId\":" +
                   _vm.ClientId + ",\"fdi\":\"" + _vm.ToothNumber +
                   "\",\"meshAsset\":\"" + MeshView.AssetName +
                   "\",\"showDetailed\":" + (_vm.ShowDetailedViewer ? "true" : "false") +
                   ",\"implant\":" + (_vm.IsImplantSelected ? "true" : "false") +
                   ",\"clinical\":" + (_vm.ShowClinicalTools ? "true" : "false") +
                   ",\"labPatients\":false},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { System.IO.File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { }
    }

    private static bool ContainsText(DependencyObject root, string text)
    {
        if (root is TextBlock block && string.Equals(block.Text, text, StringComparison.Ordinal))
            return true;
        if (root is Button button && string.Equals(button.Content?.ToString(), text, StringComparison.Ordinal))
            return true;
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is DependencyObject node && ContainsText(node, text))
                return true;
        }
        return false;
    }
    // #endregion
}
