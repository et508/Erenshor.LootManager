using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Newtonsoft.Json;

namespace LootManager
{
    [Serializable]
    public sealed class LootBankListData
    {
        [JsonProperty] public List<string> Items = new List<string>();
    }

    public static class LootBanklist
    {
        private const string FileName  = "LootBanklist.json";
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
                    var defaults = new LootBankListData { Items = new List<string>() };
                    var json = JsonConvert.SerializeObject(defaults, _jsonSettings);
                    File.WriteAllText(newPath, json);

                    Plugin.Banklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    Plugin.Log.LogInfo($"[Loot Manager] Created default {FileName} at {newPath}");
                    return;
                }
                
                var text = File.ReadAllText(newPath);
                var data = JsonConvert.DeserializeObject<LootBankListData>(text) ?? new LootBankListData();

                var items = (data.Items ?? new List<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim());

                Plugin.Banklist = new HashSet<string>(items, StringComparer.OrdinalIgnoreCase);
                Plugin.Log.LogInfo($"[Loot Manager] Loaded {Plugin.Banklist.Count} banklisted items.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Loot Manager] Failed to load {FileName}: {ex}");
                Plugin.Banklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }
        
        public static void SaveBanklist()
        {
            try
            {
                string dir     = Path.Combine(Paths.ConfigPath, ConfigSub);
                string newPath = Path.Combine(dir, FileName);
                Directory.CreateDirectory(dir);

                var ordered = (Plugin.Banklist ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var data = new LootBankListData { Items = ordered };
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
            return new HashSet<string>(Plugin.Banklist ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                                       StringComparer.OrdinalIgnoreCase);
        }
        
        public static void SaveAll(IEnumerable<string> items)
        {
            Plugin.Banklist = new HashSet<string>(
                (items ?? Enumerable.Empty<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim()),
                StringComparer.OrdinalIgnoreCase);

            SaveBanklist();
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
