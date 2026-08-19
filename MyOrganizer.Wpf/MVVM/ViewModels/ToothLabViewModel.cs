using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.Dental;
using MyOrganizer.Wpf.MVVM.Infrastructure;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class ToothLabViewModel : ObservableObject
{
    private double _toothSize = 340;
    private double _occlusalSize = 220;
    private string _status = "Hover: None\nSelected: None\nDerived Filling: —\nProcedures: 0";
    private string? _hoverName;
    private Guid? _editingId;
    private IReadOnlyList<ToothSurfaceType> _selected = [];
    private IReadOnlyDictionary<ToothSurfaceType, ToothSurfaceVisual> _surfaceStates;
    private ToothAssetDefinition _asset = ToothAssetRegistry.Get(ToothAssetRegistry.ApprovedFdi);
    private readonly Dictionary<string, LabToothSession> _sessions = new();
    private LabToothSession _session = null!;

    public ToothLabViewModel()
    {
        _session = SessionFor("16");
        SurfaceChoices =
        [
            new LabSurfaceChoice(this, ToothSurfaceType.Occlusal, "Occlusal"),
            new LabSurfaceChoice(this, ToothSurfaceType.Buccal, "Buccal"),
            new LabSurfaceChoice(this, ToothSurfaceType.Lingual, "Palatal"),
            new LabSurfaceChoice(this, ToothSurfaceType.Mesial, "Mesial"),
            new LabSurfaceChoice(this, ToothSurfaceType.Distal, "Distal")
        ];
        Surfaces =
        [
            new LabSurfaceRow(this, ToothSurfaceType.Occlusal, "Occlusal"),
            new LabSurfaceRow(this, ToothSurfaceType.Mesial, "Mesial"),
            new LabSurfaceRow(this, ToothSurfaceType.Distal, "Distal"),
            new LabSurfaceRow(this, ToothSurfaceType.Buccal, "Buccal"),
            new LabSurfaceRow(this, ToothSurfaceType.Lingual, "Palatal")
        ];
        _surfaceStates = BuildStates();
        ClearSelectionCommand = new RelayCommand(ClearPendingSelection, () => HasPendingSurfaces);
        ResetHealthyCommand = new RelayCommand(ResetHealthy);
        DemoMixedCommand = new RelayCommand(DemoMixed);
        CreateProcedureCommand = new RelayCommand(CreateProcedure, () => !IsEditing && HasPendingSurfaces);
        SaveProcedureCommand = new RelayCommand(SaveProcedure, () => IsEditing && HasPendingSurfaces);
        NewProcedureCommand = new RelayCommand(StartNewProcedure);
        SelectToothCommand = new RelayCommand(p => SelectTooth(p?.ToString() ?? ToothAssetRegistry.ApprovedFdi));
        ChartRows =
        [
            new LabChartRow("UPPER", ["18", "17", "16", "15", "14", "13", "12", "11"], ["21", "22", "23", "24", "25", "26", "27", "28"]),
            new LabChartRow("LOWER", ["48", "47", "46", "45", "44", "43", "42", "41"], ["31", "32", "33", "34", "35", "36", "37", "38"])
        ];
        ProcedureItems = [];
        SelectTooth(ToothAssetRegistry.ApprovedFdi);
    }

    public ToothLabClinicalState Clinical => _session.Clinical;
    public event EventHandler? ClinicalChanged;
    public event EventHandler? PendingSelectionChanged;

    public string ToothNumber => _asset.FdiNumber;
    public string Hint =>
        "Click an FDI number to inspect it. FDI 16 and 26 are the approved maxillary first-molar pair. " +
        "FDI 36 and 46 are the approved mandibular first-molar pair. FDI 14 is the first maxillary " +
        "first-premolar reference (not a molar). All imported teeth share hover, multi-select, " +
        "Filling, Create Procedure, Edit, and New Procedure. Terminology is tooth-aware " +
        "(Palatal on maxillary teeth, Lingual on mandibular). Procedure records stay isolated per tooth. " +
        "Other FDI positions remain placeholders.";

    public string SourceNote =>
        "Mesh: Maxillary First Molar, University of Dundee School of Dentistry (Emily McDougall; " +
        "Dr. Andrew Mason; Mark Roughley). CC BY 4.0. Original Sketchfab file UL6sketch_1.OBJ is a left " +
        "maxillary first molar (FDI 26), 28,506 vertices / 28,504 quad faces from CT via ZBrush. " +
        "Tooth Lab triangulates, mirrors/orients to FDI 16, and renders it in native WPF Viewport3D. " +
        "Source kept at Assets/Teeth/Source. Optional debug overlay classifies existing crown triangles " +
        "into Occlusal / Buccal / Palatal / Mesial / Distal; OFF restores the approved healthy tooth. " +
        "https://sketchfab.com/3d-models/maxillary-first-molar-e719a474ef7e4bd7abec508f85f1e984";

    public string InspectorNote =>
        _asset.FdiNumber == ToothAssetRegistry.ApprovedFdi
            ? SourceNote
            : _asset.FdiNumber == "14"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Right maxillary first premolar generated from MaxillaryFirstPremolarTemplate " +
                  "(chewing Occlusal, Palatal not Lingual, FDI14SurfaceMap). " +
                  _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "26"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Left maxillary first molar generated from MaxillaryFirstMolarTemplate " +
                  "(same rules as approved FDI 16, left laterality, FDI26SurfaceMap). " +
                  "Palatal, not Lingual. " + _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "36"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Healthy anatomy is frozen. Debug overlay: Occlusal / Buccal / Lingual / Mesial / Distal. " +
                  "Clinical interaction uses FDI36SurfaceMap (Lingual, not Palatal). " + _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "46"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Right mandibular first molar generated from MandibularFirstMolarTemplate " +
                  "(same rules as approved FDI 36, right laterality, FDI46SurfaceMap). " +
                  "Lingual, not Palatal. " + _asset.Attribution.SketchfabUrl
                : _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Healthy anatomy only; clinical surface map not created yet. " +
                  _asset.Attribution.SketchfabUrl;

    public IReadOnlyList<ProcedureOption> ProcedureOptions { get; } =
        ToothSurfaceAppearance.Keys
            .Select(key => new ProcedureOption(key, ToothSurfaceAppearance.DisplayName(key)))
            .ToList();
    public ObservableCollection<LabSurfaceRow> Surfaces { get; }
    public IReadOnlyList<LabSurfaceChoice> SurfaceChoices { get; }
    public ObservableCollection<ProcedureListItem> ProcedureItems { get; }
    public ICommand ClearSelectionCommand { get; }
    public ICommand ResetHealthyCommand { get; }
    public ICommand DemoMixedCommand { get; }
    public ICommand CreateProcedureCommand { get; }
    public ICommand SaveProcedureCommand { get; }
    public ICommand NewProcedureCommand { get; }
    public ICommand SelectToothCommand { get; }
    public IReadOnlyList<LabChartRow> ChartRows { get; }

    public ToothAssetDefinition SelectedAsset => _asset;
    public bool ShowInspector => _asset.RuntimeImported;
    public bool ShowPlaceholder => !_asset.RuntimeImported;
    public bool ShowClinicalTools => _asset.ClinicalInteraction;
    public bool ShowSegTools => _asset.SurfaceMapAvailable;
    public bool ShowAssetStatus => !_asset.SurfaceMapAvailable;
    public string InnerCameraLabel => _asset.InnerSurfaceName;

    public string LabTitle => "Tooth Lab · FDI " + _asset.FdiNumber;

    public string PlaceholderBody
    {
        get
        {
            var src = _asset.SourceAvailable ? "available" : "missing";
            var runtime = _asset.RuntimeImported ? "imported" : "not configured yet";
            var map = _asset.SurfaceMapAvailable ? "created" : "not created yet";
            var lines =
                "FDI " + _asset.FdiNumber + "\n" +
                _asset.DisplayName + "\n" +
                _asset.Jaw + " · " + _asset.Side + " · " + _asset.ToothKind + "\n\n" +
                "Source: " + src + " (" + _asset.SourceZipFileName + ")\n" +
                "Inner OBJ: " + _asset.InnerObjName + "\n" +
                "Runtime mesh: " + runtime + "\n" +
                "Surface map: " + map + "\n" +
                "Interaction: " + (_asset.ClinicalInteraction ? "available" : "Not available yet") + "\n" +
                "MirrorX: " + _asset.MirrorX + " (contralateral FDI " + _asset.ContralateralFdi + ")\n" +
                "Clinical inner surface: " + _asset.InnerSurfaceName + "\n" +
                "Chewing/incisal label: " + _asset.ChewingSurfaceName + "\n" +
                "License: " + _asset.Attribution.License + "\n" +
                _asset.Attribution.SketchfabUrl;
            if (!string.IsNullOrWhiteSpace(_asset.SourceNote))
                lines += "\n\n" + _asset.SourceNote;
            return lines;
        }
    }

    public string LabelTop => "Buccal";
    public string LabelBottom => _asset.InnerSurfaceName;
    public string LabelLeft => _asset.ContralateralCameraMirror ? "Mesial" : "Distal";
    public string LabelRight => _asset.ContralateralCameraMirror ? "Distal" : "Mesial";

    public double ToothSize
    {
        get => _toothSize;
        set => SetProperty(ref _toothSize, Math.Clamp(value, 250, 360));
    }

    public double OcclusalSize
    {
        get => _occlusalSize;
        set => SetProperty(ref _occlusalSize, Math.Clamp(value, 40, 250));
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public bool IsEditing => _editingId.HasValue;

    public bool HasPendingSurfaces => SurfaceChoices.Any(c => c.IsSelected);

    public bool HasProcedures => ProcedureItems.Count > 0;

    public string EditorStatus =>
        _editingId is Guid id && Clinical.Find(id) is { } editing
            ? $"Editing #{editing.DisplayNumber} Filling"
            : "New procedure";

    public string SelectedSurfacesLabel
    {
        get
        {
            var names = PendingDisplayNames();
            return names.Count == 0 ? "None" : string.Join(", ", names);
        }
    }

    public string ClinicalSummary
    {
        get
        {
            var names = Clinical.FillingSurfaceNames(InnerCameraLabel);
            var derived = names.Count == 0 ? "Derived Filling: —" : "Derived Filling: " + string.Join(", ", names);
            return derived + "\nProcedure records: " + Clinical.Procedures.Count;
        }
    }

    public IReadOnlyList<string> FillingSurfaceNames => Clinical.FillingSurfaceNames(InnerCameraLabel);

    public IReadOnlyList<string> PendingSurfaceNames => PendingDisplayNames();

    public IReadOnlyList<ToothSurfaceType> SelectedSurfaces
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }

    public IReadOnlyDictionary<ToothSurfaceType, ToothSurfaceVisual> SurfaceStates
    {
        get => _surfaceStates;
        private set => SetProperty(ref _surfaceStates, value);
    }

    public void SelectTooth(string fdi)
    {
        if (!ToothAssetRegistry.TryGet(fdi, out var asset))
            return;
        var fromFdi = _session.Clinical.ToothNumber;
        var fromProcs = Clinical.Procedures.Count;
        var fromPending = string.Join(",", PendingDisplayNames());
        if (fromFdi != asset.FdiNumber)
            StashCurrentSession();
        _asset = asset;
        _session = SessionFor(asset.FdiNumber);
        OnPropertyChanged(nameof(SelectedAsset));
        OnPropertyChanged(nameof(ToothNumber));
        OnPropertyChanged(nameof(ShowInspector));
        OnPropertyChanged(nameof(ShowPlaceholder));
        OnPropertyChanged(nameof(ShowClinicalTools));
        OnPropertyChanged(nameof(ShowSegTools));
        OnPropertyChanged(nameof(ShowAssetStatus));
        OnPropertyChanged(nameof(InnerCameraLabel));
        OnPropertyChanged(nameof(InspectorNote));
        OnPropertyChanged(nameof(LabTitle));
        OnPropertyChanged(nameof(PlaceholderBody));
        OnPropertyChanged(nameof(LabelBottom));
        OnPropertyChanged(nameof(LabelLeft));
        OnPropertyChanged(nameof(LabelRight));
        OnPropertyChanged(nameof(Hint));
        foreach (var row in ChartRows)
        {
            foreach (var slot in row.Right)
                slot.SetSelected(slot.Fdi == asset.FdiNumber);
            foreach (var slot in row.Left)
                slot.SetSelected(slot.Fdi == asset.FdiNumber);
        }
        RestoreSession();
        // #region agent log
        AgentLog("B", "select-tooth",
            "{\"fdi\":\"" + asset.FdiNumber +
            "\",\"from\":\"" + fromFdi +
            "\",\"source\":\"" + asset.SourceKind +
            "\",\"mirrorX\":" + (asset.MirrorX ? "true" : "false") +
            ",\"imported\":" + (asset.RuntimeImported ? "true" : "false") +
            ",\"sourceAvailable\":" + (asset.SourceAvailable ? "true" : "false") +
            ",\"map\":" + (asset.SurfaceMapAvailable ? "true" : "false") +
            ",\"interaction\":" + (asset.ClinicalInteraction ? "true" : "false") +
            ",\"inner\":\"" + asset.InnerSurfaceName +
            "\",\"mapAsset\":\"" + (asset.SurfaceMap ?? "") +
            "\",\"fromProcs\":" + fromProcs +
            ",\"toProcs\":" + Clinical.Procedures.Count +
            ",\"fromPending\":\"" + fromPending +
            "\",\"toPending\":\"" + string.Join(",", PendingDisplayNames()) +
            "\",\"toDerived\":\"" + string.Join(",", FillingSurfaceNames) +
            "\",\"contra\":\"" + asset.ContralateralFdi + "\"}");
        // #endregion
        if (!asset.RuntimeImported)
            Status = "FDI " + asset.FdiNumber + " placeholder · source " +
                     (asset.SourceAvailable ? "available" : "missing") + " · runtime not imported.";
        else if (!asset.SurfaceMapAvailable)
            Status = "FDI " + asset.FdiNumber + " · " + asset.DisplayName +
                     "\nClinical surface map: Not created\nInteraction: Not available yet";
        else if (!asset.ClinicalInteraction)
            Status = "FDI " + asset.FdiNumber + " · " + asset.DisplayName +
                     "\nSurface map: debug overlay available\nClinical interaction: Not available";
        else
            RefreshStatus();
    }

    public void SetInteraction(string? hover, IReadOnlyList<string>? selected)
    {
        _hoverName = hover;
        var next = ParsePending(selected);
        var selectedChanged = !PendingDomain().SetEquals(next);
        if (selectedChanged)
        {
            foreach (var choice in SurfaceChoices)
                choice.SetSilent(next.Contains(choice.Surface));
            NotifyPending(log: true);
        }
        RefreshStatus();
    }

    public void OnSurfaceClicked(ToothSurfaceEventArgs e)
    {
        SelectedSurfaces = e.SelectedSurfaces.ToList();
        Status = e.SelectedSurfaces.Count == 0
            ? "No surface selected."
            : "Selected: " + string.Join(", ", e.SelectedSurfaces);
    }

    public void OnSurfaceHovered(ToothSurfaceEventArgs e)
    {
        if (e.Surface is null)
            return;
        Status = $"Hover: {e.Surface}  ·  click to select";
    }

    internal void PublishStates() => SurfaceStates = BuildStates();

    internal void OnChoiceChanged() => NotifyPending(log: true);

    internal void BeginEdit(Guid id)
    {
        var procedure = Clinical.Find(id);
        if (procedure is null)
            return;
        _editingId = id;
        foreach (var choice in SurfaceChoices)
            choice.SetSilent(procedure.Surfaces.Contains(choice.Surface));
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(EditorStatus));
        NotifyPending(log: false);
        RefreshStatus();
    }

    private void CreateProcedure()
    {
        if (IsEditing)
            return;
        var created = Clinical.TryCreate(DentalProcedureType.Filling, PendingDomain());
        // #region agent log
        AgentLog("A", "procedure-commit", ProcedureLog("create", created, created is not null));
        // #endregion
        if (created is null)
            return;
        RebuildProcedureItems();
        StartNewProcedure();
        NotifyClinical();
    }

    private void SaveProcedure()
    {
        if (_editingId is not Guid id)
            return;
        var changed = Clinical.TryUpdateSurfaces(id, PendingDomain());
        var saved = Clinical.Find(id);
        // #region agent log
        AgentLog("C", "procedure-commit", ProcedureLog("save", saved, changed));
        // #endregion
        if (saved is null)
            return;
        RebuildProcedureItems();
        StartNewProcedure();
        if (changed)
            NotifyClinical();
    }

    private void StartNewProcedure()
    {
        _editingId = null;
        foreach (var choice in SurfaceChoices)
            choice.SetSilent(false);
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(EditorStatus));
        NotifyPending(log: false);
        RefreshStatus();
    }

    private void ClearPendingSelection()
    {
        foreach (var choice in SurfaceChoices)
            choice.SetSilent(false);
        NotifyPending(log: true);
        RefreshStatus();
    }

    private void NotifyPending(bool log)
    {
        OnPropertyChanged(nameof(HasPendingSurfaces));
        OnPropertyChanged(nameof(SelectedSurfacesLabel));
        OnPropertyChanged(nameof(PendingSurfaceNames));
        ((RelayCommand)ClearSelectionCommand).RaiseCanExecuteChanged();
        ((RelayCommand)CreateProcedureCommand).RaiseCanExecuteChanged();
        ((RelayCommand)SaveProcedureCommand).RaiseCanExecuteChanged();
        PendingSelectionChanged?.Invoke(this, EventArgs.Empty);
        if (log)
        {
            // #region agent log
            AgentLog("E", "pending-selection",
                "{\"fdi\":\"" + ToothNumber +
                "\",\"selected\":\"" + string.Join(",", PendingDisplayNames()) +
                "\",\"procedureCount\":" + Clinical.Procedures.Count +
                ",\"editing\":" + (_editingId.HasValue ? "true" : "false") + "}");
            // #endregion
        }
    }

    private void NotifyClinical()
    {
        OnPropertyChanged(nameof(ClinicalSummary));
        OnPropertyChanged(nameof(FillingSurfaceNames));
        RefreshStatus();
        ClinicalChanged?.Invoke(this, EventArgs.Empty);
        // #region agent log
        AgentLog("B", "procedure-project",
            "{\"fdi\":\"" + ToothNumber +
            "\",\"procedureCount\":" + Clinical.Procedures.Count +
            ",\"history\":\"" + HistoryLog() +
            "\",\"derived\":\"" + string.Join(",", FillingSurfaceNames) + "\"}");
        // #endregion
    }

    private void RebuildProcedureItems()
    {
        ProcedureItems.Clear();
        foreach (var procedure in Clinical.Procedures)
            ProcedureItems.Add(new ProcedureListItem(this, procedure));
        OnPropertyChanged(nameof(HasProcedures));
    }

    private HashSet<ToothSurfaceType> PendingDomain() =>
        SurfaceChoices.Where(c => c.IsSelected).Select(c => c.Surface).ToHashSet();

    private IReadOnlyList<string> PendingDisplayNames() =>
        LabSurfaces.DisplayNames(PendingDomain(), InnerCameraLabel);

    private LabToothSession SessionFor(string fdi)
    {
        if (!_sessions.TryGetValue(fdi, out var session))
        {
            session = new LabToothSession(fdi);
            _sessions[fdi] = session;
        }
        return session;
    }

    private void StashCurrentSession()
    {
        _session.EditingId = _editingId;
        _session.Pending.Clear();
        foreach (var surface in PendingDomain())
            _session.Pending.Add(surface);
    }

    private void RestoreSession()
    {
        _editingId = _session.EditingId;
        foreach (var choice in SurfaceChoices)
        {
            if (choice.Surface == ToothSurfaceType.Lingual)
                choice.SetLabel(_asset.InnerSurfaceName);
            choice.SetSilent(_session.Pending.Contains(choice.Surface));
        }
        foreach (var row in Surfaces)
        {
            if (row.Surface == ToothSurfaceType.Lingual)
                row.SetLabel(_asset.InnerSurfaceName);
        }
        RebuildProcedureItems();
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(EditorStatus));
        OnPropertyChanged(nameof(ClinicalSummary));
        OnPropertyChanged(nameof(FillingSurfaceNames));
        OnPropertyChanged(nameof(HasPendingSurfaces));
        OnPropertyChanged(nameof(SelectedSurfacesLabel));
        OnPropertyChanged(nameof(PendingSurfaceNames));
        ((RelayCommand)ClearSelectionCommand).RaiseCanExecuteChanged();
        ((RelayCommand)CreateProcedureCommand).RaiseCanExecuteChanged();
        ((RelayCommand)SaveProcedureCommand).RaiseCanExecuteChanged();
    }

    private static HashSet<ToothSurfaceType> ParsePending(IReadOnlyList<string>? names)
    {
        var set = new HashSet<ToothSurfaceType>();
        if (names is null)
            return set;
        foreach (var name in names)
        {
            if (LabSurfaces.TryParse(name, out var surface))
                set.Add(surface);
        }
        return set;
    }

    private string ProcedureLog(string mode, DentalProcedure? procedure, bool changed) =>
        "{\"fdi\":\"" + ToothNumber +
        "\",\"mode\":\"" + mode +
        "\",\"changed\":" + (changed ? "true" : "false") +
        ",\"id\":\"" + (procedure?.Id.ToString() ?? "") +
        "\",\"n\":" + (procedure?.DisplayNumber ?? 0) +
        ",\"surfaces\":\"" + (procedure is null ? "" : LabSurfaces.Join(procedure.Surfaces, InnerCameraLabel)) +
        "\",\"surfaceCount\":" + (procedure?.Surfaces.Count ?? 0) +
        ",\"procedureCount\":" + Clinical.Procedures.Count +
        ",\"history\":\"" + HistoryLog() +
        "\",\"derived\":\"" + string.Join(",", FillingSurfaceNames) + "\"}";

    private string HistoryLog() =>
        string.Join(";", Clinical.Procedures.Select(p =>
            "#" + p.DisplayNumber + ":" + p.Id.ToString("N")[..8] + ":" + LabSurfaces.Join(p.Surfaces, InnerCameraLabel)));

    // #region agent log
    private static void AgentLog(string hypothesisId, string message, string dataJson)
    {
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"procedure-v1\",\"hypothesisId\":\"" + hypothesisId +
                   "\",\"location\":\"ToothLabViewModel.cs\",\"message\":\"" + message +
                   "\",\"data\":" + dataJson + ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { /* lab logging must not break the workflow */ }
    }
    // #endregion

    private void RefreshStatus()
    {
        var names = Clinical.FillingSurfaceNames(InnerCameraLabel);
        Status =
            "Hover: " + (_hoverName ?? "None") + "\n" +
            "Selected: " + SelectedSurfacesLabel + "\n" +
            (names.Count == 0 ? "Derived Filling: —" : "Derived Filling: " + string.Join(", ", names)) + "\n" +
            "Procedures: " + Clinical.Procedures.Count +
            (IsEditing ? "\n" + EditorStatus : "");
    }

    private Dictionary<ToothSurfaceType, ToothSurfaceVisual> BuildStates() =>
        Surfaces.ToDictionary(
            row => row.Surface,
            row => new ToothSurfaceVisual { ProcedureKey = row.ProcedureKey });

    private void ResetHealthy()
    {
        foreach (var row in Surfaces)
            row.SetKeySilent(ToothSurfaceAppearance.Healthy);
        PublishStates();
        Status = "All surfaces healthy.";
    }

    private void DemoMixed()
    {
        Set(ToothSurfaceType.Mesial, ToothSurfaceAppearance.Filling);
        Set(ToothSurfaceType.Occlusal, ToothSurfaceAppearance.Caries);
        Set(ToothSurfaceType.Distal, ToothSurfaceAppearance.Healthy);
        Set(ToothSurfaceType.Buccal, ToothSurfaceAppearance.Crown);
        Set(ToothSurfaceType.Lingual, ToothSurfaceAppearance.Temporary);
        PublishStates();
        Status = "Mixed procedures: filling / caries / healthy / crown / temporary.";
    }

    private void Set(ToothSurfaceType surface, string key)
    {
        foreach (var row in Surfaces)
        {
            if (row.Surface == surface)
                row.SetKeySilent(key);
        }
    }
}

