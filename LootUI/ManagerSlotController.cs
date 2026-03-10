// ManagerSlotController.cs
// All drop-zone and slot logic is identical to the original.
// The only change: BuildAndInitialize(Transform parent) replaces the
// AssetBundle prefab instantiation — it creates the manager slot panel in code
// and then calls Initialize() as before.

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

        private static Toggle _addBanklistToggle;

        // ─────────────────────────────────────────────────────────────────────
        // Public entry point — builds the panel in code then wires everything up
        // ─────────────────────────────────────────────────────────────────────
        public static GameObject BuildAndInitialize(Transform parent)
        {
            // Build the manager slot panel hierarchy in code
            var panelRoot = BuildManagerSlotPanel(parent);

            // Delegate to the existing Initialize logic
            Initialize(panelRoot);

            return panelRoot;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Build the panel hierarchy
        // ─────────────────────────────────────────────────────────────────────
        private static GameObject BuildManagerSlotPanel(Transform parent)
        {
            // Root
            var root = new GameObject("managerSlotPanel");
            var rootRT = root.AddComponent<RectTransform>();
            rootRT.SetParent(parent, false);

            // panelBG
            var panelBGGO = new GameObject("panelBG");
            var panelBGRT = panelBGGO.AddComponent<RectTransform>();
            panelBGRT.SetParent(rootRT, false);
            panelBGRT.anchorMin        = Vector2.zero;
            panelBGRT.anchorMax        = Vector2.one;
            panelBGRT.offsetMin        = Vector2.zero;
            panelBGRT.offsetMax        = Vector2.zero;
            panelBGGO.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 0.95f);

            var panelOL = panelBGGO.AddComponent<Outline>();
            panelOL.effectColor    = new Color32(45, 49, 57, 255);
            panelOL.effectDistance = new Vector2(1f, -1f);

            // managerPan (inner layout)
            var managerPanGO = new GameObject("managerPan");
            var managerPanRT = managerPanGO.AddComponent<RectTransform>();
            managerPanRT.SetParent(panelBGRT, false);
            LootUIController.StretchFull(managerPanRT);

            var vl = managerPanGO.AddComponent<VerticalLayoutGroup>();
            vl.padding                = new RectOffset(8, 8, 8, 8);
            vl.spacing                = 6;
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;
            vl.childControlWidth      = true;
            vl.childControlHeight     = true;

            var csf = managerPanGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ── Loot UI button ────────────────────────────────────────────
            var lootuiBtnGO = new GameObject("lootuiBtn");
            var lootuiBtnRT = lootuiBtnGO.AddComponent<RectTransform>();
            lootuiBtnRT.SetParent(managerPanRT, false);
            lootuiBtnGO.AddComponent<Image>().color = new Color32(21, 24, 32, 255);
            var lootuiBtn = lootuiBtnGO.AddComponent<Button>();
            AddButtonLabel(lootuiBtnGO, "Loot UI");
            lootuiBtnGO.AddComponent<LayoutElement>().preferredHeight = 26;

            // ── Bank button ───────────────────────────────────────────────
            var bankBtnGO = new GameObject("bankBtn");
            bankBtnGO.AddComponent<RectTransform>().SetParent(managerPanRT, false);
            bankBtnGO.AddComponent<Image>().color = new Color32(21, 24, 32, 255);
            var bankBtn = bankBtnGO.AddComponent<Button>();
            AddButtonLabel(bankBtnGO, "Open Bank");
            bankBtnGO.AddComponent<LayoutElement>().preferredHeight = 26;

            // ── Auction button ────────────────────────────────────────────
            var auctionBtnGO = new GameObject("auctionBtn");
            auctionBtnGO.AddComponent<RectTransform>().SetParent(managerPanRT, false);
            auctionBtnGO.AddComponent<Image>().color = new Color32(21, 24, 32, 255);
            var auctionBtn = auctionBtnGO.AddComponent<Button>();
            AddButtonLabel(auctionBtnGO, "Auction House");
            auctionBtnGO.AddComponent<LayoutElement>().preferredHeight = 26;

            // Divider
            LootUIController.MakeDivider(managerPanRT);

            // ── Add-to-banklist toggle ────────────────────────────────────
            var bankToggleRow = new GameObject("bankToggleRow");
            bankToggleRow.AddComponent<RectTransform>().SetParent(managerPanRT, false);
            var btHL = bankToggleRow.AddComponent<HorizontalLayoutGroup>();
            btHL.spacing = 4;
            btHL.childForceExpandWidth  = false;
            btHL.childForceExpandHeight = false;
            btHL.childControlWidth  = true;
            btHL.childControlHeight = true;
            bankToggleRow.AddComponent<LayoutElement>().preferredHeight = 22;

            var addBanklistToggle = LootUIController.MakeToggle("addBanklistToggle", bankToggleRow.transform, "Add to Banklist");

            // Divider
            LootUIController.MakeDivider(managerPanRT);

            // ── Slot row ──────────────────────────────────────────────────
            var slotRow = new GameObject("slotRow");
            slotRow.AddComponent<RectTransform>().SetParent(managerPanRT, false);
            var srHL = slotRow.AddComponent<HorizontalLayoutGroup>();
            srHL.spacing = 8;
            srHL.childForceExpandWidth  = false;
            srHL.childForceExpandHeight = false;
            srHL.childControlWidth      = false;
            srHL.childControlHeight     = true;
            slotRow.AddComponent<LayoutElement>().preferredHeight = 42;

            var blacklistSlot = BuildSlot("BlacklistSlot", slotRow.transform, "Blacklist", new Color32(80, 20, 20, 200));
            var banklistSlot  = BuildSlot("BanklistSlot",  slotRow.transform, "Banklist",  new Color32(20, 30, 80, 200));

            return root;
        }

        private static GameObject BuildSlot(string name, Transform parent, string labelText, Color32 bgColour)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(40, 40);
            go.AddComponent<Image>().color = bgColour;

            var lbl = LootUIController.MakeTMP("SlotLabel", rt);
            var lblRT = lbl.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0, 0);
            lblRT.anchorMax = new Vector2(1, 0);
            lblRT.pivot     = new Vector2(0.5f, 0);
            lblRT.anchoredPosition = new Vector2(0, -14);
            lblRT.sizeDelta        = new Vector2(0, 14);
            lbl.text      = labelText;
            lbl.fontSize  = 8;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color     = LootUIController.C_TextMuted;

            var iconGO = new GameObject("ItemIcon");
            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.SetParent(rt, false);
            iconRT.anchorMin = new Vector2(0.1f, 0.1f);
            iconRT.anchorMax = new Vector2(0.9f, 0.9f);
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.color = new Color(1f, 1f, 1f, 0f);

            return go;
        }

        private static void AddButtonLabel(GameObject btn, string text)
        {
            var lblGO = new GameObject("Label");
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.SetParent(btn.transform, false);
            LootUIController.StretchFull(lblRT);
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.color     = LootUIController.C_TextPri;
            tmp.fontSize  = 11;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Initialize — identical logic to original, just receives the built root
        // ─────────────────────────────────────────────────────────────────────
        public static void Initialize(GameObject managerSlotPrefabRoot)
        {
            _managerSlotPrefabRoot = managerSlotPrefabRoot;
            if (_managerSlotPrefabRoot == null)
            {
                Debug.LogError("[ManagerSlotController] managerSlotPrefabRoot is null.");
                return;
            }

            _managerSlotPanel = _managerSlotPrefabRoot;

            _panelBG = UICommon.Find(_managerSlotPrefabRoot, "panelBG") != null
                     ? UICommon.Find(_managerSlotPrefabRoot, "panelBG").gameObject
                     : null;

            _managerPan = UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan") != null
                        ? UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan").gameObject
                        : null;

            SetupLootUIButton();
            SetupBankButton();
            SetupAuctionButton();
            SetupAddBanklistToggle();
            SetupBlacklistSlot();
            SetupBanklistSlot();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Setup methods — identical to original
        // ─────────────────────────────────────────────────────────────────────
        private static void SetupLootUIButton()
        {
            Transform t = UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan/lootuiBtn");
            _lootuiBtn = t != null ? t.GetComponent<Button>() : null;
            if (_lootuiBtn != null)
            {
                _lootuiBtn.onClick.RemoveAllListeners();
                _lootuiBtn.onClick.AddListener(() =>
                {
                    if (LootUI.Instance != null) LootUI.Instance.ToggleUI();
                });
            }
        }

        private static void SetupBankButton()
        {
            Transform t = UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan/bankBtn");
            _bankBtn = t != null ? t.GetComponent<Button>() : null;
            if (_bankBtn != null)
            {
                _bankBtn.onClick.RemoveAllListeners();
                _bankBtn.onClick.AddListener(() =>
                {
                    if (GameData.ItemOnCursor == null || GameData.ItemOnCursor == GameData.PlayerInv.Empty)
                    {
                        if (GameData.PlayerControl != null && GameData.BankUI != null)
                            GameData.BankUI.OpenBank(GameData.PlayerControl.transform.position);
                    }
                    else
                    {
                        ChatFilterInjector.SendLootMessage("Remove item from cursor before interacting with a vendor.", "yellow");
                    }
                });
            }
        }

        private static void SetupAuctionButton()
        {
            Transform t = UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan/auctionBtn");
            _auctionBtn = t != null ? t.GetComponent<Button>() : null;
            if (_auctionBtn != null)
            {
                _auctionBtn.onClick.RemoveAllListeners();
                _auctionBtn.onClick.AddListener(() =>
                {
                    if (GameData.ItemOnCursor == null || GameData.ItemOnCursor == GameData.PlayerInv.Empty)
                    {
                        if (GameData.PlayerControl != null && GameData.AHUI != null)
                            GameData.AHUI.OpenAuctionHouse(GameData.PlayerControl.transform.position);
                    }
                    else
                    {
                        ChatFilterInjector.SendLootMessage("Remove item from cursor before interacting with a vendor.", "yellow");
                    }
                });
            }
        }

        private static void SetupAddBanklistToggle()
        {
            Transform t = UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan/bankToggleRow/addBanklistToggle");
            _addBanklistToggle = t != null ? t.GetComponent<Toggle>() : null;

            if (_addBanklistToggle == null)
            {
                Debug.LogWarning("[ManagerSlotController] addBanklistToggle not found. Falling back to config only.");
                return;
            }

            try { _addBanklistToggle.SetIsOnWithoutNotify(Plugin.BankslotAddToList.Value); }
            catch { }

            _addBanklistToggle.onValueChanged.RemoveAllListeners();
            _addBanklistToggle.onValueChanged.AddListener(v =>
            {
                try { Plugin.BankslotAddToList.Value = v; }
                catch (Exception ex)
                {
                    if (Plugin.Log != null) Plugin.Log.LogWarning($"[Loot Manager] Failed to update BankslotAddToList: {ex.Message}");
                }
            });
        }

        private static void SetupBlacklistSlot()
        {
            Transform slotT = UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan/slotRow/BlacklistSlot");
            _blacklistSlot = slotT != null ? slotT.gameObject : null;

            Transform iconT = UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan/slotRow/BlacklistSlot/ItemIcon");
            _blacklistSlotItemIcon = iconT != null ? iconT.gameObject : null;

            if (_blacklistSlot == null || _blacklistSlotItemIcon == null)
            {
                Debug.LogWarning("[ManagerSlotController] BlacklistSlot or child ItemIcon not found.");
                return;
            }
            EnsureDropTarget(_blacklistSlotItemIcon, typeof(BlacklistDropZoneMarker));
        }

        private static void SetupBanklistSlot()
        {
            Transform slotT = UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan/slotRow/BanklistSlot");
            _banklistSlot = slotT != null ? slotT.gameObject : null;

            Transform iconT = UICommon.Find(_managerSlotPrefabRoot, "panelBG/managerPan/slotRow/BanklistSlot/ItemIcon");
            _banklistSlotItemIcon = iconT != null ? iconT.gameObject : null;

            if (_banklistSlot == null || _banklistSlotItemIcon == null)
            {
                Debug.LogWarning("[ManagerSlotController] BanklistSlot or child ItemIcon not found.");
                return;
            }
            EnsureDropTarget(_banklistSlotItemIcon, typeof(BanklistDropZoneMarker));
        }

        private static void EnsureDropTarget(GameObject itemIconChild, Type wantMarker)
        {
            if (itemIconChild == null) return;

            itemIconChild.tag = "ItemSlot";

            Image img = itemIconChild.GetComponent<Image>();
            if (img == null)
            {
                img = itemIconChild.AddComponent<Image>();
                if (img != null) { img.raycastTarget = false; img.color = new Color(1f, 1f, 1f, 0f); }
            }

            BoxCollider2D col = itemIconChild.GetComponent<BoxCollider2D>() ?? itemIconChild.AddComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = true;

            Rigidbody2D rb = itemIconChild.GetComponent<Rigidbody2D>() ?? itemIconChild.AddComponent<Rigidbody2D>();
            if (rb != null) { rb.bodyType = RigidbodyType2D.Kinematic; rb.gravityScale = 0f; rb.useFullKinematicContacts = true; }

            ItemIcon icon = itemIconChild.GetComponent<ItemIcon>() ?? itemIconChild.AddComponent<ItemIcon>();
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
                if (icon.MyItem == null) icon.MyItem = GameData.PlayerInv.Empty;
                if (icon.QuantityBox != null) icon.QuantityBox.SetActive(false);
                icon.UpdateSlotImage();
            }

            if (itemIconChild.GetComponent(wantMarker) == null)
                itemIconChild.AddComponent(wantMarker);
        }

        private static bool ShouldAddToBanklist()
        {
            if (_addBanklistToggle != null) return _addBanklistToggle.isOn;
            try { return Plugin.BankslotAddToList.Value; }
            catch { return false; }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Drop-zone Harmony patch — UNCHANGED from original
        // ─────────────────────────────────────────────────────────────────────
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

                    ItemIcon target = GetSwapTargetFromMouseSlot(__instance);
                    if (target == null) return true;

                    bool isBlacklist =
                        target.GetComponent<BlacklistDropZoneMarker>() != null ||
                        (target.transform.parent != null && target.transform.parent.GetComponent<BlacklistDropZoneMarker>() != null);

                    bool isBanklist =
                        target.GetComponent<BanklistDropZoneMarker>() != null ||
                        (target.transform.parent != null && target.transform.parent.GetComponent<BanklistDropZoneMarker>() != null);

                    if (!isBlacklist && !isBanklist) return true;

                    Item draggedItem = GameData.MouseSlot.MyItem;
                    int draggedQty   = Mathf.Max(1, GameData.MouseSlot.Quantity);
                    if (draggedItem == null || draggedItem == GameData.PlayerInv.Empty) return true;

                    if (isBlacklist) { HandleBlacklist(draggedItem); ClearCursorNoSnap(); GameData.PlayerInv.UpdatePlayerInventory(); return false; }
                    if (isBanklist)  { HandleBanklistAndDeposit(draggedItem, draggedQty, ShouldAddToBanklist()); ClearCursorNoSnap(); GameData.PlayerInv.UpdatePlayerInventory(); return false; }

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
                string key  = item.ItemName;
                bool added  = false;
                if (!Plugin.Blacklist.Contains(key)) { Plugin.Blacklist.Add(key); added = true; LootBlacklist.SaveBlacklist(); }
                ChatFilterInjector.SendLootMessage(added
                    ? "[Loot Manager] Blacklisted \"" + key + "\" and destroyed it."
                    : "[Loot Manager] \"" + key + "\" is already blacklisted. Destroyed it.", "grey");
                PlayDropSound();
            }

            private static void HandleBanklistAndDeposit(Item item, int qty, bool addToList)
            {
                string key = item.ItemName;
                bool added = false;
                if (addToList && !Plugin.Banklist.Contains(key)) { Plugin.Banklist.Add(key); added = true; LootBanklist.SaveBanklist(); }
                if (addToList) ChatFilterInjector.SendLootMessage(added
                    ? "[Loot Manager] Added \"" + key + "\" to banklist."
                    : "[Loot Manager] \"" + key + "\" already on banklist.", "lightblue");
                BankLoot.DepositLoot(new BankLoot.LootEntry[] { new BankLoot.LootEntry(item.Id, qty, item.ItemName) });
                PlayDropSound();
            }

            private static void PlayDropSound()
            {
                if (GameData.PlayerAud != null && GameData.GM != null)
                {
                    var misc = GameData.GM.GetComponent<Misc>();
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
    public class BanklistDropZoneMarker  : MonoBehaviour { }
}