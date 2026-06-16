using System.Collections.Generic;
using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// Runtime lookup of ScriptableObject content by id. This is a data REGISTRY,
    /// not a manager (the §3.4 manager set is fixed at 7). Content assets placed
    /// under any Resources folder are loaded once at startup and indexed by their
    /// <c>id</c> field. Adding content = creating SO assets; no code change.
    /// </summary>
    public static class ContentDatabase
    {
        private static readonly Dictionary<string, AdventurerClassData> _classes = new();
        private static readonly Dictionary<string, DungeonData> _dungeons = new();
        private static readonly Dictionary<string, ItemData> _items = new();
        private static readonly Dictionary<string, AbilityData> _abilities = new();
        private static readonly Dictionary<string, FacilityData> _facilities = new();
        private static readonly Dictionary<string, TalentData> _talents = new();

        public static bool IsLoaded { get; private set; }

        public static void LoadAll()
        {
            _classes.Clear();
            _dungeons.Clear();
            _items.Clear();
            _abilities.Clear();
            _facilities.Clear();
            _talents.Clear();

            Index(Resources.LoadAll<AdventurerClassData>(string.Empty), c => c.id, _classes);
            Index(Resources.LoadAll<DungeonData>(string.Empty), d => d.id, _dungeons);
            Index(Resources.LoadAll<ItemData>(string.Empty), i => i.id, _items);
            Index(Resources.LoadAll<AbilityData>(string.Empty), a => a.id, _abilities);
            Index(Resources.LoadAll<FacilityData>(string.Empty), f => f.id, _facilities);
            Index(Resources.LoadAll<TalentData>(string.Empty), t => t.id, _talents);

            IsLoaded = true;
        }

        private static void Index<T>(T[] assets, System.Func<T, string> idSelector, Dictionary<string, T> map)
            where T : Object
        {
            foreach (var asset in assets)
            {
                string id = idSelector(asset);
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning($"[ContentDatabase] {typeof(T).Name} '{asset.name}' has no id; skipped.");
                    continue;
                }
                if (map.ContainsKey(id))
                {
                    Debug.LogWarning($"[ContentDatabase] Duplicate {typeof(T).Name} id '{id}'; keeping first.");
                    continue;
                }
                map[id] = asset;
            }
        }

        public static AdventurerClassData GetClass(string id) => Get(_classes, id);
        public static DungeonData GetDungeon(string id) => Get(_dungeons, id);
        public static ItemData GetItem(string id) => Get(_items, id);
        public static AbilityData GetAbility(string id) => Get(_abilities, id);
        public static FacilityData GetFacility(string id) => Get(_facilities, id);
        public static TalentData GetTalent(string id) => Get(_talents, id);

        public static IReadOnlyCollection<DungeonData> AllDungeons() => _dungeons.Values;
        public static IReadOnlyCollection<AdventurerClassData> AllClasses() => _classes.Values;

        // For tests / runtime stubs: register an in-memory asset not loaded from Resources.
        public static void RegisterClass(AdventurerClassData c) { if (c != null && !string.IsNullOrEmpty(c.id)) _classes[c.id] = c; }
        public static void RegisterDungeon(DungeonData d) { if (d != null && !string.IsNullOrEmpty(d.id)) _dungeons[d.id] = d; }

        private static T Get<T>(Dictionary<string, T> map, string id) where T : Object
        {
            if (string.IsNullOrEmpty(id)) return null;
            return map.TryGetValue(id, out var value) ? value : null;
        }
    }
}
