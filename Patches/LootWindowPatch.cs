using HarmonyLib;

namespace LootManager
{
    [HarmonyPatch(typeof(LootWindow), nameof(LootWindow.LootAll))]
    [HarmonyPriority(Priority.First)]
    public static class LootWindowPatch
    {
        public static bool Prefix(LootWindow __instance)
        {
            string method = Plugin.LootMethod.Value;

            foreach (ItemIcon itemIcon in __instance.LootSlots)
            {
                if (itemIcon.MyItem != GameData.PlayerInv.Empty)
                {
                    string itemName = itemIcon.MyItem.ItemName;

                    bool shouldDestroy = false;

                    if (method == "Blacklist" && Plugin.Blacklist.Contains(itemName))
                    {
                        shouldDestroy = true;
                    }
                    // else if (method == "Whitelist" && !Plugin.Whitelist.Contains(itemName))
                    // {
                    //    shouldDestroy = true;
                    // }

                    if (shouldDestroy)
                    {
                        UpdateSocialLog.LogAdd("[Loot Manager] Destroyed item: " + itemName, "grey");
                        itemIcon.MyItem = GameData.PlayerInv.Empty;
                        itemIcon.UpdateSlotImage();
                        continue;
                    }

                    bool added;
                    if (itemIcon.MyItem.RequiredSlot == Item.SlotType.General)
                    {
                        added = GameData.PlayerInv.AddItemToInv(itemIcon.MyItem);
                    }
                    else
                    {
                        added = GameData.PlayerInv.AddItemToInv(itemIcon.MyItem, itemIcon.Quantity);
                    }

                    if (added)
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

            GameData.PlayerAud.PlayOneShot(
                GameData.GM.GetComponent<Misc>().DropItem,
                GameData.PlayerAud.volume / 2f * GameData.SFXVol
            );

            __instance.CloseWindow();
            return false;
        }
    }
}
