using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LootManager
{
    [Serializable]
    public class LootBlacklistData
    {
        public List<string> items = new List<string>();
    }

    public static class LootBlacklist
    {
        private const string BlacklistFileName = "LootBlacklist.json";

        public static void Load()
        {
            string path = Path.Combine(Paths.ConfigPath, BlacklistFileName);

            if (!File.Exists(path))
            {
                File.WriteAllText(path, JsonUtility.ToJson(new LootBlacklistData(), true));
                Plugin.Log.LogInfo("Created empty loot blacklist file.");
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<LootBlacklistData>(json);
                if (data?.items != null)
                {
                    Plugin.Blacklist = new HashSet<string>(data.items, StringComparer.OrdinalIgnoreCase);
                    Plugin.Log.LogInfo($"Loaded {Plugin.Blacklist.Count} blacklisted items.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Failed to load blacklist: " + ex);
            }
        }
    }
}