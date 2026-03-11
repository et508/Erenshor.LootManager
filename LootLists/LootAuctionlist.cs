using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Newtonsoft.Json;

namespace LootManager
{
    public static class LootAuctionlist
    {
        private const string FileName  = "LootAuctionlist.json";
        private const string ConfigSub = "LootManager";

        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        public static void Load()
        {
            string dir  = Path.Combine(Paths.ConfigPath, ConfigSub);
            string path = Path.Combine(dir, FileName);

            try
            {
                Directory.CreateDirectory(dir);

                if (!File.Exists(path))
                {
                    var defaults = new LootListData { Items = new List<string>() };
                    var json     = JsonConvert.SerializeObject(defaults, _jsonSettings);
                    File.WriteAllText(path, json);

                    Plugin.Auctionlist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    Plugin.Log.LogInfo($"[Loot Manager] Created default {FileName}");
                    return;
                }

                var text = File.ReadAllText(path);
                var data = JsonConvert.DeserializeObject<LootListData>(text) ?? new LootListData();

                var items = (data.Items ?? new List<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim());

                Plugin.Auctionlist = new HashSet<string>(items, StringComparer.OrdinalIgnoreCase);
                Plugin.Log.LogInfo($"[Loot Manager] Loaded {Plugin.Auctionlist.Count} auctionlisted items.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Loot Manager] Failed to load {FileName}: {ex}");
                Plugin.Auctionlist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public static void SaveAuctionlist()
        {
            try
            {
                string dir  = Path.Combine(Paths.ConfigPath, ConfigSub);
                string path = Path.Combine(dir, FileName);
                Directory.CreateDirectory(dir);

                var ordered = (Plugin.Auctionlist ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var data = new LootListData { Items = ordered };
                var json = JsonConvert.SerializeObject(data, _jsonSettings);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Loot Manager] Failed to save {FileName}: {ex}");
            }
        }

        public static HashSet<string> Read()
        {
            return new HashSet<string>(
                Plugin.Auctionlist ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
        }

        public static void SaveAll(IEnumerable<string> items)
        {
            Plugin.Auctionlist = new HashSet<string>(
                (items ?? Enumerable.Empty<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim()),
                StringComparer.OrdinalIgnoreCase);

            SaveAuctionlist();
        }
    }
}