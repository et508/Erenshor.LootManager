using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace LootManager
{
    public static class ManagerSlotController
    {
        [HarmonyPatch(typeof(ItemIcon), nameof(ItemIcon.InteractItemSlot))]
        public static class ItemIcon_InteractItemSlot_DropZonesPrefix
        {
            public static bool Prefix(ItemIcon __instance)
            {
                try
                {
                    if (GameData.MouseSlot == null ||
                        GameData.MouseSlot.MyItem == null ||
                        GameData.MouseSlot.MyItem == GameData.PlayerInv.Empty)
                        return true;

                    ItemIcon target = GetSwapTarget(__instance);
                    if (target == null) return true;

                    bool isBlacklist   = target.GetComponent<BlacklistDropZoneMarker>()   != null;
                    bool isBanklist    = target.GetComponent<BanklistDropZoneMarker>()    != null;
                    bool isJunklist    = target.GetComponent<JunklistDropZoneMarker>()    != null;
                    bool isAuctionlist = target.GetComponent<AuctionlistDropZoneMarker>() != null;

                    if (!isBlacklist && !isBanklist && !isJunklist && !isAuctionlist)
                        return true;

                    Item item = GameData.MouseSlot.MyItem;
                    int  qty  = Mathf.Max(1, GameData.MouseSlot.Quantity);

                    if (item == null || item == GameData.PlayerInv.Empty) return true;

                    if (isBlacklist)   { HandleBlacklist(item);        ClearCursor();  GameData.PlayerInv.UpdatePlayerInventory(); return false; }
                    if (isBanklist)    { HandleBanklist(item, qty);    ClearCursor();  GameData.PlayerInv.UpdatePlayerInventory(); return false; }
                    if (isJunklist)    { HandleJunklist(item);         ReturnCursor(); GameData.PlayerInv.UpdatePlayerInventory(); return false; }
                    if (isAuctionlist) { HandleAuctionlist(item);      ClearCursor();  GameData.PlayerInv.UpdatePlayerInventory(); return false; }

                    return true;
                }
                catch (Exception ex)
                {
                    if (Plugin.Log != null) Plugin.Log.LogError("[Loot Manager] Drop zone prefix error: " + ex);
                    return true;
                }
            }

            private static void HandleBlacklist(Item item)
            {
                string key   = item.ItemName;
                bool   added = false;
                if (!Plugin.Blacklist.Contains(key))
                {
                    Plugin.Blacklist.Add(key);
                    LootBlacklist.SaveBlacklist();
                    added = true;
                }
                ChatFilterInjector.SendLootMessage(added
                    ? $"[Loot Manager] Blacklisted \"{key}\" and destroyed it."
                    : $"[Loot Manager] \"{key}\" is already blacklisted. Destroyed it.", "grey");
                PlayDropSound();
            }

            private static void HandleBanklist(Item item, int qty)
            {
                string key   = item.ItemName;
                bool   added = false;
                if (!Plugin.Banklist.Contains(key))
                {
                    Plugin.Banklist.Add(key);
                    LootBanklist.SaveBanklist();
                    added = true;
                }
                ChatFilterInjector.SendLootMessage(added
                    ? $"[Loot Manager] Added \"{key}\" to banklist."
                    : $"[Loot Manager] \"{key}\" already on banklist.", "lightblue");
                BankLoot.DepositLoot(new BankLoot.LootEntry[] { new BankLoot.LootEntry(item.Id, qty, item.ItemName) });
                PlayDropSound();
            }

            private static void HandleJunklist(Item item)
            {
                string key   = item.ItemName;
                bool   added = false;
                if (!Plugin.Junklist.Contains(key))
                {
                    Plugin.Junklist.Add(key);
                    LootJunklist.SaveJunklist();
                    added = true;
                }
                ChatFilterInjector.SendLootMessage(added
                    ? $"[Loot Manager] Added \"{key}\" to junklist."
                    : $"[Loot Manager] \"{key}\" already on junklist.", "yellow");
                PlayDropSound();
            }

            private static void HandleAuctionlist(Item item)
            {
                string key   = item.ItemName;
                bool   added = false;
                if (!Plugin.Auctionlist.Contains(key))
                {
                    Plugin.Auctionlist.Add(key);
                    LootAuctionlist.SaveAuctionlist();
                    added = true;
                }
                ChatFilterInjector.SendLootMessage(added
                    ? $"[Loot Manager] Added \"{key}\" to auctionlist."
                    : $"[Loot Manager] \"{key}\" already on auctionlist.", "yellow");

                if (item.ItemValue > 0 && !item.NoTradeNoDestroy)
                {
                    AuctionLoot.TryListItem(item);
                }
                PlayDropSound();
            }

            private static void PlayDropSound()
            {
                if (GameData.PlayerAud != null && GameData.GM != null)
                {
                    var misc = GameData.GM.GetComponent<Misc>();
                    if (misc != null)
                        GameData.PlayerAud.PlayOneShot(misc.DropItem,
                            GameData.PlayerAud.volume / 2f * GameData.SFXVol);
                }
            }

            private static void ClearCursor()
            {
                if (GameData.MouseSlot == null) return;

                var homeField = typeof(ItemIcon).GetField("MouseHomeSlot",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var homeSlot = homeField?.GetValue(GameData.MouseSlot) as ItemIcon;

                if (homeSlot != null && !homeSlot.BankSlot)
                {
                    homeSlot.MyItem   = GameData.PlayerInv.Empty;
                    homeSlot.Quantity = 1;
                    homeSlot.UpdateSlotImage();
                }

                homeField?.SetValue(GameData.MouseSlot, null);
                GameData.MouseSlot.MyItem   = GameData.PlayerInv.Empty;
                GameData.MouseSlot.Quantity = 1;
                GameData.ItemOnCursor       = null;
                GameData.MouseSlot.dragging = false;
                GameData.MouseSlot.UpdateSlotImage();
            }

            /// <summary>
            /// Returns the dragged item to its original inventory slot.
            /// Used for drop zones that only register the item name (Junklist, Auctionlist)
            /// without actually removing the item from the player's inventory.
            /// </summary>
            private static void ReturnCursor()
            {
                if (GameData.MouseSlot == null) return;

                var homeField = typeof(ItemIcon).GetField("MouseHomeSlot",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var homeSlot = homeField?.GetValue(GameData.MouseSlot) as ItemIcon;

                // Put the item back in its home slot
                if (homeSlot != null && !homeSlot.BankSlot)
                {
                    homeSlot.MyItem   = GameData.MouseSlot.MyItem;
                    homeSlot.Quantity = GameData.MouseSlot.Quantity;
                    homeSlot.UpdateSlotImage();
                }

                homeField?.SetValue(GameData.MouseSlot, null);
                GameData.MouseSlot.MyItem   = GameData.PlayerInv.Empty;
                GameData.MouseSlot.Quantity = 1;
                GameData.ItemOnCursor       = null;
                GameData.MouseSlot.dragging = false;
                GameData.MouseSlot.UpdateSlotImage();
            }

            private static ItemIcon GetSwapTarget(ItemIcon fallback)
            {
                FieldInfo fSwap = typeof(ItemIcon).GetField("SwapWith",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (fSwap != null && GameData.MouseSlot != null)
                {
                    var v = fSwap.GetValue(GameData.MouseSlot) as ItemIcon;
                    if (v != null) return v;
                }
                if (fSwap != null && fallback != null)
                {
                    var v2 = fSwap.GetValue(fallback) as ItemIcon;
                    if (v2 != null) return v2;
                }
                return null;
            }
        }
    }
}