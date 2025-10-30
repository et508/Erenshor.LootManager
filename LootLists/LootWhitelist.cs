using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Newtonsoft.Json;

namespace LootManager
{
    [Serializable]
    public sealed class LootWhiteListData
    {
        [JsonProperty] public List<string> Items = new List<string>();
    }

    public static class LootWhitelist
    {
        private const string FileName  = "LootWhitelist.json";
        private const string ConfigSub = "LootManager";

        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };
        
        public static void Load()
        {
            string dir        = Path.Combine(Paths.ConfigPath, ConfigSub);
            string newPath    = Path.Combine(dir, FileName);
            string legacyPath = Path.Combine(Paths.ConfigPath, FileName);

            try
            {
                Directory.CreateDirectory(dir);
                
                if (!File.Exists(newPath) && File.Exists(legacyPath))
                {
                    TryMigrateLegacyFile(legacyPath, newPath);
                }
                
                if (!File.Exists(newPath))
                {
                    var defaults = new LootWhiteListData { Items = new List<string>() };
                    var json = JsonConvert.SerializeObject(defaults, _jsonSettings);
                    File.WriteAllText(newPath, json);

                    Plugin.Whitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    Plugin.Log.LogInfo($"[Loot Manager] Created default {FileName} at {newPath}");
                    return;
                }
                
                var text = File.ReadAllText(newPath);
                var data = JsonConvert.DeserializeObject<LootWhiteListData>(text) ?? new LootWhiteListData();

                var items = (data.Items ?? new List<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim());

                Plugin.Whitelist = new HashSet<string>(items, StringComparer.OrdinalIgnoreCase);
                Plugin.Log.LogInfo($"[Loot Manager] Loaded {Plugin.Whitelist.Count} whitelisted items.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Loot Manager] Failed to load {FileName}: {ex}");
                Plugin.Whitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }
        
        public static void SaveWhitelist()
        {
            try
            {
                string dir     = Path.Combine(Paths.ConfigPath, ConfigSub);
                string newPath = Path.Combine(dir, FileName);
                Directory.CreateDirectory(dir);

                var ordered = (Plugin.Whitelist ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var data = new LootWhiteListData { Items = ordered };
                var json = JsonConvert.SerializeObject(data, _jsonSettings);
                File.WriteAllText(newPath, json);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Loot Manager] Failed to save {FileName}: {ex}");
            }
        }
        
        public static HashSet<string> Read()
        {
            return new HashSet<string>(Plugin.Whitelist ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                                       StringComparer.OrdinalIgnoreCase);
        }
        
        public static void SaveAll(IEnumerable<string> items)
        {
            Plugin.Whitelist = new HashSet<string>(
                (items ?? Enumerable.Empty<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim()),
                StringComparer.OrdinalIgnoreCase);

            SaveWhitelist();
        }

        private static void TryMigrateLegacyFile(string legacyPath, string newPath)
        {
            try
            {
                if (File.Exists(newPath))
                {
                    string backup = legacyPath + ".bak";
                    File.Copy(legacyPath, backup, overwrite: true);
                    File.Delete(legacyPath);
                    Plugin.Log.LogWarning($"[Loot Manager] Found both legacy and new {FileName}. " +
                                          $"Kept new; backed up legacy to {backup} and removed original legacy file.");
                    return;
                }
                
                File.Move(legacyPath, newPath);
                Plugin.Log.LogInfo($"[Loot Manager] Migrated legacy {FileName} to {newPath}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Loot Manager] Failed to migrate legacy {FileName}: {ex}");
            }
        }
    }
}
