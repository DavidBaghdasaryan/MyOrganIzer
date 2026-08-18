using System.Collections.ObjectModel;
using System.Windows.Input;
using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.MVVM.Infrastructure;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public abstract class ProcedureContextViewModel : ObservableObject;

public sealed class ToothSummaryContextViewModel : ProcedureContextViewModel
{
    public ToothSummaryContextViewModel(string? unclassifiedProcedure = null)
    {
        UnclassifiedProcedure = unclassifiedProcedure ?? "";
    }

    public string UnclassifiedProcedure { get; }
    public bool HasUnclassifiedProcedure => UnclassifiedProcedure.Length > 0;
}

public abstract class ProcedureApplyContextViewModel : ProcedureContextViewModel
{
    private PriceTierOption? _selectedTier;

    protected ProcedureApplyContextViewModel(
        string toothNumber,
        string procedureName,
        int[] prices,
        Func<PriceTierOption, Task> apply,
        Action cancel)
    {
        ToothNumber = toothNumber;
        ProcedureName = procedureName;
        Tiers = new ObservableCollection<PriceTierOption>(PriceTierOption.FromPrices(prices));
        _selectedTier = Tiers.Count > 0 ? Tiers[0] : null;
        ApplyCommand = new AsyncRelayCommand(() => apply(SelectedTier!), () => SelectedTier is not null);
        CancelCommand = new RelayCommand(cancel);
    }

    public string ToothNumber { get; }
    public string ProcedureName { get; }
    public ObservableCollection<PriceTierOption> Tiers { get; }

    public PriceTierOption? SelectedTier
    {
        get => _selectedTier;
        set
        {
            if (!SetProperty(ref _selectedTier, value))
                return;
            ((AsyncRelayCommand)ApplyCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand ApplyCommand { get; }
    public ICommand CancelCommand { get; }
}

public sealed class SurfaceProcedureContextViewModel : ProcedureApplyContextViewModel
{
    private string _selectedSurfacesDisplay = "";
    private readonly Action<IReadOnlyList<ToothSurfaceType>, bool> _onSurfacesChanged;

    public SurfaceProcedureContextViewModel(
        string toothNumber,
        string procedureName,
        int[] prices,
        string selectedSurfacesDisplay,
        Action<IReadOnlyList<ToothSurfaceType>, bool> onSurfacesChanged,
        Func<PriceTierOption, Task> apply,
        Action cancel)
        : base(toothNumber, procedureName, prices, apply, cancel)
    {
        _selectedSurfacesDisplay = selectedSurfacesDisplay;
        _onSurfacesChanged = onSurfacesChanged;
    }

    public string SelectedSurfacesDisplay
    {
        get => _selectedSurfacesDisplay;
        private set => SetProperty(ref _selectedSurfacesDisplay, value);
    }

    public void NotifySurfacesDisplay(string display) => SelectedSurfacesDisplay = display;

    public void OnSurfacesChanged(IReadOnlyList<ToothSurfaceType> surfaces, bool wholeTooth) =>
        _onSurfacesChanged(surfaces, wholeTooth);
}

public sealed class EndodonticProcedureContextViewModel : ProcedureApplyContextViewModel
{
    public EndodonticProcedureContextViewModel(
        string toothNumber,
        string procedureName,
        int[] prices,
        Func<PriceTierOption, Task> apply,
        Action cancel)
        : base(toothNumber, procedureName, prices, apply, cancel)
    {
    }
}

public sealed class WholeToothProcedureContextViewModel : ProcedureApplyContextViewModel
{
    public WholeToothProcedureContextViewModel(
        string toothNumber,
        string procedureName,
        int[] prices,
        Func<PriceTierOption, Task> apply,
        Action cancel)
        : base(toothNumber, procedureName, prices, apply, cancel)
    {
    }
}
