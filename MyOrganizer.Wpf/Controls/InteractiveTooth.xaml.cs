using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MyOrganizer.Wpf.Controls;

public partial class InteractiveTooth : UserControl
{
    public static readonly DependencyProperty ToothNumberProperty =
        DependencyProperty.Register(nameof(ToothNumber), typeof(string), typeof(InteractiveTooth),
            new PropertyMetadata("16", OnVisualChanged));

    public static readonly DependencyProperty SurfaceStatesProperty =
        DependencyProperty.Register(nameof(SurfaceStates), typeof(IReadOnlyDictionary<ToothSurfaceType, ToothSurfaceVisual>),
            typeof(InteractiveTooth), new PropertyMetadata(null, OnStatesChanged));

    public static readonly DependencyProperty SelectedSurfacesProperty =
        DependencyProperty.Register(nameof(SelectedSurfaces), typeof(IReadOnlyList<ToothSurfaceType>),
            typeof(InteractiveTooth), new PropertyMetadata(Array.Empty<ToothSurfaceType>(), OnSelectedChanged));

    private readonly Dictionary<ToothSurfaceType, Path> _fills = [];
    private readonly Dictionary<ToothSurfaceType, Path> _hovers = [];
    private readonly Dictionary<ToothSurfaceType, Path> _hits = [];
    private readonly HashSet<ToothSurfaceType> _selected = [];
    private ToothSurfaceType? _hovered;
    private bool _suppressSelectedCallback;

    public InteractiveTooth()
    {
        InitializeComponent();
        Loaded += (_, _) => Rebuild();
    }

    public string ToothNumber
    {
        get => (string)GetValue(ToothNumberProperty);
        set => SetValue(ToothNumberProperty, value);
    }

    public IReadOnlyDictionary<ToothSurfaceType, ToothSurfaceVisual>? SurfaceStates
    {
        get => (IReadOnlyDictionary<ToothSurfaceType, ToothSurfaceVisual>?)GetValue(SurfaceStatesProperty);
        set => SetValue(SurfaceStatesProperty, value);
    }

    public IReadOnlyList<ToothSurfaceType> SelectedSurfaces
    {
        get => (IReadOnlyList<ToothSurfaceType>)GetValue(SelectedSurfacesProperty);
        set => SetValue(SelectedSurfacesProperty, value);
    }

