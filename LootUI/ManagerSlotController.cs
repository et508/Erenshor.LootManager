using System;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public class ManagerSlotController
    {
        private static GameObject _managerSlotPrefabRoot;
        
        private static GameObject _managerSlotPanel;
        private static GameObject _panelBG;
        private static GameObject _managerPan;
        
        private static GameObject _blacklistSlot;
        private static GameObject _blacklistSlotItemIcon;

        private static GameObject _banklistSlot;
        private static GameObject _banklistSlotItemIcon;
        
        private static Button _lootuiBtn;
        private static Button _bankBtn;
        private static Button _auctionBtn;

        public static void Initialize(GameObject managerSlotPrefabRoot)
        {
            _managerSlotPrefabRoot = managerSlotPrefabRoot;
            if (_managerSlotPrefabRoot == null)
            {
                Debug.LogError("[ManagerSlotController] managerSlotPrefabRoot is null.");
                return;
            }

            _managerSlotPanel = _managerSlotPrefabRoot;

            _panelBG    = UICommon.Find(_managerSlotPrefabRoot, "panelBG") != null
                        ? UICommon.Find(_managerSlotPrefabRoot, "panelBG").gameObject
                        : null;

            _managerPan = UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan") != null
                        ? UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan").gameObject
                        : null;
            
            SetupLootUIButton();
            SetupBankButton();
            SetupAuctionButton();
            
            SetupBlacklistSlot();
            SetupBanklistSlot();
        }
        
        private static void SetupLootUIButton()
        {
            Transform t = UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan/lootuiBtn");
            _lootuiBtn = t != null ? t.GetComponent<Button>() : null;

            if (_lootuiBtn != null)
            {
                _lootuiBtn.onClick.RemoveAllListeners();
                _lootuiBtn.onClick.AddListener(delegate
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
            Transform t = UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan/bankBtn");
            _bankBtn = t != null ? t.GetComponent<Button>() : null;

            if (_bankBtn != null)
            {
                _bankBtn.onClick.RemoveAllListeners();
                _bankBtn.onClick.AddListener(delegate
                {
                    if (GameData.ItemOnCursor == null || GameData.ItemOnCursor == GameData.PlayerInv.Empty)
                    {
                        if (GameData.PlayerControl != null && GameData.BankUI != null)
                            GameData.BankUI.OpenBank(GameData.PlayerControl.transform.position);
                    }
                    else
                    {
                        UpdateSocialLog.LogAdd("Remove item from cursor before interacting with a vendor.", "yellow");
                    }
                });
            }
            else
            {
                Debug.LogWarning("[ManagerSlotController] bankBtn not found in managerSlotPanel.");
            }
        }

        private static void SetupAuctionButton()
        {
            Transform t = UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan/auctionBtn");
            _auctionBtn = t != null ? t.GetComponent<Button>() : null;

            if (_auctionBtn != null)
            {
                _auctionBtn.onClick.RemoveAllListeners();
                _auctionBtn.onClick.AddListener(delegate
                {
                    if (GameData.ItemOnCursor == null || GameData.ItemOnCursor == GameData.PlayerInv.Empty)
                    {
                        if (GameData.PlayerControl != null && GameData.AHUI != null)
                            GameData.AHUI.OpenAuctionHouse(GameData.PlayerControl.transform.position);
                    }
                    else
                    {
                        UpdateSocialLog.LogAdd("Remove item from cursor before interacting with a vendor.", "yellow");
                    }
                });
            }
            else
            {
                Debug.LogWarning("[ManagerSlotController] auctionBtn not found in managerSlotPanel.");
            }
        }
        
        private static void SetupBlacklistSlot()
        {
            Transform slotT = UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan/BlacklistSlot");
            _blacklistSlot = slotT != null ? slotT.gameObject : null;

            Transform iconT = UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan/BlacklistSlot/ItemIcon");
            _blacklistSlotItemIcon = iconT != null ? iconT.gameObject : null;

            if (_blacklistSlot == null || _blacklistSlotItemIcon == null)
            {
                Debug.LogWarning("[ManagerSlotController] BlacklistSlot or child ItemIcon not found.");
                return;
            }

            EnsureDropTarget(_blacklistSlotItemIcon, typeof(BlacklistDropZoneMarker));
            Debug.Log("[ManagerSlotController] BlacklistSlot ready.");
        }
        
        private static void SetupBanklistSlot()
        {
            Transform slotT = UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan/BanklistSlot");
            _banklistSlot = slotT != null ? slotT.gameObject : null;

            Transform iconT = UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan/BanklistSlot/ItemIcon");
            _banklistSlotItemIcon = iconT != null ? iconT.gameObject : null;

            if (_banklistSlot == null || _banklistSlotItemIcon == null)
            {
                Debug.LogWarning("[ManagerSlotController] BanklistSlot or child ItemIcon not found.");
                return;
            }

            EnsureDropTarget(_banklistSlotItemIcon, typeof(BanklistDropZoneMarker));
            Debug.Log("[ManagerSlotController] BanklistSlot ready.");
        }
        
        private static void EnsureDropTarget(GameObject itemIconChild, Type wantMarker)
        {
            if (itemIconChild == null) return;
            
            itemIconChild.tag = "ItemSlot";
            
            Image img = itemIconChild.GetComponent<Image>();
            if (img == null)
            {
                img = itemIconChild.AddComponent<Image>();
                if (img != null)
                {
                    img.raycastTarget = false;
                    img.color = new Color(1f, 1f, 1f, 0f); 
                }
            }
            
            BoxCollider2D col = itemIconChild.GetComponent<BoxCollider2D>();
            if (col == null) col = itemIconChild.AddComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = true;
            
            Rigidbody2D rb = itemIconChild.GetComponent<Rigidbody2D>();
            if (rb == null) rb = itemIconChild.AddComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.useFullKinematicContacts = true;
            }
            
            ItemIcon icon = itemIconChild.GetComponent<ItemIcon>();
            if (icon == null) icon = itemIconChild.AddComponent<ItemIcon>();
            if (icon != null)
            {
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
            }
            
            if (itemIconChild.GetComponent(wantMarker) == null)
                itemIconChild.AddComponent(wantMarker);
        }
        

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
                    {
                        return true;
                    }
                    
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

                    Item draggedItem = GameData.MouseSlot.MyItem;
                    int draggedQty  = Mathf.Max(1, GameData.MouseSlot.Quantity);
                    if (draggedItem == null || draggedItem == GameData.PlayerInv.Empty)
                        return true;

                    if (isBlacklist)
                    {
                        HandleBlacklist(draggedItem);
                        ClearCursorNoSnap();
                        GameData.PlayerInv.UpdatePlayerInventory();
                        return false;
                    }

                    if (isBanklist)
                    {
                        HandleBanklistAndDeposit(draggedItem, draggedQty);
                        ClearCursorNoSnap();
                        GameData.PlayerInv.UpdatePlayerInventory();
                        return false;
                    }

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
                string key = item.ItemName;

                bool added = false;
                if (!Plugin.Blacklist.Contains(key))
                {
                    Plugin.Blacklist.Add(key);
                    added = true;
                    LootBlacklist.SaveBlacklist();
                }

                if (added)
                    UpdateSocialLog.LogAdd("[Loot Manager] Blacklisted \"" + key + "\" and destroyed it.", "grey");
                else
                    UpdateSocialLog.LogAdd("[Loot Manager] \"" + key + "\" is already blacklisted. Destroyed it.", "grey");

                if (GameData.PlayerAud != null && GameData.GM != null)
                {
                    Misc misc = GameData.GM.GetComponent<Misc>();
                    if (misc != null)
                        GameData.PlayerAud.PlayOneShot(misc.DropItem, GameData.PlayerAud.volume / 2f * GameData.SFXVol);
                }
            }

            private static void HandleBanklistAndDeposit(Item item, int qty)
            {
                string key = item.ItemName;
                bool added = false;
                if (!Plugin.Banklist.Contains(key))
                {
                    Plugin.Banklist.Add(key);
                    added = true;
                    LootBanklist.SaveBanklist();
                }

                if (added)
                    UpdateSocialLog.LogAdd("[Loot Manager] Added \"" + key + "\" to banklist.", "lightblue");
                
                string id = item.Id;
                BankLoot.LootEntry entry = new BankLoot.LootEntry(id, qty, item.ItemName);
                BankLoot.DepositLoot(new BankLoot.LootEntry[] { entry });

                if (GameData.PlayerAud != null && GameData.GM != null)
                {
                    Misc misc = GameData.GM.GetComponent<Misc>();
                    if (misc != null)
                        GameData.PlayerAud.PlayOneShot(misc.DropItem, GameData.PlayerAud.volume / 2f * GameData.SFXVol);
                }
            }

            private static void ClearCursorNoSnap()
            {
                if (GameData.MouseSlot == null) return;

                GameData.MouseSlot.MyItem   = GameData.PlayerInv.Empty;
                GameData.MouseSlot.Quantity = 1;
                GameData.ItemOnCursor       = null;
                GameData.MouseSlot.dragging = false;

                FieldInfo fHome = typeof(ItemIcon).GetField("MouseHomeSlot", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fHome != null) fHome.SetValue(GameData.MouseSlot, null);

                GameData.MouseSlot.UpdateSlotImage();
            }

            private static ItemIcon GetSwapTargetFromMouseSlot(ItemIcon fallbackSource)
            {
                FieldInfo fSwap = typeof(ItemIcon).GetField("SwapWith", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fSwap != null && GameData.MouseSlot != null)
                {
                    ItemIcon v = fSwap.GetValue(GameData.MouseSlot) as ItemIcon;
                    if (v != null) return v;
                }

                if (fSwap != null && fallbackSource != null)
                {
                    ItemIcon v2 = fSwap.GetValue(fallbackSource) as ItemIcon;
                    if (v2 != null) return v2;
                }

                return null;
            }
        }
    }
    
    public class BlacklistDropZoneMarker : MonoBehaviour { }
    
    public class BanklistDropZoneMarker : MonoBehaviour { }
}
