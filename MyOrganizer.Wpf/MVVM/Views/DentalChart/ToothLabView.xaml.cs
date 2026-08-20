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
        // #region agent log
        if (DataContext is ToothLabViewModel vm)
        {
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"registry-v1\",\"hypothesisId\":\"D\",\"location\":\"ToothLabView.xaml.cs\",\"message\":\"lab-loaded\",\"data\":{\"fdi\":\"" +
                       vm.ToothNumber + "\",\"imported\":" + (vm.ShowInspector ? "true" : "false") +
                       ",\"meshAsset\":\"" + MeshView.AssetName + "\"},\"timestamp\":" +
                       DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            try { System.IO.File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
            catch { }
            var paint = ContainsText(this, "2D surface paint");
            var demo = ContainsText(this, "Demo mixed");
            var procedure = ContainsText(this, "Procedure");
            var fillingType = ContainsText(this, "Procedure type: Filling");
            var a = "{\"sessionId\":\"ee2893\",\"runId\":\"paint-cleanup\",\"hypothesisId\":\"A\",\"location\":\"ToothLabView.xaml.cs\",\"message\":\"legacy-ui-absent\",\"data\":{\"paintTitle\":" +
                    (paint ? "true" : "false") + ",\"demoMixed\":" + (demo ? "true" : "false") +
                    ",\"interactiveTooth\":" + (FindName("OcclusalTooth") is not null ? "true" : "false") +
                    ",\"procedureTitle\":" + (procedure ? "true" : "false") +
                    ",\"fillingType\":" + (fillingType ? "true" : "false") +
                    ",\"meshAsset\":\"" + MeshView.AssetName +
                    "\",\"derivedFillingCount\":" + vm.FillingSurfaceNames.Count +
                    "},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            try { System.IO.File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", a); }
            catch { }
        }
        // #endregion
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
        LogInspectorState();
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
            var text = item.Content?.ToString() ?? "";
            if (text is "Palatal" or "Lingual")
                item.Content = _vm.InnerCameraLabel;
        }
    }

    // #region agent log
    private void LogInspectorState()
    {
        if (_vm is null || MeshView is null)
            return;
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"registry-v1\",\"hypothesisId\":\"C\",\"location\":\"ToothLabView.xaml.cs\",\"message\":\"inspector-visibility\",\"data\":{\"fdi\":\"" +
                   _vm.ToothNumber + "\",\"showInspector\":" + (_vm.ShowInspector ? "true" : "false") +
                   ",\"meshAsset\":\"" + MeshView.AssetName +
                   "\",\"meshVisible\":" + (MeshView.IsVisible ? "true" : "false") +
                   ",\"clinical\":" + (_vm.ShowClinicalTools ? "true" : "false") +
                   ",\"segTools\":" + (_vm.ShowSegTools ? "true" : "false") +
                   ",\"inner\":\"" + _vm.InnerCameraLabel + "\"" +
                   ",\"occlusalVisible\":" + (FindName("OcclusalTooth") is UIElement occlusal && occlusal.IsVisible ? "true" : "false") +
                   "},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { System.IO.File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { }
    }
    // #endregion

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
        // #region agent log
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"paint-cleanup\",\"hypothesisId\":\"B\",\"location\":\"ToothLabView.xaml.cs\",\"message\":\"filling-from-clinical\",\"data\":{\"fdi\":\"" +
                   vm.ToothNumber + "\",\"inner\":\"" + vm.InnerCameraLabel +
                   "\",\"derived\":\"" + string.Join(",", vm.FillingSurfaceNames) +
                   "\",\"canals\":\"" + string.Join(",", vm.TreatedRootCanalIds) +
                   "\",\"pending\":\"" + string.Join(",", vm.PendingSurfaceNames) +
                   "\",\"procedureCount\":" + vm.Clinical.Procedures.Count +
                   "},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { System.IO.File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { }
        // #endregion
    }

    private void PushSelection(ToothLabViewModel vm) =>
        MeshView.SetSelectedSurfaces(vm.PendingSurfaceNames);

    private static bool ContainsText(DependencyObject root, string text)
    {
        if (root is TextBlock block && string.Equals(block.Text, text, StringComparison.Ordinal))
            return true;
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is DependencyObject node && ContainsText(node, text))
                return true;
        }
        return false;
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
