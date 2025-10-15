using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LootManager
{
    public static class ItemLookup
    {
        private static readonly Dictionary<string, Sprite> _iconByName =
            new Dictionary<string, Sprite>(System.StringComparer.Ordinal);

        private static readonly List<string> _allItemNames = new List<string>();
        private static bool _built;

        public static void EnsureBuilt()
        {
            if (_built) return;

            var db = GameData.ItemDB?.ItemDB;
            if (db == null) return;

            _allItemNames.Clear();
            foreach (var it in db)
            {
                if (it == null) continue;
                var name = it.ItemName;
                if (string.IsNullOrWhiteSpace(name)) continue;

                if (!_iconByName.ContainsKey(name))
                    _iconByName[name] = it.ItemIcon;

                _allItemNames.Add(name);
            }
            
            var distinct = _allItemNames.Distinct().OrderBy(x => x).ToList();
            _allItemNames.Clear();
            _allItemNames.AddRange(distinct);

            _built = true;
        }

        public static IReadOnlyList<string> AllItems
        {
            get { EnsureBuilt(); return _allItemNames; }
        }

        public static Sprite GetIcon(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            EnsureBuilt();
            _iconByName.TryGetValue(itemName, out var s);
            return s;
        }
    }
}