using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BepInEx;

namespace LootManager
{
    [Serializable]
    public class LootWhitelistData
    {
        public List<string> items = new List<string>();
    }

    public static class LootWhitelist
    {
        private const string WhitelistFileName = "LootWhitelist.json";

        public static void Load()
        {
            string path = Path.Combine(Paths.ConfigPath, WhitelistFileName);

            if (!File.Exists(path))
            {
                File.WriteAllText(path, JsonUtility.ToJson(new LootWhitelistData(), true));
                Plugin.Log.LogInfo("Created empty loot whitelist file.");
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<LootWhitelistData>(json);
                if (data?.items != null)
                {
                    Plugin.Whitelist = new HashSet<string>(data.items, StringComparer.OrdinalIgnoreCase);
                    Plugin.Log.LogInfo($"Loaded {Plugin.Whitelist.Count} whitelisted items.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Failed to load whitelist: " + ex);
            }
        }

        public static void SaveWhitelist()
        {
            var data = new LootBlacklistData { items = new List<string>(Plugin.Whitelist) };
            string json = JsonUtility.ToJson(data, true);
            string path = Path.Combine(Paths.ConfigPath, WhitelistFileName);
            File.WriteAllText(path, json);
        }
    }
}