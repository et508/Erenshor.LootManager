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
            
            if (Plugin.LootRare.Value && item.RequiredSlot != Item.SlotType.General)
            {
                if (quantity >= 2)
                    return false;
            }

            string itemName = item.ItemName;
            
            if (Plugin.Blacklist.Contains(itemName))
                return true;
            
            return false;
        }
    }
}