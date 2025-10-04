using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HarmonyLib;

namespace LootManager
{
    /// <summary>
    /// Manages the Manager Slot UI prefab (managerSlotPanel) and wires up special slots like Blacklist.
    /// </summary>
    public class ManagerSlotController
    {
        private static GameObject _managerSlotPrefab;

        // Panels
        private static GameObject _managerSlotPanel;
        private static GameObject _panelBG;
        private static GameObject _managerPan;

        // Slots
        private static GameObject _BlacklistSlot;
        private static GameObject _BlacklistSlotItemIcon;
        private static GameObject _BankSlot; // reserved for later

        // Buttons
        private static Button _lootuiBtn;
        private static Button _bankBtn;
        private static Button _auctionBtn;

        public static void Initialize(GameObject managerSlotPrefab)
        {
            _managerSlotPrefab = managerSlotPrefab;

            _managerSlotPanel = managerSlotPrefab;
            _panelBG = Find("panelBG")?.gameObject;
            _managerPan = Find("panelBG/managerPan")?.gameObject;

            // Buttons
            SetupLootUIButton();
            SetupBankButton();
            SetupAuctionButton();

            // Special drop slots
            SetupBlacklistSlot();
            // Bank slot comes later once Blacklist is perfect:
            // SetupBanklistSlot();
        }

        // -------------------------
        // Buttons
        // -------------------------

        private static void SetupLootUIButton()
        {
            _lootuiBtn = Find("panelBG/managerPan/lootuiBtn")?.GetComponent<Button>();

            if (_lootuiBtn != null)
            {
                _lootuiBtn.onClick.RemoveAllListeners();
                _lootuiBtn.onClick.AddListener(() =>
                {
                    if (LootUI.Instance != null)
                        LootUI.Instance.ToggleUI();
                });
            }
            else
            {
                Debug.LogWarning("[ManagerSlotController] lootuiBtn not found in managerSlotPanel.");
            }
        }

        private static void SetupBankButton()
        {
            _bankBtn = Find("panelBG/managerPan/bankBtn")?.GetComponent<Button>();

            if (_bankBtn != null)
            {
                _bankBtn.onClick.RemoveAllListeners();
                _bankBtn.onClick.AddListener(() =>
                {
                    if (GameData.ItemOnCursor == null || GameData.ItemOnCursor == GameData.PlayerInv.Empty)
                    {
                        GameData.BankUI.OpenBank(GameData.PlayerControl.transform.position);
                    }
                    else
                    {
                        UpdateSocialLog.LogAdd("Remove item from cursor before interacting with a vendor.", "yellow");
                    }
                });
            }
        }

        private static void SetupAuctionButton()
        {
            _auctionBtn = Find("panelBG/managerPan/auctionBtn")?.GetComponent<Button>();

            if (_auctionBtn != null)
            {
                _auctionBtn.onClick.RemoveAllListeners();
                _auctionBtn.onClick.AddListener(() =>
                {
                    if (GameData.ItemOnCursor == null || GameData.ItemOnCursor == GameData.PlayerInv.Empty)
                    {
                        GameData.AHUI.OpenAuctionHouse(GameData.PlayerControl.transform.position);
                    }
                    else
                    {
                        UpdateSocialLog.LogAdd("Remove item from cursor before interacting with a vendor.", "yellow");
                    }
                });
            }
        }

        // -------------------------
        // Blacklist Slot (Drop to add & destroy)
        // -------------------------

        private static void SetupBlacklistSlot()
        {
            // Your prefab path: managerSlotPanel -> panelBG -> BlacklistSlot
            _BlacklistSlot = Find("panelBG/managerPan/BlacklistSlot")?.gameObject;
            _BlacklistSlotItemIcon = Find("panelBG/managerPan/BlacklistSlot/ItemIcon")?.gameObject;

            if (_BlacklistSlot == null)
            {
                Debug.LogWarning("[ManagerSlotController] BlacklistSlot not found at 'panelBG/BlacklistSlot'.");
                return;
            }

            EnsureBlacklistDropTarget(_BlacklistSlotItemIcon);
            Debug.Log("[ManagerSlotController] BlacklistSlot ready.");
        }

        /// <summary>
        /// Ensures the given GO behaves like a valid ItemIcon drop target and is marked as our Blacklist zone.
        /// </summary>
        private static void EnsureBlacklistDropTarget(GameObject slotGO)
        {
            slotGO.tag = "ItemSlot";
            
            // 1) Ensure a subtle Image (ItemIcon.Awake() expects an Image on the same GO)
            var img = slotGO.GetComponent<Image>();
            if (img == null)
            {
                Debug.LogWarning("[ManagerSlotController] 'BlacklistSlot/Icon' Image not found'.");
                return;
            }

            // 2) Ensure a 2D trigger collider so ItemIcon.OnTriggerStay2D sees it
            var col = slotGO.GetComponent<BoxCollider2D>();
            if (col == null) col = slotGO.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            // 5) Ensure ItemIcon exists and is configured as a neutral dummy slot
            var icon = slotGO.GetComponent<ItemIcon>();
            if (icon == null) icon = slotGO.AddComponent<ItemIcon>();

            icon.ThisSlotType = Item.SlotType.General;
            icon.VendorSlot = false;
            icon.LootSlot = false;
            icon.BankSlot = false;
            icon.TrashSlot = false;
            icon.PlayerOwned = false;
            icon.MouseSlot = false;
            icon.CanTakeBlessedItem = true;
            icon.NotInInventory = true;
            icon.Quantity = 1;

            if (icon.MyItem == null)
                icon.MyItem = GameData.PlayerInv.Empty;

            if (icon.QuantityBox != null)
                icon.QuantityBox.SetActive(false);

            icon.UpdateSlotImage();

            // 6) Add our marker so the Harmony prefix can detect this is the Blacklist drop zone
            if (slotGO.GetComponent<BlacklistDropZoneMarker>() == null)
                slotGO.AddComponent<BlacklistDropZoneMarker>();
        }

