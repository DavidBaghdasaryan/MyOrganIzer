using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Lab-only 3D tooth viewer. Canonical clinical view is orthographic occlusal.
/// Lights follow the camera so every preset has the same medical lighting.
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
    private Material? _fillingMaterial;
    private Material? _hoverMaterial;
    private Material? _selectedMaterial;
    private Material? _hoverOnFillingMaterial;
    private Material? _selectedOnFillingMaterial;
    private double _theta;
    private double _phi;
    private double _radius = 6;
    private bool _interactionEnabled = true;
    private string _loadedFdi = "16";
    private string _orientationProfile = "ApprovedFdi16";

    public ToothMeshView()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadMesh();
        IsVisibleChanged += OnVisibleChanged;
        PreviewMouseLeftButtonDown += OnDragStart;
        PreviewMouseLeftButtonUp += OnDragEnd;
        PreviewMouseMove += OnDragMove;
        PreviewMouseWheel += OnWheel;
        MouseLeave += OnMouseLeft;
        MouseDoubleClick += (_, _) => ResetToOcclusal();
    }

    public string AssetName
    {
        get => (string)GetValue(AssetNameProperty);
        set => SetValue(AssetNameProperty, value);
    }

    private static void OnAssetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToothMeshView view && view.IsLoaded)
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
        if (IsVisible && IsLoaded && CrownModel.Geometry is not null)
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
                // #region agent log
                AgentLog("G", "mesh-skip",
                    "{\"requested\":\"" + requested +
                    "\",\"imported\":false,\"reason\":\"not-runtime-imported\"}");
                // #endregion
                return;
            }
            if (_loadedFdi == asset.FdiNumber && CrownModel.Geometry is not null &&
                string.Equals(_orientationProfile, asset.OrientationProfile, StringComparison.Ordinal))
            {
                ToothLabAppearance.Apply(asset.FdiNumber, CrownModel, RootModel, CervicalModel);
                FrameOcclusal();
                // #region agent log
                AgentLog("H", "appearance", AppearanceLog(asset.FdiNumber, reused: true));
                AgentLog("I", "cervical", CervicalLog(asset.FdiNumber));
                AgentLog("G", "mesh-reuse",
                    "{\"fdi\":\"" + asset.FdiNumber + "\",\"file\":\"" + Esc(asset.RuntimeMesh ?? "") + "\"}");
                // #endregion
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
                AgentLog("A", "stl-load-failed", ToJson());
                return;
            }

            using (stream)
            {
                var parts = StlToothLoader.LoadAlignedParts(stream, out _stats, new MeshLoadOptions
                {
                    MirrorX = asset.MirrorX,
                    OrientFdi16 = asset.OrientationProfile == "ApprovedFdi16",
                    OrientationProfile = asset.OrientationProfile
                });
                _stats.SourcePath = path ?? "";
                CrownModel.Geometry = parts.Crown;
                RootModel.Geometry = parts.Root;
                CervicalModel.Geometry = parts.Cervical.TriangleIndices.Count == 0 ? null : parts.Cervical;
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
                    AgentLog("B", "map-missing", "{\"source\":\"not-configured\",\"fdi\":\"" + asset.FdiNumber + "\"}");
                }
                FrameOcclusal();
                AgentLog("A", "mesh-loaded", ToJson());
                // #region agent log
                AgentLog("F", "asset-loaded",
                    "{\"fdi\":\"" + asset.FdiNumber +
                    "\",\"file\":\"" + Esc(fileName) +
                    "\",\"path\":\"" + Esc(path ?? "") +
                    "\",\"mirrorX\":" + (asset.MirrorX ? "true" : "false") +
                    ",\"profile\":\"" + Esc(asset.OrientationProfile) +
                    "\",\"map\":" + (asset.SurfaceMapAvailable ? "true" : "false") +
                    ",\"interaction\":" + (_interactionEnabled ? "true" : "false") +
                    ",\"inner\":\"" + Esc(asset.InnerSurfaceName) +
                    "\",\"polypaint\":" + _stats.PolypaintColors +
                    ",\"split\":\"" + Esc(_stats.SplitSource) +
                    "\",\"rootClusters\":" + _stats.RootClusters +
                    ",\"vertices\":" + _stats.VertexCount +
                    ",\"yawDeg\":" + _stats.YawDeg.ToString("0.###", CultureInfo.InvariantCulture) + "}");
                AgentLog("H", "appearance", AppearanceLog(asset.FdiNumber, reused: false));
                AgentLog("I", "cervical", CervicalLog(asset.FdiNumber));
                // #endregion
            }
        }
        catch (Exception ex)
        {
            _stats = new StlMeshStats { LoadFailed = true, Error = ex.GetType().Name + ": " + ex.Message };
            MissingOverlay.Visibility = Visibility.Visible;
            MissingOverlay.Text = _stats.Error;
            AgentLog("A", "stl-exception", ToJson());
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

    public void ShowMesial() => FrameSide("mesial", new Vector3D(1, 0, 0.16), new Vector3D(0, 0, 1));

    public void ShowDistal() => FrameSide("distal", new Vector3D(-1, 0, 0.16), new Vector3D(0, 0, 1));

    public void SetSurfaceDebug(bool show, string inspect)
    {
        _showSurfaces = show;
        _inspectSurface = string.IsNullOrWhiteSpace(inspect) ? "All" : inspect;
        ApplyOverlayVisibility();
        // #region agent log
        AgentLog("A", "surface-debug", ToJson());
        // #endregion
    }

    public void SetFillingSurfaces(IEnumerable<string> names)
    {
        _fillingSurfaces.Clear();
        foreach (var name in names)
        {
            if (Enum.TryParse<ClinicalSurface>(name, true, out var surface))
                _fillingSurfaces.Add(surface);
        }
        ApplyClinicalOverlays();
        ApplyInteractionOverlays();
        // #region agent log
        AgentLog("F", "clinical-fillings",
            "{\"count\":" + _fillingSurfaces.Count +
            ",\"names\":\"" + Esc(string.Join(",", _fillingSurfaces)) + "\"}");
        // #endregion
    }

    public void SetSelectedSurfaces(IEnumerable<string> names)
    {
        _selectedSurfaces.Clear();
        foreach (var name in names)
        {
            if (Enum.TryParse<ClinicalSurface>(name, true, out var surface))
                _selectedSurfaces.Add(surface);
        }
        ApplyInteractionOverlays();
    }

    private void RebuildSurfaceOverlays(MeshGeometry3D crown)
    {
        _overlayGroup.Children.Clear();
        SurfaceOverlayVisual.Content = null;
        _surfaceMap = null;
        try
        {
            _surfaceMap = ToothSurfaceMapStore.TryLoad(_loadedFdi, crown);
            var source = _surfaceMap is null ? "missing" : "asset";
            if (_surfaceMap is null)
            {
                AgentLog("B", "map-missing",
                    "{\"source\":\"missing\",\"fdi\":\"" + Esc(_loadedFdi) +
                    "\",\"nTri\":" + (crown.TriangleIndices.Count / 3) + "}");
                return;
            }
            var colors = ToothSurfaceColorConvention.Overlay;
            var overlayVerts = 0;
            for (var s = 0; s < 5; s++)
            {
                var surface = (ClinicalSurface)s;
                var mesh = CrownSurfaceClassifier.OverlayMesh(crown, _surfaceMap.Triangles(surface), OverlayNormalEps);
                overlayVerts += mesh.Positions.Count;
                var model = new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = OverlayMaterial(colors[s])
                };
                _overlayModels[s] = model;
                _overlayGroup.Children.Add(model);
            }
            ApplyOverlayVisibility();
            // #region agent log
            var nTri = crown.TriangleIndices.Count / 3;
            var idx = crown.TriangleIndices;
            var pos = crown.Positions;
            var palDistalFlank = 0;
            var distPalatalFlank = 0;
            for (var t = 0; t < nTri; t++)
            {
                var a = pos[idx[t * 3]];
                var b = pos[idx[t * 3 + 1]];
                var c = pos[idx[t * 3 + 2]];
                var cx = (a.X + b.X + c.X) / 3.0;
                var cy = (a.Y + b.Y + c.Y) / 3.0;
                var lab = _surfaceMap.SurfaceOf(t);
                if (lab == ClinicalSurface.Palatal && cx < -0.15) palDistalFlank++;
                if (lab == ClinicalSurface.Distal && cy < -0.15) distPalatalFlank++;
            }
            AgentLog("A", "overlay-built",
                "{\"fdi\":\"" + Esc(_loadedFdi) +
                "\",\"eps\":" + OverlayNormalEps.ToString("0.####", CultureInfo.InvariantCulture) +
                ",\"overlayVerts\":" + overlayVerts +
                ",\"crownTris\":" + nTri +
                ",\"occlusal\":" + _surfaceMap.Counts[0] +
                ",\"buccal\":" + _surfaceMap.Counts[1] +
                ",\"lingual\":" + _surfaceMap.Counts[2] +
                ",\"mesial\":" + _surfaceMap.Counts[3] +
                ",\"distal\":" + _surfaceMap.Counts[4] +
                ",\"interaction\":" + (_interactionEnabled ? "true" : "false") +
                ",\"mapSource\":\"" + source + "\"" +
                ",\"color0\":\"" + colors[0].ToString() + "\"" +
                ",\"color1\":\"" + colors[1].ToString() + "\"" +
                ",\"color2\":\"" + colors[2].ToString() + "\"" +
                ",\"palDistalFlank\":" + palDistalFlank +
                ",\"distPalatalFlank\":" + distPalatalFlank +
                ",\"contentNull\":" + (SurfaceOverlayVisual.Content is null ? "true" : "false") +
                ",\"show\":" + (_showSurfaces ? "true" : "false") + "}");
            ToothSurfaceLayoutStats.Log("C", _loadedFdi, "asset", crown, _surfaceMap.TriangleSurface);
            ApplyClinicalOverlays();
            ApplyInteractionOverlays();
        }
        catch (Exception ex)
        {
            _surfaceMap = null;
            SurfaceOverlayVisual.Content = null;
            // #region agent log
            AgentLog("B", "classify-failed", "{\"error\":\"" + Esc(ex.Message) + "\"}");
            // #endregion
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
            // #region agent log
            AgentLog("A", "overlay-off",
                "{\"show\":" + (_showSurfaces ? "true" : "false") +
                ",\"hasMap\":" + (_surfaceMap is null ? "false" : "true") +
                ",\"contentNull\":true}");
            // #endregion
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
        // #region agent log
        AgentLog("P", "overlay-on",
            "{\"inspect\":\"" + Esc(inspect) +
            "\",\"children\":" + _overlayGroup.Children.Count +
            ",\"palatal\":" + _surfaceMap.Counts[2] +
            ",\"distal\":" + _surfaceMap.Counts[4] +
            ",\"shownColor\":\"" + inspect switch
            {
                "Palatal" => "#2EBB6B green",
                "Lingual" => "#2EBB6B green",
                "Distal" => "#9B59B6 pink",
                "Occlusal" => "#E85D4C coral",
                "Buccal" => "#3D7CFF blue",
                "Mesial" => "#F4D03F yellow",
                _ => "all-five"
            } + "\"}");
        // #endregion
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
        LogFraming("frame-occlusal");
        CaptureViewPreview("occlusal");
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
        LogFraming("frame-" + name);
        CaptureViewPreview(name);
    }

    private void ApplyOrbitCamera()
    {
        View3D.Camera = _orbitCam;
        var x = _radius * Math.Sin(_phi) * Math.Cos(_theta);
        var y = _radius * Math.Sin(_phi) * Math.Sin(_theta);
        var z = _radius * Math.Cos(_phi);
        _orbitCam.Position = new Point3D(x, y, z);
        _orbitCam.LookDirection = new Vector3D(-x, -y, -z);
        _orbitCam.UpDirection = _phi < 18 * Deg
            ? new Vector3D(0, 1, 0)
            : new Vector3D(0, 0, 1);
        SyncLights(_orbitCam.LookDirection, _orbitCam.UpDirection);
        UpdateChrome();
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
        // #region agent log
        if (!_dragging)
            AgentLog("C", "lights-synced", ToJson());
        // #endregion
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

    private double FillRatio()
    {
        var tooth = _viewMode switch
        {
            "occlusal" => Math.Max(_stats.Dx, _stats.Dy),
            "buccal" or "palatal" or "lingual" => Math.Max(_stats.Dx, _stats.Dz),
            "mesial" or "distal" => Math.Max(_stats.Dy, _stats.Dz),
            _ => 2 * BoundingRadius()
        };
        if (View3D.Camera is OrthographicCamera ortho)
            return tooth / Math.Max(1e-6, ortho.Width);
        if (_radius < 1e-6) return 0;
        var visible = 2 * _radius * Math.Tan(_orbitCam.FieldOfView * Math.PI / 360.0);
        return tooth / Math.Max(1e-6, visible);
    }

    private void OnDragStart(object sender, MouseButtonEventArgs e)
    {
        _pressing = true;
        _orbitMoved = false;
        _dragging = false;
        _downPos = e.GetPosition(this);
        _last = _downPos;
        CaptureMouse();
        Focus();
        e.Handled = true;
    }

    private void OnDragEnd(object sender, MouseButtonEventArgs e)
    {
        var click = _pressing && !_orbitMoved;
        _pressing = false;
        _dragging = false;
        if (IsMouseCaptured)
            ReleaseMouseCapture();
        if (click)
            TrySelectAt(e.GetPosition(View3D));
        UpdateHover(e.GetPosition(View3D));
        e.Handled = true;
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
        if (!_dragging)
            UpdateHover(e.GetPosition(View3D));
    }

    private void OnMouseLeft(object sender, MouseEventArgs e)
    {
        if (IsMouseCaptured)
            return;
        _dragging = false;
        _pressing = false;
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
        // #region agent log
        AgentLog("H", "hover",
            "{\"hover\":\"" + (hit?.ToString() ?? "None") +
            "\",\"selected\":\"" + Esc(SelectedJoin()) +
            "\",\"tri\":" + tri +
            ",\"viewMode\":\"" + Esc(_viewMode) + "\"}");
        // #endregion
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
        // #region agent log
        AgentLog("H", "select",
            "{\"hover\":\"" + (_hoverSurface?.ToString() ?? "None") +
            "\",\"selected\":\"" + Esc(SelectedJoin()) +
            "\",\"tri\":" + tri + "}");
        // #endregion
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
            // #region agent log
            AgentLog("C", "clinical-overlay",
                "{\"fillCount\":0,\"added\":0,\"skipped\":0,\"contentNull\":true}");
            // #endregion
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
        // #region agent log
        AgentLog("C", "clinical-overlay",
            "{\"fillCount\":" + _fillingSurfaces.Count +
            ",\"added\":" + group.Children.Count +
            ",\"skipped\":" + skipped +
            ",\"contentNull\":" + (ClinicalOverlayVisual.Content is null ? "true" : "false") + "}");
        // #endregion
    }

    private void ApplyInteractionOverlays()
    {
        _hoverMaterial ??= InteractionMaterial(Color.FromArgb(0x22, 0x72, 0xB8, 0xE4), 0x10);
        _selectedMaterial ??= InteractionMaterial(Color.FromArgb(0x4C, 0x3A, 0x8C, 0xD2), 0x1C);
        _hoverOnFillingMaterial ??= InteractionMaterial(Color.FromArgb(0x18, 0x8A, 0xC4, 0xE8), 0x0C);
        _selectedOnFillingMaterial ??= InteractionMaterial(Color.FromArgb(0x2A, 0x5A, 0x9A, 0xD4), 0x14);
        if (_selectedSurfaces.Count == 0)
        {
            SelectedOverlayVisual.Content = null;
        }
        else
        {
            var group = new Model3DGroup();
            foreach (var surface in _selectedSurfaces)
            {
                var mat = _fillingSurfaces.Contains(surface)
                    ? _selectedOnFillingMaterial
                    : _selectedMaterial;
                var model = OverlayFor(surface, mat!);
                if (model is not null)
                    group.Children.Add(model);
            }
            SelectedOverlayVisual.Content = group.Children.Count == 0 ? null : group;
        }
        var hover = _hoverSurface is ClinicalSurface h && !_selectedSurfaces.Contains(h)
            ? _hoverSurface
            : null;
        var hoverMat = hover is ClinicalSurface hs && _fillingSurfaces.Contains(hs)
            ? _hoverOnFillingMaterial
            : _hoverMaterial;
        HoverOverlayVisual.Content = OverlayFor(hover, hoverMat!);
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
            Hover = _hoverSurface?.ToString(),
            Selected = selected.Count == 0 ? null : string.Join(",", selected),
            SelectedSurfaces = selected,
            Triangle = triangle
        });
    }

    private IReadOnlyList<string> SelectedNames()
    {
        var names = new List<string>();
        foreach (var surface in Enum.GetValues<ClinicalSurface>())
        {
            if (_selectedSurfaces.Contains(surface))
                names.Add(surface.ToString());
        }
        return names;
    }

    private string SelectedJoin() =>
        _selectedSurfaces.Count == 0 ? "None" : string.Join(",", SelectedNames());

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

    // #region agent log
    private void LogFraming(string message)
    {
        AgentLog(_viewMode == "occlusal" ? "A" : "B", message, ToJson());
    }

    private string AppearanceLog(string fdi, bool reused)
    {
        var crown = DiffuseHex(CrownModel.Material);
        var root = DiffuseHex(RootModel.Material);
        return "{\"fdi\":\"" + Esc(fdi) +
               "\",\"reused\":" + (reused ? "true" : "false") +
               ",\"crownDiff\":\"" + crown +
               "\",\"rootDiff\":\"" + root +
               "\",\"intendedCrown\":\"" + ToothLabAppearance.CrownDiffuseHex(fdi) +
               "\",\"intendedRoot\":\"" + ToothLabAppearance.RootDiffuseHex(fdi) +
               "\",\"contrastRgb\":" + ContrastRgb(crown, root) +
               ",\"crownTris\":" + _stats.CrownTriangles +
               ",\"rootTris\":" + _stats.RootTriangles +
               ",\"cervicalTris\":" + _stats.CervicalTriangles +
               ",\"cervicalDiff\":\"" + DiffuseHex(CervicalModel.Material) +
               "\",\"split\":\"" + Esc(_stats.SplitSource) +
               "\",\"map\":" + (_surfaceMap is null ? "false" : "true") +
               ",\"interaction\":" + (_interactionEnabled ? "true" : "false") +
               ",\"amb\":\"" + LightHex(AmbLight) +
               "\",\"key\":\"" + LightHex(KeyLight) +
               "\",\"fill\":\"" + LightHex(FillLight) +
               "\",\"rim\":\"" + LightHex(RimLight) + "\"}";
    }

    private string CervicalLog(string fdi)
    {
        var cervGeom = CervicalModel.Geometry as MeshGeometry3D;
        var cervTris = cervGeom?.TriangleIndices.Count / 3 ?? 0;
        return "{\"fdi\":\"" + Esc(fdi) +
               "\",\"split\":\"" + Esc(_stats.SplitSource) +
               "\",\"cervicalTris\":" + _stats.CervicalTriangles +
               ",\"cervicalGeomTris\":" + cervTris +
               ",\"crownTris\":" + _stats.CrownTriangles +
               ",\"rootTris\":" + _stats.RootTriangles +
               ",\"crownDiff\":\"" + DiffuseHex(CrownModel.Material) +
               "\",\"cervicalDiff\":\"" + DiffuseHex(CervicalModel.Material) +
               "\",\"rootDiff\":\"" + DiffuseHex(RootModel.Material) +
               "\",\"map\":" + (_surfaceMap is null ? "false" : "true") +
               ",\"interaction\":" + (_interactionEnabled ? "true" : "false") + "}";
    }

    private static string DiffuseHex(Material? mat)
    {
        if (mat is MaterialGroup group)
        {
            foreach (var child in group.Children)
            {
                if (child is DiffuseMaterial d && d.Brush is SolidColorBrush b)
                    return "#" + b.Color.R.ToString("X2") + b.Color.G.ToString("X2") + b.Color.B.ToString("X2");
            }
        }
        if (mat is DiffuseMaterial dm && dm.Brush is SolidColorBrush br)
            return "#" + br.Color.R.ToString("X2") + br.Color.G.ToString("X2") + br.Color.B.ToString("X2");
        return "?";
    }

    private static int ContrastRgb(string a, string b)
    {
        if (!TryRgb(a, out var ar, out var ag, out var ab) || !TryRgb(b, out var br, out var bg, out var bb))
            return -1;
        return Math.Abs(ar - br) + Math.Abs(ag - bg) + Math.Abs(ab - bb);
    }

    private static bool TryRgb(string hex, out int r, out int g, out int b)
    {
        r = g = b = 0;
        if (hex.Length != 7 || hex[0] != '#') return false;
        return int.TryParse(hex.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r)
            && int.TryParse(hex.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)
            && int.TryParse(hex.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
    }

    private static string LightHex(Light light) =>
        "#" + light.Color.A.ToString("X2") + light.Color.R.ToString("X2") +
        light.Color.G.ToString("X2") + light.Color.B.ToString("X2");

    private static void AgentLog(string hypothesisId, string message, string dataJson)
    {
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"interact-v1\",\"hypothesisId\":\"" + hypothesisId +
                   "\",\"location\":\"ToothMeshView.xaml.cs\",\"message\":\"" + message +
                   "\",\"data\":" + dataJson + ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { /* lab logging must not break rendering */ }
    }

    private string ToJson()
    {
        var s = _stats;
        var cam = View3D.Camera as ProjectionCamera;
        var p = cam?.Position ?? new Point3D();
        var kind = View3D.Camera is OrthographicCamera ? "ortho" : "persp";
        var width = View3D.Camera is OrthographicCamera o ? o.Width : 0;
        return "{" +
               "\"header\":\"" + Esc(s.Header) + "\"," +
               "\"format\":\"" + Esc(s.Format) + "\"," +
               "\"sourcePath\":\"" + Esc(s.SourcePath) + "\"," +
               "\"fdi\":\"" + Esc(_loadedFdi) + "\"," +
               "\"profile\":\"" + Esc(_orientationProfile) + "\"," +
               "\"interaction\":" + (_interactionEnabled ? "true" : "false") + "," +
               "\"mapLoaded\":" + (_surfaceMap is null ? "false" : "true") + "," +
               "\"triangles\":" + s.TriangleCount + "," +
               "\"vertices\":" + s.VertexCount + "," +
               "\"dx\":" + F(s.Dx) + ",\"dy\":" + F(s.Dy) + ",\"dz\":" + F(s.Dz) + "," +
               "\"xyAspect\":" + F(s.XyAspect) + "," +
               "\"occlusalRelief\":" + F(s.OcclusalRelief) + "," +
               "\"mirrored\":" + (s.Mirrored ? "true" : "false") + "," +
               "\"flippedX\":" + (s.FlippedX ? "true" : "false") + "," +
               "\"yawDeg\":" + F(s.YawDeg) + "," +
               "\"rootClusters\":" + s.RootClusters + "," +
               "\"palatal\":\"" + Esc(s.Palatal) + "\"," +
               "\"mb\":\"" + Esc(s.Mb) + "\"," +
               "\"db\":\"" + Esc(s.Db) + "\"," +
               "\"loadFailed\":" + (s.LoadFailed ? "true" : "false") + "," +
               "\"error\":\"" + Esc(s.Error) + "\"," +
               "\"viewMode\":\"" + _viewMode + "\"," +
               "\"cameraKind\":\"" + kind + "\"," +
               "\"orthoWidth\":" + F(width) + "," +
               "\"thetaDeg\":" + F(_theta / Deg) + "," +
               "\"phiDeg\":" + F(_phi / Deg) + "," +
               "\"radius\":" + F(_radius) + "," +
               "\"camX\":" + F(p.X) + ",\"camY\":" + F(p.Y) + ",\"camZ\":" + F(p.Z) + "," +
               "\"splitSource\":\"" + Esc(s.SplitSource) + "\"," +
               "\"polypaint\":" + s.PolypaintColors + "," +
               "\"crownTris\":" + s.CrownTriangles + "," +
               "\"rootTris\":" + s.RootTriangles + "," +
               "\"occlusalLeakFixed\":" + s.OcclusalRootLeakFixed + "," +
               "\"crownMeanZ\":" + F(s.CrownMeanZ) + ",\"rootMeanZ\":" + F(s.RootMeanZ) + "," +
               "\"fillRatio\":" + F(FillRatio()) + "," +
               "\"keyDx\":" + F(KeyLight.Direction.X) + ",\"keyDy\":" + F(KeyLight.Direction.Y) + ",\"keyDz\":" + F(KeyLight.Direction.Z) + "," +
               "\"lightsFollow\":true," +
               LightingJson() +
               "\"viewportW\":" + F(ActualWidth) + "," +
               "\"viewportH\":" + F(ActualHeight) + "," +
               "\"showSurfaces\":" + (_showSurfaces ? "true" : "false") + "," +
               "\"inspectSurface\":\"" + Esc(_inspectSurface) + "\"," +
               "\"overlayVisible\":" + (SurfaceOverlayVisual.Content is null ? "false" : "true") + "," +
               "\"occlusalTris\":" + C(0) + ",\"buccalTris\":" + C(1) +
               ",\"palatalTris\":" + C(2) + ",\"mesialTris\":" + C(3) + ",\"distalTris\":" + C(4) +
               "}";
    }

    private int C(int surface) =>
        _surfaceMap?.Counts is { Length: >= 5 } counts ? counts[surface] : 0;

    private string LightingJson()
    {
        var look = (View3D.Camera as ProjectionCamera)?.LookDirection ?? new Vector3D(0, 0, -1);
        var camUp = (View3D.Camera as ProjectionCamera)?.UpDirection ?? new Vector3D(0, 1, 0);
        if (look.LengthSquared > 1e-12) look.Normalize();
        if (camUp.LengthSquared > 1e-12) camUp.Normalize();
        var right = Vector3D.CrossProduct(look, camUp);
        if (right.LengthSquared < 1e-10)
            right = Math.Abs(look.Z) < 0.92 ? new Vector3D(0, 0, 1) : new Vector3D(1, 0, 0);
        else
            right.Normalize();

        var keyDir = KeyLight.Direction;
        var fillDir = FillLight.Direction;
        var rimDir = RimLight.Direction;
        if (keyDir.LengthSquared > 1e-12) keyDir.Normalize();
        if (fillDir.LengthSquared > 1e-12) fillDir.Normalize();
        if (rimDir.LengthSquared > 1e-12) rimDir.Normalize();

        var amb = AmbLight.Color;
        var key = KeyLight.Color;
        var fill = FillLight.Color;
        var rim = RimLight.Color;
        var diff = Colors.White;
        var ambMat = Colors.White;
        var em = Colors.Transparent;
        var spec = Colors.Transparent;
        var specPow = 0.0;
        if (CrownModel.Material is MaterialGroup group)
        {
            foreach (var m in group.Children)
            {
                if (m is DiffuseMaterial d)
                {
                    ambMat = d.AmbientColor;
                    if (d.Brush is SolidColorBrush db) diff = db.Color;
                }
                else if (m is EmissiveMaterial e && e.Brush is SolidColorBrush eb) em = eb.Color;
                else if (m is SpecularMaterial s && s.Brush is SolidColorBrush sb)
                {
                    spec = sb.Color;
                    specPow = s.SpecularPower;
                }
            }
        }

        static double Lum(Color c) => (c.R + c.G + c.B) / (3.0 * 255.0);
        static double Ndot(Vector3D n, Vector3D lightDir) => Math.Max(0, -Vector3D.DotProduct(n, lightDir));
        var ambTerm = Lum(amb) * Lum(diff) * Lum(ambMat);
        var emTerm = em.A / 255.0;
        double Shade(Vector3D n)
        {
            if (n.LengthSquared > 1e-12) n.Normalize();
            return ambTerm + emTerm
                + Lum(key) * Lum(diff) * Ndot(n, keyDir)
                + Lum(fill) * Lum(diff) * Ndot(n, fillDir)
                + Lum(rim) * Lum(diff) * Ndot(n, rimDir);
        }

        var nTable = new Vector3D(-look.X, -look.Y, -look.Z);
        var nWall = new Vector3D(-right.X, -right.Y, -right.Z);
        var unlitEnergy = ambTerm + emTerm;
        var tableEnergy = Shade(nTable);
        var wallEnergy = Shade(nWall);
        var cuspEnergy = Shade(new Vector3D(-keyDir.X, -keyDir.Y, -keyDir.Z));
        var keyLookDot = Vector3D.DotProduct(keyDir, look);
        var fillLookDot = Vector3D.DotProduct(fillDir, look);
        var facingEnergy = Lum(amb) * Lum(diff)
            + Lum(key) * Lum(diff)
            + Lum(fill) * Lum(diff) * 0.4
            + Lum(rim) * Lum(diff) * 0.2
            + emTerm;

        return "\"amb\":\"" + amb + "\",\"keyCol\":\"" + key + "\",\"fillCol\":\"" + fill + "\",\"rimCol\":\"" + rim + "\"," +
               "\"diffCol\":\"" + diff + "\",\"ambMat\":\"" + ambMat + "\",\"emissive\":\"" + em + "\",\"specCol\":\"" + spec + "\",\"specPow\":" + F(specPow) + "," +
               "\"keyLookDot\":" + F(keyLookDot) + ",\"fillLookDot\":" + F(fillLookDot) + "," +
               "\"unlitEnergy\":" + F(unlitEnergy) + ",\"wallEnergy\":" + F(wallEnergy) + "," +
               "\"tableEnergy\":" + F(tableEnergy) + ",\"cuspEnergy\":" + F(cuspEnergy) + "," +
               "\"facingEnergy\":" + F(facingEnergy) + ",";
    }

    private int _previewTries;

    private void CaptureViewPreview(string mode)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                UpdateLayout();
                if (ActualWidth < 120 && _previewTries++ < 8)
                {
                    CaptureViewPreview(mode);
                    return;
                }
                var w = Math.Max(1, (int)Math.Round(Math.Max(ActualWidth, 1)));
                var h = Math.Max(1, (int)Math.Round(Math.Max(ActualHeight, 1)));
                var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(this);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                var path = @"c:\Users\david\source\repos\MyOrganIzer\" + mode + "-preview.png";
                using (var fs = File.Create(path))
                    encoder.Save(fs);
                AgentLog("D", "view-preview", "{\"mode\":\"" + Esc(mode) + "\",\"w\":" + w + ",\"h\":" + h + ",\"bytes\":" + new FileInfo(path).Length + "}");
            }
            catch (Exception ex)
            {
                AgentLog("D", "preview-failed", "{\"error\":\"" + Esc(ex.Message) + "\"}");
            }
        }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    private static string F(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);
    private static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
    // #endregion
}

public sealed class ToothLabHitEventArgs : EventArgs
{
    public string? Hover { get; init; }
    public string? Selected { get; init; }
    public IReadOnlyList<string> SelectedSurfaces { get; init; } = [];
    public int Triangle { get; init; }
}
