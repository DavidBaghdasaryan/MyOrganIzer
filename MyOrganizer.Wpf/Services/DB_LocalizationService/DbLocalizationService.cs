using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyOrganizer.Wpf.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyOrganizer.Wpf.Services.DB_LocalizationService
{
    public class DbLocalizationService : IDbLocalizationService
    {
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;

        // защищаем инициализацию от гонок
        private static readonly SemaphoreSlim _initLock = new(1, 1);
        private const string AllLangsCacheKey = "L10N::ALL";

        public DbLocalizationService(AppDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        private static string LangCacheKey(string lang) => $"L10N::{lang}";

        /// <summary>
        /// Греем кеш. По умолчанию грузим ВСЕ языки один раз.
        /// </summary>
        public async Task WarmUpAsync(string lang)
        {
            lang = NormalizeLang(lang);
            // быстрый выход, если уже было
            if (_cache.TryGetValue(AllLangsCacheKey, out _)) return;

            await _initLock.WaitAsync().ConfigureAwait(false);
            try
            {
                // двойная проверка после входа в критическую секцию
                if (_cache.TryGetValue(AllLangsCacheKey, out _)) return;

                // Один запрос: Key, Lang, Value
                var rows = await _db.L10nValues
                    .AsNoTracking()
                    .Join(_db.L10nKeys.AsNoTracking(),
                          v => v.KeyId,
                          k => k.Id,
                          (v, k) => new { k.Key, v.Lang, v.Value })
                    .ToListAsync()
                    .ConfigureAwait(false);

                // собираем словарь key -> (lang -> value)
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

                // кладем общий кеш
                _cache.Set(AllLangsCacheKey, dict, TimeSpan.FromHours(6));

                // опционально кладем и «срез» по текущему языку, чтобы T/TAsync быстрее работали
                var byLang = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (k, v) in dict)
                    if (v.TryGetValue(lang, out var val)) byLang[k] = val;

                _cache.Set(LangCacheKey(lang), byLang, TimeSpan.FromHours(6));
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>
        /// Синхронный доступ. НЕ запускает фоновые Task.Run.
        /// Если кеш не прогрет — вернем ключ как фолбэк.
        /// </summary>
        public string T(string key, string lang)
        {
            lang = NormalizeLang(lang);
            // Lazy warm (sync) — only once
            if (!_cache.TryGetValue(AllLangsCacheKey, out _))
            {
                _initLock.Wait();
                try
                {
                    if (!_cache.TryGetValue(AllLangsCacheKey, out _))
                    {
                        // synchronous warm
                        var rows = _db.L10nValues.AsNoTracking()
                            .Join(_db.L10nKeys.AsNoTracking(),
                                  v => v.KeyId, k => k.Id,
                                  (v, k) => new { k.Key, v.Lang, v.Value })
                            .ToList();

                        var dict = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                        foreach (var r in rows)
                        {
                            if (!dict.TryGetValue(r.Key, out var _perLang))
                                dict[r.Key] = _perLang = new(StringComparer.OrdinalIgnoreCase);
                            _perLang[r.Lang] = r.Value;
                        }
                        _cache.Set(AllLangsCacheKey, dict, TimeSpan.FromHours(6));

                        var byLang = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var (k, _val) in dict)
                            if (_val.TryGetValue(lang, out var val)) byLang[k] = val;
                        _cache.Set(LangCacheKey(lang), byLang, TimeSpan.FromHours(6));
                    }
                }
                finally { _initLock.Release(); }
            }

            // fast path as before
            if (_cache.TryGetValue(LangCacheKey(lang), out Dictionary<string, string>? langMap)
                && langMap != null
                && langMap.TryGetValue(key, out var v))
                return v;

            if (_cache.TryGetValue(AllLangsCacheKey, out Dictionary<string, Dictionary<string, string>>? all)
                && all != null
                && all.TryGetValue(key, out var perLang))
            {
                if (perLang.TryGetValue(lang, out var exact)) return exact;
                if (perLang.TryGetValue("en", out var en)) return en;
                if (perLang.Values.FirstOrDefault() is string any) return any;
            }
            return key;
        }

        private static string NormalizeLang(string lang)
        {
            if (string.IsNullOrWhiteSpace(lang)) return "en";
            var i = lang.IndexOf('-');
            return (i > 0 ? lang[..i] : lang).ToLowerInvariant();
        }

        /// <summary>
        /// Асинхронный доступ. При необходимости прогревает кеш.
        /// </summary>
        public async Task<string> TAsync(string key, string lang)
        {
            lang = NormalizeLang(lang);
            if (!_cache.TryGetValue(AllLangsCacheKey, out _))
            {
                await WarmUpAsync(lang).ConfigureAwait(false);
            }
            return T(key, lang);
        }
    }
}
