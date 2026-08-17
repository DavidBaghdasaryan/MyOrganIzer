using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.Navigation;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class NavItemViewModel : ObservableObject
{
    private bool _isSelected;

    public NavItemViewModel(AppSection section, string locKey, string glyph)
    {
        Section = section;
        LocKey = locKey;
        Glyph = glyph;
    }

    public AppSection Section { get; }
    public string LocKey { get; }
    public string Glyph { get; }
    public string Label => LocKey.T();

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public void Refresh() => OnPropertyChanged(nameof(Label));
}
