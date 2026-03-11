using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Newtonsoft.Json;

namespace LootManager
{
    [Serializable]
    public class LootFilterCategory
    {
        [JsonProperty] public bool IsEnabled          = true;
        [JsonProperty] public bool AppliedToBlacklist = false;
        [JsonProperty] public bool AppliedToWhitelist = false;
        [JsonProperty] public bool AppliedToBanklist  = false;
        [JsonProperty] public bool AppliedToSelllist  = false;
        [JsonProperty] public bool AppliedToAuctionlist = false;
        [JsonProperty] public List<string> Items = new List<string>();
    }

    public static class LootFilterlist
    {
        private const string FileName = "LootFilterlist.json";
        private const string ConfigSub = "LootManager";

        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        public static void Load()
        {
            string dir = Path.Combine(Paths.ConfigPath, ConfigSub);
            string path = Path.Combine(dir, FileName);

            try
            {
                Directory.CreateDirectory(dir);

                if (!File.Exists(path))
                {
                    var defaults = LootFilterlistDefaults.GetDefaultData()
                                   ?? new Dictionary<string, LootFilterCategory>(StringComparer.OrdinalIgnoreCase);

                    var json = JsonConvert.SerializeObject(defaults, _jsonSettings);
                    File.WriteAllText(path, json);

                    ApplyToPluginState(defaults);
                    Plugin.Log.LogInfo($"[Loot Manager] Created default LootFilterlist.json at {path}");
                    return;
                }

                var text = File.ReadAllText(path);
                var loaded = JsonConvert.DeserializeObject<Dictionary<string, LootFilterCategory>>(text)
                             ?? new Dictionary<string, LootFilterCategory>(StringComparer.OrdinalIgnoreCase);

                ApplyToPluginState(loaded);
                Plugin.Log.LogInfo($"[Loot Manager] Loaded {Plugin.FilterList.Count} filter groups.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("[Loot Manager] Failed to load LootFilterlist.json: " + ex);
                Plugin.FilterList = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                Plugin.EnabledFilterCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public static void SaveFilterlist()
        {
            try
            {
                string dir = Path.Combine(Paths.ConfigPath, ConfigSub);
                string path = Path.Combine(dir, FileName);
                Directory.CreateDirectory(dir);

                var data = new Dictionary<string, LootFilterCategory>(StringComparer.OrdinalIgnoreCase);

                foreach (var kv in Plugin.FilterList.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                {
                    string sectionName = kv.Key;
                    var set = kv.Value ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    IEnumerable<string> itemsSeq = set.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim());

                    data[sectionName] = new LootFilterCategory
                    {
                        IsEnabled            = Plugin.EnabledFilterCategories?.Contains(sectionName) == true,
                        AppliedToBlacklist   = Plugin.FilterAppliedToBlacklist?.Contains(sectionName)  == true,
                        AppliedToWhitelist   = Plugin.FilterAppliedToWhitelist?.Contains(sectionName)  == true,
                        AppliedToBanklist    = Plugin.FilterAppliedToBanklist?.Contains(sectionName)   == true,
                        AppliedToSelllist    = Plugin.FilterAppliedToSelllist?.Contains(sectionName)   == true,
                        AppliedToAuctionlist = Plugin.FilterAppliedToAuctionlist?.Contains(sectionName) == true,
                        Items = itemsSeq.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList()
                    };
                }

                var json = JsonConvert.SerializeObject(data, _jsonSettings);
                File.WriteAllText(path, json);

                Plugin.Log.LogInfo("[Loot Manager] Saved LootFilterlist.json.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("[Loot Manager] Failed to save LootFilterlist.json: " + ex);
            }
        }

        public static void ReadAll(out Dictionary<string, HashSet<string>> sections, out HashSet<string> enabled)
        {
            sections = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in Plugin.FilterList)
            {
                var items = kv.Value != null
                    ? kv.Value
                    : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                sections[kv.Key] = new HashSet<string>(items, StringComparer.OrdinalIgnoreCase);
            }

            enabled = new HashSet<string>(
                Plugin.EnabledFilterCategories ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase
            );
        }

        public static HashSet<string> ReadEnabled()
        {
            return new HashSet<string>(Plugin.EnabledFilterCategories ?? new HashSet<string>(), StringComparer.OrdinalIgnoreCase);
        }

        public static HashSet<string> ReadSectionItems(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (Plugin.FilterList.TryGetValue(sectionName, out var items) && items != null)
                return new HashSet<string>(items, StringComparer.OrdinalIgnoreCase);

            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public static void SetAppliedTo(string sectionName, string listKey, bool value)
        {
            if (string.IsNullOrWhiteSpace(sectionName)) return;
            HashSet<string> set = null;
            switch (listKey)
            {
                case "Blacklist":   set = Plugin.FilterAppliedToBlacklist;   break;
                case "Whitelist":   set = Plugin.FilterAppliedToWhitelist;   break;
                case "Banklist":    set = Plugin.FilterAppliedToBanklist;    break;
                case "Selllist":    set = Plugin.FilterAppliedToSelllist;    break;
                case "Auctionlist": set = Plugin.FilterAppliedToAuctionlist; break;
            }
            if (set == null) return;
            if (value) set.Add(sectionName);
            else set.Remove(sectionName);
            SaveFilterlist();
        }

        public static void SetSectionEnabled(string sectionName, bool isEnabled)
        {
            if (string.IsNullOrWhiteSpace(sectionName)) return;

            if (!Plugin.FilterList.ContainsKey(sectionName))
                Plugin.FilterList[sectionName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (isEnabled) Plugin.EnabledFilterCategories.Add(sectionName);
            else Plugin.EnabledFilterCategories.Remove(sectionName);

            SaveFilterlist();
        }

        public static void SaveSectionItems(string sectionName, IEnumerable<string> items, bool? setEnabled = null)
        {
            if (string.IsNullOrWhiteSpace(sectionName)) return;

            var clean = new HashSet<string>(
                (items ?? Enumerable.Empty<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim()),
                StringComparer.OrdinalIgnoreCase);

            Plugin.FilterList[sectionName] = clean;

            if (setEnabled.HasValue)
            {
                if (setEnabled.Value) Plugin.EnabledFilterCategories.Add(sectionName);
                else Plugin.EnabledFilterCategories.Remove(sectionName);
            }

            SaveFilterlist();
        }

        public static void SaveAll(Dictionary<string, HashSet<string>> sections, HashSet<string> enabled)
        {
            Plugin.FilterList = sections ?? new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            Plugin.EnabledFilterCategories = enabled ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            SaveFilterlist();
        }

        private static void ApplyToPluginState(Dictionary<string, LootFilterCategory> input)
        {
            var sections = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var enabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (input != null)
            {
                foreach (var kv in input)
                {
                    var name = kv.Key?.Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    var cat = kv.Value ?? new LootFilterCategory();

                    var items = new HashSet<string>(
                        (cat.Items ?? new List<string>())
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Select(s => s.Trim()),
                        StringComparer.OrdinalIgnoreCase);

                    sections[name] = items;
                    if (cat.IsEnabled)            enabled.Add(name);
                    if (cat.AppliedToBlacklist)   Plugin.FilterAppliedToBlacklist.Add(name);
                    if (cat.AppliedToWhitelist)   Plugin.FilterAppliedToWhitelist.Add(name);
                    if (cat.AppliedToBanklist)    Plugin.FilterAppliedToBanklist.Add(name);
                    if (cat.AppliedToSelllist)    Plugin.FilterAppliedToSelllist.Add(name);
                    if (cat.AppliedToAuctionlist) Plugin.FilterAppliedToAuctionlist.Add(name);
                }
            }

            Plugin.FilterList                = sections;
            Plugin.EnabledFilterCategories   = enabled;
            // AppliedTo sets are rebuilt from scratch on each load
            Plugin.FilterAppliedToBlacklist   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Plugin.FilterAppliedToWhitelist   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Plugin.FilterAppliedToBanklist    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Plugin.FilterAppliedToSelllist    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Plugin.FilterAppliedToAuctionlist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}