public sealed class LabChartRow
{
    public LabChartRow(string jaw, IReadOnlyList<string> right, IReadOnlyList<string> left)
    {
        Jaw = jaw;
        Right = right.Select(fdi => new LabFdiSlot(fdi)).ToList();
        Left = left.Select(fdi => new LabFdiSlot(fdi)).ToList();
    }

    public string Jaw { get; }
    public IReadOnlyList<LabFdiSlot> Right { get; }
    public IReadOnlyList<LabFdiSlot> Left { get; }
}

public sealed class LabFdiSlot : ObservableObject
{
    private bool _isSelected;

    public LabFdiSlot(string fdi) => Fdi = fdi;

    public string Fdi { get; }
    public bool IsSelected
    {
        get => _isSelected;
        private set => SetProperty(ref _isSelected, value);
    }

    internal void SetSelected(bool value) => IsSelected = value;
}

public sealed class LabSurfaceChoice : ObservableObject
{
    private readonly ToothLabViewModel _owner;
    private bool _isSelected;

    public LabSurfaceChoice(ToothLabViewModel owner, ToothSurfaceType surface, string label)
    {
        _owner = owner;
        Surface = surface;
        Label = label;
    }

    public ToothSurfaceType Surface { get; }
    public string Label { get; private set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!SetProperty(ref _isSelected, value))
                return;
            _owner.OnChoiceChanged();
        }
    }

    internal void SetSilent(bool value)
    {
        if (_isSelected == value)
            return;
        _isSelected = value;
        OnPropertyChanged(nameof(IsSelected));
    }

    internal void SetLabel(string value)
    {
        if (Label == value)
            return;
        Label = value;
        OnPropertyChanged(nameof(Label));
    }
}

