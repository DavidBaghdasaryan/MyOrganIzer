using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
    private int _orbitCanalSync;
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
    private int _orbitMoves;
    private int _orbitSlow;
    private int _orbitSlowLogged;
    private double _orbitMaxMs;
    private int _rebuildDuringDrag;
    private int _hoverDuringPress;
    private int _upSnaps;
    private int _lostCaptures;
    private int _orbitPhiClamps;
    private int _orbitUpSwitches;
    private int _orbitJumpDeltas;
    private int _orbitCamAssigns;
    private bool _orbitUpY;
    private Vector3D _prevOrbitUp;
    private long _orbitLastMoveMs;
    private string _orbitRebuildKinds = "";

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
        GotMouseCapture += OnGotCapture;
        MouseLeave += OnMouseLeft;
        MouseDoubleClick += (_, _) => ResetToOcclusal();
    }

    private void OnFirstLoad(object sender, RoutedEventArgs e)
    {
        if (!IsVisible)
        {
            // #region agent log
            AgentLog("A", "mesh-defer",
                "{\"visible\":false,\"asset\":\"" + Esc(AssetName) + "\",\"loaded\":false}");
            // #endregion
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
            NoteRebuildDuringDrag("load-mesh");
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
        // #region agent log
        AgentLog("A", "surface-debug", ToJson());
        // #endregion
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
        // #region agent log
        AgentLog("C", "clinical-fillings",
            "{\"fdi\":\"" + Esc(_loadedFdi) +
            "\",\"incoming\":\"" + Esc(incoming) +
            "\",\"count\":" + _fillingSurfaces.Count +
            ",\"parsed\":\"" + Esc(string.Join(",", _fillingSurfaces.Select(DisplaySurfaceName))) +
            "\",\"mapAsset\":\"" + MapAssetName() + "\"}");
        // #endregion
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
        ApplyCanalOverlays(log: true);
        // #region agent log
        AgentLog("A", "canal-overlay-set",
            "{\"fdi\":\"" + Esc(_loadedFdi) +
            "\",\"ids\":\"" + Esc(string.Join(",", _rootCanalIds)) +
            "\",\"pathCount\":" + _canalPaths.Count +
            ",\"drawn\":" + ((CanalOverlayVisual.Content as Model3DGroup)?.Children.Count ?? 0) +
            ",\"ghost\":" + ((CanalGhostVisual.Content as Model3DGroup)?.Children.Count ?? 0) + "}");
        // #endregion
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
        NoteRebuildDuringDrag("rebuild-overlays");
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
            var colors = OverlayColorsFor(_loadedFdi);
            var overlayVerts = 0;
            var nTri = crown.TriangleIndices.Count / 3;
            var owned = 0;
            var missing = 0;
            var dup = 0;
            var seen = new int[nTri];
            for (var s = 0; s < 5; s++)
            {
                var surface = (ClinicalSurface)s;
                var tris = _surfaceMap.Triangles(surface);
                foreach (var t in tris)
                {
                    if ((uint)t >= (uint)nTri)
                    {
                        missing++;
                        continue;
                    }
                    seen[t]++;
                    if (seen[t] == 1)
                        owned++;
                    else
                        dup++;
                }
                var mesh = CrownSurfaceClassifier.OverlayMesh(crown, tris, OverlayNormalEps);
                overlayVerts += mesh.Positions.Count;
                var model = new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = OverlayMaterial(colors[s])
                };
                _overlayModels[s] = model;
                _overlayGroup.Children.Add(model);
            }
            for (var t = 0; t < nTri; t++)
            {
                if (seen[t] == 0)
                    missing++;
            }
            ApplyOverlayVisibility();
            // #region agent log
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
                ",\"sharedOverlay\":true" +
                ",\"overlayLift\":\"faceNormal\"" +
                ",\"owned\":" + owned +
                ",\"dup\":" + dup +
                ",\"missing\":" + missing +
                ",\"overlayVerts\":" + overlayVerts +
                ",\"crownTris\":" + nTri +
                ",\"occlusal\":" + _surfaceMap.Counts[0] +
                ",\"buccal\":" + _surfaceMap.Counts[1] +
                ",\"lingual\":" + _surfaceMap.Counts[2] +
                ",\"mesial\":" + _surfaceMap.Counts[3] +
                ",\"distal\":" + _surfaceMap.Counts[4] +
                ",\"interaction\":" + (_interactionEnabled ? "true" : "false") +
                ",\"mapSource\":\"" + source + "\"" +
                ",\"mapAsset\":\"" + MapAssetName() + "\"" +
                ",\"color0\":\"" + colors[0].ToString() + "\"" +
                ",\"color1\":\"" + colors[1].ToString() + "\"" +
                ",\"color2\":\"" + colors[2].ToString() + "\"" +
                ",\"color3\":\"" + colors[3].ToString() + "\"" +
                ",\"color4\":\"" + colors[4].ToString() + "\"" +
                ",\"palDistalFlank\":" + palDistalFlank +
                ",\"distPalatalFlank\":" + distPalatalFlank +
                ",\"contentNull\":" + (SurfaceOverlayVisual.Content is null ? "true" : "false") +
                ",\"show\":" + (_showSurfaces ? "true" : "false") + "}");
            ToothSurfaceLayoutStats.Log("C", _loadedFdi, "asset", crown, _surfaceMap.TriangleSurface);
            ToothSurfaceLayoutStats.LogRedHeight("C", _loadedFdi, crown, _surfaceMap.TriangleSurface);
            CervicalSeamProbe.Log("A", _loadedFdi, crown, _surfaceMap.TriangleSurface);
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
            SyncCervicalForDebug();
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
        SyncCervicalForDebug();
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
            // #region agent log
            AgentLog("I", "cervical-overlay",
                "{\"fdi\":\"" + Esc(_loadedFdi) +
                "\",\"mode\":\"root-fill\",\"tris\":" +
                (((MeshGeometry3D)CervicalModel.Geometry).TriangleIndices.Count / 3) + "}");
            // #endregion
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
        SyncCanalGhostCamera();
        LogFraming("frame-" + name);
        CaptureViewPreview(name);
    }

    private void ApplyOrbitCamera()
    {
        var switched = !ReferenceEquals(View3D.Camera, _orbitCam);
        if (switched && _dragging)
            _orbitCamAssigns++;
        View3D.Camera = _orbitCam;
        var x = _radius * Math.Sin(_phi) * Math.Cos(_theta);
        var y = _radius * Math.Sin(_phi) * Math.Sin(_theta);
        var z = _radius * Math.Cos(_phi);
        _orbitCam.Position = new Point3D(x, y, z);
        _orbitCam.LookDirection = new Vector3D(-x, -y, -z);
        var up = SharedOrbitUp(_orbitCam.LookDirection);
        _orbitUpY = _phi < 18 * Deg;
        if (_dragging && _prevOrbitUp.LengthSquared > 0 &&
            Vector3D.DotProduct(_prevOrbitUp, up) < 0.985)
            _upSnaps++;
        _prevOrbitUp = up;
        _orbitCam.UpDirection = up;
        SyncLights(_orbitCam.LookDirection, _orbitCam.UpDirection);
        UpdateChrome();
        SyncCanalGhostCamera();
        if (_rootCanalIds.Count > 0 && (++_orbitCanalSync % 12) == 0)
        {
            // #region agent log
            AgentLog("A", "canal-ghost-sync",
                "{\"fdi\":\"" + Esc(_loadedFdi) +
                "\",\"runId\":\"post-fix\"" +
                ",\"cam\":\"" + (View3D.Camera is OrthographicCamera ? "ortho" : "persp") +
                "\",\"offset\":false" +
                ",\"front\":" + ((CanalOverlayVisual.Content as Model3DGroup)?.Children.Count ?? 0) +
                ",\"ghost\":" + ((CanalGhostVisual.Content as Model3DGroup)?.Children.Count ?? 0) + "}");
            // #endregion
        }
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
        _hoverDuringPress = 0;
        _lostCaptures = 0;
        _downPos = e.GetPosition(this);
        _last = _downPos;
        CaptureMouse();
        View3D.IsHitTestVisible = false;
        Focus();
        // #region agent log
        AgentLog("D", "orbit-down",
            "{\"fdi\":\"" + Esc(_loadedFdi) +
            "\",\"captured\":" + (IsMouseCaptured ? "true" : "false") +
            ",\"capturedEl\":\"" + Esc(Mouse.Captured?.GetType().Name ?? "none") +
            "\",\"hitVis\":" + (View3D.IsHitTestVisible ? "true" : "false") +
            ",\"mode\":\"element\"}");
        // #endregion
        e.Handled = true;
    }

    private void OnDragEnd(object sender, MouseButtonEventArgs e)
    {
        var click = _pressing && !_orbitMoved;
        var wasDragging = _dragging;
        _pressing = false;
        _dragging = false;
        View3D.IsHitTestVisible = true;
        if (IsMouseCaptured)
            ReleaseMouseCapture();
        if (click)
            TrySelectAt(e.GetPosition(View3D));
        UpdateHover(e.GetPosition(View3D));
        // #region agent log
        if (wasDragging || _orbitMoves > 0)
            AgentLog("B", "orbit-end",
                "{\"fdi\":\"" + Esc(_loadedFdi) +
                "\",\"moves\":" + _orbitMoves +
                ",\"slow\":" + _orbitSlow +
                ",\"maxMs\":" + _orbitMaxMs.ToString("0.###", CultureInfo.InvariantCulture) +
                ",\"rebuilds\":" + _rebuildDuringDrag +
                ",\"rebuildKinds\":\"" + Esc(_orbitRebuildKinds) +
                "\",\"hoverDuringPress\":" + _hoverDuringPress +
                ",\"upSnaps\":" + _upSnaps +
                ",\"camAssigns\":" + _orbitCamAssigns +
                ",\"lostCaptures\":" + _lostCaptures +
                ",\"phiClamps\":" + _orbitPhiClamps +
                ",\"upSwitches\":" + _orbitUpSwitches +
                ",\"jumpDeltas\":" + _orbitJumpDeltas +
                ",\"captured\":" + (IsMouseCaptured ? "true" : "false") +
                ",\"capturedEl\":\"" + Esc(Mouse.Captured?.GetType().Name ?? "none") +
                "\",\"sharedOrbit\":true" +
                ",\"thetaDeg\":" + F(_theta / Deg) +
                ",\"phiDeg\":" + F(_phi / Deg) +
                ",\"radius\":" + F(_radius) +
                ",\"click\":" + (click ? "true" : "false") + "}");
        // #endregion
        e.Handled = true;
    }

    private void OnGotCapture(object sender, MouseEventArgs e)
    {
        // #region agent log
        AgentLog("D", "orbit-got-capture",
            "{\"fdi\":\"" + Esc(_loadedFdi) +
            "\",\"capturedEl\":\"" + Esc(Mouse.Captured?.GetType().Name ?? "none") +
            "\",\"pressing\":" + (_pressing ? "true" : "false") + "}");
        // #endregion
    }

    private void OnLostCapture(object sender, MouseEventArgs e)
    {
        // #region agent log
        AgentLog("D", "orbit-lost-capture",
            "{\"fdi\":\"" + Esc(_loadedFdi) +
            "\",\"pressing\":" + (_pressing ? "true" : "false") +
            ",\"dragging\":" + (_dragging ? "true" : "false") +
            ",\"hitVis\":" + (View3D.IsHitTestVisible ? "true" : "false") +
            ",\"capturedEl\":\"" + Esc(Mouse.Captured?.GetType().Name ?? "none") +
            "\",\"left\":\"" + Mouse.LeftButton + "\"}");
        // #endregion
        if (_pressing && Mouse.LeftButton == MouseButtonState.Pressed)
        {
            _lostCaptures++;
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
        var clock = Stopwatch.StartNew();
        var p = e.GetPosition(this);
        var seeded = false;
        var seedFrom = _viewMode;
        var radiusBefore = _radius;
        var phiBefore = _phi;
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
                    _orbitMoves = 0;
                    _orbitSlow = 0;
                    _orbitSlowLogged = 0;
                    _orbitMaxMs = 0;
                    _rebuildDuringDrag = 0;
                    _orbitRebuildKinds = "";
                    _upSnaps = 0;
                    _lostCaptures = 0;
                    _orbitPhiClamps = 0;
                    _orbitUpSwitches = 0;
                    _orbitJumpDeltas = 0;
                    _orbitCamAssigns = 0;
                    _orbitLastMoveMs = 0;
                    _prevOrbitUp = new Vector3D();
                    if (_viewMode != "orbit")
                    {
                        seeded = true;
                        SeedOrbitFromCurrent();
                        _viewMode = "orbit";
                        ApplyOrbitCamera();
                    }
                    _last = p;
                    // #region agent log
                    AgentLog("A", "orbit-begin",
                        "{\"fdi\":\"" + Esc(_loadedFdi) +
                        "\",\"fromView\":\"" + Esc(seedFrom) +
                        "\",\"seeded\":" + (seeded ? "true" : "false") +
                        ",\"cam\":\"" + (View3D.Camera is OrthographicCamera ? "ortho" : "persp") +
                        "\",\"boundR\":" + F(BoundingRadius()) +
                        ",\"radiusBefore\":" + F(radiusBefore) +
                        ",\"radiusAfter\":" + F(_radius) +
                        ",\"phiBefore\":" + F(phiBefore / Deg) +
                        ",\"phiAfter\":" + F(_phi / Deg) +
                        ",\"thetaDeg\":" + F(_theta / Deg) +
                        ",\"dx\":" + F(_stats.Dx) +
                        ",\"dy\":" + F(_stats.Dy) +
                        ",\"dz\":" + F(_stats.Dz) +
                        ",\"cervicalTris\":" + _stats.CervicalTriangles +
                        ",\"overlayVerts\":" + OverlayVertCount() +
                        ",\"fillCount\":" + _fillingSurfaces.Count +
                        ",\"segOn\":" + (_showSurfaces ? "true" : "false") +
                        ",\"hoverDuringPress\":" + _hoverDuringPress + "}");
                    // #endregion
                }
            }
            if (_dragging)
            {
                var dx = p.X - _last.X;
                var dy = p.Y - _last.Y;
                _last = p;
                if (Math.Abs(dx) > 30 || Math.Abs(dy) > 30)
                {
                    _orbitJumpDeltas++;
                    // #region agent log
                    AgentLog("D", "orbit-jump-delta",
                        "{\"fdi\":\"" + Esc(_loadedFdi) +
                        "\",\"dx\":" + F(dx) +
                        ",\"dy\":" + F(dy) +
                        ",\"move\":" + _orbitMoves +
                        ",\"captured\":" + (IsMouseCaptured ? "true" : "false") + "}");
                    // #endregion
                }
                var phiBeforeMove = _phi;
                var upBefore = _phi < 18 * Deg;
                _theta -= dx * 0.008;
                _phi = Math.Clamp(_phi - dy * 0.008, 6 * Deg, 165 * Deg);
                if ((_phi <= 6 * Deg && phiBeforeMove > 6 * Deg) ||
                    (_phi >= 165 * Deg && phiBeforeMove < 165 * Deg))
                {
                    _orbitPhiClamps++;
                    // #region agent log
                    AgentLog("D", "orbit-phi-clamp",
                        "{\"fdi\":\"" + Esc(_loadedFdi) +
                        "\",\"phiDeg\":" + F(_phi / Deg) +
                        ",\"dy\":" + F(dy) +
                        ",\"move\":" + _orbitMoves + "}");
                    // #endregion
                }
                if (upBefore != (_phi < 18 * Deg))
                {
                    _orbitUpSwitches++;
                    // #region agent log
                    AgentLog("D", "orbit-up-switch",
                        "{\"fdi\":\"" + Esc(_loadedFdi) +
                        "\",\"phiDeg\":" + F(_phi / Deg) +
                        ",\"upY\":" + (_phi < 18 * Deg ? "true" : "false") +
                        ",\"move\":" + _orbitMoves + "}");
                    // #endregion
                }
                ApplyOrbitCamera();
            }
        }
        if (!_pressing && !_dragging)
            UpdateHover(e.GetPosition(View3D));
        clock.Stop();
        if (_dragging)
        {
            _orbitMoves++;
            var ms = clock.Elapsed.TotalMilliseconds;
            if (ms > _orbitMaxMs)
                _orbitMaxMs = ms;
            if (ms >= 8)
                _orbitSlow++;
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var gap = _orbitLastMoveMs == 0 ? 0 : now - _orbitLastMoveMs;
            _orbitLastMoveMs = now;
            // #region agent log
            if ((ms >= 8 || gap >= 40) && _orbitSlowLogged < 6)
            {
                _orbitSlowLogged++;
                AgentLog("B", "orbit-hitch",
                    "{\"fdi\":\"" + Esc(_loadedFdi) +
                    "\",\"ms\":" + ms.ToString("0.###", CultureInfo.InvariantCulture) +
                    ",\"gapMs\":" + gap +
                    ",\"move\":" + _orbitMoves +
                    ",\"rebuilds\":" + _rebuildDuringDrag +
                    ",\"phiDeg\":" + F(_phi / Deg) +
                    ",\"upY\":" + (_orbitUpY ? "true" : "false") + "}");
            }
            // #endregion
        }
    }

    private void OnMouseLeft(object sender, MouseEventArgs e)
    {
        // #region agent log
        if (_pressing || _dragging)
            AgentLog("D", "orbit-leave",
                "{\"fdi\":\"" + Esc(_loadedFdi) +
                "\",\"pressing\":" + (_pressing ? "true" : "false") +
                ",\"dragging\":" + (_dragging ? "true" : "false") +
                ",\"captured\":" + (IsMouseCaptured ? "true" : "false") + "}");
        // #endregion
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
        if (_pressing)
            _hoverDuringPress++;
        // #region agent log
        if (!_pressing)
            AgentLog("A", "hover",
                "{\"fdi\":\"" + Esc(_loadedFdi) +
                "\",\"hoverEnum\":\"" + (hit?.ToString() ?? "None") +
                "\",\"hoverDisplay\":\"" + (hit is ClinicalSurface hs ? DisplaySurfaceName(hs) : "None") +
                "\",\"selected\":\"" + Esc(SelectedJoin()) +
                "\",\"tri\":" + tri +
                ",\"mapAsset\":\"" + MapAssetName() +
                "\",\"viewMode\":\"" + Esc(_viewMode) + "\"}");
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
        AgentLog("A", "select",
            "{\"fdi\":\"" + Esc(_loadedFdi) +
            "\",\"hoverEnum\":\"" + (_hoverSurface?.ToString() ?? "None") +
            "\",\"hoverDisplay\":\"" + (_hoverSurface is ClinicalSurface hs ? DisplaySurfaceName(hs) : "None") +
            "\",\"selected\":\"" + Esc(SelectedJoin()) +
            "\",\"tri\":" + tri +
            ",\"mapAsset\":\"" + MapAssetName() + "\"}");
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
        NoteRebuildDuringDrag("clinical-overlay");
        if (_fillingSurfaces.Count == 0)
        {
            ClinicalOverlayVisual.Content = null;
            // #region agent log
            AgentLog("C", "clinical-overlay",
                "{\"fdi\":\"" + Esc(_loadedFdi) +
                "\",\"fillCount\":0,\"added\":0,\"skipped\":0,\"contentNull\":true}");
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
            "{\"fdi\":\"" + Esc(_loadedFdi) +
            "\",\"fillCount\":" + _fillingSurfaces.Count +
            ",\"added\":" + group.Children.Count +
            ",\"skipped\":" + skipped +
            ",\"contentNull\":" + (ClinicalOverlayVisual.Content is null ? "true" : "false") +
            ",\"parsed\":\"" + Esc(string.Join(",", _fillingSurfaces.Select(DisplaySurfaceName))) + "\"}");
        // #endregion
    }

    private void ApplyCanalOverlays(bool log = false)
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
        if (!log)
            return;
        // #region agent log
        AgentLog("A", "canal-overlay",
            "{\"fdi\":\"" + Esc(_loadedFdi) +
            "\",\"runId\":\"canal-polish\"" +
            ",\"ids\":\"" + Esc(string.Join(",", _rootCanalIds)) +
            "\",\"dualPass\":true,\"offset\":false,\"glow\":false" +
            ",\"frontArgb\":\"B0B03A3A\",\"ghostArgb\":\"30942828\"" +
            ",\"front\":" + front.Children.Count +
            ",\"ghost\":" + ghost.Children.Count +
            ",\"paths\":" + _canalPaths.Count + "}");
        // #endregion
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
        _hoverMaterial ??= InteractionMaterial(Color.FromArgb(0x22, 0x72, 0xB8, 0xE4), 0x10);
        NoteRebuildDuringDrag("interaction-overlay");
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
            Hover = _hoverSurface is ClinicalSurface hover ? DisplaySurfaceName(hover) : null,
            Selected = selected.Count == 0 ? null : string.Join(",", selected),
            SelectedSurfaces = selected,
            Triangle = triangle
        });
    }

    private int OverlayVertCount()
    {
        var n = 0;
        foreach (var model in _overlayModels)
        {
            if (model?.Geometry is MeshGeometry3D mesh)
                n += mesh.Positions.Count;
        }
        return n;
    }

    private void NoteRebuildDuringDrag(string kind)
    {
        if (!_dragging && !_orbitMoved)
            return;
        _rebuildDuringDrag++;
        if (_orbitRebuildKinds.Length == 0)
            _orbitRebuildKinds = kind;
        else if (!_orbitRebuildKinds.Contains(kind, StringComparison.Ordinal))
            _orbitRebuildKinds += "," + kind;
        // #region agent log
        if (_rebuildDuringDrag <= 4)
            AgentLog("C", "orbit-rebuild",
                "{\"fdi\":\"" + Esc(_loadedFdi) +
                "\",\"kind\":\"" + Esc(kind) +
                "\",\"n\":" + _rebuildDuringDrag + "}");
        // #endregion
    }

    private string DisplaySurfaceName(ClinicalSurface surface) =>
        surface == ClinicalSurface.Palatal ? LabelPalatal.Text : surface.ToString();

    private string MapAssetName() =>
        _loadedFdi == "11" ? "FDI11SurfaceMap.json"
        : _loadedFdi == "12" ? "FDI12SurfaceMap.json"
        : _loadedFdi == "21" ? "FDI21SurfaceMap.json"
        : _loadedFdi == "22" ? "FDI22SurfaceMap.json"
        : _loadedFdi == "31" ? "FDI31SurfaceMap.json"
        : _loadedFdi == "41" ? "FDI41SurfaceMap.json"
        : _loadedFdi == "32" ? "FDI32SurfaceMap.json"
        : _loadedFdi == "42" ? "FDI42SurfaceMap.json"
        : _loadedFdi == "17" ? "FDI17SurfaceMap.json"
        : _loadedFdi == "27" ? "FDI27SurfaceMap.json"
        : _loadedFdi == "37" ? "FDI37SurfaceMap.json"
        : _loadedFdi == "47" ? "FDI47SurfaceMap.json"
        : _loadedFdi == "18" ? "FDI18SurfaceMap.json"
        : _loadedFdi == "28" ? "FDI28SurfaceMap.json"
        : _loadedFdi == "38" ? "FDI38SurfaceMap.json"
        : _loadedFdi == "48" ? "FDI48SurfaceMap.json"
        : _loadedFdi == "13" ? "FDI13SurfaceMap.json"
        : _loadedFdi == "23" ? "FDI23SurfaceMap.json"
        : _loadedFdi == "33" ? "FDI33SurfaceMap.json"
        : _loadedFdi == "43" ? "FDI43SurfaceMap.json"
        : _loadedFdi == "14" ? "FDI14SurfaceMap.json"
        : _loadedFdi == "15" ? "FDI15SurfaceMap.json"
        : _loadedFdi == "25" ? "FDI25SurfaceMap.json"
        : _loadedFdi == "24" ? "FDI24SurfaceMap.json"
        : _loadedFdi == "34" ? "FDI34SurfaceMap.json"
        : _loadedFdi == "44" ? "FDI44SurfaceMap.json"
        : _loadedFdi == "35" ? "FDI35SurfaceMap.json"
        : _loadedFdi == "45" ? "FDI45SurfaceMap.json"
        : _loadedFdi == "26" ? "FDI26SurfaceMap.json"
        : _loadedFdi == "46" ? "FDI46SurfaceMap.json"
        : _loadedFdi == "36" ? "FDI36SurfaceMap.json"
        : _loadedFdi == "16" ? "FDI16SurfaceMap.json"
        : "";

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
