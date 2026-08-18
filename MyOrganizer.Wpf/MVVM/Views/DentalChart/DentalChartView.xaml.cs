using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.Dental;
using MyOrganizer.Wpf.MVVM.ViewModels;

namespace MyOrganizer.Wpf.MVVM.Views.DentalChart;

public partial class DentalChartView : UserControl
{
    private const double SideBySideWide = 1180;
    private const double SideBySideMin = 980;

    private static readonly string[] UpperFdi =
        ["18", "17", "16", "15", "14", "13", "12", "11", "21", "22", "23", "24", "25", "26", "27", "28"];

    private static readonly string[] LowerFdi =
        ["48", "47", "46", "45", "44", "43", "42", "41", "31", "32", "33", "34", "35", "36", "37", "38"];

    private readonly Dictionary<string, ToothControl> _teeth = new(StringComparer.Ordinal);
    private DentalChartViewModel? _vm;
    private ToothControl? _activeTooth;
    private bool _built;

    public DentalChartView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_built)
        {
            BuildChart(ChartUpper, UpperFdi);
            BuildChart(ChartLower, LowerFdi);
            _built = true;
        }

        HookVm(DataContext as DentalChartViewModel);
        ApplyCurrentStates();
        ArrangeInspector(ActualWidth);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => UnhookVm();

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        HookVm(e.NewValue as DentalChartViewModel);
        if (_built)
            ApplyCurrentStates();
    }

    private void OnChartSizeChanged(object sender, SizeChangedEventArgs e) =>
        ArrangeInspector(e.NewSize.Width);

    private void ArrangeInspector(double width)
    {
        if (InspectorCol is null || InspectorRow is null || InspectorHost is null)
            return;

        if (width >= SideBySideMin)
        {
            InspectorCol.Width = new GridLength(width >= SideBySideWide ? 268 : 220);
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
            return;
        UnhookVm();
        _vm = vm;
        if (_vm is not null)
            _vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void UnhookVm()
    {
        if (_vm is null)
            return;
        _vm.PropertyChanged -= OnVmPropertyChanged;
        _vm = null;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DentalChartViewModel.CurrentStates)
            or nameof(DentalChartViewModel.Marks)
            or nameof(DentalChartViewModel.IsBusy))
            ApplyCurrentStates();
    }

    private void BuildChart(Grid host, IReadOnlyList<string> fdis)
    {
        host.Children.Clear();
        host.ColumnDefinitions.Clear();

        for (var i = 0; i < 8; i++)
            host.ColumnDefinitions.Add(Star(fdis[i]));
        host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        for (var i = 8; i < fdis.Count; i++)
            host.ColumnDefinitions.Add(Star(fdis[i]));

        for (var i = 0; i < fdis.Count; i++)
        {
            var tooth = new ToothControl
            {
                ToothNumber = fdis[i],
                Margin = new Thickness(1, 2, 1, 2),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            tooth.ToothClicked += Tooth_Clicked;
            tooth.ContextMenuOpening += Tooth_ContextMenu;
            tooth.ContextMenu = null;
            Grid.SetColumn(tooth, i < 8 ? i : i + 1);
            _teeth[fdis[i]] = tooth;
            host.Children.Add(tooth);
        }
    }

    private static ColumnDefinition Star(string fdi) =>
        new() { Width = new GridLength(ToothFdi.ColumnWeight(fdi), GridUnitType.Star) };

    private void FocusTooth(ToothControl current)
    {
        _activeTooth = current;
        foreach (var tooth in _teeth.Values)
        {
            if (ReferenceEquals(tooth, current))
                continue;
            tooth.ClearSurfaceSelection();
            tooth.IsToothSelected = false;
        }
    }

    private IReadOnlyList<ToothSurfaceType> InspectorSurfaces() =>
        _vm?.InspectorSurfaces.ToList() ?? [];

    private void SyncInspector(ToothControl tooth)
    {
        var surfaces = InspectorSurfaces();
        _vm?.UpdateSelection(tooth.ToothNumber, surfaces, surfaces.Count == 0);
    }

    private void Tooth_Clicked(object? sender, ToothSurfaceEventArgs e)
    {
        if (sender is not ToothControl current)
            return;

        FocusTooth(current);
        if (!current.IsToothSelected)
        {
            _activeTooth = null;
            _vm?.ClearSelectionStatus();
            return;
        }

        _vm?.UpdateSelection(e.ToothNumber, [], wholeTooth: true);
    }

    private void Tooth_ContextMenu(object sender, ContextMenuEventArgs e)
    {
        e.Handled = true;
        if (sender is not ToothControl tooth)
            return;

        tooth.IsToothSelected = true;
        FocusTooth(tooth);
        SyncInspector(tooth);
        _ = OpenApplyDialogAsync(tooth);
    }

    private async void ApplyProcedure_Click(object sender, RoutedEventArgs e)
    {
        if (_activeTooth is null)
            return;
        await OpenApplyDialogAsync(_activeTooth);
    }

    private async Task OpenApplyDialogAsync(ToothControl tooth)
    {
        if (_vm is null)
            return;

        var surfaces = InspectorSurfaces();
        var applied = await _vm.OpenApplyDialogAsync(tooth.ToothNumber, surfaces, surfaces.Count == 0);
        if (!applied)
            return;

        SyncInspector(tooth);
    }

    private async void ClearSurfaces_Click(object sender, RoutedEventArgs e)
    {
        if (_vm is null || _activeTooth is null || !_vm.HasSurfaceSelection)
            return;
        var names = _vm.InspectorSurfaces.Select(s => s.ToString()).ToList();
        await _vm.ClearSurfacesAsync(_activeTooth.ToothNumber, names);
        _activeTooth.IsToothSelected = true;
        _vm.UpdateSelection(_activeTooth.ToothNumber, [], wholeTooth: true);
    }

    private async void ClearTooth_Click(object sender, RoutedEventArgs e)
    {
        if (_vm is null || _activeTooth is null)
            return;
        await _vm.ClearToothAsync(_activeTooth.ToothNumber);
        _activeTooth.IsToothSelected = true;
        SyncInspector(_activeTooth);
    }

    private void ApplyCurrentStates()
    {
        if (_vm is null || !_built)
            return;

        foreach (var (fdi, tooth) in _teeth)
            tooth.SetCurrentState(ToothCurrentStateCalculator.ForTooth(fdi, _vm.CurrentStates));
    }
}
