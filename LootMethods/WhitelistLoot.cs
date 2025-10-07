namespace LootManager
{
    public static class WhitelistLoot
    {
        public static bool ShouldLoot(Item item, int quantity)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemName))
                return false;

            // Always loot story items
            if (item.NoTradeNoDestroy)
                return true;

            // Always allow equipment if enabled and it passes tier filters
            if (Plugin.LootEquipment.Value && item.RequiredSlot != Item.SlotType.General)
            {
                switch (Plugin.LootEquipmentTier.Value)
                {
                    case EquipmentTierSetting.All:
                        return true;
                    case EquipmentTierSetting.NormalOnly:
                        return quantity == 1;
                    case EquipmentTierSetting.BlessedOnly:
                        return quantity == 2;
                    case EquipmentTierSetting.GodlyOnly:
                        return quantity == 3;
                    case EquipmentTierSetting.BlessedAndUp:
                        return quantity >= 2;
                }
            }

            string itemName = item.ItemName;

            // Direct whitelist
            if (Plugin.Whitelist.Contains(itemName))
                return true;

            // FilterList check (reads directly from LootFilterlist.ini)
            LootFilterlist.ReadAll(out var filterSections, out var enabledSet);

            foreach (var kvp in filterSections)
            {
                // Skip disabled sections
                if (!enabledSet.Contains(kvp.Key))
                    continue;

                // Match found
                if (kvp.Value.Contains(item.ItemName))
                    return true;
            }


            return false;
        }
    }
}