using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

namespace LootManager
{
    /// <summary>
    /// Manages the Manager Slot UI prefab (managerSlotPanel) and wires up special slots like Blacklist/Banklist.
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

        private static GameObject _BanklistSlot;
        private static GameObject _BanklistSlotItemIcon;

        // Buttons
        private static Button _lootuiBtn;
        private static Button _bankBtn;
        private static Button _auctionBtn;

        public static void Initialize(GameObject managerSlotPrefab)
        {
            _managerSlotPrefab = managerSlotPrefab;

            _managerSlotPanel = managerSlotPrefab;
            _panelBG    = Find("panelBG")?.gameObject;
            _managerPan = Find("panelBG/managerPan")?.gameObject;

            // Buttons
            SetupLootUIButton();
            SetupBankButton();
            SetupAuctionButton();

            // Special drop slots
            SetupBlacklistSlot();
            SetupBanklistSlot();
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
            _BlacklistSlot          = Find("panelBG/managerPan/BlacklistSlot")?.gameObject;
            _BlacklistSlotItemIcon  = Find("panelBG/managerPan/BlacklistSlot/ItemIcon")?.gameObject;

            if (_BlacklistSlot == null || _BlacklistSlotItemIcon == null)
            {
                Debug.LogWarning("[ManagerSlotController] BlacklistSlot or child ItemIcon not found.");
                return;
            }

            EnsureDropTarget(_BlacklistSlotItemIcon, wantMarker: typeof(BlacklistDropZoneMarker));
            Debug.Log("[ManagerSlotController] BlacklistSlot ready.");
        }

        // -------------------------
        // Banklist Slot (Drop to add & deposit via BankLoot)
        // -------------------------

        private static void SetupBanklistSlot()
        {
            _BanklistSlot         = Find("panelBG/managerPan/BanklistSlot")?.gameObject;
            _BanklistSlotItemIcon = Find("panelBG/managerPan/BanklistSlot/ItemIcon")?.gameObject;

            if (_BanklistSlot == null || _BanklistSlotItemIcon == null)
            {
                Debug.LogWarning("[ManagerSlotController] BanklistSlot or child ItemIcon not found.");
                return;
            }

            EnsureDropTarget(_BanklistSlotItemIcon, wantMarker: typeof(BanklistDropZoneMarker));
            Debug.Log("[ManagerSlotController] BanklistSlot ready.");
        }

        /// <summary>
        /// Ensures the given GO (the child ItemIcon node) behaves like a valid ItemIcon drop target and is marked by a zone marker.
        /// </summary>
        private static void EnsureDropTarget(GameObject itemIconChild, Type wantMarker)
        {
            // Tag must be ItemSlot at runtime (bundle tag indexes can remap)
            itemIconChild.tag = "ItemSlot";

            // Needs an Image (vanilla child has its own Image)
            var img = itemIconChild.GetComponent<Image>();
            if (img == null)
            {
                img = itemIconChild.AddComponent<Image>();
                img.raycastTarget = false;
                img.color = new Color(1, 1, 1, 0); // invisible helper if needed
            }

            // Collider (trigger)
            var col = itemIconChild.GetComponent<BoxCollider2D>();
            if (col == null) col = itemIconChild.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            // Match vanilla: kinematic RB2D improves trigger reliability with UI
            var rb = itemIconChild.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = itemIconChild.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.useFullKinematicContacts = true;
            }

            // ItemIcon
            var icon = itemIconChild.GetComponent<ItemIcon>();
            if (icon == null) icon = itemIconChild.AddComponent<ItemIcon>();

            icon.ThisSlotType       = Item.SlotType.General;
            icon.VendorSlot         = false;
            icon.LootSlot           = false;
            icon.BankSlot           = false;
            icon.TrashSlot          = false;
            icon.PlayerOwned        = false;
            icon.MouseSlot          = false;
            icon.CanTakeBlessedItem = true;
            icon.NotInInventory     = true;
            icon.Quantity           = 1;

            if (icon.MyItem == null)
                icon.MyItem = GameData.PlayerInv.Empty;

            if (icon.QuantityBox != null)
                icon.QuantityBox.SetActive(false);

            icon.UpdateSlotImage();

            // Marker
            if (itemIconChild.GetComponent(wantMarker) == null)
                itemIconChild.AddComponent(wantMarker);
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

    /// <summary>Marker used to identify the Blacklist drop zone.</summary>
    public class BlacklistDropZoneMarker : MonoBehaviour { }

    /// <summary>Marker used to identify the Banklist drop zone.</summary>
    public class BanklistDropZoneMarker : MonoBehaviour { }

    // ======================================================================
    // Harmony: Intercept release on ItemIcon drag.
    // If target is Blacklist -> add to blacklist + destroy.
    // If target is Banklist  -> add to banklist + deposit via BankLoot (no bank UI needed).
    // ======================================================================

