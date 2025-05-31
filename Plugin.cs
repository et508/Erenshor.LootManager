using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LootManager
{
    [BepInPlugin("et508.erenshor.lootmanager", "Loot Manager", "1.1.1")]
    [BepInProcess("Erenshor.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static HashSet<string> Blacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Config entries
        public static ConfigEntry<bool> AutoLootEnabled;
        public static ConfigEntry<float> AutoLootDistance;
        public static ConfigEntry<string> LootMethod;


        private void Awake()
        {
            Log = Logger;

            // Load configs
            AutoLootEnabled = Config.Bind("Autoloot Settings", "Enable Autoloot", true, "Enable or disable auto looting.");
            AutoLootDistance = Config.Bind("Autoloot Settings", "Autoloot Distance", 20f, "Maximum distance for auto looting.");
            LootMethod = Config.Bind("Loot Method Settings", "LootMethod", "Blacklist", "Loot method to use: Blacklist, Whitelist, or Standard.");


            // Load saved blacklist
            LootBlacklist.Load();

            Log.LogInfo("Loot Manager loaded.");

            var harmony = new Harmony("et508.erenshor.lootmanager");

            // Conditionally unpatch QoL if it's loaded
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "ErenshorQoL")
                {
                    var lootAllMethod = AccessTools.Method(typeof(LootWindow), nameof(LootWindow.LootAll));
                    harmony.Unpatch(lootAllMethod, HarmonyPatchType.Prefix, "Brumdail.ErenshorQoLMod");
                    Log.LogWarning("[LootManager] Unpatched ErenshorQoL LootAll prefix.");
                    break;
                }
            }

            harmony.PatchAll();
            LootManagerController.Initialize();
        }
    }
}