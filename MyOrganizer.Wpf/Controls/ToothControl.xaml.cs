using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MyOrganizer.Wpf.Dental;

namespace MyOrganizer.Wpf.Controls;

public sealed class ToothSurfaceEventArgs : EventArgs
{
    public required string ToothNumber { get; init; }
    public ToothSurfaceType? Surface { get; init; }
    public IReadOnlyList<ToothSurfaceType> SelectedSurfaces { get; init; } = [];
    public bool WholeTooth { get; init; }
}

public sealed class ToothMark
{
    public ToothSurfaceType? Surface { get; init; }
    public string Procedure { get; init; } = "";
    public string Code { get; init; } = "";
    public Brush Brush { get; init; } = Brushes.SlateGray;
    public ToothClinicalKind Kind { get; init; }
}

public partial class ToothControl : UserControl
{
    public static readonly DependencyProperty ToothNumberProperty =
        DependencyProperty.Register(nameof(ToothNumber), typeof(string), typeof(ToothControl),
            new PropertyMetadata("11", OnToothNumberChanged));

    public static readonly DependencyProperty IsToothSelectedProperty =
        DependencyProperty.Register(nameof(IsToothSelected), typeof(bool), typeof(ToothControl),
            new PropertyMetadata(false, OnToothSelectedChanged));

    public string ToothNumber
    {
        get => (string)GetValue(ToothNumberProperty);
        set => SetValue(ToothNumberProperty, value);
    }

    public bool IsToothSelected
    {
        get => (bool)GetValue(IsToothSelectedProperty);
        set => SetValue(IsToothSelectedProperty, value);
    }

    public event EventHandler<ToothSurfaceEventArgs>? ToothClicked;

    private readonly Dictionary<ToothSurfaceType, Path> _surfaceOverlays = [];
    private readonly Dictionary<ToothSurfaceType, Path> _lesionOverlays = [];
    private readonly Dictionary<ToothSurfaceType, Geometry> _surfaceGeometry = [];
    private readonly List<Path> _rootParts = [];
    private readonly List<Path> _crownParts = [];
    private readonly List<Path> _detailParts = [];
    private Path? _implant;
    private Path? _canal;
    private Path? _crownMetal;
    private Path? _hit;
    private Path? _selectionFill;
    private bool _hovered;
    private readonly HashSet<ToothSurfaceType> _selected = [];
    private ToothCurrentState _current = ToothCurrentState.Healthy("11");

    public ToothControl()
    {
        InitializeComponent();
        Loaded += (_, _) => Rebuild();
        MouseEnter += (_, _) => { _hovered = true; ApplyChrome(); };
        MouseLeave += (_, _) => { _hovered = false; ApplyChrome(); };
        ToolTip = " ";
        ToolTipOpening += OnToolTipOpening;
    }

    public IReadOnlyCollection<ToothSurfaceType> SelectedSurfaces => _selected;
    public bool HasSurfaceSelection => _selected.Count > 0;

    public void ClearSurfaceSelection()
    {
        _selected.Clear();
    }

    public void PreviewSelect(params ToothSurfaceType[] surfaces)
    {
        _selected.Clear();
        foreach (var surface in surfaces)
            _selected.Add(surface);
    }

    public void SetCurrentState(ToothCurrentState? state)
    {
        _current = state ?? ToothCurrentState.Healthy(ToothNumber);
        if (IsLoaded)
            ApplyClinical();
    }

    public void SetMarks(IReadOnlyList<ToothMark> marks)
    {
        PartBadges.ItemsSource = null;
    }