    [HarmonyPatch(typeof(ItemIcon), nameof(ItemIcon.InteractItemSlot))]
    public static class ItemIcon_InteractItemSlot_DropZonesPrefix
    {
        public static bool Prefix(ItemIcon __instance)
        {
            try
            {
                // Must have an item on cursor
                if (GameData.MouseSlot == null ||
                    GameData.MouseSlot.MyItem == null ||
                    GameData.MouseSlot.MyItem == GameData.PlayerInv.Empty)
                {
                    return true;
                }

                // Resolve target from the DRAGGING icon (MouseSlot)
                ItemIcon target = GetSwapTargetFromMouseSlot(__instance);
                if (target == null)
                    return true;

                bool isBlacklist =
                    target.GetComponent<BlacklistDropZoneMarker>() != null ||
                    (target.transform.parent != null && target.transform.parent.GetComponent<BlacklistDropZoneMarker>() != null);

                bool isBanklist =
                    target.GetComponent<BanklistDropZoneMarker>() != null ||
                    (target.transform.parent != null && target.transform.parent.GetComponent<BanklistDropZoneMarker>() != null);

                if (!isBlacklist && !isBanklist)
                    return true;

                var draggedItem = GameData.MouseSlot.MyItem;
                int draggedQty  = Mathf.Max(1, GameData.MouseSlot.Quantity);
                if (draggedItem == null || draggedItem == GameData.PlayerInv.Empty)
                    return true;

                if (isBlacklist)
                {
                    HandleBlacklist(draggedItem);
                    ClearCursorNoSnap();
                    GameData.PlayerInv.UpdatePlayerInventory();
                    return false; // swallow vanilla
                }

                if (isBanklist)
                {
                    HandleBanklistAndDeposit(draggedItem, draggedQty);
                    ClearCursorNoSnap();
                    GameData.PlayerInv.UpdatePlayerInventory();
                    return false; // swallow vanilla
                }

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Loot Manager] Drop zone prefix error: {ex}");
                return true;
            }
        }

        private static void HandleBlacklist(Item item)
        {
            string key = item.ItemName; // or item.Id if preferred

            bool added = false;
            if (!Plugin.Blacklist.Contains(key))
            {
                Plugin.Blacklist.Add(key);
                added = true;
                LootBlacklist.SaveBlacklist();
            }

            if (added)
                UpdateSocialLog.LogAdd($"[Loot Manager] Blacklisted \"{key}\" and destroyed it.", "grey");
            else
                UpdateSocialLog.LogAdd($"[Loot Manager] \"{key}\" is already blacklisted. Destroyed it.", "grey");

            GameData.PlayerAud.PlayOneShot(GameData.GM.GetComponent<Misc>().DropItem,
                GameData.PlayerAud.volume / 2f * GameData.SFXVol);
        }

        private static void HandleBanklistAndDeposit(Item item, int qty)
        {
            // 1) Add to persistent banklist
            string key = item.ItemName; // list key (human-readable)
            bool added = false;
            if (!Plugin.Banklist.Contains(key))
            {
                Plugin.Banklist.Add(key);
                added = true;
                LootBanklist.SaveBanklist();
            }

            if (added)
                UpdateSocialLog.LogAdd($"[Loot Manager] Added \"{key}\" to banklist.", "lightblue");

            // 2) Deposit via BankLoot (works even if bank UI is closed; honors Page Mode)
            //    Important: BankLoot expects the Item **Id** to match storage format.
            string id = item.Id;
            var entry = new BankLoot.LootEntry(id, qty, item.ItemName);
            BankLoot.DepositLoot(new[] { entry });

            // Audio feedback (optional here; BankLoot logs deposits per item)
            GameData.PlayerAud.PlayOneShot(GameData.GM.GetComponent<Misc>().DropItem,
                GameData.PlayerAud.volume / 2f * GameData.SFXVol);
        }

        private static void ClearCursorNoSnap()
        {
            // Clear cursor WITHOUT snapping back to MouseHomeSlot
            GameData.MouseSlot.MyItem   = GameData.PlayerInv.Empty;
            GameData.MouseSlot.Quantity = 1;
            GameData.ItemOnCursor       = null;
            GameData.MouseSlot.dragging = false;

            var fHome = typeof(ItemIcon).GetField("MouseHomeSlot", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fHome != null) fHome.SetValue(GameData.MouseSlot, null);

            GameData.MouseSlot.UpdateSlotImage();
        }

        private static ItemIcon GetSwapTargetFromMouseSlot(ItemIcon fallbackSource)
        {
            var fSwap = typeof(ItemIcon).GetField("SwapWith", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fSwap != null && GameData.MouseSlot != null)
            {
                var v = fSwap.GetValue(GameData.MouseSlot) as ItemIcon;
                if (v != null) return v;
            }

            if (fSwap != null && fallbackSource != null)
            {
                var v2 = fSwap.GetValue(fallbackSource) as ItemIcon;
                if (v2 != null) return v2;
            }

            return null;
        }
    }
}
