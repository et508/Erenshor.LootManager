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
            
            if (Plugin.Whitelist.Contains(itemName))
                return true;
            
            LootFilterlist.ReadAll(out var filterSections, out var enabledSet);

            foreach (var kvp in filterSections)
            {
                if (!enabledSet.Contains(kvp.Key))
                    continue;
                
                if (kvp.Value.Contains(item.ItemName))
                    return true;
            }


            return false;
        }
    }
}