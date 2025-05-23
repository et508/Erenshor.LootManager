// Plugin.cs
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace LootManager
{
    [BepInPlugin("et508.erenshor.lootmanager", "Loot Manager", "0.0.0")]
    [BepInProcess("Erenshor.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static HashSet<string> Blacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private void Awake()
        {
            Log = Logger;
            LootBlacklist.Load();
            Log.LogInfo("LootManager loaded.");
            var harmony = new Harmony("et508.erenshor.lootmanager");
            harmony.PatchAll();
        }
    }
}