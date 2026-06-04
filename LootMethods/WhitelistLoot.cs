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
                    case EquipmentTierSetting.ImprovedOnly:
                        return quantity > 10;
                    case EquipmentTierSetting.BlessedOnly:
                        return quantity == 2;
                    case EquipmentTierSetting.AscendedOnly:
                        return quantity == 3;
                    case EquipmentTierSetting.ImprovedAndUp:
                        return quantity >= 2;
                    case EquipmentTierSetting.BlessedAndUp:
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