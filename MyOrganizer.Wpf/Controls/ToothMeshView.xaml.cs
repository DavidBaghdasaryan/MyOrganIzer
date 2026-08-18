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
    private double _theta;
    private double _phi;
    private double _radius = 6;

    public ToothMeshView()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadMesh();
        MouseLeftButtonDown += OnDragStart;
        MouseLeftButtonUp += OnDragEnd;
        MouseMove += OnDragMove;
        MouseWheel += OnWheel;
        MouseLeave += (_, _) => _dragging = false;
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

    private void LoadMesh()
    {
        try
        {
            MissingOverlay.Visibility = Visibility.Collapsed;
            var path = FindSourceFile();
            Stream? stream = path is not null ? File.OpenRead(path) : TryOpenPack(KnownPackUri, out _);
            if (stream is not null && string.IsNullOrEmpty(path))
                path = KnownPackUri;

            if (stream is null)
            {
                _stats = new StlMeshStats { LoadFailed = true, Error = "source-missing" };
                MissingOverlay.Text =
                    "Drop the Dundee maxillary first molar (OBJ / STL / ZIP) into Assets/Teeth/Source";
                MissingOverlay.Visibility = Visibility.Visible;
                AgentLog("A", "stl-load-failed", ToJson());
                return;
            }

            using (stream)
            {
                var parts = StlToothLoader.LoadAlignedParts(stream, out _stats, new MeshLoadOptions
                {
                    MirrorX = true,
                    OrientFdi16 = true
                });
                _stats.SourcePath = path ?? "";
                CrownModel.Geometry = parts.Crown;
                RootModel.Geometry = parts.Root;
                RebuildSurfaceOverlays(parts.Crown);
                FrameOcclusal();
                AgentLog("A", "mesh-loaded", ToJson());
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

    private static string? FindSourceFile()
    {
        foreach (var dir in CandidateDirs())
        {
            if (!Directory.Exists(dir)) continue;
            var preferred = Path.Combine(dir, "FDI16_High.obj");
            if (File.Exists(preferred))
                return preferred;
            var files = Directory.GetFiles(dir)
                .Where(f =>
                {
                    var ext = Path.GetExtension(f);
                    return ext.Equals(".obj", StringComparison.OrdinalIgnoreCase)
                        || ext.Equals(".stl", StringComparison.OrdinalIgnoreCase);
                })
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToList();
            if (files.Count > 0)
                return files[0];
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

    public void ResetToOcclusal() => FrameOcclusal();

    public void ShowOcclusal() => FrameOcclusal();

    public void ShowBuccal() => FrameSide("buccal", new Vector3D(0, 1, 0.16), new Vector3D(0, 0, 1));

    public void ShowPalatal() => FrameSide("palatal", new Vector3D(0, -1, 0.16), new Vector3D(0, 0, 1));

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

    private void RebuildSurfaceOverlays(MeshGeometry3D crown)
    {
        _overlayGroup.Children.Clear();
        SurfaceOverlayVisual.Content = null;
        _surfaceMap = null;
        try
        {
            _surfaceMap = Fdi16SurfaceMapStore.TryLoad(crown);
            var source = "asset";
            if (_surfaceMap is null)
            {
                _surfaceMap = Fdi16SurfaceMapStore.Build(crown);
                source = "generated";
            }
            var colors = new[]
            {
                Color.FromArgb(0x7A, 0xE8, 0x5D, 0x4C),
                Color.FromArgb(0x7A, 0x3D, 0x7C, 0xFF),
                Color.FromArgb(0x7A, 0x2E, 0xBB, 0x6B),
                Color.FromArgb(0x7A, 0xF4, 0xD0, 0x3F),
                Color.FromArgb(0x7A, 0x9B, 0x59, 0xB6)
            };
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
            AgentLog("P", "overlay-built",
                "{\"eps\":" + OverlayNormalEps.ToString("0.####", CultureInfo.InvariantCulture) +
                ",\"overlayVerts\":" + overlayVerts +
                ",\"crownTris\":" + nTri +
                ",\"occlusal\":" + _surfaceMap.Counts[0] +
                ",\"palatal\":" + _surfaceMap.Counts[2] +
                ",\"distal\":" + _surfaceMap.Counts[4] +
                ",\"palColor\":\"#2EBB6B\"" +
                ",\"distColor\":\"#9B59B6\"" +
                ",\"mapSource\":\"" + source + "\"" +
                ",\"palDistalFlank\":" + palDistalFlank +
                ",\"distPalatalFlank\":" + distPalatalFlank +
                ",\"contentNull\":" + (SurfaceOverlayVisual.Content is null ? "true" : "false") +
                ",\"show\":" + (_showSurfaces ? "true" : "false") + "}");
            // #endregion
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
            var name = ((ClinicalSurface)s).ToString();
            if (inspect is "All" || string.Equals(inspect, name, StringComparison.OrdinalIgnoreCase))
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
        var across = name is "buccal" or "palatal"
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
            "palatal" => "Palatal view",
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
            "buccal" or "palatal" => Math.Max(_stats.Dx, _stats.Dz),
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
        if (_viewMode != "orbit")
        {
            SeedOrbitFromCurrent();
            _viewMode = "orbit";
            ApplyOrbitCamera();
        }
        _dragging = true;
        _last = e.GetPosition(this);
        CaptureMouse();
        Focus();
    }

    private void OnDragEnd(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        ReleaseMouseCapture();
    }

    private void OnDragMove(object sender, MouseEventArgs e)
    {
        if (!_dragging || e.LeftButton != MouseButtonState.Pressed)
            return;
        var p = e.GetPosition(this);
        var dx = p.X - _last.X;
        var dy = p.Y - _last.Y;
        _last = p;
        _theta -= dx * 0.008;
        _phi = Math.Clamp(_phi - dy * 0.008, 6 * Deg, 165 * Deg);
        ApplyOrbitCamera();
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

    // #region agent log
    private void LogFraming(string message)
    {
        AgentLog(_viewMode == "occlusal" ? "A" : "B", message, ToJson());
    }

    private static void AgentLog(string hypothesisId, string message, string dataJson)
    {
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"enamel-balance\",\"hypothesisId\":\"" + hypothesisId +
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
