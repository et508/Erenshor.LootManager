using HarmonyLib;

namespace LootManager
{
    [HarmonyPatch(typeof(LootWindow), nameof(LootWindow.LootAll))]
    [HarmonyPriority(Priority.First)]
    public static class LootWindowPatch
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
                        UpdateSocialLog.LogAdd("[Loot Manager] Destroyed blacklisted item: " + itemName, "grey");
                        itemIcon.MyItem = GameData.PlayerInv.Empty;
                        itemIcon.UpdateSlotImage();
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

            GameData.PlayerAud.PlayOneShot(GameData.GM.GetComponent<Misc>().DropItem, GameData.PlayerAud.volume / 2f * GameData.SFXVol);
            __instance.CloseWindow();
            return false;
        }
    }
}