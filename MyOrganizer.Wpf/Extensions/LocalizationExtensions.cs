using Microsoft.Extensions.DependencyInjection;
using MyOrganizer.Wpf.Config;
using MyOrganizer.Wpf.Services.DB_LocalizationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOrganizer.Wpf.Extensions
{
    public static class LocalizationExtensions
    {
        /// <summary>
        /// Translates the current string key using the globally configured language.
        /// </summary>
        public static string T(this string key)
        {
            var loc = App.HostInstance.Services.GetRequiredService<IDbLocalizationService>();
            return loc.T(key, AppSettings.CurrentLang);
        }

        /// <summary>
        /// Translates the current string key into a specific language (e.g. "en", "ru", "hy").
        /// </summary>
        public static string T(this string key, string lang)
        {
            var loc = App.HostInstance.Services.GetRequiredService<IDbLocalizationService>();
            return loc.T(key, lang);
        }
    }
}
