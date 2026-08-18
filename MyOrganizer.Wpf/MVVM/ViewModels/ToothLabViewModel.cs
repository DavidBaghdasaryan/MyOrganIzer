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
    private string _status = "Hover: None\nSelected: None\nFilling: —";
    private string? _hoverName;
    private string? _selectedName;
    private IReadOnlyList<ToothSurfaceType> _selected = [];
    private IReadOnlyDictionary<ToothSurfaceType, ToothSurfaceVisual> _surfaceStates;

    public ToothLabViewModel()
    {
        Clinical = new ToothLabClinicalState("16");
        Surfaces =
        [
            new LabSurfaceRow(this, ToothSurfaceType.Occlusal, "Occlusal"),
            new LabSurfaceRow(this, ToothSurfaceType.Mesial, "Mesial"),
            new LabSurfaceRow(this, ToothSurfaceType.Distal, "Distal"),
            new LabSurfaceRow(this, ToothSurfaceType.Buccal, "Buccal"),
            new LabSurfaceRow(this, ToothSurfaceType.Lingual, "Palatal")
        ];
        _surfaceStates = BuildStates();
        ClearSelectionCommand = new RelayCommand(ClearSelection);
        ResetHealthyCommand = new RelayCommand(ResetHealthy);
        DemoMixedCommand = new RelayCommand(DemoMixed);
        AssignNoneCommand = new RelayCommand(AssignNone, () => HasSelectedSurface);
        AssignFillingCommand = new RelayCommand(AssignFilling, () => HasSelectedSurface);
    }

    public ToothLabClinicalState Clinical { get; }
    public event EventHandler? ClinicalChanged;

    public string ToothNumber => "16";
    public string Hint =>
        "Select a crown surface, then assign None or Filling. Filling persists after hover and selection change. Drag to orbit.";

    public string SourceNote =>
        "Mesh: Maxillary First Molar, University of Dundee School of Dentistry (Emily McDougall; " +
        "Dr. Andrew Mason; Mark Roughley). CC BY 4.0. Original Sketchfab file UL6sketch_1.OBJ is a left " +
        "maxillary first molar (FDI 26), 28,506 vertices / 28,504 quad faces from CT via ZBrush. " +
        "Tooth Lab triangulates, mirrors/orients to FDI 16, and renders it in native WPF Viewport3D. " +
        "Source kept at Assets/Teeth/Source. Optional debug overlay classifies existing crown triangles " +
        "into Occlusal / Buccal / Palatal / Mesial / Distal; OFF restores the approved healthy tooth. " +
        "https://sketchfab.com/3d-models/maxillary-first-molar-e719a474ef7e4bd7abec508f85f1e984";

    public IReadOnlyList<ProcedureOption> ProcedureOptions { get; } =
        ToothSurfaceAppearance.Keys
            .Select(key => new ProcedureOption(key, ToothSurfaceAppearance.DisplayName(key)))
            .ToList();
    public ObservableCollection<LabSurfaceRow> Surfaces { get; }
    public ICommand ClearSelectionCommand { get; }
    public ICommand ResetHealthyCommand { get; }
    public ICommand DemoMixedCommand { get; }
    public ICommand AssignNoneCommand { get; }
    public ICommand AssignFillingCommand { get; }

    public string LabelTop => "Buccal";
    public string LabelBottom => "Palatal";
    public string LabelLeft => "Distal";
    public string LabelRight => "Mesial";

    public double ToothSize
    {
        get => _toothSize;
        set => SetProperty(ref _toothSize, Math.Clamp(value, 250, 360));
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public bool HasSelectedSurface => TryParseSurface(_selectedName, out _);

    public string SelectedSurfaceLabel => _selectedName ?? "None";

    public string ClinicalSummary
    {
        get
        {
            var names = Clinical.FillingSurfaceNames();
            return names.Count == 0 ? "Filling: —" : "Filling: " + string.Join(", ", names);
        }
    }

    public IReadOnlyList<string> FillingSurfaceNames => Clinical.FillingSurfaceNames();

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

    public void SetInteraction(string? hover, string? selected)
    {
        _hoverName = hover;
        var selectedChanged = !string.Equals(_selectedName, selected, StringComparison.Ordinal);
        _selectedName = selected;
        RefreshStatus();
        if (!selectedChanged)
            return;
        OnPropertyChanged(nameof(HasSelectedSurface));
        OnPropertyChanged(nameof(SelectedSurfaceLabel));
        ((RelayCommand)AssignNoneCommand).RaiseCanExecuteChanged();
        ((RelayCommand)AssignFillingCommand).RaiseCanExecuteChanged();
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

    private void AssignNone() => Assign(DentalProcedureType.None);

    private void AssignFilling() => Assign(DentalProcedureType.Filling);

    private void Assign(DentalProcedureType procedure)
    {
        if (!TryParseSurface(_selectedName, out var surface))
            return;
        var changed = Clinical.Set(surface, procedure);
        // #region agent log
        AgentLog("A", "assign-procedure",
            "{\"selected\":\"" + (_selectedName ?? "") +
            "\",\"domain\":\"" + surface +
            "\",\"procedure\":\"" + procedure +
            "\",\"changed\":" + (changed ? "true" : "false") +
            ",\"fillings\":\"" + string.Join(",", Clinical.FillingSurfaceNames()) + "\"}");
        // #endregion
        if (!changed)
            return;
        NotifyClinical();
    }

    // #region agent log
    private static void AgentLog(string hypothesisId, string message, string dataJson)
    {
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"filling-v1\",\"hypothesisId\":\"" + hypothesisId +
                   "\",\"location\":\"ToothLabViewModel.cs\",\"message\":\"" + message +
                   "\",\"data\":" + dataJson + ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { /* lab logging must not break the workflow */ }
    }
    // #endregion

    private void NotifyClinical()
    {
        OnPropertyChanged(nameof(ClinicalSummary));
        OnPropertyChanged(nameof(FillingSurfaceNames));
        RefreshStatus();
        ClinicalChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshStatus()
    {
        var names = Clinical.FillingSurfaceNames();
        Status =
            "Hover: " + (_hoverName ?? "None") + "\n" +
            "Selected: " + (_selectedName ?? "None") + "\n" +
            (names.Count == 0 ? "Filling: —" : "Filling: " + string.Join(", ", names));
    }

    private static bool TryParseSurface(string? name, out ToothSurfaceType surface)
    {
        surface = ToothSurfaceType.Occlusal;
        if (string.IsNullOrWhiteSpace(name))
            return false;
        if (name.Equals("Palatal", StringComparison.OrdinalIgnoreCase))
        {
            surface = ToothSurfaceType.Lingual;
            return true;
        }
        return Enum.TryParse(name, true, out surface);
    }

    private Dictionary<ToothSurfaceType, ToothSurfaceVisual> BuildStates() =>
        Surfaces.ToDictionary(
            row => row.Surface,
            row => new ToothSurfaceVisual { ProcedureKey = row.ProcedureKey });

    private void ClearSelection()
    {
        SelectedSurfaces = [];
        Status = "Selection cleared.";
    }

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
    public string Label { get; }

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
}

public sealed record ProcedureOption(string Key, string Name);
