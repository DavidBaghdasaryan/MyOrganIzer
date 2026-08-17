using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using MyOrganizer.Wpf.Extensions;

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

    public event EventHandler<ToothSurfaceEventArgs>? SurfaceClicked;
    public event EventHandler<ToothSurfaceEventArgs>? SurfaceContextRequested;
    public event EventHandler<ToothSurfaceEventArgs>? ToothClicked;

    private readonly Dictionary<ToothSurfaceType, Path> _paths = [];
    private Path? _outline;
    private readonly HashSet<ToothSurfaceType> _selected = [];
    private ToothSurfaceType? _hovered;
    private IReadOnlyList<ToothMark> _marks = [];

    public ToothControl()
    {
        InitializeComponent();
        Loaded += (_, _) => Rebuild();
        ToolTip = " ";
        ToolTipOpening += OnToolTipOpening;
    }

    public IReadOnlyCollection<ToothSurfaceType> SelectedSurfaces => _selected;

    public bool HasSurfaceSelection => _selected.Count > 0;

    public void ClearSurfaceSelection()
    {
        _selected.Clear();
        RefreshFills();
    }

    public void SetMarks(IReadOnlyList<ToothMark> marks)
    {
        _marks = marks;
        PartBadges.ItemsSource = marks
            .Select(m => new { m.Code, m.Brush })
            .DistinctBy(m => m.Code)
            .ToList();
        RefreshFills();
    }

    private static void OnToothNumberChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToothControl c && c.IsLoaded)
            c.Rebuild();
    }

    private static void OnToothSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToothControl { IsLoaded: true } c)
            c.ApplyWholeToothChrome();
    }

    private void Rebuild()
    {
        PartCanvas.Children.Clear();
        _paths.Clear();

        var kind = ToothFdi.Kind(ToothNumber);
        var geo = ToothGeometries.Get(kind);
        var visual = CreateToothTransform();

        _outline = new Path
        {
            Data = geo.Outline,
            Fill = ToothBrushes.Enamel,
            Stroke = ToothBrushes.Outline,
            StrokeThickness = 1.5,
            StrokeLineJoin = PenLineJoin.Round,
            IsHitTestVisible = false,
            RenderTransform = visual
        };
        PartCanvas.Children.Add(_outline);

        PartCanvas.Children.Add(new Path
        {
            Data = geo.Highlight,
            Fill = ToothBrushes.Highlight,
            IsHitTestVisible = false,
            RenderTransform = visual
        });

        AddSurface(ToothSurfaceType.Buccal, geo.Buccal);
        AddSurface(ToothSurfaceType.Lingual, geo.Lingual);
        AddSurface(ToothSurfaceType.Mesial, geo.Mesial);
        AddSurface(ToothSurfaceType.Distal, geo.Distal);
        AddSurface(ToothSurfaceType.Occlusal, geo.Occlusal);

        ApplyWholeToothChrome();
        RefreshFills();
    }

    private void AddSurface(ToothSurfaceType surface, Geometry data)
    {
        var path = new Path
        {
            Data = data,
            Stroke = ToothBrushes.Seam,
            StrokeThickness = 0.85,
            StrokeLineJoin = PenLineJoin.Round,
            Cursor = Cursors.Hand,
            Tag = surface,
            RenderTransform = CreateToothTransform()
        };
        path.MouseEnter += (_, _) => { _hovered = surface; RefreshFills(); };
        path.MouseLeave += (_, _) => { if (_hovered == surface) _hovered = null; RefreshFills(); };
        path.MouseLeftButtonDown += Surface_MouseLeftButtonDown;
        path.MouseRightButtonDown += Surface_MouseRightButtonDown;
        _paths[surface] = path;
        PartCanvas.Children.Add(path);
    }

    private void Surface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Path { Tag: ToothSurfaceType surface })
            return;

        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
            _selected.Clear();

        if (!_selected.Add(surface))
            _selected.Remove(surface);

        RefreshFills();
        SurfaceClicked?.Invoke(this, CreateArgs(surface));
        e.Handled = true;
    }

    private void Surface_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Path { Tag: ToothSurfaceType surface })
            return;

        if (_selected.Count == 0)
            _selected.Add(surface);

        RefreshFills();
        SurfaceContextRequested?.Invoke(this, CreateArgs(surface));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.OriginalSource is Path)
            return;

        _selected.Clear();
        IsToothSelected = !IsToothSelected;
        RefreshFills();
        ToothClicked?.Invoke(this, CreateArgs(null, wholeTooth: true));
    }

    /// <summary>
    /// Canonical geometry is buccal-top, mesial-left. Scale around the 100×100 design
    /// origin so Mesial/Distal and Buccal/Lingual tags stay correct after mirroring.
    /// </summary>
    private Transform CreateToothTransform()
    {
        var group = new TransformGroup();
        group.Children.Add(new ScaleTransform(
            ToothFdi.MesialOnLeft(ToothNumber) ? 1 : -1,
            ToothFdi.IsUpper(ToothNumber) ? 1 : -1,
            50, 50));
        group.Children.Add(new TranslateTransform(6, 6));
        group.Freeze();
        return group;
    }

    private ToothSurfaceEventArgs CreateArgs(ToothSurfaceType? surface, bool wholeTooth = false) => new()
    {
        ToothNumber = ToothNumber,
        Surface = surface,
        SelectedSurfaces = _selected.ToList(),
        WholeTooth = wholeTooth || _selected.Count == 0
    };

    private void ApplyWholeToothChrome()
    {
        if (_outline is null)
            return;

        if (IsToothSelected)
        {
            _outline.Stroke = ToothBrushes.WholeSelected;
            _outline.StrokeThickness = 2.35;
            _outline.Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(0x1D, 0x4E, 0xD8),
                BlurRadius = 4,
                ShadowDepth = 0,
                Opacity = 0.35
            };
            PartNumber.Foreground = ToothBrushes.WholeSelected;
            PartNumber.FontWeight = FontWeights.Bold;
        }
        else
        {
            _outline.Stroke = ToothBrushes.Outline;
            _outline.StrokeThickness = 1.5;
            _outline.Effect = null;
            PartNumber.Foreground = ToothBrushes.Number;
            PartNumber.FontWeight = FontWeights.SemiBold;
        }
    }

    private void RefreshFills()
    {
        foreach (var (surface, path) in _paths)
        {
            var selected = _selected.Contains(surface);
            var hovered = _hovered == surface;

            path.Fill = ResolveFill(surface, selected, hovered);
            path.StrokeThickness = selected ? 1.15 : hovered ? 1.05 : 0.85;
            path.Stroke = selected
                ? ToothBrushes.SelectedStroke
                : hovered
                    ? ToothBrushes.HoverStroke
                    : ToothBrushes.Seam;
        }
    }

    private Brush ResolveFill(ToothSurfaceType surface, bool selected, bool hovered)
    {
        if (selected)
            return hovered ? ToothBrushes.SelectedHover : ToothBrushes.Selected;
        if (hovered)
            return ToothBrushes.Hover;

        var mark = _marks.LastOrDefault(m => m.Surface == surface)
                   ?? _marks.LastOrDefault(m => m.Surface is null);
        if (mark is not null)
            return Overlay(mark.Brush);

        return surface switch
        {
            ToothSurfaceType.Buccal => ToothBrushes.BuccalTint,
            ToothSurfaceType.Lingual => ToothBrushes.LingualTint,
            ToothSurfaceType.Occlusal => ToothBrushes.OcclusalTint,
            _ => ToothBrushes.ProximalTint
        };
    }

    private static Brush Overlay(Brush source)
    {
        if (source is SolidColorBrush solid)
        {
            var c = solid.Color;
            var b = new SolidColorBrush(Color.FromArgb(150, c.R, c.G, c.B));
            b.Freeze();
            return b;
        }
        return source;
    }

    private void OnToolTipOpening(object sender, ToolTipEventArgs e)
    {
        var kind = ToothFdi.Kind(ToothNumber);
        var surface = _hovered;
        var surfaceName = surface is null
            ? "WholeTooth".T()
            : SurfaceDisplayName(surface.Value, kind).T();
        ToolTip = $"{ToothNumber}  ·  {surfaceName}";
    }

    public static string SurfaceDisplayName(ToothSurfaceType surface, ToothKind kind) =>
        surface == ToothSurfaceType.Occlusal && kind == ToothKind.Incisor
            ? "Incisal"
            : surface.ToString();
}
