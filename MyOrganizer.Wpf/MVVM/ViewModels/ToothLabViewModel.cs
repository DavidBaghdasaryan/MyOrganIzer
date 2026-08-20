using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
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
    private LabPatient _currentPatient = null!;
    private LabToothSession _session = null!;
    private DentalProcedureType _procedureType = DentalProcedureType.Filling;

    public ToothLabViewModel()
    {
        Patients =
        [
            new LabPatient("A", "Patient A"),
            new LabPatient("B", "Patient B")
        ];
        _currentPatient = Patients[0];
        _session = SessionFor("16");
        SurfaceChoices =
        [
            new LabSurfaceChoice(this, ToothSurfaceType.Occlusal, "Occlusal"),
            new LabSurfaceChoice(this, ToothSurfaceType.Buccal, "Buccal"),
            new LabSurfaceChoice(this, ToothSurfaceType.Lingual, "Palatal"),
            new LabSurfaceChoice(this, ToothSurfaceType.Mesial, "Mesial"),
            new LabSurfaceChoice(this, ToothSurfaceType.Distal, "Distal")
        ];
        CanalChoices = [];
        Surfaces =
        [
            new LabSurfaceRow(this, ToothSurfaceType.Occlusal, "Occlusal"),
            new LabSurfaceRow(this, ToothSurfaceType.Mesial, "Mesial"),
            new LabSurfaceRow(this, ToothSurfaceType.Distal, "Distal"),
            new LabSurfaceRow(this, ToothSurfaceType.Buccal, "Buccal"),
            new LabSurfaceRow(this, ToothSurfaceType.Lingual, "Palatal")
        ];
        _surfaceStates = BuildStates();
        ClearSelectionCommand = new RelayCommand(ClearPendingSelection, () => HasPendingSurfaces || HasPendingCanals);
        ResetHealthyCommand = new RelayCommand(ResetHealthy);
        DemoMixedCommand = new RelayCommand(DemoMixed);
        CreateProcedureCommand = new RelayCommand(CreateProcedure, CanCreateProcedure);
        SaveProcedureCommand = new RelayCommand(SaveProcedure, CanSaveProcedure);
        NewProcedureCommand = new RelayCommand(StartNewProcedure);
        SelectToothCommand = new RelayCommand(p => SelectTooth(p?.ToString() ?? ToothAssetRegistry.ApprovedFdi));
        ChartRows =
        [
            new LabChartRow("UPPER", ["18", "17", "16", "15", "14", "13", "12", "11"], ["21", "22", "23", "24", "25", "26", "27", "28"]),
            new LabChartRow("LOWER", ["48", "47", "46", "45", "44", "43", "42", "41"], ["31", "32", "33", "34", "35", "36", "37", "38"])
        ];
        ProcedureItems = [];
        SelectTooth(ToothAssetRegistry.ApprovedFdi);
        SeedAcceptanceDemo();
        OdontogramThumbStore.Warm();
        RefreshOdontogramClinical();
        RebuildProcedureItems();
    }

    public ToothLabClinicalState Clinical => _session.Clinical;
    public event EventHandler? ClinicalChanged;
    public event EventHandler? PendingSelectionChanged;

    public string ToothNumber => _asset.FdiNumber;
    public string Hint =>
        "Click a tooth in the odontogram to open it in the detailed 3D viewer. Marks come from this patient's procedure records. " +
        "FDI 16 and 26 are the approved maxillary first-molar pair. " +
        "FDI 36 and 46 are the approved mandibular first-molar pair. FDI 14 and 24 are the maxillary " +
        "first-premolar pair. FDI 34 and 44 are the mandibular first-premolar pair. FDI 15 and 25 are the maxillary " +
        "second-premolar pair. FDI 35 and 45 are the mandibular second-premolar pair. FDI 13 and 23 are the maxillary " +
        "canine pair. FDI 33 is the mandibular canine reference. All imported teeth share hover, multi-select, " +
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
            : _asset.FdiNumber == "15"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Right maxillary second premolar generated from MaxillarySecondPremolarTemplate " +
                  "(cervical Occlusal, Palatal not Lingual, FDI15SurfaceMap). " +
                  _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "25"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Left maxillary second premolar generated from MaxillarySecondPremolarTemplate " +
                  "(same rules as approved FDI 15, left laterality, FDI25SurfaceMap). " +
                  "Palatal, not Lingual. " + _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "24"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Left maxillary first premolar generated from MaxillaryFirstPremolarTemplate " +
                  "(same rules as approved FDI 14, left laterality, FDI24SurfaceMap). " +
                  "Palatal, not Lingual. " + _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "34"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Left mandibular first premolar generated from MandibularFirstPremolarTemplate " +
                  "(cervical Occlusal, Lingual not Palatal, FDI34SurfaceMap). " +
                  _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "44"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Right mandibular first premolar generated from MandibularFirstPremolarTemplate " +
                  "(same rules as approved FDI 34, right laterality, FDI44SurfaceMap). " +
                  "Lingual, not Palatal. " + _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "35"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Left mandibular second premolar generated from MandibularSecondPremolarTemplate " +
                  "(cervical Occlusal, Lingual not Palatal, FDI35SurfaceMap). " +
                  _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "45"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Right mandibular second premolar generated from MandibularSecondPremolarTemplate " +
                  "(same rules as approved FDI 35, right laterality, FDI45SurfaceMap). " +
                  "Lingual, not Palatal. " + _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "11"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Right maxillary central incisor generated from MaxillaryCentralIncisorTemplate " +
                  "(cervical Occlusal, Palatal not Lingual, FDI11SurfaceMap). " +
                  _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "21"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Left maxillary central incisor generated from MaxillaryCentralIncisorTemplate " +
                  "(same rules as approved FDI 11, left laterality, FDI21SurfaceMap). " +
                  "Palatal, not Lingual. " + _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "12"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Right maxillary lateral incisor generated from MaxillaryLateralIncisorTemplate " +
                  "(cervical Occlusal, Palatal not Lingual, FDI12SurfaceMap). " +
                  _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "22"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Left maxillary lateral incisor generated from MaxillaryLateralIncisorTemplate " +
                  "(same rules as approved FDI 12, left laterality, FDI22SurfaceMap). " +
                  "Palatal, not Lingual. " + _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "13"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Right maxillary canine generated from MaxillaryCanineTemplate " +
                  "(cervical Occlusal, Palatal not Lingual, FDI13SurfaceMap). " +
                  _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "23"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Left maxillary canine generated from MaxillaryCanineTemplate " +
                  "(same rules as approved FDI 13, left laterality, FDI23SurfaceMap). " +
                  "Palatal, not Lingual. " + _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "33"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Left mandibular canine generated from MandibularCanineTemplate " +
                  "(cervical Occlusal, Lingual not Palatal, FDI33SurfaceMap). " +
                  _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "43"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Right mandibular canine generated from MandibularCanineTemplate " +
                  "(same rules as approved FDI 33, right laterality, FDI43SurfaceMap). " +
                  "Lingual, not Palatal. " + _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "31"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Left mandibular central incisor generated from MandibularCentralIncisorTemplate " +
                  "(cervical Occlusal, Lingual not Palatal, FDI31SurfaceMap). " +
                  _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "41"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Right mandibular central incisor generated from MandibularCentralIncisorTemplate " +
                  "(same rules as approved FDI 31, right laterality, FDI41SurfaceMap). " +
                  "Lingual, not Palatal. " + _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "32"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Left mandibular lateral incisor generated from MandibularLateralIncisorTemplate " +
                  "(cervical Occlusal, Lingual not Palatal, FDI32SurfaceMap). " +
                  _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "42"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Right mandibular lateral incisor generated from MandibularLateralIncisorTemplate " +
                  "(same rules as approved FDI 32, right laterality, FDI42SurfaceMap). " +
                  "Lingual, not Palatal. " + _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "17"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Right maxillary second molar generated from MaxillarySecondMolarTemplate " +
                  "(cervical Occlusal, Palatal not Lingual, FDI17SurfaceMap). " +
                  _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "27"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Left maxillary second molar generated from MaxillarySecondMolarTemplate " +
                  "(same rules as approved FDI 17, left laterality, FDI27SurfaceMap). " +
                  "Palatal, not Lingual. " + _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "37"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Left mandibular second molar generated from MandibularSecondMolarTemplate " +
                  "(cervical Occlusal, Lingual not Palatal, FDI37SurfaceMap). " +
                  _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "47"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Right mandibular second molar generated from MandibularSecondMolarTemplate " +
                  "(same rules as approved FDI 37, right laterality, FDI47SurfaceMap). " +
                  "Lingual, not Palatal. " + _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "18"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Right maxillary third molar generated from MaxillaryThirdMolarTemplate " +
                  "(cervical Occlusal, Palatal not Lingual, FDI18SurfaceMap). " +
                  _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "28"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Left maxillary third molar generated from MaxillaryThirdMolarTemplate " +
                  "(same rules as approved FDI 18, left laterality, FDI28SurfaceMap). " +
                  "Palatal, not Lingual. " + _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "38"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Left mandibular third molar generated from MandibularThirdMolarTemplate " +
                  "(cervical Occlusal, Lingual not Palatal, FDI38SurfaceMap). " +
                  _asset.Attribution.SketchfabUrl
            : _asset.FdiNumber == "48"
                ? _asset.DisplayName + ", " + _asset.Attribution.Institution + " (" +
                  _asset.Attribution.License + "). Original file " + _asset.InnerObjName +
                  ". Right mandibular third molar generated from MandibularThirdMolarTemplate " +
                  "(same rules as approved FDI 38, right laterality, FDI48SurfaceMap). " +
                  "Lingual, not Palatal. " + _asset.Attribution.SketchfabUrl
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
    public ObservableCollection<LabRootCanalChoice> CanalChoices { get; }
    public ObservableCollection<ProcedureListItem> ProcedureItems { get; }
    public ICommand ClearSelectionCommand { get; }
    public ICommand ResetHealthyCommand { get; }
    public ICommand DemoMixedCommand { get; }
    public ICommand CreateProcedureCommand { get; }
    public ICommand SaveProcedureCommand { get; }
    public ICommand NewProcedureCommand { get; }
    public ICommand SelectToothCommand { get; }
    public IReadOnlyList<LabChartRow> ChartRows { get; }
    public IReadOnlyList<LabPatient> Patients { get; }
    public IReadOnlyList<LabProcedureTypeChoice> ProcedureTypeChoices { get; } =
    [
        new(DentalProcedureType.Filling, DentalProcedureTypes.DisplayName(DentalProcedureType.Filling)),
        new(DentalProcedureType.Implant, DentalProcedureTypes.DisplayName(DentalProcedureType.Implant)),
        new(DentalProcedureType.Endodontic, DentalProcedureTypes.DisplayName(DentalProcedureType.Endodontic)),
        new(DentalProcedureType.Extraction, DentalProcedureTypes.DisplayName(DentalProcedureType.Extraction))
    ];

    public LabPatient CurrentPatient
    {
        get => _currentPatient;
        set
        {
            if (value is null || ReferenceEquals(_currentPatient, value))
                return;
            SwitchPatient(value);
        }
    }

    public DentalProcedureType SelectedProcedureType
    {
        get => _procedureType;
        set
        {
            if (!SetProperty(ref _procedureType, value))
                return;
            OnPropertyChanged(nameof(ShowSurfacePicker));
            OnPropertyChanged(nameof(ShowCanalPicker));
            OnPropertyChanged(nameof(ShowWholeToothHint));
            OnPropertyChanged(nameof(EditorStatus));
            ((RelayCommand)CreateProcedureCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SaveProcedureCommand).RaiseCanExecuteChanged();
        }
    }

    public bool ShowSurfacePicker => DentalProcedureTypes.RequiresSurfaces(SelectedProcedureType);
    public bool ShowCanalPicker => DentalProcedureTypes.RequiresRootCanals(SelectedProcedureType, ToothNumber);
    public bool ShowWholeToothHint => !ShowSurfacePicker && !ShowCanalPicker;

    public ToothAssetDefinition SelectedAsset => _asset;
    public bool ShowInspector => _asset.RuntimeImported;
    public bool IsImplantSelected =>
        ToothOdontogramState.From(ToothNumber, Clinical.Procedures).ShowImplant;
    public bool ShowDetailedViewer => ShowInspector && !IsImplantSelected;
    public bool ShowEmptyImplantViewer => ShowInspector && IsImplantSelected;
    public bool ShowPlaceholder => !_asset.RuntimeImported;
    public bool ShowClinicalTools => _asset.ClinicalInteraction;
    public bool ShowSegTools => _asset.SurfaceMapAvailable && !IsImplantSelected;
    public bool ShowAssetStatus => !_asset.SurfaceMapAvailable;
    public string InnerCameraLabel => _asset.InnerSurfaceName;

    public string LabTitle => "Tooth Lab · " + _currentPatient.Name + " · FDI " + _asset.FdiNumber;

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
    public bool HasPendingCanals => CanalChoices.Any(c => c.IsSelected);

    public bool HasProcedures => ProcedureItems.Count > 0;

    public string EditorStatus =>
        _editingId is Guid id && Clinical.Find(id) is { } editing
            ? "Editing " + ProcedureListItem.FormatTitle(editing)
            : "New procedure · " + DentalProcedureTypes.DisplayName(SelectedProcedureType);

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
            var canals = ToothRootCanalCatalog.Join(ToothNumber, Clinical.TreatedRootCanalIds());
            var canalLine = string.IsNullOrEmpty(canals) ? "Derived Root Canal: —" : "Derived Root Canal: " + canals;
            return derived + "\n" + canalLine + "\nProcedure records: " + Clinical.Procedures.Count;
        }
    }

    public IReadOnlyList<string> FillingSurfaceNames => Clinical.FillingSurfaceNames(InnerCameraLabel);

    public IReadOnlyList<string> TreatedRootCanalIds => Clinical.TreatedRootCanalIds();

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
        OnPropertyChanged(nameof(ShowAssetStatus));
        NotifyImplantPresentation();
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
        RebuildCanalChoices();
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
        else if (IsImplantSelected)
            Status = "FDI " + asset.FdiNumber + " · Implant\nDetailed 3D viewer is empty for implant teeth.";
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
        SelectedProcedureType = procedure.ProcedureType;
        foreach (var choice in SurfaceChoices)
            choice.SetSilent(procedure.Surfaces.Contains(choice.Surface));
        foreach (var choice in CanalChoices)
            choice.SetSilent(procedure.RootCanalIds.Contains(choice.Id));
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(EditorStatus));
        NotifyPending(log: false);
        RefreshStatus();
    }

    private void CreateProcedure()
    {
        if (IsEditing)
            return;
        var created = Clinical.TryCreate(SelectedProcedureType, PendingDomain(), PendingCanalIds());
        // #region agent log
        AgentLog("A", "procedure-commit", ProcedureLog("create", created, created is not null));
        // #endregion
        if (created is null)
            return;
        RebuildProcedureItems();
        StartNewProcedure();
        NotifyClinical();
    }

    private bool CanCreateProcedure()
    {
        if (IsEditing)
            return false;
        if (DentalProcedureTypes.RequiresSurfaces(SelectedProcedureType) && !HasPendingSurfaces)
            return false;
        if (DentalProcedureTypes.RequiresRootCanals(SelectedProcedureType, ToothNumber) && !HasPendingCanals)
            return false;
        return true;
    }

    private bool CanSaveProcedure()
    {
        if (_editingId is not Guid id)
            return false;
        var editing = Clinical.Find(id);
        if (editing is null)
            return false;
        if (DentalProcedureTypes.RequiresSurfaces(editing.ProcedureType) && !HasPendingSurfaces)
            return false;
        if (DentalProcedureTypes.RequiresRootCanals(editing.ProcedureType, ToothNumber) && !HasPendingCanals)
            return false;
        return true;
    }

    private void SaveProcedure()
    {
        if (_editingId is not Guid id)
            return;
        var saved = Clinical.Find(id);
        if (saved is null)
            return;
        var changed = false;
        if (DentalProcedureTypes.RequiresSurfaces(saved.ProcedureType))
            changed = Clinical.TryUpdateSurfaces(id, PendingDomain());
        else if (DentalProcedureTypes.RequiresRootCanals(saved.ProcedureType, ToothNumber))
            changed = Clinical.TryUpdateRootCanals(id, PendingCanalIds());
        // #region agent log
        AgentLog("C", "procedure-commit", ProcedureLog("save", saved, changed));
        // #endregion
        RebuildProcedureItems();
        StartNewProcedure();
        if (changed || !DentalProcedureTypes.RequiresSurfaces(saved.ProcedureType))
            NotifyClinical();
    }

    internal void RemoveProcedure(Guid id)
    {
        if (!Clinical.TryRemove(id))
            return;
        if (_editingId == id)
            StartNewProcedure();
        RebuildProcedureItems();
        NotifyClinical();
    }

    private void StartNewProcedure()
    {
        _editingId = null;
        foreach (var choice in SurfaceChoices)
            choice.SetSilent(false);
        foreach (var choice in CanalChoices)
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
        foreach (var choice in CanalChoices)
            choice.SetSilent(false);
        NotifyPending(log: true);
        RefreshStatus();
    }

    private void NotifyPending(bool log)
    {
        OnPropertyChanged(nameof(HasPendingSurfaces));
        OnPropertyChanged(nameof(HasPendingCanals));
        OnPropertyChanged(nameof(SelectedSurfacesLabel));
        OnPropertyChanged(nameof(SelectedCanalsLabel));
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
                "\",\"canals\":\"" + string.Join(",", PendingCanalIds()) +
                "\",\"procedureCount\":" + Clinical.Procedures.Count +
                ",\"editing\":" + (_editingId.HasValue ? "true" : "false") + "}");
            // #endregion
        }
    }

    private void NotifyImplantPresentation()
    {
        OnPropertyChanged(nameof(IsImplantSelected));
        OnPropertyChanged(nameof(ShowDetailedViewer));
        OnPropertyChanged(nameof(ShowEmptyImplantViewer));
        OnPropertyChanged(nameof(ShowSegTools));
    }

    private void NotifyClinical()
    {
        OnPropertyChanged(nameof(ClinicalSummary));
        OnPropertyChanged(nameof(FillingSurfaceNames));
        OnPropertyChanged(nameof(TreatedRootCanalIds));
        NotifyImplantPresentation();
        RefreshOdontogramClinical();
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
        {
            var item = new ProcedureListItem(this, procedure);
            ProcedureItems.Add(item);
            // #region agent log
            AgentLog("D", "procedure-title",
                "{\"runId\":\"post-fix\"" +
                ",\"title\":\"" + item.Title.Replace("\\", "\\\\").Replace("\"", "\\\"") +
                "\",\"hasHash\":" + (item.Title.Contains('#') ? "true" : "false") +
                ",\"subtitle\":\"" + item.SurfacesDisplay.Replace("\\", "\\\\").Replace("\"", "\\\"") +
                "\",\"type\":\"" + procedure.ProcedureType + "\"}");
            // #endregion
        }
        OnPropertyChanged(nameof(HasProcedures));
    }

    private HashSet<ToothSurfaceType> PendingDomain() =>
        SurfaceChoices.Where(c => c.IsSelected).Select(c => c.Surface).ToHashSet();

    private IReadOnlyList<string> PendingDisplayNames() =>
        LabSurfaces.DisplayNames(PendingDomain(), InnerCameraLabel);

    private HashSet<string> PendingCanalIds() =>
        CanalChoices.Where(c => c.IsSelected).Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private void RebuildCanalChoices()
    {
        CanalChoices.Clear();
        foreach (var canal in ToothRootCanalCatalog.ForFdi(ToothNumber))
            CanalChoices.Add(new LabRootCanalChoice(this, canal.Id, canal.DisplayName));
        OnPropertyChanged(nameof(ShowCanalPicker));
        OnPropertyChanged(nameof(ShowWholeToothHint));
        OnPropertyChanged(nameof(SelectedCanalsLabel));
    }

    public string SelectedCanalsLabel
    {
        get
        {
            var names = CanalChoices.Where(c => c.IsSelected).Select(c => c.Label).ToList();
            return names.Count == 0 ? "None" : string.Join(", ", names);
        }
    }

    private LabToothSession SessionFor(string fdi) => SessionFor(_currentPatient, fdi);

    private static LabToothSession SessionFor(LabPatient patient, string fdi)
    {
        if (!patient.Sessions.TryGetValue(fdi, out var session))
        {
            session = new LabToothSession(fdi);
            patient.Sessions[fdi] = session;
        }
        return session;
    }

    private void SwitchPatient(LabPatient patient)
    {
        StashCurrentSession();
        _currentPatient = patient;
        OnPropertyChanged(nameof(CurrentPatient));
        OnPropertyChanged(nameof(LabTitle));
        _session = SessionFor(ToothNumber);
        RestoreSession();
        NotifyClinical();
    }

    private void SeedAcceptanceDemo()
    {
        var a = Patients[0];
        SessionFor(a, "16").Clinical.TryCreate(DentalProcedureType.Filling, [ToothSurfaceType.Occlusal]);
        SessionFor(a, "24").Clinical.TryCreate(DentalProcedureType.Implant, []);
        SessionFor(a, "34").Clinical.TryCreate(DentalProcedureType.Implant, []);
        SessionFor(a, "46").Clinical.TryCreate(DentalProcedureType.Extraction, []);
    }

    private void RefreshOdontogramClinical()
    {
        foreach (var row in ChartRows)
        {
            foreach (var slot in row.Right.Concat(row.Left))
            {
                var clinical = SessionFor(slot.Fdi).Clinical;
                slot.ApplyPresentation(
                    OdontogramThumbStore.Get(slot.Fdi),
                    ToothOdontogramState.From(slot.Fdi, clinical.Procedures));
            }
        }
    }

    private void StashCurrentSession()
    {
        _session.EditingId = _editingId;
        _session.Pending.Clear();
        foreach (var surface in PendingDomain())
            _session.Pending.Add(surface);
        _session.PendingCanals.Clear();
        foreach (var id in PendingCanalIds())
            _session.PendingCanals.Add(id);
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
        RebuildCanalChoices();
        foreach (var choice in CanalChoices)
            choice.SetSilent(_session.PendingCanals.Contains(choice.Id));
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
        OnPropertyChanged(nameof(HasPendingCanals));
        OnPropertyChanged(nameof(SelectedSurfacesLabel));
        OnPropertyChanged(nameof(SelectedCanalsLabel));
        OnPropertyChanged(nameof(PendingSurfaceNames));
        OnPropertyChanged(nameof(TreatedRootCanalIds));
        OnPropertyChanged(nameof(ShowSurfacePicker));
        OnPropertyChanged(nameof(ShowCanalPicker));
        OnPropertyChanged(nameof(ShowWholeToothHint));
        NotifyImplantPresentation();
        ((RelayCommand)ClearSelectionCommand).RaiseCanExecuteChanged();
        ((RelayCommand)CreateProcedureCommand).RaiseCanExecuteChanged();
        ((RelayCommand)SaveProcedureCommand).RaiseCanExecuteChanged();
        PendingSelectionChanged?.Invoke(this, EventArgs.Empty);
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
        "\",\"canals\":\"" + (procedure is null ? "" : ToothRootCanalCatalog.Join(ToothNumber, procedure.RootCanalIds)) +
        "\",\"type\":\"" + (procedure?.ProcedureType.ToString() ?? "") +
        "\",\"surfaceCount\":" + (procedure?.Surfaces.Count ?? 0) +
        ",\"canalCount\":" + (procedure?.RootCanalIds.Count ?? 0) +
        ",\"procedureCount\":" + Clinical.Procedures.Count +
        ",\"history\":\"" + HistoryLog() +
        "\",\"derived\":\"" + string.Join(",", FillingSurfaceNames) + "\"}";

    private string HistoryLog() =>
        string.Join(";", Clinical.Procedures.Select(p =>
            "#" + p.DisplayNumber + ":" + p.ProcedureType + ":" + p.Id.ToString("N")[..8] + ":" + LabSurfaces.Join(p.Surfaces, InnerCameraLabel) + ":" + ToothRootCanalCatalog.Join(ToothNumber, p.RootCanalIds)));

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
        if (IsImplantSelected)
        {
            Status = "FDI " + ToothNumber + " · Implant\nDetailed 3D viewer is empty for implant teeth.\nProcedures: " +
                     Clinical.Procedures.Count;
            return;
        }
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

public sealed class LabPatient
{
    public LabPatient(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; }
    public string Name { get; }
    internal Dictionary<string, LabToothSession> Sessions { get; } = new();
}

public sealed class LabProcedureTypeChoice
{
    public LabProcedureTypeChoice(DentalProcedureType type, string name)
    {
        Type = type;
        Name = name;
    }

    public DentalProcedureType Type { get; }
    public string Name { get; }
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
    private ImageSource? _preview;
    private bool _showNaturalTooth = true;
    private bool _showImplant;
    private bool _showMissing;
    private bool _showEndodontic;
    private bool _showFilling;
    private string _treatedCanalIds = "";

    public LabFdiSlot(string fdi)
    {
        Fdi = fdi;
        IsUpper = fdi.StartsWith('1') || fdi.StartsWith('2');
    }

    public string Fdi { get; }
    public bool IsUpper { get; }
    public bool IsLower => !IsUpper;

    public ImageSource? Preview
    {
        get => _preview;
        private set => SetProperty(ref _preview, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        private set => SetProperty(ref _isSelected, value);
    }

    public bool ShowNaturalTooth
    {
        get => _showNaturalTooth;
        private set => SetProperty(ref _showNaturalTooth, value);
    }

    public bool ShowImplant
    {
        get => _showImplant;
        private set => SetProperty(ref _showImplant, value);
    }

    public bool ShowMissing
    {
        get => _showMissing;
        private set => SetProperty(ref _showMissing, value);
    }

    public bool ShowEndodontic
    {
        get => _showEndodontic;
        private set => SetProperty(ref _showEndodontic, value);
    }

    public bool ShowFilling
    {
        get => _showFilling;
        private set => SetProperty(ref _showFilling, value);
    }

    public string TreatedCanalIds
    {
        get => _treatedCanalIds;
        private set => SetProperty(ref _treatedCanalIds, value);
    }

    internal void SetSelected(bool value) => IsSelected = value;

    internal void ApplyPresentation(ImageSource? preview, ToothOdontogramState state)
    {
        Preview = preview;
        ShowNaturalTooth = state.ShowNaturalTooth;
        ShowImplant = state.ShowImplant;
        ShowMissing = state.ShowMissing;
        ShowEndodontic = state.ShowEndodontic;
        ShowFilling = state.ShowFilling;
        TreatedCanalIds = string.Join(",", state.TreatedRootCanalIds);
    }
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

public sealed class LabRootCanalChoice : ObservableObject
{
    private readonly ToothLabViewModel _owner;
    private bool _isSelected;

    public LabRootCanalChoice(ToothLabViewModel owner, string id, string label)
    {
        _owner = owner;
        Id = id;
        Label = label;
    }

    public string Id { get; }
    public string Label { get; }

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
}

internal sealed class LabToothSession
{
    public LabToothSession(string fdi) => Clinical = new ToothLabClinicalState(fdi);

    public ToothLabClinicalState Clinical { get; }
    public HashSet<ToothSurfaceType> Pending { get; } = [];
    public HashSet<string> PendingCanals { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Guid? EditingId { get; set; }
}

public sealed class ProcedureListItem
{
    public ProcedureListItem(ToothLabViewModel owner, DentalProcedure procedure)
    {
        Id = procedure.Id;
        DisplayNumber = procedure.DisplayNumber;
        Title = FormatTitle(procedure);
        SurfacesDisplay = DentalProcedureTypes.RequiresSurfaces(procedure.ProcedureType)
            ? LabSurfaces.Join(procedure.Surfaces, owner.InnerCameraLabel)
            : DentalProcedureTypes.RequiresRootCanals(procedure.ProcedureType, procedure.ToothNumber)
                ? ""
                : "Whole tooth";
        EditCommand = new RelayCommand(() => owner.BeginEdit(Id));
        RemoveCommand = new RelayCommand(() => owner.RemoveProcedure(Id));
    }

    public static string FormatTitle(DentalProcedure procedure)
    {
        var typeName = DentalProcedureTypes.DisplayName(procedure.ProcedureType);
        if (!DentalProcedureTypes.RequiresRootCanals(procedure.ProcedureType, procedure.ToothNumber))
            return typeName;
        var canals = ToothRootCanalCatalog.Join(procedure.ToothNumber, procedure.RootCanalIds);
        return string.IsNullOrEmpty(canals) ? typeName : typeName + " — " + canals;
    }

    public Guid Id { get; }
    public int DisplayNumber { get; }
    public string Title { get; }
    public string SurfacesDisplay { get; }
    public bool HasDetail => !string.IsNullOrWhiteSpace(SurfacesDisplay);
    public ICommand EditCommand { get; }
    public ICommand RemoveCommand { get; }
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
