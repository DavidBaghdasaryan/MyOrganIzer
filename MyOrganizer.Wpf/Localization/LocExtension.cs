using System;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Extensions.DependencyInjection;
using MyOrganizer.Wpf.Config;
using MyOrganizer.Wpf.Services.DB_LocalizationService;

namespace MyOrganizer.Wpf.Localization
{
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

            // Get initial value
            var initial = Resolve(AppSettings.CurrentLang ?? "en");

            // Hook updates if we have a real target
            if (targetObject != null && dp != null)
            {
                Action? handler = null;

                handler = () =>
                {
                    var value = Resolve(AppSettings.CurrentLang ?? "en");
                    if (targetObject.CheckAccess())
                        targetObject.SetValue(dp, value);
                    else
                        targetObject.Dispatcher.Invoke(() => targetObject.SetValue(dp, value));
                };

                // subscribe (idempotent)
                AppSettings.LanguageChanged -= handler;
                AppSettings.LanguageChanged += handler;

                // auto-unsubscribe to avoid leaks
                if (targetObject is FrameworkElement fe)
                {
                    fe.Unloaded += (_, __) => AppSettings.LanguageChanged -= handler;
                }
                else if (targetObject is FrameworkContentElement fce)
                {
                    fce.Unloaded += (_, __) => AppSettings.LanguageChanged -= handler;
                }
            }

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
    }
}
