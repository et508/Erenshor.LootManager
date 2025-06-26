namespace LootManager
{
    public static class WhitelistLoot
    {
        public static bool ShouldLoot(string itemName)
        {
            return Plugin.Whitelist.Contains(itemName);
        }
    }
}