using UnityEngine;

namespace LootManager
{
    public static class AuctionLoot
    {
        public static bool TryListItem(Item item, int quantity = 1)
        {
            if (item == null || item == GameData.PlayerInv.Empty)
                return false;

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

            // quantity encodes both tier (1=Normal, 2=Blessed, 3=Ascended)
            // and improvement level (11-15 = Normal+1 through Normal+5)
            // Pass it through directly as itemQual so the AH stores and displays correctly.
            playerData.ItemsForSale.Add(new AHItemSaveData(item.Id, quantity, listPrice));

            AuctionHouse.SavePlayerAHData(playerData);

            ChatFilterInjector.SendLootMessage(
                $"[Loot Manager] Listed \"{item.ItemName}\" on AH for {listPrice}g.", "yellow");

            return true;
        }
    }
}