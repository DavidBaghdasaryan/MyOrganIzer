using System.IO;
using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.Entities;

namespace MyOrganizer.Wpf.Dental;

/// <summary>
/// Production copy of Lab's in-memory per-tooth session (Clinical + pending +
/// create/edit/remove). Tooth Lab keeps LabToothSession. Persistence still
/// round-trips through ToothWork; this chart is rebuilt from those rows.
/// </summary>
internal sealed class PatientToothSession
{
    public PatientToothSession(string fdi) => Clinical = new ToothLabClinicalState(fdi);

    public ToothLabClinicalState Clinical { get; }
    public HashSet<ToothSurfaceType> Pending { get; } = [];
    public HashSet<string> PendingCanals { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Guid? EditingId { get; set; }
    public bool IsEditing => EditingId.HasValue;
    public bool HasPendingSurfaces => Pending.Count > 0;
    public bool HasPendingCanals => PendingCanals.Count > 0;

    public bool CanCreate(DentalProcedureType type)
    {
        if (IsEditing)
            return false;
        if (DentalProcedureTypes.RequiresSurfaces(type) && !HasPendingSurfaces)
            return false;
        if (DentalProcedureTypes.RequiresRootCanals(type, Clinical.ToothNumber) && !HasPendingCanals)
            return false;
        return true;
    }

    public bool CanSave()
    {
        if (EditingId is not Guid id)
            return false;
        var editing = Clinical.Find(id);
        if (editing is null)
            return false;
        if (DentalProcedureTypes.RequiresSurfaces(editing.ProcedureType) && !HasPendingSurfaces)
            return false;
        if (DentalProcedureTypes.RequiresRootCanals(editing.ProcedureType, Clinical.ToothNumber) && !HasPendingCanals)
            return false;
        return true;
    }

    public DentalProcedure? TryCreate(DentalProcedureType type)
    {
        if (!CanCreate(type))
            return null;
        var created = Clinical.TryCreate(type, Pending, PendingCanals);
        if (created is not null)
            StartNew();
        return created;
    }

    public bool TrySave()
    {
        if (EditingId is not Guid id || !CanSave())
            return false;
        var saved = Clinical.Find(id);
        if (saved is null)
            return false;
        var changed = false;
        if (DentalProcedureTypes.RequiresSurfaces(saved.ProcedureType))
            changed = Clinical.TryUpdateSurfaces(id, Pending);
        else if (DentalProcedureTypes.RequiresRootCanals(saved.ProcedureType, Clinical.ToothNumber))
            changed = Clinical.TryUpdateRootCanals(id, PendingCanals);
        StartNew();
        return changed || !DentalProcedureTypes.RequiresSurfaces(saved.ProcedureType);
    }

    public bool BeginEdit(Guid id)
    {
        var procedure = Clinical.Find(id);
        if (procedure is null)
            return false;
        EditingId = id;
        Pending.Clear();
        foreach (var surface in procedure.Surfaces)
            Pending.Add(surface);
        PendingCanals.Clear();
        foreach (var canal in procedure.RootCanalIds)
            PendingCanals.Add(canal);
        return true;
    }

    public bool TryRemove(Guid id)
    {
        if (!Clinical.TryRemove(id))
            return false;
        if (EditingId == id)
            StartNew();
        return true;
    }

    public void StartNew()
    {
        EditingId = null;
        Pending.Clear();
        PendingCanals.Clear();
    }

    public void ClearPending()
    {
        Pending.Clear();
        PendingCanals.Clear();
    }
}

/// <summary>
/// In-memory treatment sessions for one production client. Not a Lab patient.
/// </summary>
internal sealed class PatientTreatmentChart
{
    private readonly Dictionary<string, PatientToothSession> _sessions = new(StringComparer.Ordinal);
    private PatientToothSession? _current;

    public int ClientId { get; private set; }
    public PatientToothSession? Current => _current;

    public void ReloadFromWorks(
        int clientId,
        IEnumerable<ToothWork> works,
        IReadOnlyDictionary<string, int>? nameToId)
    {
        var selected = _current?.Clinical.ToothNumber;
        ClientId = clientId;
        _sessions.Clear();
        _current = null;
        foreach (var group in works.GroupBy(w => w.ToothFdi, StringComparer.Ordinal))
            ToothWorkOdontogramProjection.FillClinical(SessionFor(group.Key).Clinical, group, nameToId);
        if (!string.IsNullOrWhiteSpace(selected))
            _current = SessionFor(selected);
        // #region agent log
        Stage3Log("A", "reload-sessions", IsolationJson());
        // #endregion
    }

    public PatientToothSession SessionFor(string fdi)
    {
        fdi = ToothAssetRegistry.Normalize(fdi);
        if (!_sessions.TryGetValue(fdi, out var session))
        {
            session = new PatientToothSession(fdi);
            _sessions[fdi] = session;
        }
        return session;
    }

    public PatientToothSession Activate(string fdi)
    {
        _current = SessionFor(fdi);
        // #region agent log
        Stage3Log("B", "activate-session",
            "{\"clientId\":" + ClientId +
            ",\"fdi\":\"" + _current.Clinical.ToothNumber +
            "\",\"procedureCount\":" + _current.Clinical.Procedures.Count +
            ",\"pending\":" + _current.Pending.Count +
            ",\"editing\":" + (_current.IsEditing ? "true" : "false") +
            ",\"labPatients\":false}");
        // #endregion
        return _current;
    }

    public ToothOdontogramState OdontogramFor(string fdi) =>
        ToothOdontogramState.From(fdi, SessionFor(fdi).Clinical.Procedures);

    public string IsolationJson()
    {
        string Slot(string fdi)
        {
            _sessions.TryGetValue(fdi, out var session);
            var n = session?.Clinical.Procedures.Count ?? 0;
            var state = session is null
                ? ToothOdontogramState.Healthy(fdi)
                : ToothOdontogramState.From(fdi, session.Clinical.Procedures);
            return "\"" + fdi + "\":{\"n\":" + n +
                   ",\"filling\":" + (state.ShowFilling ? "true" : "false") +
                   ",\"implant\":" + (state.ShowImplant ? "true" : "false") +
                   ",\"missing\":" + (state.ShowMissing ? "true" : "false") +
                   ",\"endo\":" + (state.ShowEndodontic ? "true" : "false") + "}";
        }

        return "{\"clientId\":" + ClientId +
               ",\"sessionCount\":" + _sessions.Count +
               ",\"labPatients\":false" +
               ",\"fdi\":\"" + (_current?.Clinical.ToothNumber ?? "") + "\"," +
               Slot("16") + "," + Slot("24") + "," + Slot("34") + "," + Slot("46") + "}";
    }

    // #region agent log
    private static void Stage3Log(string hypothesisId, string message, string dataJson)
    {
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"stage3\",\"hypothesisId\":\"" + hypothesisId +
                   "\",\"location\":\"PatientTreatmentChart.cs\",\"message\":\"" + message +
                   "\",\"data\":" + dataJson + ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { /* session logging must not break the chart */ }
    }
    // #endregion
}