    public event EventHandler<ToothSurfaceEventArgs>? SurfaceClicked;
    public event EventHandler<ToothSurfaceEventArgs>? SurfaceHovered;

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InteractiveTooth { IsLoaded: true } tooth)
            tooth.Rebuild();
    }

    private static void OnStatesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InteractiveTooth { IsLoaded: true } tooth)
            tooth.ApplyFills();
    }

    private static void OnSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InteractiveTooth tooth)
            tooth.SyncSelectionFromProperty();
    }

    private void Rebuild()
    {
        PartCanvas.Children.Clear();
        _fills.Clear();
        _hovers.Clear();
        _hits.Clear();

        var geo = FacialMolar16Geometry.Get();
        var flip = CreateFlip();

        Decor(geo.PalatalRoot, RootFill, RootStroke, 0.7, flip, hit: false);
        Decor(geo.Trunk, RootFill, RootStroke, 0.7, flip, hit: false);
        Decor(geo.MesialRoot, RootFill, RootStroke, 0.7, flip, hit: false);
        Decor(geo.DistalRoot, RootFill, RootStroke, 0.7, flip, hit: false);

        var enamel = Decor(geo.Crown, EnamelFill, OutlineStroke, 1.05, flip, hit: false);
        enamel.StrokeLineJoin = PenLineJoin.Round;

        AddSurface(ToothSurfaceType.Buccal, geo.Buccal, flip);
        AddSurface(ToothSurfaceType.Mesial, geo.Mesial, flip);
        AddSurface(ToothSurfaceType.Distal, geo.Distal, flip);
        AddSurface(ToothSurfaceType.Lingual, geo.Lingual, flip);
        AddSurface(ToothSurfaceType.Occlusal, geo.Occlusal, flip);

        Decor(geo.Highlight, HighlightFill, Brushes.Transparent, 0, flip, hit: false);
        var fissure = Decor(geo.Fissure, Brushes.Transparent, FissureStroke, 1.2, flip, hit: false);
        fissure.StrokeStartLineCap = PenLineCap.Round;
        fissure.StrokeEndLineCap = PenLineCap.Round;
        var cervix = Decor(geo.Cervix, Brushes.Transparent, CervixStroke, 1.1, flip, hit: false);
        cervix.StrokeStartLineCap = PenLineCap.Round;
        cervix.StrokeEndLineCap = PenLineCap.Round;

        ApplyFills();
        ApplySelectionVisuals();
    }

    private void AddSurface(ToothSurfaceType surface, Geometry data, Transform flip)
    {
        var fill = Decor(data, Brushes.Transparent, Brushes.Transparent, 0, flip, hit: false);
        _fills[surface] = fill;

        var hover = Decor(data, HoverWash, Brushes.Transparent, 0, flip, hit: false);
        hover.Opacity = 0;
        _hovers[surface] = hover;

        var hit = Decor(data, Brushes.Transparent, Brushes.Transparent, 0, flip, hit: true);
        hit.Tag = surface;
        hit.Cursor = Cursors.Hand;
        hit.MouseLeftButtonDown += Surface_MouseLeftButtonDown;
        hit.MouseEnter += Surface_MouseEnter;
        hit.MouseLeave += Surface_MouseLeave;
        _hits[surface] = hit;
    }

    private Path Decor(Geometry data, Brush fill, Brush stroke, double thickness, Transform flip, bool hit)
    {
        var path = new Path
        {
            Data = data,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = thickness,
            StrokeLineJoin = PenLineJoin.Round,
            IsHitTestVisible = hit,
            RenderTransform = flip
        };
        PartCanvas.Children.Add(path);
        return path;
    }

    private Transform CreateFlip()
    {
        var scale = new ScaleTransform(ToothFdi.MesialOnLeft(ToothNumber) ? 1 : -1, 1, 100, 140);
        scale.Freeze();
        return scale;
    }

    private void ApplyFills()
    {
        foreach (var (surface, path) in _fills)
        {
            ToothSurfaceVisual? visual = null;
            SurfaceStates?.TryGetValue(surface, out visual);
            var fill = ToothSurfaceAppearance.FillFor(visual);
            path.Fill = fill;
            path.Stroke = ToothSurfaceAppearance.IsHealthy(visual?.ProcedureKey) && visual?.Color is null
                ? Brushes.Transparent
                : SeamStroke;
            path.StrokeThickness = path.Stroke == Brushes.Transparent ? 0 : 0.7;
        }

        // #region agent log
        AgentLogFills();
        // #endregion

        ApplySelectionVisuals();
    }

    private void SyncSelectionFromProperty()
    {
        if (_suppressSelectedCallback)
            return;

        _selected.Clear();
        foreach (var surface in SelectedSurfaces ?? [])
            _selected.Add(surface);
        if (IsLoaded)
            ApplySelectionVisuals();
    }

    private void ApplySelectionVisuals()
    {
        foreach (var (surface, path) in _hits)
        {
            var on = _selected.Contains(surface);
            path.Stroke = on ? SelectedStroke : Brushes.Transparent;
            path.StrokeThickness = on ? 1.35 : 0;
            path.Fill = on ? SelectedWash : Brushes.Transparent;
        }
    }

    private void Surface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Path { Tag: ToothSurfaceType surface })
            return;

        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
            _selected.Clear();

        if (!_selected.Add(surface))
            _selected.Remove(surface);

        PushSelectedProperty();
        ApplySelectionVisuals();
        SurfaceClicked?.Invoke(this, CreateArgs(surface));
        e.Handled = true;
    }

    private void Surface_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not Path { Tag: ToothSurfaceType surface })
            return;
        _hovered = surface;
        Fade(_hovers[surface], 0.2);
        SurfaceHovered?.Invoke(this, CreateArgs(surface));
    }

    private void Surface_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is not Path { Tag: ToothSurfaceType surface })
            return;
        if (_hovered == surface)
            _hovered = null;
        Fade(_hovers[surface], 0);
        SurfaceHovered?.Invoke(this, CreateArgs(null));
    }

    private void PushSelectedProperty()
    {
        _suppressSelectedCallback = true;
        SetCurrentValue(SelectedSurfacesProperty, _selected.ToList());
        _suppressSelectedCallback = false;
    }

    private ToothSurfaceEventArgs CreateArgs(ToothSurfaceType? surface) => new()
    {
        ToothNumber = ToothNumber,
        Surface = surface,
        SelectedSurfaces = _selected.ToList(),
        WholeTooth = _selected.Count == 0
    };

    private static void Fade(Path path, double to)
    {
        var animation = new DoubleAnimation(to, TimeSpan.FromMilliseconds(120))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        path.BeginAnimation(OpacityProperty, animation);
    }

    private static readonly Brush EnamelFill = CreateEnamel();
    private static readonly Brush RootFill = CreateRoot();
    private static readonly Brush OutlineStroke = FreezeRgb(0xD4, 0xC0, 0xA0);
    private static readonly Brush RootStroke = FreezeRgb(0xC2, 0xA0, 0x78);
    private static readonly Brush SeamStroke = FreezeArgb(0x55, 0xB8, 0xA4, 0x88);
    private static readonly Brush FissureStroke = FreezeArgb(0x9A, 0xA8, 0x88, 0x68);
    private static readonly Brush CervixStroke = FreezeArgb(0x66, 0xC4, 0xB0, 0x90);
    private static readonly Brush HighlightFill = FreezeArgb(0x48, 0xFF, 0xFF, 0xFF);
    private static readonly Brush HoverWash = FreezeArgb(0x5A, 0xFF, 0xFF, 0xFF);
    private static readonly Brush SelectedWash = FreezeArgb(0x3A, 0x37, 0x82, 0xF6);
    private static readonly Brush SelectedStroke = FreezeRgb(0x25, 0x63, 0xEB);

    private static Brush CreateEnamel()
    {
        var brush = new RadialGradientBrush
        {
            GradientOrigin = new Point(0.34, 0.78),
            Center = new Point(0.46, 0.58),
            RadiusX = 0.78,
            RadiusY = 0.9
        };
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xFC, 0xF6), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xF6, 0xEE, 0xDC), 0.42));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xE8, 0xD6, 0xB6), 0.78));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xD0, 0xB6, 0x92), 1));
        brush.Freeze();
        return brush;
    }

    private static Brush CreateRoot()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0.5),
            EndPoint = new Point(1, 0.5)
        };
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xB4, 0x90, 0x64), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xEC, 0xD4, 0xB0), 0.36));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xF6, 0xE8, 0xCC), 0.5));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xE2, 0xC6, 0x9C), 0.7));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xA8, 0x86, 0x5C), 1));
        brush.Freeze();
        return brush;
    }

    private static SolidColorBrush FreezeRgb(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    private static SolidColorBrush FreezeArgb(byte a, byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
        brush.Freeze();
        return brush;
    }

    // #region agent log
    private void AgentLogFills()
    {
        var parts = _fills.Select(kv =>
        {
            ToothSurfaceVisual? visual = null;
            SurfaceStates?.TryGetValue(kv.Key, out visual);
            var fill = kv.Value.Fill;
            var alpha = fill is SolidColorBrush solid ? solid.Color.A : (byte)(fill == Brushes.Transparent ? 0 : 255);
            return "\"" + kv.Key + "\":{\"key\":\"" + (visual?.ProcedureKey ?? "null") + "\",\"alpha\":" + alpha + "}";
        });
        var json = "{\"tooth\":\"" + ToothNumber + "\",\"view\":\"buccal\",\"surfaces\":{" + string.Join(",", parts) + "}}";
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"refit\",\"hypothesisId\":\"R1\",\"location\":\"InteractiveTooth.xaml.cs:ApplyFills\",\"message\":\"surface fill alphas\",\"data\":" + json + ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { System.IO.File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { /* debug ingest must not affect the tooth */ }
    }
    // #endregion
}
