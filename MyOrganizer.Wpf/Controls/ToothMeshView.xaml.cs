using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using MyOrganizer.Wpf.Dental;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Lab-only 3D tooth viewer. Canonical clinical view is orthographic occlusal.
/// Lights follow the camera so every preset has the same medical lighting.
/// Rendering, overlays, orbit, and procedures are the FDI 16 golden template.
/// Per-tooth data (mesh, bounds, orientation, surface map, terminology) comes
/// from <see cref="ToothAssetDefinition"/>.
/// </summary>
public partial class ToothMeshView : UserControl
{
    public static readonly DependencyProperty AssetNameProperty =
        DependencyProperty.Register(
            nameof(AssetName),
            typeof(string),
            typeof(ToothMeshView),
            new PropertyMetadata("FDI16", OnAssetChanged));

    private const double Deg = Math.PI / 180.0;
    private const string KnownPackUri =
        "pack://application:,,,/MyOrganizer.Wpf;component/Assets/Teeth/Source/FDI16_High.obj";
    private const string PackPrefix =
        "pack://application:,,,/MyOrganizer.Wpf;component/Assets/Teeth/Source/";

    private readonly PerspectiveCamera _orbitCam = new()
    {
        FieldOfView = 32,
        NearPlaneDistance = 0.05,
        FarPlaneDistance = 200
    };
    private readonly PerspectiveCamera _ghostOrbitCam = new()
    {
        FieldOfView = 32,
        NearPlaneDistance = 0.05,
        FarPlaneDistance = 200
    };

    private Point _last;
    private bool _dragging;
    private StlMeshStats _stats = new();
    private string _viewMode = "none";
    private ClinicalSurfaceMap? _surfaceMap;
    private readonly GeometryModel3D[] _overlayModels = new GeometryModel3D[5];
    private readonly Model3DGroup _overlayGroup = new();
    private bool _showSurfaces;
    private string _inspectSurface = "All";
    private const double OverlayNormalEps = 0.0009;
    private const double DragSlopPx = 5;
    private ClinicalSurface? _hoverSurface;
    private readonly HashSet<ClinicalSurface> _selectedSurfaces = [];
    private bool _pressing;
    private bool _orbitMoved;
    private Point _downPos;
    private int _lastHoverTri = -1;
    private readonly Dictionary<(int, int, int), int> _triByVerts = new();
    private readonly HashSet<ClinicalSurface> _fillingSurfaces = [];
    private readonly HashSet<string> _rootCanalIds = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<string, IReadOnlyList<CanalSample>> _canalPaths =
        new Dictionary<string, IReadOnlyList<CanalSample>>();
    private Material? _canalMaterial;
    private Material? _canalGhostMaterial;
    private Material? _fillingMaterial;
    private Material? _selectedMaterial;
    private Material[]? _surfaceInteractionMaterials;
    private double _theta;
    private double _phi;
    private double _radius = 6;
    private bool _interactionEnabled = true;
    private string _loadedFdi = "16";
    private string _orientationProfile = "ApprovedFdi16";
    private Vector3D _prevOrbitUp;

    public ToothMeshView()
    {
        InitializeComponent();
        Loaded += OnFirstLoad;
        IsVisibleChanged += OnVisibleChanged;
        PreviewMouseLeftButtonDown += OnDragStart;
        PreviewMouseLeftButtonUp += OnDragEnd;
        PreviewMouseMove += OnDragMove;
        PreviewMouseWheel += OnWheel;
        LostMouseCapture += OnLostCapture;
        MouseLeave += OnMouseLeft;
        MouseDoubleClick += (_, _) => ResetToOcclusal();
    }

    private void OnFirstLoad(object sender, RoutedEventArgs e)
    {
        if (!IsVisible)
        {
            return;
        }
        LoadMesh();
    }

    public string AssetName
    {
        get => (string)GetValue(AssetNameProperty);
        set => SetValue(AssetNameProperty, value);
    }

