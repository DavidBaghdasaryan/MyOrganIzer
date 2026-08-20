using System.Globalization;
using MyOrganizer.Wpf.Extensions;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

internal static class MoneyFormat
{
    public static string Display(decimal value) =>
        string.Format(CultureInfo.CurrentCulture, "{0:N2} {1}", value, "Currency".T());
}
