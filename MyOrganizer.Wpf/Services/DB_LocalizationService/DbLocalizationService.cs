using System.Collections.Concurrent;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using MyOrganizer.Wpf.Data;

namespace MyOrganizer.Wpf.Services.DB_LocalizationService;

public class DbLocalizationService : IDbLocalizationService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IMemoryCache _cache;
    private static readonly SemaphoreSlim InitLock = new(1, 1);
    private const string AllLangsCacheKey = "L10N::ALL";
    private readonly ConcurrentDictionary<string, byte> _missingKeys = new(StringComparer.Ordinal);

    public DbLocalizationService(IServiceScopeFactory scopes, IMemoryCache cache)
    {
        _scopes = scopes;
        _cache = cache;
    }

    private static string LangCacheKey(string lang) => $"L10N::{lang}";

    public async Task WarmUpAsync(string lang)
    {
        lang = NormalizeLang(lang);
        if (_cache.TryGetValue(AllLangsCacheKey, out _))
            return;

        await InitLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_cache.TryGetValue(AllLangsCacheKey, out _))
                return;

            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rows = await db.L10nValues
                .AsNoTracking()
                .Join(db.L10nKeys.AsNoTracking(),
                    v => v.KeyId,
                    k => k.Id,
                    (v, k) => new L10nRow(k.Key, v.Lang, v.Value))
                .ToListAsync()
                .ConfigureAwait(false);

            Store(rows, lang);
            ProbeSeededKeys(lang, rows.Select(r => r.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            ProbeUnicodeCorruption();
        }
        finally
        {
            InitLock.Release();
        }
    }

    public string T(string key, string lang)
    {
        lang = NormalizeLang(lang);

        if (_cache.TryGetValue(LangCacheKey(lang), out Dictionary<string, string>? langMap)
            && langMap is not null
            && langMap.TryGetValue(key, out var v))
        {
            if (string.IsNullOrWhiteSpace(v))
                RecordEmpty(key, lang);
            return v;
        }

        if (_cache.TryGetValue(AllLangsCacheKey, out Dictionary<string, Dictionary<string, string>>? all)
            && all is not null
            && all.TryGetValue(key, out var perLang))
        {
            if (perLang.TryGetValue(lang, out var exact))
            {
                if (string.IsNullOrWhiteSpace(exact))
                    RecordEmpty(key, lang);
                return exact;
            }
            if (perLang.TryGetValue("en", out var en))
                return en;
            if (perLang.Values.FirstOrDefault() is string any)
                return any;
        }

        RecordMiss(key, lang);
        return key;
    }

    public IReadOnlyCollection<string> GetMissingKeys() => _missingKeys.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();

    public async Task<string> TAsync(string key, string lang)
    {
        lang = NormalizeLang(lang);
        if (!_cache.TryGetValue(AllLangsCacheKey, out _))
            await WarmUpAsync(lang).ConfigureAwait(false);
        return T(key, lang);
    }

    private void Store(List<L10nRow> rows, string lang)
    {
        var dict = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
        {
            if (!dict.TryGetValue(r.Key, out var perLang))
            {
                perLang = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                dict[r.Key] = perLang;
            }
            perLang[r.Lang] = r.Value;
        }

        _cache.Set(AllLangsCacheKey, dict, TimeSpan.FromHours(6));

        var byLang = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in dict)
        {
            if (v.TryGetValue(lang, out var val))
                byLang[k] = val;
        }
        _cache.Set(LangCacheKey(lang), byLang, TimeSpan.FromHours(6));
    }

    private static string NormalizeLang(string lang)
    {
        if (string.IsNullOrWhiteSpace(lang))
            return "en";
        var i = lang.IndexOf('-');
        return (i > 0 ? lang[..i] : lang).ToLowerInvariant();
    }

    private sealed record L10nRow(string Key, string Lang, string Value);

    private void ProbeSeededKeys(string lang, int loadedKeyCount)
    {
        string[] expected =
        [
            "ToothLab", "DayOfRegistration", "Payment", "Remains", "DoubleVisit",
            "Incorrectpassword", "SelectClient", "Selecttheclienttodelete", "Deletelient.",
            "session", "Remove", "ClientsList", "Info", "MidlName", "ConditionMissing",
            "SurfaceCaries", "MediumCaries", "DeepCaries", "OcclusalView", "ResetView",
            "CreateProcedure", "SaveProcedure", "NewProcedure", "FdiNotImported", "Patient"
        ];
        var missing = expected.Where(k => T(k, lang) == k).ToArray();
        // #region agent log
        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                sessionId = "ee2893",
                runId = "post-fix",
                hypothesisId = "A",
                location = "DbLocalizationService.WarmUpAsync",
                message = "loc warmup probe",
                data = new { lang, loadedKeyCount, probed = expected.Length, missingCount = missing.Length, missing },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", payload + Environment.NewLine);
        }
        catch { }
        // #endregion
    }

    private void ProbeUnicodeCorruption()
    {
        var samples = new (string Key, string Lang)[]
        {
            ("Dashboard", "hy"), ("Dashboard", "ru"),
            ("Clients", "hy"), ("ToothLab", "hy"), ("Procedures", "hy")
        };
        var rows = samples.Select(s =>
        {
            var value = T(s.Key, s.Lang);
            return new
            {
                s.Key,
                s.Lang,
                firstCp = value.Length == 0 ? -1 : (int)value[0],
                qmarks = value.Length > 0 && value.All(c => c == '?'),
                len = value.Length
            };
        }).ToArray();
        // #region agent log
        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                sessionId = "ee2893",
                runId = "post-fix",
                hypothesisId = "F",
                location = "DbLocalizationService.WarmUpAsync",
                message = "loc unicode probe",
                data = new { rows },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", payload + Environment.NewLine);
        }
        catch { }
        // #endregion
    }

    private void RecordMiss(string key, string lang)
    {
        if (!_missingKeys.TryAdd(key, 0))
            return;
        // #region agent log
        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                sessionId = "ee2893",
                runId = "post-fix",
                hypothesisId = "C",
                location = "DbLocalizationService.T",
                message = "loc key missing",
                data = new { key, lang },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", payload + Environment.NewLine);
        }
        catch { }
        // #endregion
    }

    private void RecordEmpty(string key, string lang)
    {
        // #region agent log
        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                sessionId = "ee2893",
                runId = "post-fix",
                hypothesisId = "B",
                location = "DbLocalizationService.T",
                message = "loc value empty",
                data = new { key, lang },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", payload + Environment.NewLine);
        }
        catch { }
        // #endregion
    }
}
