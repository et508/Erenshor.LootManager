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
            Log.LogInfo("LootManager loaded.");
            var harmony = new Harmony("et508.erenshor.lootmanager");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(LootWindow), nameof(LootWindow.LootAll))]
    public static class LootWindowLogPatch
    {
        public static bool Prefix(LootWindow __instance)
        {
            foreach (ItemIcon itemIcon in __instance.LootSlots)
            {
                if (!(itemIcon.MyItem == GameData.PlayerInv.Empty))
                {
                    bool flag;
                    if (itemIcon.MyItem.RequiredSlot == Item.SlotType.General)
                    {
                        flag = GameData.PlayerInv.AddItemToInv(itemIcon.MyItem);
                    }
                    else
                    {
                        flag = GameData.PlayerInv.AddItemToInv(itemIcon.MyItem, itemIcon.Quantity);
                        int quantity = itemIcon.Quantity;
                        int quantity2 = itemIcon.Quantity;
                        int quantity3 = itemIcon.Quantity;
                    }
                    if (flag)
                    {
                        UpdateSocialLog.LogAdd("[Loot Manager] Looted Item: " + itemIcon.MyItem.ItemName, "yellow");
                        itemIcon.InformGroupOfLoot(itemIcon.MyItem);
                        itemIcon.MyItem = GameData.PlayerInv.Empty;
                        itemIcon.UpdateSlotImage();
                    }
                    else
                    {
                        UpdateSocialLog.LogAdd("No room for " + itemIcon.MyItem.ItemName, "yellow");
                    }
                }
            }
            UpdateSocialLog.LogAdd("[LootManager] LootAll was called.", "yellow");
            Plugin.Log.LogInfo("[LootManager] LootAll was called.");

            return false;
        }
    }
}