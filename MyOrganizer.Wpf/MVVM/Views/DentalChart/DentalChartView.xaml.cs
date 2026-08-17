using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.MVVM.ViewModels;

namespace MyOrganizer.Wpf.MVVM.Views.DentalChart;

public partial class DentalChartView : UserControl
{
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
            BuildChart(ChartUpper, UpperFdi, upper: true);
            BuildChart(ChartLower, LowerFdi, upper: false);
            _built = true;
        }

        HookVm(DataContext as DentalChartViewModel);
        ApplyMarks();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => UnhookVm();

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        HookVm(e.NewValue as DentalChartViewModel);
        if (_built)
            ApplyMarks();
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
        if (e.PropertyName is nameof(DentalChartViewModel.Marks) or nameof(DentalChartViewModel.IsBusy))
            ApplyMarks();
    }

    private void BuildChart(Grid host, IReadOnlyList<string> fdis, bool upper)
    {
        host.Children.Clear();
        host.ColumnDefinitions.Clear();

        for (var i = 0; i < 8; i++)
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        for (var i = 0; i < 8; i++)
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (var i = 0; i < fdis.Count; i++)
        {
            var lift = ArchLift(i);
            var tooth = new ToothControl
            {
                ToothNumber = fdis[i],
                Margin = upper
                    ? new Thickness(4, 2, 4, lift)
                    : new Thickness(4, lift, 4, 2),
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            tooth.SurfaceClicked += Tooth_SurfaceClicked;
            tooth.ToothClicked += Tooth_Clicked;
            tooth.ContextMenuOpening += Tooth_ContextMenu;
            tooth.ContextMenu = null;
            Grid.SetColumn(tooth, i < 8 ? i : i + 1);
            _teeth[fdis[i]] = tooth;
            host.Children.Add(tooth);
        }
    }

    private static double ArchLift(int index)
    {
        var fromEnd = Math.Min(index, 15 - index);
        return fromEnd switch
        {
            0 => 6,
            1 => 4,
            2 => 2,
            _ => 0
        };
    }

    private void Tooth_SurfaceClicked(object? sender, ToothSurfaceEventArgs e)
    {
        if (sender is ToothControl tooth)
            _activeTooth = tooth;
        _vm?.UpdateSelection(e.ToothNumber, e.SelectedSurfaces, e.WholeTooth);
    }

    private void Tooth_Clicked(object? sender, ToothSurfaceEventArgs e)
    {
        if (sender is ToothControl current && e.WholeTooth)
        {
            _activeTooth = current;
            foreach (var tooth in _teeth.Values)
            {
                if (!ReferenceEquals(tooth, current))
                    tooth.IsToothSelected = false;
            }
        }

        _vm?.UpdateSelection(e.ToothNumber, e.SelectedSurfaces, e.WholeTooth);
    }

    private void Tooth_ContextMenu(object sender, ContextMenuEventArgs e)
    {
        e.Handled = true;
        if (sender is not ToothControl tooth)
            return;

        _activeTooth = tooth;
        var wholeTooth = !tooth.HasSurfaceSelection;
        _vm?.UpdateSelection(tooth.ToothNumber, tooth.SelectedSurfaces.ToList(), wholeTooth);
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

        var surfaces = tooth.SelectedSurfaces.ToList();
        var applied = await _vm.OpenApplyDialogAsync(tooth.ToothNumber, surfaces, surfaces.Count == 0);
        if (!applied)
            return;

        tooth.ClearSurfaceSelection();
        tooth.IsToothSelected = false;
        _vm.ClearSelectionStatus();
    }

    private async void ClearSurfaces_Click(object sender, RoutedEventArgs e)
    {
        if (_vm is null || _activeTooth is null || !_activeTooth.HasSurfaceSelection)
            return;
        var names = _activeTooth.SelectedSurfaces.Select(s => s.ToString()).ToList();
        await _vm.ClearSurfacesAsync(_activeTooth.ToothNumber, names);
        _activeTooth.ClearSurfaceSelection();
        _vm.ClearSelectionStatus();
    }

    private async void ClearTooth_Click(object sender, RoutedEventArgs e)
    {
        if (_vm is null || _activeTooth is null)
            return;
        await _vm.ClearToothAsync(_activeTooth.ToothNumber);
        _activeTooth.ClearSurfaceSelection();
        _activeTooth.IsToothSelected = false;
        _vm.ClearSelectionStatus();
    }

    private void ApplyMarks()
    {
        if (_vm is null || !_built)
            return;

        foreach (var (fdi, tooth) in _teeth)
        {
            if (_vm.Marks.TryGetValue(fdi, out var marks))
                tooth.SetMarks(marks);
            else
                tooth.SetMarks([]);
        }
    }
}
