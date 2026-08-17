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
            return v;

        if (_cache.TryGetValue(AllLangsCacheKey, out Dictionary<string, Dictionary<string, string>>? all)
            && all is not null
            && all.TryGetValue(key, out var perLang))
        {
            if (perLang.TryGetValue(lang, out var exact))
                return exact;
            if (perLang.TryGetValue("en", out var en))
                return en;
            if (perLang.Values.FirstOrDefault() is string any)
                return any;
        }

        return key;
    }

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
}
