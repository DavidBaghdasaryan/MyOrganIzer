namespace MyOrganizer.Wpf.Navigation;

/// <summary>
/// Temporary host for existing top-level windows until those screens
/// are migrated into the shell content area.
/// </summary>
public interface ILegacyWindowService
{
    void Open(AppSection section);
}
