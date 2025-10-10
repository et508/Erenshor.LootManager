using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BepInEx;

namespace LootManager
{
    [Serializable]
    public class LootBanklistData
    {
        public List<string> items = new List<string>();
    }

    public static class LootBanklist
    {
        private const string BanklistFileName = "LootBanklist.json";

        public static void Load()
        {
            string path = Path.Combine(Paths.ConfigPath, BanklistFileName);

            if (!File.Exists(path))
            {
                File.WriteAllText(path, JsonUtility.ToJson(new LootBanklistData(), true));
                Plugin.Log.LogInfo("Created empty loot banklist file.");
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<LootBanklistData>(json);
                if (data?.items != null)
                {
                    Plugin.Banklist = new HashSet<string>(data.items, StringComparer.OrdinalIgnoreCase);
                    Plugin.Log.LogInfo($"[Loot Manager] Loaded {Plugin.Banklist.Count} banklisted items.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Failed to load banklist: " + ex);
            }
        }

        public static void SaveBanklist()
        {
            var data = new LootBanklistData { items = new List<string>(Plugin.Banklist) };
            string json = JsonUtility.ToJson(data, true);
            string path = Path.Combine(Paths.ConfigPath, BanklistFileName);
            File.WriteAllText(path, json);
        }
    }
}