        // -------------------------
        // Utilities
        // -------------------------

        private static Transform Find(string path)
        {
            if (_managerSlotPrefab == null) return null;
            if (string.IsNullOrEmpty(path)) return _managerSlotPrefab.transform;
            return _managerSlotPrefab.transform.Find(path);
        }
    }

    /// <summary>
    /// Simple marker used to identify our special Blacklist drop zone.
    /// </summary>
    public class BlacklistDropZoneMarker : MonoBehaviour
    {
    }

    // ======================================================================
    // Harmony: Intercept release on ItemIcon drag. If target is our marker,
    // add to blacklist and DESTROY the item (clear mouse; leave origin empty).
    // ======================================================================

    [HarmonyPatch(typeof(ItemIcon), nameof(ItemIcon.InteractItemSlot))]
    public static class ItemIcon_InteractItemSlot_BlacklistPrefix
    {
        public static bool Prefix(ItemIcon __instance)
        {
            try
            {
                // Do we actually have an item on the cursor?
                if (GameData.MouseSlot == null ||
                    GameData.MouseSlot.MyItem == null ||
                    GameData.MouseSlot.MyItem == GameData.PlayerInv.Empty)
                {
                    return true; // let vanilla continue
                }

                // Get SwapWith from the DRAGGING icon (MouseSlot), not from __instance
                ItemIcon target = GetSwapTargetFromMouseSlot(__instance);

                if (target == null)
                    return true;

                // Accept the marker on target or its parent (depending on where you put it)
                bool isBlacklist =
                    target.GetComponent<BlacklistDropZoneMarker>() != null ||
                    (target.transform.parent != null &&
                     target.transform.parent.GetComponent<BlacklistDropZoneMarker>() != null);

                if (!isBlacklist)
                    return true;

                // We handle it: add to blacklist, destroy cursor item, skip vanilla
                var draggedItem = GameData.MouseSlot.MyItem;
                string itemName = draggedItem.ItemName; // or draggedItem.Id if that’s your key

                bool added = false;
                if (!Plugin.Blacklist.Contains(itemName))
                {
                    Plugin.Blacklist.Add(itemName);
                    added = true;
                    LootBlacklist.SaveBlacklist();
                }

                // Clear cursor WITHOUT snapping back to MouseHomeSlot
                GameData.MouseSlot.MyItem = GameData.PlayerInv.Empty;
                GameData.MouseSlot.Quantity = 1;
                GameData.ItemOnCursor = null;
                GameData.MouseSlot.dragging = false;

                var fHome = typeof(ItemIcon).GetField("MouseHomeSlot", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fHome != null) fHome.SetValue(GameData.MouseSlot, null);

                GameData.MouseSlot.UpdateSlotImage();
                GameData.PlayerInv.UpdatePlayerInventory();

                if (added)
                    UpdateSocialLog.LogAdd($"[Loot Manager] Blacklisted \"{itemName}\" and destroyed it.", "grey");
                else
                    UpdateSocialLog.LogAdd($"[Loot Manager] \"{itemName}\" is already blacklisted. Destroyed it.",
                        "grey");

                GameData.PlayerAud.PlayOneShot(GameData.GM.GetComponent<Misc>().DropItem,
                    GameData.PlayerAud.volume / 2f * GameData.SFXVol);

                // Block vanilla InteractItemSlot (prevents "This item cannot go in this slot.")
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Loot Manager] InteractItemSlot blacklist prefix error: {ex}");
                return true; // fail-safe
            }
        }

        private static ItemIcon GetSwapTargetFromMouseSlot(ItemIcon fallbackSource)
        {
            // Primary: read private SwapWith from GameData.MouseSlot
            var fSwap = typeof(ItemIcon).GetField("SwapWith", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fSwap != null && GameData.MouseSlot != null)
            {
                var v = fSwap.GetValue(GameData.MouseSlot) as ItemIcon;
                if (v != null) return v;
            }

            // Fallback: try the __instance (sometimes also has SwapWith)
            if (fSwap != null && fallbackSource != null)
            {
                var v2 = fSwap.GetValue(fallbackSource) as ItemIcon;
                if (v2 != null) return v2;
            }

            // Last resort: small Physics2D overlap at cursor (since Physics2D module is referenced)
            var mp = Input.mousePosition;
            var world = Camera.main != null ? Camera.main.ScreenToWorldPoint(mp) : new Vector3(mp.x, mp.y, 0f);
            foreach (var h in Physics2D.OverlapPointAll(new Vector2(world.x, world.y)))
            {
                if (h != null && h.CompareTag("ItemSlot"))
                {
                    var slot = h.GetComponent<ItemIcon>();
                    if (slot != null) return slot;
                }
            }

            return null;
        }
    }
}
