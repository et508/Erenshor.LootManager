// AuctionHouseUIPatch.cs
// Patches AuctionHouseUI.UpdatePlayerSlots to guard against IndexOutOfRange when
// the player has more AH listings than the UI has display slots (18).
// We bypass the original method entirely and re-implement it with a bounds check,
// so the UI safely shows the first 18 listings and silently ignores any beyond that.

using HarmonyLib;

namespace LootManager
{
    [HarmonyPatch(typeof(AuctionHouseUI), nameof(AuctionHouseUI.UpdatePlayerSlots))]
    public static class AuctionHouseUIPatch
    {
        public static bool Prefix(AuctionHouseUI __instance)
        {
            // Clear all slots first (same as original)
            foreach (var slot in __instance.Slots)
            {
                slot.MyItem = GameData.PlayerInv.Empty;
                slot.UpdateSlotImage();
                slot.PlayerOwned = true;
                slot.GetComponent<PriceOverride>().DispPrice.text = "";
            }

            // Guard (same null check as original, though it has a bug — we preserve it)
            if (__instance.CurrentSellerData == null &&
                __instance.CurrentSellerData.SellerName != GameData.PlayerStats.MyName)
                return false;

            int maxSlots = __instance.Slots.Count; // always 18 in vanilla
            int num = 0;

            foreach (string id in __instance.CurrentSellerData.SellerItems)
            {
                // Stop filling UI slots once we hit the display limit —
                // overflow listings are still active and will sell normally.
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

            return false; // skip original
        }
    }
}