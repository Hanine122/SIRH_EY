using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Services
{
    public class ParametreService : IParametreService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public ParametreService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public T GetValue<T>(string code, T defaultValue = default)
        {
            var cacheKey = $"Param_{code}";
            if (!_cache.TryGetValue(cacheKey, out string? valeur))
            {
                var param = _context.Parametres.FirstOrDefault(p => p.Code == code);
                valeur = param?.Valeur;
                if (valeur != null)
                    _cache.Set(cacheKey, valeur, TimeSpan.FromMinutes(10));
            }
            if (string.IsNullOrEmpty(valeur))
                return defaultValue;
            return (T)Convert.ChangeType(valeur, typeof(T));
        }

        public void SetValue(string code, string valeur)
        {
            var param = _context.Parametres.FirstOrDefault(p => p.Code == code);
            if (param == null)
            {
                param = new Parametre { Code = code, Valeur = valeur };
                _context.Parametres.Add(param);
            }
            else
            {
                param.Valeur = valeur;
                param.DerniereModification = DateTime.Now;
            }
            _context.SaveChanges();
            _cache.Remove($"Param_{code}");
        }
    }
}