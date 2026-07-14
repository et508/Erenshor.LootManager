namespace LootManager
{
    public static class WhitelistLoot
    {
        public static bool ShouldLoot(Item item, int quantity)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemName))
                return false;

            if (item.NoTradeNoDestroy)
                return true;

            if (Plugin.GetLootEquipment() && item.RequiredSlot != Item.SlotType.General)
            {
                switch (Plugin.GetLootEquipmentTier())
                {
                    case "All":
                        return true;
                    case "Normal Only":
                        return quantity == 1;
                    case "Improved Only":
                        return quantity > 10;
                    case "Blessed Only":
                        return quantity == 2;
                    case "Ascended Only":
                        return quantity == 3;
                    case "Improved and Up":
                        return quantity >= 2;
                    case "Blessed and Up":
                        return quantity >= 2 && quantity <= 3;
                }
            }

            string itemName = item.ItemName;

            if (Plugin.Whitelist.Contains(itemName))
                return true;

            foreach (var kvp in Plugin.FilterList)
            {
                if (!Plugin.EnabledFilterCategories.Contains(kvp.Key))  continue;
                if (!Plugin.FilterAppliedToWhitelist.Contains(kvp.Key)) continue;
                if (kvp.Value.Contains(itemName)) return true;
            }

            return false;
        }
    }
}