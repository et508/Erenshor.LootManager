

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

            if (__instance.CurrentSellerData == null &&
                __instance.CurrentSellerData.SellerName != GameData.PlayerStats.MyName)
                return false;

            int maxSlots = __instance.Slots.Count;
            int num = 0;

            foreach (string id in __instance.CurrentSellerData.SellerItems)
            {

                if (num >= maxSlots) break;

                __instance.Slots[num].MyItem = GameData.ItemDB.GetItemByID(id);
                __instance.Slots[num].UpdateSlotImage();
                __instance.Slots[num].GetComponent<PriceOverride>().Price =
                    __instance.CurrentSellerData.PlayerPrices[num];

                if (__instance.Slots[num].MyItem != null &&
                    __instance.Slots[num].MyItem != GameData.PlayerInv.Empty)
                {
                    __instance.Slots[num].GetComponent<PriceOverride>().DispPrice.text =
                        __instance.CurrentSellerData.PlayerPrices[num].ToString() + "g";
                }
                else
                {
                    __instance.Slots[num].GetComponent<PriceOverride>().DispPrice.text =
                        __instance.CurrentSellerData.PlayerPrices[num].ToString() ?? "";
                }

                num++;
            }

            return false;
        }
    }
}