using HarmonyLib;

namespace LootManager
{
    /// <summary>
    /// Applies loot filters to mining yields BEFORE the item enters inventory.
    ///
    /// A prefix on PlayerCombat.TryMine sets _inMining = true, and a postfix
    /// clears it. While _inMining is true, prefixes on AddItemToInv and
    /// ForceItemToInv intercept the mined item and run it through the active
    /// loot filters before it ever touches the player's inventory.
    /// </summary>
    public static class MiningPatch
    {
        private static bool _inMining = false;
        private static bool _inFilter = false;

        // ── Filter logic ──────────────────────────────────────────────────────

        private static bool ShouldAddToInventory(Item item)
        {
            if (!Plugin.MiningFilterEnabled.Value) return true;

            string name       = item.ItemName;
            string lootMethod = Plugin.LootMethod.Value;

            // ── Auctionlist ───────────────────────────────────────────────────
            if (Plugin.Auctionlist != null && Plugin.Auctionlist.Contains(name))
            {
                bool listed = AuctionLoot.TryListItem(item, 1);
                if (listed) return false;
                // Listing failed — fall through, keep in inventory
            }

            // ── Banklist ──────────────────────────────────────────────────────
            if (Plugin.BankLootEnabled.Value &&
                Plugin.Banklist != null && Plugin.Banklist.Contains(name))
            {
                BankLoot.DepositLoot(new BankLoot.LootEntry[]
                {
                    new BankLoot.LootEntry(item.Id, 1, name)
                });
                ChatFilterInjector.SendLootMessage(
                    $"[Loot Manager] Mining: banked \"{name}\".", "lightblue");
                return false;
            }

            // ── Blacklist mode ────────────────────────────────────────────────
            if (lootMethod == "Blacklist" && BlacklistLoot.ShouldLoot(item, 1))
            {
                ChatFilterInjector.SendLootMessage(
                    $"[Loot Manager] Mining: discarded \"{name}\" (blacklisted).", "grey");
                return false;
            }

            // ── Whitelist mode ────────────────────────────────────────────────
            if (lootMethod == "Whitelist" && !WhitelistLoot.ShouldLoot(item, 1))
            {
                ChatFilterInjector.SendLootMessage(
                    $"[Loot Manager] Mining: discarded \"{name}\" (not on whitelist).", "grey");
                return false;
            }

            return true;
        }

        // ── TryMine bracket ───────────────────────────────────────────────────

        [HarmonyPatch(typeof(PlayerCombat), "TryMine")]
        public static class TryMine_Patch
        {
            public static void Prefix()  => _inMining = true;
            public static void Postfix() => _inMining = false;
        }

        // ── AddItemToInv prefix ───────────────────────────────────────────────

        [HarmonyPatch(typeof(Inventory), "AddItemToInv", new[] { typeof(Item) })]
        public static class AddItemToInv_Patch
        {
            public static bool Prefix(Item _item, ref bool __result)
            {
                try
                {
                    if (!_inMining || _inFilter) return true;

                    _inFilter = true;
                    bool allow;
                    try   { allow = ShouldAddToInventory(_item); }
                    finally { _inFilter = false; }

                    if (!allow)
                    {
                        __result = true; // prevent ForceItemToInv fallback
                        return false;
                    }
                }
                catch (System.Exception ex)
                {
                    if (Plugin.Log != null)
                        Plugin.Log.LogError("[Loot Manager] MiningPatch.AddItemToInv error: " + ex);
                }
                return true;
            }
        }

        // ── ForceItemToInv prefix ─────────────────────────────────────────────

        [HarmonyPatch(typeof(Inventory), "ForceItemToInv", new[] { typeof(Item) })]
        public static class ForceItemToInv_Patch
        {
            public static bool Prefix(Item _item)
            {
                try
                {
                    if (!_inMining || _inFilter) return true;

                    _inFilter = true;
                    bool allow;
                    try   { allow = ShouldAddToInventory(_item); }
                    finally { _inFilter = false; }

                    if (!allow) return false;
                }
                catch (System.Exception ex)
                {
                    if (Plugin.Log != null)
                        Plugin.Log.LogError("[Loot Manager] MiningPatch.ForceItemToInv error: " + ex);
                }
                return true;
            }
        }
    }
}