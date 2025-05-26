using BepInEx;
using BepInEx.Logging;
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

        private void Awake()
        {
            Log = Logger;
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