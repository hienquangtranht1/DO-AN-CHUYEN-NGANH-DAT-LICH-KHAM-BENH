using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BookinhMVC.Helpers
{
    public static class OnlineUserMap
    {
        private static readonly ConcurrentDictionary<int, byte> _map = new();

        public static void Add(int id) => _map.TryAdd(id, 0);

        public static void Remove(int id) => _map.TryRemove(id, out _);

        // Snapshot các id hiện online
        public static IEnumerable<int> Snapshot() => _map.Keys.ToList();
    }
}