// AuctionLoot.cs
// Handles posting an item directly to the player's Auction House listings at loot time.
// Bypasses the UI's 18-slot cap — items above 18 are still listed and will sell,
// they just won't all be visible in the player's AH listings panel (cosmetic limitation).

using UnityEngine;

namespace LootManager
{
    public static class AuctionLoot
    {
        /// <summary>
        /// Lists <paramref name="item"/> on the player's AH at the standard price
        /// (ItemValue * 6 - 1). Removes the item from the loot slot.
        /// Returns true if the listing was successfully added.
        /// </summary>
        public static bool TryListItem(Item item)
        {
            if (item == null || item == GameData.PlayerInv.Empty)
                return false;

            // Items with no value or flagged no-trade cannot be listed
            if (item.ItemValue <= 0)
            {
                ChatFilterInjector.SendLootMessage(
                    $"[Loot Manager] Cannot list \"{item.ItemName}\" on AH (no sell value).", "red");
                return false;
            }

            if (item.NoTradeNoDestroy)
            {
                ChatFilterInjector.SendLootMessage(
                    $"[Loot Manager] Cannot list \"{item.ItemName}\" on AH (no-trade item).", "red");
                return false;
            }

            // Get or create the player's AH save data
            AuctionHouseSave playerData = AuctionHouse.ReadCharData(GameData.PlayerStats.MyName);
            if (playerData == null)
            {
                playerData = AuctionHouse.LoadCharData(GameData.PlayerStats.MyName);
            }

            if (playerData == null)
            {
                Plugin.Log.LogError("[Loot Manager] AuctionLoot: Failed to get player AH data.");
                return false;
            }

            // Price formula: ItemValue * 6 - 1
            int listPrice = (item.ItemValue * 6) - 1;

            playerData.SellerItems.Add(item.Id);
            playerData.PlayerPrices.Add(listPrice);

            AuctionHouse.SavePlayerAHData(playerData);

            ChatFilterInjector.SendLootMessage(
                $"[Loot Manager] Listed \"{item.ItemName}\" on AH for {listPrice}g.", "yellow");

            return true;
        }
    }
}