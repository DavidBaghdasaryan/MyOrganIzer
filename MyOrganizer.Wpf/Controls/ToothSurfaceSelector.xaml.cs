using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MyOrganizer.Wpf.Extensions;

namespace MyOrganizer.Wpf.Controls;

public partial class ToothSurfaceSelector : UserControl
{
    public static readonly DependencyProperty ToothNumberProperty =
        DependencyProperty.Register(nameof(ToothNumber), typeof(string), typeof(ToothSurfaceSelector),
            new PropertyMetadata("11", OnToothNumberChanged));

    private readonly Dictionary<ToothSurfaceType, Path> _paths = [];
    private readonly HashSet<ToothSurfaceType> _selected = [];
    private Path? _outline;

    public ToothSurfaceSelector()
    {
        InitializeComponent();
        Loaded += (_, _) => Rebuild();
    }

    public string ToothNumber
    {
        get => (string)GetValue(ToothNumberProperty);
        set => SetValue(ToothNumberProperty, value);
    }

    public event EventHandler<ToothSurfaceEventArgs>? SurfacesChanged;

    public IReadOnlyCollection<ToothSurfaceType> SelectedSurfaces => _selected;
    public bool HasSurfaceSelection => _selected.Count > 0;

    public void ClearSelection()
    {
        _selected.Clear();
        Refresh();
        RaiseChanged(null);
    }

    public void SetSelection(IEnumerable<ToothSurfaceType> surfaces)
    {
        _selected.Clear();
        foreach (var surface in surfaces)
            _selected.Add(surface);
        Refresh();
    }

    private static void OnToothNumberChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToothSurfaceSelector c && c.IsLoaded)
        {
            c._selected.Clear();
            c.Rebuild();
        }
    }

    private void Rebuild()
    {
        PartCanvas.Children.Clear();
        _paths.Clear();

        var occlusal = ToothGeometries.Occlusal(ToothFdi.Kind(ToothNumber));
        var visual = CreateVisualTransform();

        _outline = Decor(occlusal.Outline, ToothBrushes.Enamel, ToothBrushes.Outline, 1.4, visual, hit: false);
        AddZone(ToothSurfaceType.Buccal, occlusal.Buccal, visual);
        AddZone(ToothSurfaceType.Lingual, occlusal.Lingual, visual);
        AddZone(ToothSurfaceType.Mesial, occlusal.Mesial, visual);
        AddZone(ToothSurfaceType.Distal, occlusal.Distal, visual);
        AddZone(ToothSurfaceType.Occlusal, occlusal.Occlusal, visual);
        Decor(occlusal.Highlight, ToothBrushes.Highlight, Brushes.Transparent, 0, visual, hit: false);
        if (occlusal.Fissure is not null)
        {
            var fissure = Decor(occlusal.Fissure, Brushes.Transparent, ToothBrushes.Fissure, 1.15, visual, hit: false);
            fissure.StrokeStartLineCap = PenLineCap.Round;
            fissure.StrokeEndLineCap = PenLineCap.Round;
        }

        AddLetter(ToothSurfaceType.Buccal, 50, 20);
        AddLetter(ToothSurfaceType.Lingual, 50, 80);
        AddLetter(ToothSurfaceType.Occlusal, 50, 50);
        if (ToothFdi.MesialOnLeft(ToothNumber))
        {
            AddLetter(ToothSurfaceType.Mesial, 18, 50);
            AddLetter(ToothSurfaceType.Distal, 82, 50);
        }
        else
        {
            AddLetter(ToothSurfaceType.Mesial, 82, 50);
            AddLetter(ToothSurfaceType.Distal, 18, 50);
        }

        UpdateLabels();
        Refresh();
    }

    private Transform CreateVisualTransform()
    {
        var scale = new ScaleTransform(ToothFdi.MesialOnLeft(ToothNumber) ? 1 : -1, 1, 50, 50);
        scale.Freeze();
        return scale;
    }

    private Path Decor(Geometry data, Brush fill, Brush stroke, double thickness, Transform visual, bool hit)
    {
        var path = new Path
        {
            Data = data,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = thickness,
            StrokeLineJoin = PenLineJoin.Round,
            IsHitTestVisible = hit,
            Cursor = hit ? Cursors.Hand : Cursors.Arrow,
            RenderTransform = visual
        };
        PartCanvas.Children.Add(path);
        return path;
    }

    private void AddZone(ToothSurfaceType surface, Geometry data, Transform visual)
    {
        var path = Decor(data, Brushes.Transparent, ToothBrushes.Seam, 0.85, visual, hit: true);
        path.Tag = surface;
        path.MouseLeftButtonDown += Zone_MouseLeftButtonDown;
        _paths[surface] = path;
    }

    private void AddLetter(ToothSurfaceType surface, double x, double y)
    {
        var letter = SurfaceLetter(surface);
        var label = new TextBlock
        {
            Text = letter,
            FontFamily = new FontFamily("Segoe UI"),
            FontWeight = FontWeights.SemiBold,
            FontSize = 10,
            Foreground = ToothBrushes.Number,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(label, x - 5);
        Canvas.SetTop(label, y - 8);
        PartCanvas.Children.Add(label);
    }

    private void Zone_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Path { Tag: ToothSurfaceType surface })
            return;

        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
            _selected.Clear();

        if (!_selected.Add(surface))
            _selected.Remove(surface);

        Refresh();
        RaiseChanged(surface);
        e.Handled = true;
    }

    private void Refresh()
    {
        foreach (var (surface, path) in _paths)
        {
            var on = _selected.Contains(surface);
            path.Fill = on ? ToothBrushes.SelectorFill : Brushes.Transparent;
            path.Stroke = on ? ToothBrushes.SelectedStroke : ToothBrushes.Seam;
            path.StrokeThickness = on ? 1.45 : 0.85;
        }
    }

    private void UpdateLabels()
    {
        var fdi = ToothNumber;
        LabelBuccal.Text = ToothControl.SurfaceDisplayName(ToothSurfaceType.Buccal, fdi).T();
        LabelLingual.Text = ToothControl.SurfaceDisplayName(ToothSurfaceType.Lingual, fdi).T();
        var mesial = ToothControl.SurfaceDisplayName(ToothSurfaceType.Mesial, fdi).T();
        var distal = ToothControl.SurfaceDisplayName(ToothSurfaceType.Distal, fdi).T();
        if (ToothFdi.MesialOnLeft(fdi))
        {
            LabelMesial.Text = mesial;
            LabelDistal.Text = distal;
        }
        else
        {
            LabelMesial.Text = distal;
            LabelDistal.Text = mesial;
        }
    }

    private static string SurfaceLetter(ToothSurfaceType surface) => surface switch
    {
        ToothSurfaceType.Mesial => "M",
        ToothSurfaceType.Distal => "D",
        ToothSurfaceType.Buccal => "B",
        ToothSurfaceType.Lingual => "L",
        _ => "O"
    };

    private void RaiseChanged(ToothSurfaceType? surface) =>
        SurfacesChanged?.Invoke(this, new ToothSurfaceEventArgs
        {
            ToothNumber = ToothNumber,
            Surface = surface,
            SelectedSurfaces = _selected.ToList(),
            WholeTooth = _selected.Count == 0
        });
}
