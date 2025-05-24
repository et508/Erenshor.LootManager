using HarmonyLib;

namespace LootManager
{
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.UpdatePlayerInventory))]
    public class TrashSlotPatch
    {
        private static string lastTrashItem = null;

        public static void Postfix(Inventory __instance)
        {
            foreach (var slot in __instance.StoredSlots)
            {
                if (slot.TrashSlot)
                {
                    var currentItem = slot.MyItem;

                    // Item is placed in the TrashSlot
                    if (currentItem != GameData.PlayerInv.Empty && currentItem.ItemName != lastTrashItem)
                    {
                        if (!Plugin.Blacklist.Contains(currentItem.ItemName))
                        {
                            Plugin.Blacklist.Add(currentItem.ItemName);
                            UpdateSocialLog.LogAdd("[Loot Manager] Added to blacklist: " + currentItem.ItemName, "yellow");
                            LootBlacklist.SaveBlacklist();
                        }
                        lastTrashItem = currentItem.ItemName;
                    }

                    // TrashSlot has been cleared or item was removed
                    if (currentItem == GameData.PlayerInv.Empty && !string.IsNullOrEmpty(lastTrashItem))
                    {
                        if (Plugin.Blacklist.Contains(lastTrashItem))
                        {
                            Plugin.Blacklist.Remove(lastTrashItem);
                            UpdateSocialLog.LogAdd("[Loot Manager] Removed from blacklist: " + lastTrashItem, "yellow");
                            LootBlacklist.SaveBlacklist();
                        }
                        lastTrashItem = null;
                    }
                }
            }
        }
    }
}