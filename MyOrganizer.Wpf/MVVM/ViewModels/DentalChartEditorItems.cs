using System.Windows.Input;
using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.Dental;
using MyOrganizer.Wpf.MVVM.Infrastructure;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

/// <summary>
/// Production copies of Lab editor row types. Tooth Lab keeps LabSurfaceChoice /
/// LabRootCanalChoice / ProcedureListItem.
/// </summary>
public sealed class ChartSurfaceChoice : ObservableObject
{
    private readonly DentalChartViewModel _owner;
    private bool _isSelected;

    public ChartSurfaceChoice(DentalChartViewModel owner, ToothSurfaceType surface, string label)
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

public sealed class ChartRootCanalChoice : ObservableObject
{
    private readonly DentalChartViewModel _owner;
    private bool _isSelected;

    public ChartRootCanalChoice(DentalChartViewModel owner, string id, string label)
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

public sealed class ChartProcedureItem
{
    public ChartProcedureItem(DentalChartViewModel owner, DentalProcedure procedure)
    {
        Id = procedure.Id;
        Title = FormatTitle(procedure);
        SurfacesDisplay = DentalProcedureTypes.RequiresSurfaces(procedure.ProcedureType)
            ? LabSurfaces.Join(procedure.Surfaces, owner.InnerCameraLabel)
            : DentalProcedureTypes.RequiresRootCanals(procedure.ProcedureType, procedure.ToothNumber)
                ? ""
                : "Whole tooth";
        EditCommand = new RelayCommand(() => owner.BeginEdit(Id));
        RemoveCommand = new AsyncRelayCommand(() => owner.RemoveProcedureAsync(Id));
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
    public string Title { get; }
    public string SurfacesDisplay { get; }
    public bool HasDetail => !string.IsNullOrWhiteSpace(SurfacesDisplay);
    public ICommand EditCommand { get; }
    public ICommand RemoveCommand { get; }
}

public sealed class ChartProcedureTypeChoice
{
    public ChartProcedureTypeChoice(DentalProcedureType type, string name)
    {
        Type = type;
        Name = name;
    }

    public DentalProcedureType Type { get; }
    public string Name { get; }
}
