using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace LootManager
{
    [BepInPlugin("et508.erenshor.lootmanager", "Loot Manager", "0.0.0")]
    [BepInProcess("Erenshor.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo("[LootManager] Plugin loaded.");
            var harmony = new Harmony("et508.erenshor.lootmanager");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(LootWindow), nameof(LootWindow.LootAll))]
    public static class LootWindowLogPatch
    {
        public static void Prefix(LootWindow __instance)
        {
            foreach (ItemIcon itemIcon in __instance.LootSlots)
            {
                if (!(itemIcon.MyItem == GameData.PlayerInv.Empty))
                {
                    UpdateSocialLog.LogAdd("[LootManager] Looted " + itemIcon.MyItem.ItemName, "lightblue");
                }
            }
            UpdateSocialLog.LogAdd("[LootManager] LootAll was called.", "yellow");
            Plugin.Log.LogInfo("[LootManager] LootAll was called.");
        }
    }
}