using HarmonyLib;

namespace LootManager
{
    [HarmonyPatch(typeof(AuctionHouseUI), nameof(AuctionHouseUI.UpdatePlayerSlots))]
    public static class AuctionHouseUIPatch
    {
        public static bool Prefix(AuctionHouseUI __instance)
        {
            foreach (var slot in __instance.Slots)
            {
                slot.MyItem = GameData.PlayerInv.Empty;
                slot.UpdateSlotImage();
                slot.PlayerOwned = true;
                slot.GetComponent<PriceOverride>().DispPrice.text = "";
            }

            // Bail if no seller data, or if it belongs to someone else
            if (__instance.CurrentSellerData == null ||
                __instance.CurrentSellerData.SellerName != GameData.PlayerStats.MyName)
                return false;

            int maxSlots = __instance.Slots.Count;
            int num = 0;

            foreach (AHItemSaveData entry in __instance.CurrentSellerData.ItemsForSale)
            {
                if (num >= maxSlots) break;
                if (entry == null) continue;

                __instance.Slots[num].MyItem = GameData.ItemDB.GetItemByID(entry.itemID);
                __instance.Slots[num].UpdateSlotImage();
                __instance.Slots[num].Quantity = entry.itemQual;
                __instance.Slots[num].GetComponent<PriceOverride>().Price = entry.itemPrice;

                if (__instance.Slots[num].MyItem != null &&
                    __instance.Slots[num].MyItem != GameData.PlayerInv.Empty)
                {
                    __instance.Slots[num].GetComponent<PriceOverride>().DispPrice.text =
                        entry.itemPrice.ToString() + "g";
                }
                else
                {
                    __instance.Slots[num].GetComponent<PriceOverride>().DispPrice.text = "";
                }

                num++;
            }

            return false;
        }
    }
}