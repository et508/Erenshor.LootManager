namespace LootManager
{
    public static class WhitelistLoot
    {
        public static bool ShouldLoot(Item item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemName))
                return false;

            string itemName = item.ItemName;

            // Always allow equipment if enabled
            if (Plugin.LootEquipment.Value && item.RequiredSlot != Item.SlotType.General)
                return true;

            // Direct whitelist
            if (Plugin.Whitelist.Contains(itemName))
                return true;

            // FilterList check (all active categories)
            foreach (var category in Plugin.FilterList.Values)
            {
                if (category.Contains(itemName))
                    return true;
            }

            return false;
        }
    }
}