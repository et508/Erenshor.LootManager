using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LootManager
{
    [BepInPlugin("et508.erenshor.lootmanager", "Loot Manager", "0.0.0")]
    [BepInProcess("Erenshor.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static HashSet<string> Blacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private const string BlacklistFileName = "LootBlacklist.json";

        private void Awake()
        {
            Log = Logger;
            LoadBlacklist();
            Log.LogInfo("LootManager loaded.");
            var harmony = new Harmony("et508.erenshor.lootmanager");
            harmony.PatchAll();
        }

        private void LoadBlacklist()
        {
            string path = Path.Combine(Paths.ConfigPath, BlacklistFileName);

            if (!File.Exists(path))
            {
                File.WriteAllText(path, JsonUtility.ToJson(new LootBlacklistData(), true));
                Log.LogInfo("Created empty loot blacklist file.");
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<LootBlacklistData>(json);
                if (data?.items != null)
                {
                    Blacklist = new HashSet<string>(data.items, StringComparer.OrdinalIgnoreCase);
                    Log.LogInfo($"Loaded {Blacklist.Count} blacklisted items.");
                }
            }
            catch (Exception ex)
            {
                Log.LogError("Failed to load blacklist: " + ex);
            }
        }
    }

    [Serializable]
    public class LootBlacklistData
    {
        public List<string> items = new List<string>();
    }

    [HarmonyPatch(typeof(LootWindow), nameof(LootWindow.LootAll))]
    public static class LootWindowLogPatch
    {
        public static bool Prefix(LootWindow __instance)
        {
            foreach (ItemIcon itemIcon in __instance.LootSlots)
            {
                if (itemIcon.MyItem != GameData.PlayerInv.Empty)
                {
                    string itemName = itemIcon.MyItem.ItemName;
                    if (Plugin.Blacklist.Contains(itemName))
                    {
                        UpdateSocialLog.LogAdd("[Loot Manager] Skipped blacklisted item: " + itemName, "grey");
                        continue;
                    }

                    bool flag;
                    if (itemIcon.MyItem.RequiredSlot == Item.SlotType.General)
                    {
                        flag = GameData.PlayerInv.AddItemToInv(itemIcon.MyItem);
                    }
                    else
                    {
                        flag = GameData.PlayerInv.AddItemToInv(itemIcon.MyItem, itemIcon.Quantity);
                    }

                    if (flag)
                    {
                        UpdateSocialLog.LogAdd("[Loot Manager] Looted Item: " + itemName, "yellow");
                        itemIcon.InformGroupOfLoot(itemIcon.MyItem);
                        itemIcon.MyItem = GameData.PlayerInv.Empty;
                        itemIcon.UpdateSlotImage();
                    }
                    else
                    {
                        UpdateSocialLog.LogAdd("[Loot Manager] No room for " + itemName, "yellow");
                    }
                }
            }

            UpdateSocialLog.LogAdd("[LootManager] LootAll was called.", "yellow");
            Plugin.Log.LogInfo("[LootManager] LootAll was called.");

            return false;
        }
    }
}
