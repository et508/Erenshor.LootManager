using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace LootManager
{
    [BepInPlugin("et508.erenshor.lootmanager", "Loot Manager", "2.1.0")]
    [BepInProcess("Erenshor.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static HashSet<string> Blacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal static HashSet<string> Whitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal static HashSet<string> Banklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, HashSet<string>> FilterList = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> EnabledFilterGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);





        // Config entries
        public static ConfigEntry<bool> AutoLootEnabled;
        public static ConfigEntry<float> AutoLootDistance;
        public static ConfigEntry<string> LootMethod;
        public static ConfigEntry<bool> BankLootEnabled;
        public static ConfigEntry<string> BankLootMethod;
        public static ConfigEntry<string> BankLootPageMode;
        public static ConfigEntry<int> BankPageFirst;
        public static ConfigEntry<int> BankPageLast;
        public static ConfigEntry<bool> LootEquipment;
        public static ConfigEntry<EquipmentTierSetting> LootEquipmentTier;






        private void Awake()
        {
            Log = Logger;

            // Load configs
            AutoLootEnabled = Config.Bind("Autoloot Settings", "Enable Autoloot", true, "Enable or disable auto looting.");
            AutoLootDistance = Config.Bind("Autoloot Settings", "Autoloot Distance", 20f, "Maximum distance for auto looting.");
            LootMethod = Config.Bind("Loot Method Settings", "Loot Method", "Blacklist", "Loot method to use: Blacklist, Whitelist, or Standard.");
            BankLootEnabled = Config.Bind("Bankloot Settings", "Bankloot Enabled", false, "If enabled, looted items will be deposited to bank instead of inventory.");
            BankLootMethod = Config.Bind("Bankloot Settings", "Bankloot Method", "All", "Method for bank looting: All or Filtered");
            BankLootPageMode = Config.Bind("Bankloot Settings", "Bankloot Page Mode", "First Empty", "Mode for depositing items to bank: First Empty or Page Range");
            BankPageFirst = Config.Bind("Bankloot Settings", "Bank Page First", 20, new ConfigDescription("First bank page to use when in Page Range mode.", new AcceptableValueRange<int>(1, 98)));
            BankPageLast = Config.Bind("Bankloot Settings", "Bank Page Last", 20, new ConfigDescription("Last bank page to use when in Page Range mode.", new AcceptableValueRange<int>(1, 98)));
            LootEquipment = Config.Bind("Filter Settings", "Loot Equipment", true, "If true, loot all equipment.");
            LootEquipmentTier = Config.Bind("Filter Settings", "Loot Equipment Tier", EquipmentTierSetting.All, "Which tiers of equipment to loot: All, Normal Only, Blessed Only, Godly Only, Blessed and Up.");
            
            






            // Load loot lists
            LootBlacklist.Load();
            LootWhitelist.Load();
            LootBanklist.Load();
            LootFilterlist.Load();
            
            // Enable all categories by default
            EnabledFilterGroups = new HashSet<string>(FilterList.Keys, StringComparer.OrdinalIgnoreCase);




            Log.LogInfo("Loot Manager loaded.");

            var harmony = new Harmony("et508.erenshor.lootmanager");

            // Conditionally unpatch QoL if it's loaded
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "ErenshorQoL")
                {
                    var harmonyID = "Brumdail.ErenshorQoLMod";
                    var lootAllMethod = AccessTools.Method(typeof(LootWindow), nameof(LootWindow.LootAll));
                    var doDeathMethod = AccessTools.Method(typeof(Character), "DoDeath", new Type[0]);

                    if (lootAllMethod != null)
                    {
                        harmony.Unpatch(lootAllMethod, HarmonyPatchType.Prefix, harmonyID);
                        Log.LogWarning("[Loot Manager] Unpatched ErenshorQoL LootAll prefix.");
                    }
                    else
                    {
                        Log.LogError("[Loot Manager] Failed to find LootWindow.LootAll for unpatching.");
                    }

                    if (doDeathMethod != null)
                    {
                        harmony.Unpatch(doDeathMethod, HarmonyPatchType.Postfix, harmonyID);
                        Log.LogWarning("[Loot Manager] Unpatched ErenshorQoL DoDeath postfix.");
                    }
                    else
                    {
                        Log.LogError("[LootManager] Failed to find Character.DoDeath for unpatching.");
                    }
                    
                    break;
                }
            }

            harmony.PatchAll();
            LootManagerController.Initialize();
        }
    }
}