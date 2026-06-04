using UnityEngine;

namespace LootManager
{
    public static class AuctionLoot
    {
        public static bool TryListItem(Item item, int quantity = 1)
        {
            if (item == null || item == GameData.PlayerInv.Empty)
                return false;

            // Blessed (qty=2) and ascended (qty=3) equipment cannot be listed on the AH,
            // matching the game's own restriction in AuctionHouseUI.
            if (item.RequiredSlot != Item.SlotType.General && quantity > 1)
            {
                string tier = quantity == 2 ? "Blessed" : "Ascended";
                ChatFilterInjector.SendLootMessage(
                    $"[Loot Manager] Cannot list \"{item.ItemName}\" on AH ({tier} items not supported).", "red");
                return false;
            }

            // Match the game's own AH eligibility rules
            if (item.SimPlayersCantGet)
            {
                ChatFilterInjector.SendLootMessage(
                    $"[Loot Manager] Cannot list \"{item.ItemName}\" on AH (not eligible for AH).", "red");
                return false;
            }

            if (item.FurnitureSet)
            {
                ChatFilterInjector.SendLootMessage(
                    $"[Loot Manager] Cannot list \"{item.ItemName}\" on AH (furniture items not allowed).", "red");
                return false;
            }

            if (item.ItemLevel <= 0 || item.ItemLevel > 39)
            {
                ChatFilterInjector.SendLootMessage(
                    $"[Loot Manager] Cannot list \"{item.ItemName}\" on AH (item level out of range).", "red");
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

            if (playerData.ItemsForSale.Count >= 18)
            {
                ChatFilterInjector.SendLootMessage(
                    $"[Loot Manager] Cannot list \"{item.ItemName}\" on AH (no empty seller slots).", "red");
                return false;
            }

            int listPrice = (item.ItemValue * 6) - 1;

            // AHItemSaveData(itemID, itemQual, itemPrice)
            // itemQual stores quantity for stackables, quality tier for equipment
            playerData.ItemsForSale.Add(new AHItemSaveData(item.Id, quantity, listPrice));

            AuctionHouse.SavePlayerAHData(playerData);

            ChatFilterInjector.SendLootMessage(
                $"[Loot Manager] Listed \"{item.ItemName}\" on AH for {listPrice}g.", "yellow");

            return true;
        }
    }
}