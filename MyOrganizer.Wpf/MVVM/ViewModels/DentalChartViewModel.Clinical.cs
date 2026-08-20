using System.Collections.ObjectModel;
using System.Windows.Input;
using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.Dental;
using MyOrganizer.Wpf.MVVM.Infrastructure;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed partial class DentalChartViewModel
{
    private ToothAssetDefinition _asset = ToothAssetRegistry.Get(ToothAssetRegistry.ApprovedFdi);
    private DentalProcedureType _procedureType = DentalProcedureType.Filling;
    private double _toothSize = 280;
    private string _status = "Click a tooth in the odontogram to open it in 3D.";

    public event EventHandler? ClinicalChanged;
    public event EventHandler? PendingSelectionChanged;

    public IReadOnlyList<ChartSurfaceChoice> SurfaceChoices { get; private set; } = [];
    public ObservableCollection<ChartRootCanalChoice> CanalChoices { get; } = [];
    public ObservableCollection<ChartProcedureItem> ProcedureItems { get; } = [];
    public IReadOnlyList<ChartProcedureTypeChoice> ProcedureTypeChoices { get; } =
    [
        new(DentalProcedureType.Filling, DentalProcedureTypes.DisplayName(DentalProcedureType.Filling)),
        new(DentalProcedureType.Implant, DentalProcedureTypes.DisplayName(DentalProcedureType.Implant)),
        new(DentalProcedureType.Endodontic, DentalProcedureTypes.DisplayName(DentalProcedureType.Endodontic)),
        new(DentalProcedureType.Extraction, DentalProcedureTypes.DisplayName(DentalProcedureType.Extraction))
    ];

    public ICommand CreateProcedureCommand { get; private set; } = null!;
    public ICommand SaveProcedureCommand { get; private set; } = null!;
    public ICommand NewProcedureCommand { get; private set; } = null!;

    public ObservableCollection<PriceTierOption> PriceTiers { get; } = [];
    private PriceTierOption? _selectedPriceTier;

    public PriceTierOption? SelectedPriceTier
    {
        get => _selectedPriceTier;
        set => SetProperty(ref _selectedPriceTier, value);
    }

    public bool ShowPriceTiers =>
        ShowClinicalTools && SelectedProcedureType != DentalProcedureType.Extraction;

    public ToothLabClinicalState Clinical =>
        _treatment.Current?.Clinical ?? new ToothLabClinicalState(_asset.FdiNumber);

    public string ToothNumber => _asset.FdiNumber;
    public string InnerCameraLabel => _asset.InnerSurfaceName;
    public bool ShowInspector => HasSelection && _asset.RuntimeImported;
    public bool IsImplantSelected =>
        HasSelection && ToothOdontogramState.From(ToothNumber, Clinical.Procedures).ShowImplant;
    public bool ShowDetailedViewer => ShowInspector && !IsImplantSelected;
    public bool ShowEmptyImplantViewer => ShowInspector && IsImplantSelected;
    public bool ShowPlaceholder => HasSelection && !_asset.RuntimeImported;
    public bool ShowClinicalTools => HasSelection && _asset.ClinicalInteraction;
    public bool ShowAssetStatus => HasSelection && !_asset.SurfaceMapAvailable;
    public bool ShowSurfacePicker => ShowClinicalTools && DentalProcedureTypes.RequiresSurfaces(SelectedProcedureType);
    public bool ShowCanalPicker => ShowClinicalTools && DentalProcedureTypes.RequiresRootCanals(SelectedProcedureType, ToothNumber);
    public bool ShowWholeToothHint => ShowClinicalTools && !ShowSurfacePicker && !ShowCanalPicker;
    public bool IsEditing => _treatment.Current?.IsEditing == true;
    public bool HasProcedures => ProcedureItems.Count > 0;
    public bool HasPendingSurfaces => SurfaceChoices.Any(c => c.IsSelected);
    public bool HasPendingCanals => CanalChoices.Any(c => c.IsSelected);

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
            RebuildPriceTiers();
            ((AsyncRelayCommand)CreateProcedureCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)SaveProcedureCommand).RaiseCanExecuteChanged();
        }
    }

    public double ToothSize
    {
        get => _toothSize;
        set => SetProperty(ref _toothSize, Math.Clamp(value, 220, 360));
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string EditorStatus
    {
        get
        {
            var session = _treatment.Current;
            if (session?.EditingId is Guid id && session.Clinical.Find(id) is { } editing)
                return "Editing " + ChartProcedureItem.FormatTitle(editing);
            return "New procedure · " + DentalProcedureTypes.DisplayName(SelectedProcedureType);
        }
    }

    public string SelectedSurfacesLabel
    {
        get
        {
            var names = PendingDisplayNames();
            return names.Count == 0 ? "None" : string.Join(", ", names);
        }
    }

    public string SelectedCanalsLabel
    {
        get
        {
            var names = CanalChoices.Where(c => c.IsSelected).Select(c => c.Label).ToList();
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

    private void InitClinicalEditor()
    {
        SurfaceChoices =
        [
            new ChartSurfaceChoice(this, ToothSurfaceType.Occlusal, "Occlusal"),
            new ChartSurfaceChoice(this, ToothSurfaceType.Buccal, "Buccal"),
            new ChartSurfaceChoice(this, ToothSurfaceType.Lingual, "Palatal"),
            new ChartSurfaceChoice(this, ToothSurfaceType.Mesial, "Mesial"),
            new ChartSurfaceChoice(this, ToothSurfaceType.Distal, "Distal")
        ];
        CreateProcedureCommand = new AsyncRelayCommand(CreateProcedureAsync, CanCreateProcedure);
        SaveProcedureCommand = new AsyncRelayCommand(SaveProcedureAsync, CanSaveProcedure);
        NewProcedureCommand = new RelayCommand(StartNewProcedure);
    }

    internal void OnChoiceChanged()
    {
        SyncPendingToSession();
        NotifyPending();
    }

    public void SetInteraction(string? hover, IReadOnlyList<string>? selected)
    {
        if (selected is not null)
        {
            var next = ParsePending(selected);
            if (!PendingDomain().SetEquals(next))
            {
                foreach (var choice in SurfaceChoices)
                    choice.SetSilent(next.Contains(choice.Surface));
                SyncPendingToSession();
                NotifyPending();
            }
        }
        Status = string.IsNullOrWhiteSpace(hover)
            ? "Selected: FDI " + ToothNumber
            : "Hover: " + hover + "  ·  click to select";
    }

    internal void BeginEdit(Guid id)
    {
        var session = _treatment.Current;
        if (session is null || !session.BeginEdit(id))
            return;
        var procedure = session.Clinical.Find(id);
        if (procedure is not null)
        {
            SelectedProcedureType = procedure.ProcedureType;
            SelectTier(procedure.Tier);
        }
        RestoreChoicesFromSession(session);
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(EditorStatus));
        NotifyPending();
        PendingSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task RemoveProcedureAsync(Guid id)
    {
        var session = _treatment.Current;
        if (session is null || !session.TryRemove(id))
            return;
        await PersistSessionAsync(session);
        RestoreClinicalEditor();
        NotifyClinical();
    }

    private async Task CreateProcedureAsync()
    {
        var session = _treatment.Current;
        if (session is null)
            return;
        SyncPendingToSession();
        var created = session.TryCreate(SelectedProcedureType);
        if (created is not null && SelectedPriceTier is { } tier)
            created.SetBilling(CatalogName(SelectedProcedureType), tier.Code, tier.Price);
        // #region agent log
        Stage3Log("D", "create-procedure",
            "{\"clientId\":" + ClientId +
            ",\"fdi\":\"" + ToothNumber +
            "\",\"type\":\"" + SelectedProcedureType +
            "\",\"ok\":" + (created is not null ? "true" : "false") +
            ",\"tier\":\"" + (created?.Tier ?? "") +
            "\",\"price\":" + (created?.Price ?? 0) +
            ",\"procedureCount\":" + session.Clinical.Procedures.Count +
            ",\"labPatients\":false}");
        // #endregion
        if (created is null)
            return;
        await PersistSessionAsync(session);
        RestoreClinicalEditor();
        NotifyClinical();
    }

    private async Task SaveProcedureAsync()
    {
        var session = _treatment.Current;
        if (session is null)
            return;
        SyncPendingToSession();
        if (!session.TrySave())
            return;
        await PersistSessionAsync(session);
        RestoreClinicalEditor();
        NotifyClinical();
    }

    private bool CanCreateProcedure()
    {
        var session = _treatment.Current;
        if (session is null)
            return false;
        SyncPendingToSession();
        return session.CanCreate(SelectedProcedureType);
    }

    private bool CanSaveProcedure()
    {
        var session = _treatment.Current;
        if (session is null)
            return false;
        SyncPendingToSession();
        return session.CanSave();
    }

    private void StartNewProcedure()
    {
        _treatment.Current?.StartNew();
        foreach (var choice in SurfaceChoices)
            choice.SetSilent(false);
        foreach (var choice in CanalChoices)
            choice.SetSilent(false);
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(EditorStatus));
        NotifyPending();
    }

    private void ActivateClinicalTooth(string fdi)
    {
        if (!ToothAssetRegistry.TryGet(fdi, out var asset))
            return;
        SyncPendingToSession();
        _asset = asset;
        _treatment.Activate(fdi);
        RestoreClinicalEditor();
        NotifyPresentation();
        ClinicalChanged?.Invoke(this, EventArgs.Empty);
        PendingSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RestoreClinicalEditor()
    {
        var session = _treatment.Current;
        if (session is null)
            return;
        if (session.IsEditing && session.EditingId is Guid id && session.Clinical.Find(id) is { } editing)
            SelectedProcedureType = editing.ProcedureType;
        RestoreChoicesFromSession(session);
        RebuildCanalChoices();
        foreach (var choice in CanalChoices)
            choice.SetSilent(session.PendingCanals.Contains(choice.Id));
        RebuildProcedureItems();
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(EditorStatus));
        OnPropertyChanged(nameof(ClinicalSummary));
        OnPropertyChanged(nameof(FillingSurfaceNames));
        OnPropertyChanged(nameof(TreatedRootCanalIds));
        NotifyPending();
    }

    private void RestoreChoicesFromSession(PatientToothSession session)
    {
        foreach (var choice in SurfaceChoices)
        {
            if (choice.Surface == ToothSurfaceType.Lingual)
                choice.SetLabel(InnerCameraLabel);
            choice.SetSilent(session.Pending.Contains(choice.Surface));
        }
    }

    private void RebuildCanalChoices()
    {
        CanalChoices.Clear();
        foreach (var canal in ToothRootCanalCatalog.ForFdi(ToothNumber))
            CanalChoices.Add(new ChartRootCanalChoice(this, canal.Id, canal.DisplayName));
        OnPropertyChanged(nameof(ShowCanalPicker));
        OnPropertyChanged(nameof(ShowWholeToothHint));
        OnPropertyChanged(nameof(SelectedCanalsLabel));
    }

    private void RebuildProcedureItems()
    {
        ProcedureItems.Clear();
        foreach (var procedure in Clinical.Procedures)
            ProcedureItems.Add(new ChartProcedureItem(this, procedure));
        OnPropertyChanged(nameof(HasProcedures));
    }

    private void SyncPendingToSession()
    {
        var session = _treatment.Current;
        if (session is null)
            return;
        session.Pending.Clear();
        foreach (var surface in PendingDomain())
            session.Pending.Add(surface);
        session.PendingCanals.Clear();
        foreach (var id in CanalChoices.Where(c => c.IsSelected).Select(c => c.Id))
            session.PendingCanals.Add(id);
    }

    private void NotifyPending()
    {
        OnPropertyChanged(nameof(HasPendingSurfaces));
        OnPropertyChanged(nameof(HasPendingCanals));
        OnPropertyChanged(nameof(SelectedSurfacesLabel));
        OnPropertyChanged(nameof(SelectedCanalsLabel));
        OnPropertyChanged(nameof(PendingSurfaceNames));
        ((AsyncRelayCommand)CreateProcedureCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)SaveProcedureCommand).RaiseCanExecuteChanged();
        PendingSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void NotifyPresentation()
    {
        OnPropertyChanged(nameof(ToothNumber));
        OnPropertyChanged(nameof(InnerCameraLabel));
        OnPropertyChanged(nameof(ShowInspector));
        OnPropertyChanged(nameof(IsImplantSelected));
        OnPropertyChanged(nameof(ShowDetailedViewer));
        OnPropertyChanged(nameof(ShowEmptyImplantViewer));
        OnPropertyChanged(nameof(ShowPlaceholder));
        OnPropertyChanged(nameof(ShowClinicalTools));
        OnPropertyChanged(nameof(ShowAssetStatus));
        OnPropertyChanged(nameof(ShowSurfacePicker));
        OnPropertyChanged(nameof(ShowCanalPicker));
        OnPropertyChanged(nameof(ShowWholeToothHint));
        OnPropertyChanged(nameof(SelectedVisualType));
        RebuildPriceTiers();
        Status = HasSelection ? "Selected: FDI " + ToothNumber : "Click a tooth in the odontogram to open it in 3D.";
    }

    private void NotifyClinical()
    {
        OnPropertyChanged(nameof(ClinicalSummary));
        OnPropertyChanged(nameof(FillingSurfaceNames));
        OnPropertyChanged(nameof(TreatedRootCanalIds));
        OnPropertyChanged(nameof(IsImplantSelected));
        OnPropertyChanged(nameof(ShowDetailedViewer));
        OnPropertyChanged(nameof(ShowEmptyImplantViewer));
        RefreshOdontogram();
        ClinicalChanged?.Invoke(this, EventArgs.Empty);
    }

    private HashSet<ToothSurfaceType> PendingDomain() =>
        SurfaceChoices.Where(c => c.IsSelected).Select(c => c.Surface).ToHashSet();

    private IReadOnlyList<string> PendingDisplayNames() =>
        LabSurfaces.DisplayNames(PendingDomain(), InnerCameraLabel);

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

    private async Task PersistSessionAsync(PatientToothSession session)
    {
        if (ClientId <= 0)
            return;
        var fdi = session.Clinical.ToothNumber;
        await _repo.ClearToothAsync(ClientId, fdi);
        foreach (var procedure in session.Clinical.Procedures)
        {
            var name = string.IsNullOrWhiteSpace(procedure.CatalogName)
                ? CatalogName(procedure.ProcedureType)
                : procedure.CatalogName;
            var tier = string.IsNullOrWhiteSpace(procedure.Tier)
                ? SelectedPriceTier?.Code ?? "A"
                : procedure.Tier;
            var price = string.IsNullOrWhiteSpace(procedure.Tier)
                ? SelectedPriceTier?.Price ?? 0
                : procedure.Price;
            if (procedure.ProcedureType == DentalProcedureType.Filling)
            {
                foreach (var surface in procedure.Surfaces)
                    await _repo.AddAsync(ClientId, fdi, name, tier, price, surface.ToString());
            }
            else
            {
                await _repo.AddAsync(ClientId, fdi, name, tier, price);
            }
        }
        // #region agent log
        Stage3Log("D", "persist-session",
            "{\"clientId\":" + ClientId +
            ",\"fdi\":\"" + fdi +
            "\",\"procedureCount\":" + session.Clinical.Procedures.Count +
            ",\"types\":\"" + string.Join(",", session.Clinical.Procedures.Select(p => p.ProcedureType)) +
            "\",\"tiers\":\"" + string.Join(",", session.Clinical.Procedures.Select(p => p.Tier + ":" + p.Price)) +
            "\",\"labPatients\":false}");
        // #endregion
    }

    private string CatalogName(DentalProcedureType type)
    {
        if (type == DentalProcedureType.Extraction)
            return ToothWorkOdontogramProjection.ExtractionProcedureName;
        var id = type switch
        {
            DentalProcedureType.Filling => ProcedureVisualMap.FillingId,
            DentalProcedureType.Implant => ProcedureVisualMap.ImplantZirconiaId,
            DentalProcedureType.Endodontic => ProcedureVisualMap.EndodonticId,
            _ => 0
        };
        foreach (var pair in _procedureIdByName)
        {
            if (pair.Value == id)
                return pair.Key;
        }
        return type switch
        {
            DentalProcedureType.Filling => "Filling (Composite / Amalgam)",
            DentalProcedureType.Implant => "Implant with Zirconia Crown",
            DentalProcedureType.Endodontic => "Endodontic Treatment (Root Canal)",
            _ => ToothWorkOdontogramProjection.ExtractionProcedureName
        };
    }

    private void RebuildPriceTiers()
    {
        var keep = SelectedPriceTier?.Code;
        PriceTiers.Clear();
        if (SelectedProcedureType != DentalProcedureType.Extraction)
        {
            foreach (var tier in PriceTierOption.FromPrices(PricesFor(CatalogName(SelectedProcedureType))))
                PriceTiers.Add(tier);
        }
        SelectedPriceTier = PriceTiers.FirstOrDefault(t => t.Code == keep) ?? PriceTiers.FirstOrDefault();
        OnPropertyChanged(nameof(ShowPriceTiers));
    }

    private void SelectTier(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || PriceTiers.Count == 0)
            return;
        var match = PriceTiers.FirstOrDefault(t =>
            string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
            SelectedPriceTier = match;
    }
}
