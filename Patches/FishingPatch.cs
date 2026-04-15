using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace LootManager
{
    /// <summary>
    /// Intercepts Inventory.AddItemToInv and ForceItemToInv when called from
    /// a fishing context, applying loot filters BEFORE the item enters inventory.
    ///
    /// We detect the fishing context by checking the player's Fishing component:
    /// fishCaught == true means the game is in the middle of resolving a catch.
    /// Returning false from the prefix skips the original method entirely —
    /// the item is never added so there is nothing to remove afterward.
    /// </summary>
    public static class FishingPatch
    {
        // Reentrancy guard — BankLoot/AuctionLoot may call AddItemToInv internally
        // (e.g. bank-full fallback). Without this we'd intercept our own calls.
        private static bool _inFilter = false;

        private static readonly FieldInfo _fishCaught = typeof(Fishing)
            .GetField("fishCaught", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo _caughtItem = typeof(Fishing)
            .GetField("caughtItem", BindingFlags.Instance | BindingFlags.NonPublic);

        // ── Check whether we are currently in a fishing catch resolution ─────

        private static bool IsInFishingContext(out Item caughtItem)
        {
            caughtItem = null;
            var playerControl = GameData.PlayerControl;
            if (playerControl == null) return false;

            var fishing = playerControl.GetComponent<Fishing>();
            if (fishing == null) return false;

            bool fishCaught = _fishCaught != null && (bool)_fishCaught.GetValue(fishing);
            if (!fishCaught) return false;

            caughtItem = _caughtItem?.GetValue(fishing) as Item;
            return caughtItem != null && caughtItem != GameData.PlayerInv.Empty;
        }

        // ── Shared filter logic ───────────────────────────────────────────────

        /// <summary>
        /// Returns false if the item should be blocked (skip AddItemToInv),
        /// true if it should proceed into inventory normally.
        /// Side effects: sends chat messages, deposits to bank, lists on AH.
        /// </summary>
        private static bool ShouldAddToInventory(Item item)
        {
            if (!Plugin.FishingFilterEnabled.Value) return true;

            string name       = item.ItemName;
            string lootMethod = Plugin.LootMethod.Value;

            // ── Auctionlist ───────────────────────────────────────────────────
            if (Plugin.Auctionlist != null && Plugin.Auctionlist.Contains(name))
            {
                bool listed = AuctionLoot.TryListItem(item, 1);
                if (listed)
                {
                    // Item was listed — block inventory add
                    return false;
                }
                // Listing failed (blessed/no value/etc.) — fall through,
                // keep item in inventory.
            }

            // ── Banklist ──────────────────────────────────────────────────────
            if (Plugin.BankLootEnabled.Value &&
                Plugin.Banklist != null && Plugin.Banklist.Contains(name))
            {
                var entries = new BankLoot.LootEntry[]
                {
                    new BankLoot.LootEntry(item.Id, 1, name)
                };
                BankLoot.DepositLoot(entries);
                ChatFilterInjector.SendLootMessage(
                    $"[Loot Manager] Fishing: banked \"{name}\".", "lightblue");
                return false; // block inventory add — item went to bank
            }

            // ── Blacklist mode ────────────────────────────────────────────────
            if (lootMethod == "Blacklist" && BlacklistLoot.ShouldLoot(item, 1))
            {
                ChatFilterInjector.SendLootMessage(
                    $"[Loot Manager] Fishing: discarded \"{name}\" (blacklisted).", "grey");
                return false;
            }

            // ── Whitelist mode ────────────────────────────────────────────────
            if (lootMethod == "Whitelist" && !WhitelistLoot.ShouldLoot(item, 1))
            {
                ChatFilterInjector.SendLootMessage(
                    $"[Loot Manager] Fishing: discarded \"{name}\" (not on whitelist).", "grey");
                return false;
            }

            return true; // Standard mode or item passed filters — keep it
        }

        // ── Patches ───────────────────────────────────────────────────────────

        [HarmonyPatch(typeof(Inventory), "AddItemToInv", new[] { typeof(Item) })]
        public static class AddItemToInv_Patch
        {
            public static bool Prefix(Item _item, ref bool __result)
            {
                try
                {
                    Item caughtItem;
                    if (!IsInFishingContext(out caughtItem)) return true;
                    if (_item != caughtItem) return true; // different item, not the catch

                    if (_inFilter) return true; // reentrancy guard
                    _inFilter = true;
                    bool allow;
                    try   { allow = ShouldAddToInventory(_item); }
                    finally { _inFilter = false; }

                    if (!allow)
                    {
                        __result = true; // tell the game it "succeeded" so ForceItemToInv isn't called
                        return false;    // skip original
                    }
                }
                catch (System.Exception ex)
                {
                    if (Plugin.Log != null)
                        Plugin.Log.LogError("[Loot Manager] FishingPatch.AddItemToInv error: " + ex);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Inventory), "ForceItemToInv", new[] { typeof(Item) })]
        public static class ForceItemToInv_Patch
        {
            public static bool Prefix(Item _item)
            {
                try
                {
                    Item caughtItem;
                    if (!IsInFishingContext(out caughtItem)) return true;
                    if (_item != caughtItem) return true;

                    // ForceItemToInv is the inventory-full fallback.
                    // If filters say discard, skip it — item is simply lost
                    // (same as if the inventory was full and nothing was done).
                    // ShouldAddToInventory would have already fired and sent a
                    // chat message from the AddItemToInv prefix, so no double-message.
                    if (_inFilter) return true;
                    _inFilter = true;
                    bool allow;
                    try   { allow = ShouldAddToInventory(_item); }
                    finally { _inFilter = false; }
                    if (!allow)
                        return false;
                }
                catch (System.Exception ex)
                {
                    if (Plugin.Log != null)
                        Plugin.Log.LogError("[Loot Manager] FishingPatch.ForceItemToInv error: " + ex);
                }
                return true;
            }
        }
    }
}