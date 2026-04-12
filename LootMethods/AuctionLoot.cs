using UnityEngine;

namespace LootManager
{
    public static class AuctionLoot
    {
        public static bool TryListItem(Item item, int quantity = 1)
        {
            if (item == null || item == GameData.PlayerInv.Empty)
                return false;

            // Blessed (qty=2) and godly (qty=3) equipment cannot be listed on the AH,
            // matching the game's own restriction in AuctionHouseUI.
            if (item.RequiredSlot != Item.SlotType.General && quantity > 1)
            {
                string tier = quantity == 2 ? "Blessed" : "Godly";
                ChatFilterInjector.SendLootMessage(
                    $"[Loot Manager] Cannot list \"{item.ItemName}\" on AH ({tier} items not supported).", "red");
                return false;
            }

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