using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOrganizer.Wpf.Services.DB_LocalizationService
{

    public interface IDbLocalizationService
    {
        /// <summary>
        /// Returns a translated text for the given key and language.
        /// </summary>
        string T(string key, string lang);

        /// <summary>
        /// Async version for cases where cache might not be initialized.
        /// </summary>
        Task<string> TAsync(string key, string lang);

        /// <summary>
        /// Preloads all translations for a given language into cache.
        /// </summary>
        Task WarmUpAsync(string lang);

    }
}
