using HarmonyLib;

namespace LootManager
{
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.UpdatePlayerInventory))]
    public class TrashSlotBlacklistPatch
    {
        public static void Postfix(Inventory __instance)
        {
            foreach (var slot in __instance.StoredSlots)
            {
                if (slot.TrashSlot && slot.MyItem != GameData.PlayerInv.Empty)
                {
                    string itemName = slot.MyItem.ItemName;
                    if (!Plugin.Blacklist.Contains(itemName))
                    {
                        Plugin.Blacklist.Add(itemName);
                        UpdateSocialLog.LogAdd("[Loot Manager] Added to blacklist: " + itemName, "yellow");
                        LootBlacklist.SaveBlacklist();
                    }

                    slot.MyItem = GameData.PlayerInv.Empty;
                    slot.UpdateSlotImage();
                }
            }
        }
    }
}
