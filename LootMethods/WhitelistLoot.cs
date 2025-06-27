namespace LootManager
{
    public static class WhitelistLoot
    {
        
        public static bool ShouldLoot(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return false;

            // Direct whitelist
            if (Plugin.Whitelist.Contains(itemName))
                return true;

            // FilterList check
            foreach (var category in Plugin.FilterList.Values)
            {
                if (category.Contains(itemName))
                    return true;
            }


            return false;
        }
    }
}
