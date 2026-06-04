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

            if (Plugin.LootEquipment.Value && item.RequiredSlot != Item.SlotType.General)
            {
                switch (Plugin.LootEquipmentTier.Value)
                {
                    case EquipmentTierSetting.All:
                        break;
                    case EquipmentTierSetting.NormalOnly:
                        if (quantity != 1) return false;
                        break;
                    case EquipmentTierSetting.ImprovedOnly:
                        if (quantity <= 10) return false;
                        break;
                    case EquipmentTierSetting.BlessedOnly:
                        if (quantity != 2) return false;
                        break;
                    case EquipmentTierSetting.AscendedOnly:
                        if (quantity != 3) return false;
                        break;
                    case EquipmentTierSetting.ImprovedAndUp:
                        if (quantity < 2) return false;
                        break;
                    case EquipmentTierSetting.BlessedAndUp:
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