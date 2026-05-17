using System;
using System.Collections.Generic;

namespace Gizmo.Client.UI.Services
{
    /// <summary>
    /// Simple in-memory TTL cache for GGBook API responses.
    /// Keyed by string; values are raw JSON strings.
    /// Default TTL = 60 seconds. Not thread-safe (single-threaded Blazor context).
    /// </summary>
    internal static class GGModCache
    {
        private static readonly Dictionary<string, (DateTime ExpiresAt, string Json)> _store = new();

        public static bool TryGet(string key, out string json)
        {
            if (_store.TryGetValue(key, out var entry) && DateTime.UtcNow < entry.ExpiresAt)
            {
                json = entry.Json;
                return true;
            }
            json = null!;
            return false;
        }

        public static void Set(string key, string json, int ttlSeconds = 60)
            => _store[key] = (DateTime.UtcNow.AddSeconds(ttlSeconds), json);

        public static void Invalidate(string key) => _store.Remove(key);
    }
}