internal sealed class LabToothSession
{
    public LabToothSession(string fdi) => Clinical = new ToothLabClinicalState(fdi);

    public ToothLabClinicalState Clinical { get; }
    public HashSet<ToothSurfaceType> Pending { get; } = [];
    public Guid? EditingId { get; set; }
}

public sealed class ProcedureListItem
{
    public ProcedureListItem(ToothLabViewModel owner, DentalProcedure procedure)
    {
        Id = procedure.Id;
        DisplayNumber = procedure.DisplayNumber;
        Title = $"#{procedure.DisplayNumber} Filling";
        SurfacesDisplay = LabSurfaces.Join(procedure.Surfaces, owner.InnerCameraLabel);
        EditCommand = new RelayCommand(() => owner.BeginEdit(Id));
    }

    public Guid Id { get; }
    public int DisplayNumber { get; }
    public string Title { get; }
    public string SurfacesDisplay { get; }
    public ICommand EditCommand { get; }
}

public sealed class LabSurfaceRow : ObservableObject
{
    private readonly ToothLabViewModel _owner;
    private string _procedureKey = ToothSurfaceAppearance.Healthy;

    public LabSurfaceRow(ToothLabViewModel owner, ToothSurfaceType surface, string label)
    {
        _owner = owner;
        Surface = surface;
        Label = label;
    }

    public ToothSurfaceType Surface { get; }
    public string Label { get; private set; }

    public string ProcedureKey
    {
        get => _procedureKey;
        set
        {
            if (!SetProperty(ref _procedureKey, value))
                return;
            _owner.PublishStates();
        }
    }

    public string DisplayName => ToothSurfaceAppearance.DisplayName(_procedureKey);

    internal void SetKeySilent(string key)
    {
        _procedureKey = key;
        OnPropertyChanged(nameof(ProcedureKey));
        OnPropertyChanged(nameof(DisplayName));
    }

    internal void SetLabel(string value)
    {
        if (Label == value)
            return;
        Label = value;
        OnPropertyChanged(nameof(Label));
    }
}

public sealed record ProcedureOption(string Key, string Name);
