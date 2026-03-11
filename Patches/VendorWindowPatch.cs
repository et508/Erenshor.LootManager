

using HarmonyLib;
using UnityEngine;

namespace LootManager
{
    [HarmonyPatch(typeof(VendorWindow), nameof(VendorWindow.LoadWindow))]
    public static class VendorWindowPatch
    {
        private static void Postfix()
        {
            if (Plugin.Junklist == null || Plugin.Junklist.Count == 0)
                return;

            var inv = GameData.PlayerInv;
            if (inv == null) return;

            int totalGold = 0;
            int itemsSold = 0;

            var slots = inv.StoredSlots.ToArray();
            foreach (var slot in slots)
            {
                if (slot == null) continue;
                var item = slot.MyItem;
                if (item == null || item == inv.Empty) continue;
                if (!Plugin.Junklist.Contains(item.ItemName)) continue;
                if (item.ItemValue <= 0) continue;
                if (item.NoTradeNoDestroy) continue;

                int sellPrice = Mathf.RoundToInt(item.ItemValue * 0.65f) + 1;

                if (item.RequiredSlot == Item.SlotType.General && slot.Quantity > 1)
                {
                    int stackPrice = sellPrice * slot.Quantity;
                    totalGold += stackPrice;
                    itemsSold++;
                    ChatFilterInjector.SendLootMessage(
                        $"[Loot Manager] Auto-sold {slot.Quantity}x {item.ItemName} for {stackPrice} gold.", "yellow");
                    inv.RemoveStackFromInv(slot);
                }
                else
                {
                    totalGold += sellPrice;
                    itemsSold++;
                    ChatFilterInjector.SendLootMessage(
                        $"[Loot Manager] Auto-sold {item.ItemName} for {sellPrice} gold.", "yellow");
                    inv.RemoveItemFromInv(slot);
                }
            }

            if (itemsSold > 0)
            {
                inv.Gold += totalGold;
                inv.GoldTXT.text = inv.Gold.ToString();
                GameData.PlayerAud.PlayOneShot(
                    GameData.Misc.BuyItem, GameData.UIVolume * GameData.MasterVol);
                ChatFilterInjector.SendLootMessage(
                    $"[Loot Manager] Auto-sold {itemsSold} junk item(s) for {totalGold} gold total.", "yellow");
            }
        }
    }
}