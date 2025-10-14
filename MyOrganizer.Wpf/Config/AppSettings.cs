using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyOrganizer.Wpf.Config
{
    public static class AppSettings
    {
        public static event Action? LanguageChanged;

        private static string _currentLang = "en";
        public static string CurrentLang
        {
            get => _currentLang;
            set
            {
                if (_currentLang != value)
                {
                    _currentLang = value;
                    LanguageChanged?.Invoke();
                }
            }
        }
        private static readonly string FilePath =
     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                  "MyOrganizer", "settings.json");

        

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                var json = File.ReadAllText(FilePath);
                var dto = JsonSerializer.Deserialize<SettingsDto>(json);
                if (dto is { } && !string.IsNullOrWhiteSpace(dto.CurrentLang))
                    CurrentLang = dto.CurrentLang;
            }
            catch { /* ignore */ }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                var json = JsonSerializer.Serialize(new SettingsDto { CurrentLang = CurrentLang },
                                                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch { /* ignore */ }
        }
        public static void SetLanguage(string lang)
        {
            if (string.IsNullOrWhiteSpace(lang)) return;
            CurrentLang = lang;
            LanguageChanged?.Invoke();
        }
        private sealed class SettingsDto
        {
            public string CurrentLang { get; set; } = "hy";
        }
    }
}