    private static void OnAssetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToothMeshView view && view.IsLoaded && view.IsVisible)
            view.LoadMesh();
    }

    public void LoadRegisteredAsset(string fdi)
    {
        if (NormalizeAssetName(AssetName) == NormalizeAssetName(fdi))
        {
            if (IsLoaded)
                LoadMesh();
            return;
        }
        AssetName = fdi;
    }

    private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!IsVisible || !IsLoaded || CrownModel.Geometry is null || _pressing || _dragging)
            return;
        if (ActualWidth < 8)
            Dispatcher.BeginInvoke(FrameOcclusal, System.Windows.Threading.DispatcherPriority.Loaded);
        else
            FrameOcclusal();
    }

    private void LoadMesh()
    {
        try
        {
            MissingOverlay.Visibility = Visibility.Collapsed;
            var requested = NormalizeAssetName(AssetName);
            if (!ToothAssetRegistry.TryGet(requested, out var asset) || !asset.RuntimeImported)
            {
                return;
            }
            if (_loadedFdi == asset.FdiNumber && CrownModel.Geometry is not null &&
                string.Equals(_orientationProfile, asset.OrientationProfile, StringComparison.Ordinal))
            {
                ToothLabAppearance.Apply(asset.FdiNumber, CrownModel, RootModel, CervicalModel);
                FrameOcclusal();
                return;
            }

            _loadedFdi = asset.FdiNumber;
            _orientationProfile = asset.OrientationProfile;
            _interactionEnabled = asset.ClinicalInteraction;
            LabelPalatal.Text = asset.InnerSurfaceName;
            HintCaption.Text = asset.ClinicalInteraction
                ? "Hover a surface · click to toggle · drag to inspect"
                : asset.SurfaceMapAvailable
                    ? "Surface map: debug overlay · Clinical interaction: Not available · drag to inspect"
                    : "Surface map: Not created · Clinical interaction: Not available · drag to inspect";
            _fillingSurfaces.Clear();
            _selectedSurfaces.Clear();
            _hoverSurface = null;
            _surfaceMap = null;

            var fileName = string.IsNullOrWhiteSpace(asset.RuntimeMesh) ? "FDI16_High.obj" : asset.RuntimeMesh;
            var path = FindSourceFile(fileName);
            var pack = PackPrefix + fileName;
            Stream? stream = path is not null ? File.OpenRead(path) : TryOpenPack(pack, out _);
            if (stream is not null && string.IsNullOrEmpty(path))
                path = pack;
            if (stream is null && fileName == "FDI16_High.obj")
            {
                stream = TryOpenPack(KnownPackUri, out _);
                if (stream is not null)
                    path = KnownPackUri;
            }

            if (stream is null)
            {
                _stats = new StlMeshStats { LoadFailed = true, Error = "source-missing" };
                MissingOverlay.Text =
                    "Drop the Dundee source OBJ into Assets/Teeth/Source (" + fileName + ")";
                MissingOverlay.Visibility = Visibility.Visible;
                return;
            }

            using (stream)
            {
                var parts = StlToothLoader.LoadAlignedParts(stream, out _stats, new MeshLoadOptions
                {
                    MirrorX = asset.MirrorX,
                    OrientFdi16 = asset.OrientationProfile is "ApprovedFdi16"
                        or MaxillaryFirstMolarTemplate.OrientationProfile
                        or MaxillarySecondMolarTemplate.OrientationProfile,
                    OrientationProfile = asset.OrientationProfile
                });
                _stats.SourcePath = path ?? "";
                CrownModel.Geometry = parts.Crown;
                RootModel.Geometry = parts.Root;
                CervicalModel.Geometry = parts.Cervical.TriangleIndices.Count == 0 ? null : parts.Cervical;
                _canalPaths = ToothRootCanalGuide.PathsFromRoot(
                    asset.FdiNumber, parts.Root, _stats.Mirrored, _stats.CrownMeanZ, _stats.RootMeanZ);
                ToothLabAppearance.Apply(asset.FdiNumber, CrownModel, RootModel, CervicalModel);
                BuildTriangleLookup(parts.Crown);
                if (asset.SurfaceMapAvailable)
                    RebuildSurfaceOverlays(parts.Crown);
                else
                {
                    _overlayGroup.Children.Clear();
                    SurfaceOverlayVisual.Content = null;
                    ApplyClinicalOverlays();
                    ApplyInteractionOverlays();
                }
                FrameOcclusal();
            }
        }
        catch (Exception ex)
        {
            _stats = new StlMeshStats { LoadFailed = true, Error = ex.GetType().Name + ": " + ex.Message };
            MissingOverlay.Visibility = Visibility.Visible;
            MissingOverlay.Text = _stats.Error;
        }
    }

    private static string NormalizeAssetName(string? name)
    {
        var t = (name ?? "").Trim();
        if (t.StartsWith("FDI", StringComparison.OrdinalIgnoreCase))
            t = t[3..];
        return string.IsNullOrWhiteSpace(t) ? ToothAssetRegistry.ApprovedFdi : t;
    }

    private static string? FindSourceFile(string fileName)
    {
        foreach (var dir in CandidateDirs())
        {
            if (!Directory.Exists(dir)) continue;
            var preferred = Path.Combine(dir, fileName);
            if (File.Exists(preferred))
                return preferred;
        }
        return null;
    }

    private static Stream? TryOpenPack(string pack, out string error)
    {
        error = "";
        try
        {
            return Application.GetResourceStream(new Uri(pack, UriKind.Absolute))?.Stream;
        }
        catch (IOException ex)
        {
            error = ex.Message;
            return null;
        }
    }

    private static IEnumerable<string> CandidateDirs()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            yield return Path.Combine(dir.FullName, "Assets", "Teeth", "Source");
            yield return Path.Combine(dir.FullName, "MyOrganizer.Wpf", "Assets", "Teeth", "Source");
            dir = dir.Parent;
        }
    }

    public event EventHandler<ToothLabHitEventArgs>? InteractionChanged;

    public void ResetToOcclusal() => FrameOcclusal();

    public void ShowOcclusal() => FrameOcclusal();

    public void ShowBuccal() => FrameSide("buccal", new Vector3D(0, 1, 0.16), new Vector3D(0, 0, 1));

    public void ShowPalatal() => FrameSide(LabelPalatal.Text.Equals("Lingual", StringComparison.OrdinalIgnoreCase) ? "lingual" : "palatal", new Vector3D(0, -1, 0.16), new Vector3D(0, 0, 1));

    public void ShowMesial()
    {
        var x = MandibularMdMirrored() ? -1 : 1;
        FrameSide("mesial", new Vector3D(x, 0, 0.16), new Vector3D(0, 0, 1));
    }

    public void ShowDistal()
    {
        var x = MandibularMdMirrored() ? 1 : -1;
        FrameSide("distal", new Vector3D(x, 0, 0.16), new Vector3D(0, 0, 1));
    }

    public void SetSurfaceDebug(bool show, string inspect)
    {
        _showSurfaces = show;
        _inspectSurface = string.IsNullOrWhiteSpace(inspect) ? "All" : inspect;
        ApplyOverlayVisibility();
    }

    public void SetFillingSurfaces(IEnumerable<string> names)
    {
        var incoming = string.Join(",", names);
        _fillingSurfaces.Clear();
        foreach (var name in names)
        {
            if (TryParseOverlaySurface(name, out var surface))
                _fillingSurfaces.Add(surface);
        }
        ApplyClinicalOverlays();
        ApplyInteractionOverlays();
    }

    public void SetSelectedSurfaces(IEnumerable<string> names)
    {
        _selectedSurfaces.Clear();
        foreach (var name in names)
        {
            if (TryParseOverlaySurface(name, out var surface))
                _selectedSurfaces.Add(surface);
        }
        ApplyInteractionOverlays();
    }

    public void SetRootCanals(IEnumerable<string> ids)
    {
        _rootCanalIds.Clear();
        foreach (var id in ToothRootCanalCatalog.Normalize(_loadedFdi, ids))
            _rootCanalIds.Add(id);
        ApplyCanalOverlays();
    }

    /// <summary>
    /// Empties the lab 3D viewport. Used for odontogram implant teeth, which
    /// have no 3D model in this version. Does not change loaded tooth assets.
    /// </summary>
    public void ClearViewport()
    {
        _loadedFdi = "";
        _orientationProfile = "";
        _interactionEnabled = false;
        _surfaceMap = null;
        _hoverSurface = null;
        _fillingSurfaces.Clear();
        _selectedSurfaces.Clear();
        _rootCanalIds.Clear();
        _canalPaths = new Dictionary<string, IReadOnlyList<CanalSample>>();
        _triByVerts.Clear();
        _overlayGroup.Children.Clear();
        CrownModel.Geometry = null;
        RootModel.Geometry = null;
        CervicalModel.Geometry = null;
        SurfaceOverlayVisual.Content = null;
        ClinicalOverlayVisual.Content = null;
        CanalOverlayVisual.Content = null;
        CanalGhostVisual.Content = null;
        SelectedOverlayVisual.Content = null;
        HoverOverlayVisual.Content = null;
        MissingOverlay.Visibility = Visibility.Collapsed;
    }

    private void RebuildSurfaceOverlays(MeshGeometry3D crown)
    {
        _overlayGroup.Children.Clear();
        SurfaceOverlayVisual.Content = null;
        _surfaceMap = null;
        try
        {
            _surfaceMap = ToothSurfaceMapStore.TryLoad(_loadedFdi, crown);
            if (_surfaceMap is null)
                return;
            var colors = OverlayColorsFor(_loadedFdi);
            for (var s = 0; s < 5; s++)
            {
                var surface = (ClinicalSurface)s;
                var tris = _surfaceMap.Triangles(surface);
                var mesh = CrownSurfaceClassifier.OverlayMesh(crown, tris, OverlayNormalEps);
                var model = new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = OverlayMaterial(colors[s])
                };
                _overlayModels[s] = model;
                _overlayGroup.Children.Add(model);
            }
            ApplyOverlayVisibility();
            ApplyClinicalOverlays();
            ApplyInteractionOverlays();
        }
        catch (Exception)
        {
            _surfaceMap = null;
            SurfaceOverlayVisual.Content = null;
        }
    }

    private static bool OverlayNameMatches(string inspect, ClinicalSurface surface)
    {
        if (string.Equals(inspect, surface.ToString(), StringComparison.OrdinalIgnoreCase))
            return true;
        return surface == ClinicalSurface.Palatal &&
               inspect.Equals("Lingual", StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyOverlayVisibility()
    {
        if (!_showSurfaces || _surfaceMap is null)
        {
            SurfaceOverlayVisual.Content = null;
            SyncCervicalForDebug();
            return;
        }

        _overlayGroup.Children.Clear();
            var inspect = _inspectSurface.Trim();
            for (var s = 0; s < 5; s++)
            {
                var surface = (ClinicalSurface)s;
                if (inspect is "All" || OverlayNameMatches(inspect, surface))
                    _overlayGroup.Children.Add(_overlayModels[s]);
            }
        SurfaceOverlayVisual.Content = _overlayGroup;
        SyncCervicalForDebug();
    }

    private void SyncCervicalForDebug()
    {
        if (CervicalModel.Geometry is null)
            return;
        if (_showSurfaces)
        {
            // Do not make the CEJ strip transparent: that opens a hole between
            // the red crown overlay and the root, and the viewport grey shows
            // through as a jagged white seam. Fill the strip with the root
            // material so the red band meets tan. SurfaceMap is unchanged.
            CervicalModel.Material = RootModel.Material;
            CervicalModel.BackMaterial = RootModel.BackMaterial;
        }
        else
            ToothLabAppearance.Apply(_loadedFdi, CrownModel, RootModel, CervicalModel);
    }

    private static Color[] OverlayColorsFor(string fdi)
    {
        var colors = ToothSurfaceColorConvention.Overlay;
        if (fdi != "38" && fdi != "48")
            return colors;
        var copy = (Color[])colors.Clone();
        copy[0] = Color.FromArgb(0x8C, 0xC4, 0x3A, 0x38);
        return copy;
    }

    private static Material OverlayMaterial(Color c)
    {
        var group = new MaterialGroup();
        group.Children.Add(new DiffuseMaterial(new SolidColorBrush(c))
        {
            AmbientColor = Color.FromRgb(c.R, c.G, c.B)
        });
        group.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(0x28, c.R, c.G, c.B))));
        return group;
    }

    private static Material InteractionMaterial(Color tint, byte glow)
    {
        var group = new MaterialGroup();
        group.Children.Add(new DiffuseMaterial(new SolidColorBrush(tint))
        {
            AmbientColor = Color.FromRgb(tint.R, tint.G, tint.B)
        });
        group.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(glow, tint.R, tint.G, tint.B))));
        group.Freeze();
        return group;
    }

    private void FrameOcclusal()
    {
        _theta = 0;
        _phi = 0;
        _radius = Math.Max(3.2, _stats.Dz * 1.8);
        _viewMode = "occlusal";
        View3D.Camera = OrthoCam;
        OrthoCam.Position = new Point3D(0, 0, _radius);
        OrthoCam.LookDirection = new Vector3D(0, 0, -1);
        OrthoCam.UpDirection = new Vector3D(0, 1, 0);
        var span = Math.Max(_stats.Dx, _stats.Dy);
        OrthoCam.Width = Math.Max(0.8, span * 1.32);
        SyncLights(OrthoCam.LookDirection, OrthoCam.UpDirection);
        UpdateChrome();
        SyncCanalGhostCamera();
    }

    private void FrameSide(string name, Vector3D from, Vector3D up)
    {
        from.Normalize();
        _viewMode = name;
        _radius = BoundingRadius() * 3.4;
        View3D.Camera = OrthoCam;
        OrthoCam.Position = new Point3D(from.X * _radius, from.Y * _radius, from.Z * _radius);
        OrthoCam.LookDirection = new Vector3D(-from.X, -from.Y, -from.Z);
        OrthoCam.UpDirection = up;
        var across = name is "buccal" or "palatal" or "lingual"
            ? Math.Max(_stats.Dx, _stats.Dz)
            : Math.Max(_stats.Dy, _stats.Dz);
        OrthoCam.Width = Math.Max(0.8, across * 1.36);
        SyncLights(OrthoCam.LookDirection, OrthoCam.UpDirection);
        UpdateChrome();
        SyncCanalGhostCamera();
    }

    private void ApplyOrbitCamera()
    {
        View3D.Camera = _orbitCam;
        var x = _radius * Math.Sin(_phi) * Math.Cos(_theta);
        var y = _radius * Math.Sin(_phi) * Math.Sin(_theta);
        var z = _radius * Math.Cos(_phi);
        _orbitCam.Position = new Point3D(x, y, z);
        _orbitCam.LookDirection = new Vector3D(-x, -y, -z);
        var up = SharedOrbitUp(_orbitCam.LookDirection);
        _prevOrbitUp = up;
        _orbitCam.UpDirection = up;
        SyncLights(_orbitCam.LookDirection, _orbitCam.UpDirection);
        UpdateChrome();
        SyncCanalGhostCamera();
    }

    private void SyncCanalGhostCamera()
    {
        if (ReferenceEquals(View3D.Camera, OrthoCam))
        {
            CanalGhostView.Camera = GhostOrthoCam;
            GhostOrthoCam.Position = OrthoCam.Position;
            GhostOrthoCam.LookDirection = OrthoCam.LookDirection;
            GhostOrthoCam.UpDirection = OrthoCam.UpDirection;
            GhostOrthoCam.Width = OrthoCam.Width;
            GhostOrthoCam.NearPlaneDistance = OrthoCam.NearPlaneDistance;
            GhostOrthoCam.FarPlaneDistance = OrthoCam.FarPlaneDistance;
            return;
        }

        CanalGhostView.Camera = _ghostOrbitCam;
        _ghostOrbitCam.Position = _orbitCam.Position;
        _ghostOrbitCam.LookDirection = _orbitCam.LookDirection;
        _ghostOrbitCam.UpDirection = _orbitCam.UpDirection;
        _ghostOrbitCam.FieldOfView = _orbitCam.FieldOfView;
        _ghostOrbitCam.NearPlaneDistance = _orbitCam.NearPlaneDistance;
        _ghostOrbitCam.FarPlaneDistance = _orbitCam.FarPlaneDistance;
    }

    /// <summary>
    /// Shared orbit up from the same θ/φ basis used to place the camera.
    /// Replaces the hard Y↔Z switch at 18° that produced logged upSnaps.
    /// Side views (φ ≥ 18°) remain world Z-up.
    /// </summary>
    private Vector3D SharedOrbitUp(Vector3D look)
    {
        if (look.LengthSquared < 1e-12)
            look = new Vector3D(0, 0, -1);
        else
            look.Normalize();
        var right = new Vector3D(-Math.Sin(_theta), Math.Cos(_theta), 0);
        if (right.LengthSquared < 1e-10)
            right = new Vector3D(1, 0, 0);
        else
            right.Normalize();
        var up = Vector3D.CrossProduct(right, look);
        if (up.LengthSquared < 1e-12)
            up = new Vector3D(0, 0, 1);
        else
            up.Normalize();
        if (_prevOrbitUp.LengthSquared > 0 && Vector3D.DotProduct(up, _prevOrbitUp) < 0)
            up = -up;
        return up;
    }

    private void SyncLights(Vector3D look, Vector3D up)
    {
        if (look.LengthSquared < 1e-12)
            look = new Vector3D(0, 0, -1);
        look.Normalize();
        if (up.LengthSquared < 1e-12)
            up = new Vector3D(0, 1, 0);
        var right = Vector3D.CrossProduct(look, up);
        if (right.LengthSquared < 1e-10)
            right = Math.Abs(look.Z) < 0.92 ? new Vector3D(0, 0, 1) : new Vector3D(1, 0, 0);
        else
            right.Normalize();
        up = Vector3D.CrossProduct(right, look);
        up.Normalize();

        Vector3D key;
        Vector3D fill;
        Vector3D rim;
        if (_viewMode == "occlusal")
        {
            key = look * 0.42 - 0.58 * right + 0.48 * up;
            fill = look * 0.16 + 0.94 * right + 0.10 * up;
            rim = look * 0.14 + 0.10 * right + 0.94 * up;
        }
        else
        {
            key = look * 0.58 - 0.50 * right + 0.40 * up;
            fill = look * 0.28 + 0.88 * right - 0.08 * up;
            rim = -0.48 * look + 0.80 * up + 0.14 * right;
        }
        key.Normalize();
        fill.Normalize();
        rim.Normalize();
        KeyLight.Direction = key;
        FillLight.Direction = fill;
        RimLight.Direction = rim;
    }

    private void SeedOrbitFromCurrent()
    {
        if (View3D.Camera is not ProjectionCamera cam)
            return;
        var p = cam.Position;
        _radius = Math.Max(BoundingRadius() * 2.6, Math.Sqrt(p.X * p.X + p.Y * p.Y + p.Z * p.Z));
        _phi = Math.Clamp(Math.Acos(Math.Clamp(p.Z / Math.Max(1e-6, _radius), -1, 1)), 8 * Deg, 165 * Deg);
        _theta = Math.Atan2(p.Y, p.X);
        if (double.IsNaN(_theta))
            _theta = 0;
    }

    private void UpdateChrome()
    {
        var clinical = _viewMode is "occlusal";
        var vis = clinical ? Visibility.Visible : Visibility.Collapsed;
        LabelBuccal.Visibility = vis;
        LabelPalatal.Visibility = vis;
        LabelDistal.Visibility = vis;
        LabelMesial.Visibility = vis;
        ModeCaption.Text = _viewMode switch
        {
            "occlusal" => "Occlusal view",
            "buccal" => "Buccal view",
            "palatal" or "lingual" => LabelPalatal.Text + " view",
            "mesial" => "Mesial view",
            "distal" => "Distal view",
            _ => "Free inspection"
        };
    }

    private double BoundingRadius()
    {
        var dx = Math.Max(_stats.Dx, 0.001);
        var dy = Math.Max(_stats.Dy, 0.001);
        var dz = Math.Max(_stats.Dz, 0.001);
        return 0.5 * Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }


    private void OnDragStart(object sender, MouseButtonEventArgs e)
    {
        _pressing = true;
        _orbitMoved = false;
        _dragging = false;
        _downPos = e.GetPosition(this);
        _last = _downPos;
        CaptureMouse();
        View3D.IsHitTestVisible = false;
        Focus();
        e.Handled = true;
    }

    private void OnDragEnd(object sender, MouseButtonEventArgs e)
    {
        var click = _pressing && !_orbitMoved;
        _pressing = false;
        _dragging = false;
        View3D.IsHitTestVisible = true;
        if (IsMouseCaptured)
            ReleaseMouseCapture();
        if (click)
            TrySelectAt(e.GetPosition(View3D));
        UpdateHover(e.GetPosition(View3D));
        e.Handled = true;
    }

    private void OnLostCapture(object sender, MouseEventArgs e)
    {
        if (_pressing && Mouse.LeftButton == MouseButtonState.Pressed)
        {
            CaptureMouse();
            View3D.IsHitTestVisible = false;
            _last = Mouse.GetPosition(this);
            return;
        }
        _pressing = false;
        _dragging = false;
        View3D.IsHitTestVisible = true;
    }

    private void OnDragMove(object sender, MouseEventArgs e)
    {
        var p = e.GetPosition(this);
        if (_pressing && e.LeftButton == MouseButtonState.Pressed)
        {
            if (!_orbitMoved)
            {
                var dx0 = p.X - _downPos.X;
                var dy0 = p.Y - _downPos.Y;
                if (dx0 * dx0 + dy0 * dy0 >= DragSlopPx * DragSlopPx)
                {
                    _orbitMoved = true;
                    _dragging = true;
                    _prevOrbitUp = new Vector3D();
                    if (_viewMode != "orbit")
                    {
                        SeedOrbitFromCurrent();
                        _viewMode = "orbit";
                        ApplyOrbitCamera();
                    }
                    _last = p;
                }
            }
            if (_dragging)
            {
                var dx = p.X - _last.X;
                var dy = p.Y - _last.Y;
                _last = p;
                _theta -= dx * 0.008;
                _phi = Math.Clamp(_phi - dy * 0.008, 6 * Deg, 165 * Deg);
                ApplyOrbitCamera();
            }
        }
        if (!_pressing && !_dragging)
            UpdateHover(e.GetPosition(View3D));
    }

    private void OnMouseLeft(object sender, MouseEventArgs e)
    {
        if (_pressing || _dragging)
            return;
        if (_hoverSurface is null)
            return;
        _hoverSurface = null;
        _lastHoverTri = -1;
        Cursor = Cursors.Arrow;
        ApplyInteractionOverlays();
        RaiseInteraction(-1);
    }

    private void UpdateHover(Point viewportPoint)
    {
        if (_pressing || _dragging)
            return;
        if (!_interactionEnabled)
        {
            Cursor = Cursors.Arrow;
            return;
        }
        var hit = HitCrownSurface(viewportPoint, out var tri);
        Cursor = hit is null ? Cursors.Arrow : Cursors.Hand;
        if (hit == _hoverSurface)
        {
            _lastHoverTri = tri;
            return;
        }
        _hoverSurface = hit;
        _lastHoverTri = tri;
        ApplyInteractionOverlays();
        RaiseInteraction(tri);
    }

    private void TrySelectAt(Point viewportPoint)
    {
        if (!_interactionEnabled)
            return;
        var hit = HitCrownSurface(viewportPoint, out var tri);
        if (hit is not ClinicalSurface surface)
            return;
        if (!_selectedSurfaces.Add(surface))
            _selectedSurfaces.Remove(surface);
        ApplyInteractionOverlays();
        RaiseInteraction(tri);
    }

    private ClinicalSurface? HitCrownSurface(Point viewportPoint, out int triangle)
    {
        triangle = -1;
        if (_surfaceMap is null || CrownModel.Geometry is null)
            return null;
        var tri = -1;
        ClinicalSurface? surface = null;
        VisualTreeHelper.HitTest(View3D, null, result =>
        {
            if (result is not RayMeshGeometry3DHitTestResult mesh)
                return HitTestResultBehavior.Continue;
            if (ReferenceEquals(mesh.ModelHit, RootModel))
                return HitTestResultBehavior.Stop;
            if (!ReferenceEquals(mesh.ModelHit, CrownModel))
                return HitTestResultBehavior.Continue;
            if (!_triByVerts.TryGetValue(Sort3(mesh.VertexIndex1, mesh.VertexIndex2, mesh.VertexIndex3), out tri))
                return HitTestResultBehavior.Stop;
            surface = _surfaceMap.SurfaceOf(tri);
            return HitTestResultBehavior.Stop;
        }, new PointHitTestParameters(viewportPoint));
        triangle = tri;
        return surface;
    }

    private void BuildTriangleLookup(MeshGeometry3D crown)
    {
        _triByVerts.Clear();
        var idx = crown.TriangleIndices;
        var n = idx.Count / 3;
        for (var t = 0; t < n; t++)
            _triByVerts[Sort3(idx[t * 3], idx[t * 3 + 1], idx[t * 3 + 2])] = t;
    }

    private static (int, int, int) Sort3(int a, int b, int c)
    {
        if (a > b) (a, b) = (b, a);
        if (b > c) (b, c) = (c, b);
        if (a > b) (a, b) = (b, a);
        return (a, b, c);
    }

    private void ApplyClinicalOverlays()
    {
        _fillingMaterial ??= InteractionMaterial(Color.FromArgb(0x6A, 0xB4, 0xBC, 0xC4), 0x16);
        if (_fillingSurfaces.Count == 0)
        {
            ClinicalOverlayVisual.Content = null;
            return;
        }
        var group = new Model3DGroup();
        var skipped = 0;
        foreach (var surface in _fillingSurfaces)
        {
            var model = OverlayFor(surface, _fillingMaterial);
            if (model is not null)
                group.Children.Add(model);
            else
                skipped++;
        }
        ClinicalOverlayVisual.Content = group.Children.Count == 0 ? null : group;
    }

    private void ApplyCanalOverlays()
    {
        _canalMaterial ??= CanalMaterial();
        _canalGhostMaterial ??= CanalGhostMaterial();
        if (_rootCanalIds.Count == 0 || _canalPaths.Count == 0)
        {
            CanalOverlayVisual.Content = null;
            CanalGhostVisual.Content = null;
            SyncCanalGhostCamera();
            return;
        }

        var front = new Model3DGroup();
        var ghost = new Model3DGroup();
        foreach (var id in _rootCanalIds)
        {
            if (!_canalPaths.TryGetValue(id, out var path) || path.Count < 2)
                continue;
            var mesh = ToothRootCanalGuide.Tube(ToothRootCanalGuide.Centerline(path), 0.016);
            front.Children.Add(new GeometryModel3D
            {
                Geometry = mesh,
                Material = _canalMaterial,
                BackMaterial = _canalMaterial
            });
            ghost.Children.Add(new GeometryModel3D
            {
                Geometry = mesh,
                Material = _canalGhostMaterial,
                BackMaterial = _canalGhostMaterial
            });
        }
        CanalOverlayVisual.Content = front.Children.Count == 0 ? null : front;
        CanalGhostVisual.Content = ghost.Children.Count == 0 ? null : ghost;
        SyncCanalGhostCamera();
    }

    private static Material CanalMaterial()
    {
        var group = new MaterialGroup();
        group.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(0xB0, 0xB0, 0x3A, 0x3A))));
        group.Freeze();
        return group;
    }

    private static Material CanalGhostMaterial()
    {
        var group = new MaterialGroup();
        group.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(0x30, 0x94, 0x28, 0x28))));
        group.Freeze();
        return group;
    }

    private void ApplyInteractionOverlays()
    {
        if (_selectedSurfaces.Count == 0)
        {
            SelectedOverlayVisual.Content = null;
        }
        else
        {
            var group = new Model3DGroup();
            foreach (var surface in _selectedSurfaces)
            {
                var model = OverlayFor(surface, SurfaceInteractionMaterial(surface));
                if (model is not null)
                    group.Children.Add(model);
            }
            SelectedOverlayVisual.Content = group.Children.Count == 0 ? null : group;
        }
        var hover = _hoverSurface is ClinicalSurface h && !_selectedSurfaces.Contains(h)
            ? _hoverSurface
            : null;
        HoverOverlayVisual.Content = hover is ClinicalSurface hs
            ? OverlayFor(hs, SurfaceInteractionMaterial(hs))
            : null;
    }

    private Material SurfaceInteractionMaterial(ClinicalSurface surface)
    {
        _surfaceInteractionMaterials ??= new Material[5];
        var i = (int)surface;
        if (i < 0 || i >= _surfaceInteractionMaterials.Length)
            return _selectedMaterial ??= InteractionMaterial(Color.FromArgb(0x4C, 0x3A, 0x8C, 0xD2), 0x1C);
        return _surfaceInteractionMaterials[i] ??=
            InteractionMaterial(ToothSurfaceColorConvention.Overlay[i], 0x20);
    }

    private GeometryModel3D? OverlayFor(ClinicalSurface? surface, Material material)
    {
        if (surface is null)
            return null;
        var model = _overlayModels[(int)surface];
        if (model?.Geometry is null)
            return null;
        return new GeometryModel3D
        {
            Geometry = model.Geometry,
            Material = material
        };
    }

    private void RaiseInteraction(int triangle)
    {
        var selected = SelectedNames();
        InteractionChanged?.Invoke(this, new ToothLabHitEventArgs
        {
            Hover = _hoverSurface is ClinicalSurface hover ? DisplaySurfaceName(hover) : null,
            Selected = selected.Count == 0 ? null : string.Join(",", selected),
            SelectedSurfaces = selected,
            Triangle = triangle
        });
    }



    private string DisplaySurfaceName(ClinicalSurface surface) =>
        surface == ClinicalSurface.Palatal ? LabelPalatal.Text : surface.ToString();

    private bool MandibularMdMirrored() =>
        ToothAssetRegistry.TryGet(_loadedFdi, out var asset) && asset.ContralateralCameraMirror;

    private static bool TryParseOverlaySurface(string name, out ClinicalSurface surface)
    {
        if (name.Equals("Lingual", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Palatal", StringComparison.OrdinalIgnoreCase))
        {
            surface = ClinicalSurface.Palatal;
            return true;
        }
        return Enum.TryParse(name, true, out surface);
    }

    private IReadOnlyList<string> SelectedNames()
    {
        var names = new List<string>();
        foreach (var surface in Enum.GetValues<ClinicalSurface>())
        {
            if (_selectedSurfaces.Contains(surface))
                names.Add(DisplaySurfaceName(surface));
        }
        return names;
    }

    private void OnWheel(object sender, MouseWheelEventArgs e)
    {
        var r = BoundingRadius();
        var factor = e.Delta > 0 ? 0.9 : 1.11;
        if (View3D.Camera is OrthographicCamera ortho)
        {
            ortho.Width = Math.Clamp(ortho.Width * factor, r * 0.9, r * 8);
        }
        else
        {
            _radius = Math.Clamp(_radius * factor, r * 2.2, r * 14);
            ApplyOrbitCamera();
        }
        e.Handled = true;
    }

}

public sealed class ToothLabHitEventArgs : EventArgs
{
    public string? Hover { get; init; }
    public string? Selected { get; init; }
    public IReadOnlyList<string> SelectedSurfaces { get; init; } = [];
    public int Triangle { get; init; }
}
