using ProGlassAutomation.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProGlassAutomation.Services.Cache
{
    public static class SheetStoreCache
    {
        // ================= INTERNAL CACHE =================
        private static List<Sheet> _cache = new();
        private static readonly object _lock = new object();
        private static bool _isLoaded = false;

        // ================= EVENT (AUTO REFRESH ENGINE) =================
        public static event Action? CacheChanged;

        // ================= LOAD CACHE =================
        public static void Load(Func<List<Sheet>> loader)
        {
            lock (_lock)
            {
                _cache = loader()?.ToList() ?? new List<Sheet>();
                _isLoaded = true;
            }

            Notify();
        }

        // ================= GET ALL (FAST PATH) =================
        public static List<Sheet> GetAll()
        {
            lock (_lock)
            {
                return _cache.ToList(); // return snapshot (safe UI binding)
            }
        }

        // ================= FORCE REFRESH =================
        public static void Refresh(Func<List<Sheet>> loader)
        {
            Load(loader);
        }

        // ================= UPDATE SINGLE ITEM =================
        public static void Upsert(Sheet item, Func<List<Sheet>> loader)
        {
            lock (_lock)
            {
                var index = _cache.FindIndex(x => x.Id == item.Id);

                if (index >= 0)
                    _cache[index] = item;
                else
                    _cache.Add(item);
            }

            Notify();
        }

        // ================= DELETE =================
        public static void Remove(int id)
        {
            lock (_lock)
            {
                _cache.RemoveAll(x => x.Id == id);
            }

            Notify();
        }

        // ================= NOTIFY ALL VIEWMODELS =================
        private static void Notify()
        {
            CacheChanged?.Invoke();
        }

        // ================= STATUS =================
        public static bool IsLoaded => _isLoaded;
        public static int Count => _cache.Count;
    }
}