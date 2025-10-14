using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyOrganizer.Wpf.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOrganizer.Wpf.Services.DB_LocalizationService
{
    public class DbLocalizationService : IDbLocalizationService
    {
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;

        public DbLocalizationService(AppDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        private static string CacheKey(string lang) => $"L10N::{lang}";

        public async Task WarmUpAsync(string lang)
        {
            var map =  _db.L10nKeys
                .Include(k => k.Values)
                ;

            var dict =  map
                .ToDictionary(
                    x => x.Key,
                    x => x.Values
                          .ToDictionary(v => v.Lang, v => v.Value, StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase);

            _cache.Set(CacheKey(lang), dict, TimeSpan.FromHours(6));
        }

        public string T(string key, string lang)
        {
            if (!_cache.TryGetValue(CacheKey(lang),
                    out Dictionary<string, Dictionary<string, string>>? map)
                || map is null)
            {
                // lazy load synchronously
                WarmUpAsync(lang).GetAwaiter().GetResult();
                _cache.TryGetValue(CacheKey(lang), out map);
                if (map is null) return key;
            }

            if (map.TryGetValue(key, out var perLang) && perLang.TryGetValue(lang, out var txt))
                return txt;

            if (perLang != null && perLang.TryGetValue(lang, out var en)) return en;
            if (perLang != null && perLang.Values.FirstOrDefault() is string any) return any;
            return key;
        }


        public async Task<string> TAsync(string key, string lang)
        {
            if (!_cache.TryGetValue(CacheKey(lang), out _))
                await WarmUpAsync(lang);
            return T(key, lang);
        }
    }
}
