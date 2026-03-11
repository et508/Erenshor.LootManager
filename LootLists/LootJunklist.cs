using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Newtonsoft.Json;

namespace LootManager
{
    public static class LootJunklist
    {
        private const string FileName  = "LootJunklist.json";
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

                    Plugin.Junklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    Plugin.Log.LogInfo($"[Loot Manager] Created default {FileName} at {path}");
                    return;
                }

                var text = File.ReadAllText(path);
                var data = JsonConvert.DeserializeObject<LootListData>(text) ?? new LootListData();

                var items = (data.Items ?? new List<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim());

                Plugin.Junklist = new HashSet<string>(items, StringComparer.OrdinalIgnoreCase);
                Plugin.Log.LogInfo($"[Loot Manager] Loaded {Plugin.Junklist.Count} junklisted items.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Loot Manager] Failed to load {FileName}: {ex}");
                Plugin.Junklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public static void SaveJunklist()
        {
            try
            {
                string dir  = Path.Combine(Paths.ConfigPath, ConfigSub);
                string path = Path.Combine(dir, FileName);
                Directory.CreateDirectory(dir);

                var ordered = (Plugin.Junklist ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase))
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
                Plugin.Junklist ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
        }

        public static void SaveAll(IEnumerable<string> items)
        {
            Plugin.Junklist = new HashSet<string>(
                (items ?? Enumerable.Empty<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim()),
                StringComparer.OrdinalIgnoreCase);

            SaveJunklist();
        }
    }
}