    private static void OnToothNumberChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToothControl c && c.IsLoaded)
            c.Rebuild();
    }

    private static void OnToothSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToothControl { IsLoaded: true } c)
            c.ApplyChrome();
    }

    private void Rebuild()
    {
        PartCanvas.Children.Clear();
        _surfaceOverlays.Clear();
        _lesionOverlays.Clear();
        _surfaceGeometry.Clear();
        _rootParts.Clear();
        _crownParts.Clear();
        _detailParts.Clear();

        var art = ToothVectorArt.Build(ToothNumber);
        var visual = CreateVisualTransform();

        foreach (var part in art.Parts)
        {
            var path = Decor(part.Data, part.Fill, part.Stroke, part.StrokeThickness, visual);
            if (part.Kind == ToothPartKind.Fissure)
            {
                path.StrokeStartLineCap = PenLineCap.Round;
                path.StrokeEndLineCap = PenLineCap.Round;
            }

            switch (part.Kind)
            {
                case ToothPartKind.Root:
                    _rootParts.Add(path);
                    break;
                case ToothPartKind.Crown:
                    _crownParts.Add(path);
                    break;
                default:
                    _detailParts.Add(path);
                    break;
            }
        }

        _implant = Decor(art.Implant, ToothBrushes.ImplantFill, ToothBrushes.ImplantStroke, 1.05, visual);
        _implant.Visibility = Visibility.Collapsed;

        _canal = Decor(art.Canal, Brushes.Transparent, ToothBrushes.CanalStroke, 2.1, visual);
        _canal.StrokeStartLineCap = PenLineCap.Round;
        _canal.StrokeEndLineCap = PenLineCap.Round;
        _canal.Visibility = Visibility.Collapsed;

        _hit = Decor(art.Body, Brushes.Transparent, Brushes.Transparent, 0, visual);
        _hit.IsHitTestVisible = true;
        _hit.Cursor = Cursors.Hand;
        _hit.MouseLeftButtonDown += Whole_MouseLeftButtonDown;
        _hit.MouseRightButtonDown += Whole_MouseRightButtonDown;

        foreach (var path in _rootParts.Concat(_crownParts).Append(_implant))
        {
            path.IsHitTestVisible = true;
            path.Cursor = Cursors.Hand;
            path.MouseLeftButtonDown += Whole_MouseLeftButtonDown;
            path.MouseRightButtonDown += Whole_MouseRightButtonDown;
        }

        _crownMetal = Decor(art.Crown, ToothBrushes.CrownMetal, ToothBrushes.ImplantStroke, 0.7, visual);
        _crownMetal.Opacity = 0.88;
        _crownMetal.Visibility = Visibility.Collapsed;

        AddSurfaceOverlay(ToothSurfaceType.Buccal, art.Buccal, visual);
        AddSurfaceOverlay(ToothSurfaceType.Lingual, art.Lingual, visual);
        AddSurfaceOverlay(ToothSurfaceType.Mesial, art.Mesial, visual);
        AddSurfaceOverlay(ToothSurfaceType.Distal, art.Distal, visual);
        AddSurfaceOverlay(ToothSurfaceType.Occlusal, art.Occlusal, visual);

        AddLesionOverlay(ToothSurfaceType.Buccal, art.Buccal, visual);
        AddLesionOverlay(ToothSurfaceType.Lingual, art.Lingual, visual);
        AddLesionOverlay(ToothSurfaceType.Mesial, art.Mesial, visual);
        AddLesionOverlay(ToothSurfaceType.Distal, art.Distal, visual);
        AddLesionOverlay(ToothSurfaceType.Occlusal, art.Occlusal, visual);

        _selectionFill = Decor(art.Body, ToothBrushes.SelectedPlate, Brushes.Transparent, 0, visual);
        _selectionFill.Visibility = Visibility.Collapsed;

        ApplyClinical();
        ApplyChrome();
    }

    private Transform CreateVisualTransform()
    {
        var group = new TransformGroup();
        group.Children.Add(new ScaleTransform(
            ToothFdi.MesialOnLeft(ToothNumber) ? 1 : -1,
            ToothFdi.IsUpper(ToothNumber) ? -1 : 1,
            44, 68));
        group.Freeze();
        return group;
    }

    private Path Decor(Geometry data, Brush fill, Brush stroke, double thickness, Transform visual)
    {
        var path = new Path
        {
            Data = data,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = thickness,
            StrokeLineJoin = PenLineJoin.Round,
            IsHitTestVisible = false,
            RenderTransform = visual
        };
        PartCanvas.Children.Add(path);
        return path;
    }

    private void AddSurfaceOverlay(ToothSurfaceType surface, Geometry data, Transform visual)
    {
        var path = new Path
        {
            Data = data,
            Fill = ToothBrushes.Filling,
            Stroke = ToothBrushes.FillingStroke,
            StrokeThickness = 0.4,
            StrokeLineJoin = PenLineJoin.Round,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
            Tag = surface,
            RenderTransform = visual
        };
        _surfaceOverlays[surface] = path;
        _surfaceGeometry[surface] = data;
        PartCanvas.Children.Add(path);
    }

    private void AddLesionOverlay(ToothSurfaceType surface, Geometry data, Transform visual)
    {
        var path = new Path
        {
            Fill = ToothBrushes.CariesDeep,
            Stroke = Brushes.Transparent,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
            Tag = surface,
            RenderTransform = visual
        };
        _lesionOverlays[surface] = path;
        PartCanvas.Children.Add(path);
    }

    private void Whole_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        SelectWholeTooth();
        e.Handled = true;
    }

    private void Whole_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsToothSelected)
        {
            _selected.Clear();
            IsToothSelected = true;
            ApplyChrome();
            ToothClicked?.Invoke(this, CreateArgs(null, wholeTooth: true));
        }
    }

    private void SelectWholeTooth()
    {
        _selected.Clear();
        IsToothSelected = !IsToothSelected;
        ApplyChrome();
        ToothClicked?.Invoke(this, CreateArgs(null, wholeTooth: true));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.Handled)
            return;
        SelectWholeTooth();
        e.Handled = true;
    }

    private ToothSurfaceEventArgs CreateArgs(ToothSurfaceType? surface, bool wholeTooth = false) => new()
    {
        ToothNumber = ToothNumber,
        Surface = surface,
        SelectedSurfaces = _selected.ToList(),
        WholeTooth = wholeTooth || _selected.Count == 0
    };

    private void ApplyChrome()
    {
        if (_hit is null || _selectionFill is null)
            return;

        var selected = IsToothSelected;
        var hover = _hovered && !selected;
        _selectionFill.Fill = selected
            ? ToothBrushes.SelectedPlate
            : hover
                ? ToothBrushes.HoverPlate
                : Brushes.Transparent;
        _selectionFill.Visibility = selected || hover ? Visibility.Visible : Visibility.Collapsed;
        _hit.Fill = Brushes.Transparent;

        PartNumber.Foreground = selected ? ToothBrushes.WholeSelected : ToothBrushes.Number;
        PartNumber.FontWeight = selected ? FontWeights.Bold : FontWeights.SemiBold;
    }

    private void ApplyClinical()
    {
        if (_implant is null || _canal is null || _crownMetal is null)
            return;

        var state = _current.ToothFdi == ToothNumber
            ? _current
            : ToothCurrentState.Healthy(ToothNumber);
        var missing = ToothClinicalLayers.ShowMissing(state);
        var implant = ToothClinicalLayers.ShowImplant(state);
        var crown = ToothClinicalLayers.ShowCrown(state);
        var endo = ToothClinicalLayers.ShowEndodontic(state);

        foreach (var path in _rootParts)
        {
            path.Fill = missing ? Brushes.Transparent : ToothVectorArt.RootFill;
            path.Stroke = missing ? ToothBrushes.MissingStroke : ToothVectorArt.RootStroke;
            path.StrokeDashArray = missing ? new DoubleCollection { 2.2, 1.8 } : null;
            path.StrokeThickness = missing ? 1.4 : 0.45;
            path.Visibility = implant ? Visibility.Collapsed : Visibility.Visible;
        }

        foreach (var path in _crownParts)
        {
            path.Fill = missing ? Brushes.Transparent : ToothVectorArt.EnamelFill;
            path.Stroke = missing ? ToothBrushes.MissingStroke : ToothVectorArt.CrownStroke;
            path.StrokeDashArray = missing ? new DoubleCollection { 2.2, 1.8 } : null;
            path.StrokeThickness = missing ? 1.4 : 0.55;
            path.Opacity = missing ? 0.9 : 1;
        }

        foreach (var path in _detailParts)
            path.Visibility = missing || crown ? Visibility.Collapsed : Visibility.Visible;

        _implant.Visibility = implant ? Visibility.Visible : Visibility.Collapsed;
        _canal.Visibility = endo ? Visibility.Visible : Visibility.Collapsed;
        _crownMetal.Visibility = crown && !missing ? Visibility.Visible : Visibility.Collapsed;

        foreach (var (surface, path) in _surfaceOverlays)
        {
            var show = ToothClinicalLayers.ShowFilling(state, surface);
            path.Fill = ToothBrushes.Filling;
            path.Visibility = show && !crown ? Visibility.Visible : Visibility.Collapsed;
        }

        foreach (var (surface, path) in _lesionOverlays)
        {
            if (!ToothClinicalLayers.ShowCaries(state, surface)
                || !_surfaceGeometry.TryGetValue(surface, out var geo))
            {
                path.Visibility = Visibility.Collapsed;
                continue;
            }

            var level = ToothClinicalLayers.CariesLevel(state.Surface(surface)) ?? 0.34;
            var bounds = geo.Bounds;
            var cx = bounds.X + bounds.Width / 2;
            var cy = bounds.Y + bounds.Height / 2;
            var rx = Math.Max(2.2, bounds.Width * level / 2);
            var ry = Math.Max(1.8, bounds.Height * level / 2);
            path.Data = new EllipseGeometry(new Point(cx, cy), rx, ry);
            path.Fill = ToothClinicalLayers.CariesBrush(state.Surface(surface));
            path.Visibility = Visibility.Visible;
        }

        ApplyChrome();
    }

    private void OnToolTipOpening(object sender, ToolTipEventArgs e)
    {
        var kind = ToothFdi.Kind(ToothNumber);
        var state = _current.ToothFdi == ToothNumber ? _current : ToothCurrentState.Healthy(ToothNumber);
        ToolTip = $"{ToothNumber}  ·  {kind}  ·  {ToothCurrentStateDisplay.WholeToothValue(state.WholeTooth)}";
    }

    public static string SurfaceDisplayName(ToothSurfaceType surface, string fdi)
    {
        if (surface == ToothSurfaceType.Occlusal && ToothFdi.IsAnterior(fdi))
            return "Incisal";
        if (surface == ToothSurfaceType.Lingual && ToothFdi.IsUpper(fdi))
            return "Palatal";
        return surface.ToString();
    }

    public static string SurfaceDisplayName(ToothSurfaceType surface, ToothKind kind) =>
        surface == ToothSurfaceType.Occlusal && kind == ToothKind.Incisor
            ? "Incisal"
            : surface.ToString();
}
