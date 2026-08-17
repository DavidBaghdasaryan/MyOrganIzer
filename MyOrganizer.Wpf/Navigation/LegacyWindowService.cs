using System.Windows;
using MyOrganizer.Wpf.MVVM.UI;

namespace MyOrganizer.Wpf.Navigation;

public sealed class LegacyWindowService : ILegacyWindowService
{
    public void Open(AppSection section)
    {
        switch (section)
        {
            case AppSection.Settings:
                Show<SetPricesDialog>();
                break;
        }
    }

    private static void Show<T>() where T : Window
    {
        var window = WindowFactory.Create<T>();
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
    }
}
