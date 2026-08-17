using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace MyOrganizer.Wpf;

internal static class WindowFactory
{
    public static T Create<T>() where T : Window
    {
        var scope = App.HostInstance.Services.CreateScope();
        T window;
        try
        {
            window = scope.ServiceProvider.GetRequiredService<T>();
        }
        catch
        {
            scope.Dispose();
            throw;
        }

        window.Closed += (_, _) => scope.Dispose();
        return window;
    }
}
