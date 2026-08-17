using System.Windows;
using System.Windows.Markup;
using Microsoft.Extensions.DependencyInjection;
using MyOrganizer.Wpf;
using MyOrganizer.Wpf.Config;
using MyOrganizer.Wpf.Services.DB_LocalizationService;

namespace MyOrganizer.Wpf.Localization;

[MarkupExtensionReturnType(typeof(string))]
public class LocExtension : MarkupExtension
{
    public string Key { get; set; } = "";

    public LocExtension() { }
    public LocExtension(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrWhiteSpace(Key))
            return string.Empty;

        var pvt = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
        var targetObject = pvt?.TargetObject as DependencyObject;
        var dp = pvt?.TargetProperty as DependencyProperty;

        var initial = Resolve(AppSettings.CurrentLang);

        if (targetObject is null || dp is null)
            return initial;

        if (targetObject.GetValue(LocHook.HandlerProperty) is Action existing)
            AppSettings.LanguageChanged -= existing;

        Action handler = () =>
        {
            var value = Resolve(AppSettings.CurrentLang);
            if (targetObject.CheckAccess())
                targetObject.SetValue(dp, value);
            else
                targetObject.Dispatcher.Invoke(() => targetObject.SetValue(dp, value));
        };

        targetObject.SetValue(LocHook.HandlerProperty, handler);
        AppSettings.LanguageChanged += handler;

        if (targetObject is FrameworkElement fe)
            fe.Unloaded += (_, _) => AppSettings.LanguageChanged -= handler;
        else if (targetObject is FrameworkContentElement fce)
            fce.Unloaded += (_, _) => AppSettings.LanguageChanged -= handler;

        return initial;
    }

    private string Resolve(string lang)
    {
        try
        {
            var loc = App.HostInstance?.Services.GetService<IDbLocalizationService>();
            return loc?.T(Key, lang) ?? Key;
        }
        catch
        {
            return Key;
        }
    }

    private static class LocHook
    {
        public static readonly DependencyProperty HandlerProperty =
            DependencyProperty.RegisterAttached(
                "Handler",
                typeof(Action),
                typeof(LocHook));
    }
}
