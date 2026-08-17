using System.IO;
using System.Text.Json;

namespace MyOrganizer.Wpf.Config;

public static class AppSettings
{
    public static event Action? LanguageChanged;

    private static string _currentLang = "en";
    public static string CurrentLang
    {
        get => _currentLang;
        set
        {
            if (_currentLang == value)
                return;
            _currentLang = value;
            LanguageChanged?.Invoke();
        }
    }

    public static string? PasswordHash { get; private set; }
    public static bool HasPassword => !string.IsNullOrWhiteSpace(PasswordHash);

    private static readonly string FilePath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MyOrganizer",
            "settings.json");

    public static void Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return;
            var dto = JsonSerializer.Deserialize<SettingsDto>(File.ReadAllText(FilePath));
            if (dto is null)
                return;
            if (!string.IsNullOrWhiteSpace(dto.CurrentLang))
                CurrentLang = dto.CurrentLang;
            PasswordHash = dto.PasswordHash;
        }
        catch
        {
            // Keep defaults if the file is missing or corrupt.
        }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var json = JsonSerializer.Serialize(
                new SettingsDto { CurrentLang = CurrentLang, PasswordHash = PasswordHash },
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // Non-fatal: language/password still apply for this session.
        }
    }

    public static void SetLanguage(string lang)
    {
        if (string.IsNullOrWhiteSpace(lang))
            return;
        CurrentLang = lang;
        Save();
    }

    public static void SetPassword(string password)
    {
        PasswordHash = PasswordHasher.Hash(password);
        Save();
    }

    public static bool VerifyPassword(string password) =>
        PasswordHasher.Verify(password, PasswordHash);

    private sealed class SettingsDto
    {
        public string CurrentLang { get; set; } = "en";
        public string? PasswordHash { get; set; }
    }
}
