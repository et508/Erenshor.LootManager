namespace LootManager
{
    public static class BlacklistLoot
    {
        public static bool ShouldLoot(Item item, int quantity)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemName))
                return false;

            if (item.NoTradeNoDestroy)
                return false;

            if (Plugin.GetLootEquipment() && item.RequiredSlot != Item.SlotType.General)
            {
                switch (Plugin.GetLootEquipmentTier())
                {
                    case "All":
                        break;
                    case "Normal Only":
                        if (quantity != 1) return false;
                        break;
                    case "Improved Only":
                        if (quantity <= 10) return false;
                        break;
                    case "Blessed Only":
                        if (quantity != 2) return false;
                        break;
                    case "Ascended Only":
                        if (quantity != 3) return false;
                        break;
                    case "Improved and Up":
                        if (quantity < 2) return false;
                        break;
                    case "Blessed and Up":
                        if (quantity < 2 || quantity > 3) return false;
                        break;
                }
            }

            string itemName = item.ItemName;

            if (Plugin.Blacklist.Contains(itemName))
                return true;

            foreach (var kvp in Plugin.FilterList)
            {
                if (!Plugin.EnabledFilterCategories.Contains(kvp.Key))  continue;
                if (!Plugin.FilterAppliedToBlacklist.Contains(kvp.Key)) continue;
                if (kvp.Value.Contains(itemName)) return true;
            }

            return false;
        }
    }
}