using System.Windows.Media;
using MyOrganizer.Wpf.Dental;
using MyOrganizer.Wpf.MVVM.Infrastructure;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

/// <summary>
/// Production copy of the Lab odontogram slot contract for <see cref="Controls.OdontogramView"/>.
/// Tooth Lab keeps its own LabChartRow / LabFdiSlot types.
/// </summary>
public sealed class ChartJawRow
{
    public ChartJawRow(string jaw, IReadOnlyList<string> right, IReadOnlyList<string> left)
    {
        Jaw = jaw;
        Right = right.Select(fdi => new ChartFdiSlot(fdi)).ToList();
        Left = left.Select(fdi => new ChartFdiSlot(fdi)).ToList();
    }

    public string Jaw { get; }
    public IReadOnlyList<ChartFdiSlot> Right { get; }
    public IReadOnlyList<ChartFdiSlot> Left { get; }
}

public sealed class ChartFdiSlot : ObservableObject
{
    private bool _isSelected;
    private ImageSource? _preview;
    private bool _showNaturalTooth = true;
    private bool _showImplant;
    private bool _showMissing;
    private bool _showEndodontic;
    private bool _showFilling;
    private string _treatedCanalIds = "";

    public ChartFdiSlot(string fdi)
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
