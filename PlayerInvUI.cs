// PlayerInvUI.cs
// Builds a sidebar panel that attaches to the LEFT of the player inventory window.
// Visible whenever the inventory is open. Contains four drop zones:
// Blacklist, Banklist, Junklist, Auctionlist.
// Replaces the old Manager tab approach entirely.

using HarmonyLib;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LootManager
{
    public class PlayerInvUI : MonoBehaviour
    {
        private const string PlayerInvName = "PlayerInv";
        private const string SidebarName   = "LootManagerSidebar";

        // Sidebar dimensions
        private const float SidebarW = 110f;

        // Colours
        private static readonly Color32 C_PanelBg = new Color32(10, 12, 16, 242);
        private static readonly Color32 C_Border   = new Color32(45, 49, 57, 255);

        private static GameObject _sidebar;

        // ─────────────────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────────────────
        private void Start()
        {
            StartCoroutine(BuildWhenReady());
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _sidebar = null;
            StartCoroutine(BuildWhenReady());
        }

        private IEnumerator BuildWhenReady()
        {
            while (GameObject.Find(PlayerInvName) == null)
                yield return null;

            yield return null;
            Canvas.ForceUpdateCanvases();

            BuildSidebar();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Build
        // ─────────────────────────────────────────────────────────────────────
        private static void BuildSidebar()
        {
            var playerInv = GameObject.Find(PlayerInvName);
            if (playerInv == null) return;

            // Don't build twice
            if (playerInv.transform.Find(SidebarName) != null) return;

            var invWindowGO = GameData.PlayerInv?.InvWindow;
            if (invWindowGO == null) return;

            var invRT = invWindowGO.GetComponent<RectTransform>();
            if (invRT == null) return;

            // ── Sidebar root ─────────────────────────────────────────────
            // Parent to InvWindow so it moves with it automatically
            var sidebar   = new GameObject(SidebarName);
            var sidebarRT = sidebar.AddComponent<RectTransform>();
            sidebarRT.SetParent(invRT, false);

            // Anchor to the bottom edge of InvWindow, full width
            sidebarRT.anchorMin        = new Vector2(0f, 0f);
            sidebarRT.anchorMax        = new Vector2(1f, 0f);
            sidebarRT.pivot            = new Vector2(0.5f, 1f); // top edge of sidebar touches bottom of inv
            sidebarRT.anchoredPosition = new Vector2(0f, -4f);  // 4px gap
            sidebarRT.sizeDelta        = new Vector2(0f, 80f);  // width = stretch, fixed height

            _sidebar = sidebar;

            // ── Background ───────────────────────────────────────────────
            var bg = sidebar.AddComponent<Image>();
            bg.color = C_PanelBg;

            var ol = sidebar.AddComponent<Outline>();
            ol.effectColor    = C_Border;
            ol.effectDistance = new Vector2(1f, -1f);

            // ── Horizontal layout — zones sit side by side along the bottom ──
            var hl = sidebar.AddComponent<HorizontalLayoutGroup>();
            hl.padding                = new RectOffset(8, 8, 6, 6);
            hl.spacing                = 6;
            hl.childForceExpandWidth  = true;
            hl.childForceExpandHeight = true;
            hl.childControlWidth      = true;
            hl.childControlHeight     = true;
            hl.childAlignment         = TextAnchor.MiddleCenter;

            // ── Drop zones — side by side ────────────────────────────────
            BuildDropZone(sidebarRT, "BlacklistSlot",   "BLACKLIST",   new Color32(120, 30,  30,  255), new Color32(80, 20, 20, 120));
            BuildDropZone(sidebarRT, "BanklistSlot",    "BANKLIST",    new Color32(30,  60,  140, 255), new Color32(20, 30, 80, 120));
            BuildDropZone(sidebarRT, "JunklistSlot",    "JUNKLIST",    new Color32(160, 100, 20,  255), new Color32(80, 50, 10, 120));
            BuildDropZone(sidebarRT, "AuctionlistSlot", "AUCTIONLIST", new Color32(30,  120, 60,  255), new Color32(15, 60, 30, 120));

            // ── Wire up drop targets ─────────────────────────────────────
            WireDropZone(sidebar, "BlacklistSlot",   typeof(BlacklistDropZoneMarker));
            WireDropZone(sidebar, "BanklistSlot",    typeof(BanklistDropZoneMarker));
            WireDropZone(sidebar, "JunklistSlot",    typeof(JunklistDropZoneMarker));
            WireDropZone(sidebar, "AuctionlistSlot", typeof(AuctionlistDropZoneMarker));

            // Start hidden — sync to InvWindow state
            sidebar.SetActive(invWindowGO.activeSelf);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Drop zone builder
        // ─────────────────────────────────────────────────────────────────────
        private static void BuildDropZone(Transform parent, string name, string label,
                                          Color32 borderColour, Color32 bgColour)
        {
            // Container — label below box
            var container   = new GameObject(name);
            var containerRT = container.AddComponent<RectTransform>();
            containerRT.SetParent(parent, false);

            var outerVL = container.AddComponent<VerticalLayoutGroup>();
            outerVL.spacing                = 2;
            outerVL.childForceExpandWidth  = true;
            outerVL.childForceExpandHeight = false;
            outerVL.childControlWidth      = true;
            outerVL.childControlHeight     = true;
            outerVL.childAlignment         = TextAnchor.UpperCenter;

            // Drop box
            var boxGO = new GameObject("DropBox");
            boxGO.AddComponent<RectTransform>().SetParent(containerRT, false);
            var boxLE = boxGO.AddComponent<LayoutElement>();
            boxLE.flexibleHeight = 1f; // fill available height

            // Background
            var boxBG = boxGO.AddComponent<Image>();
            boxBG.color = new Color(bgColour.r / 255f, bgColour.g / 255f, bgColour.b / 255f, bgColour.a / 255f);

            // Outer border
            var boxOL = boxGO.AddComponent<Outline>();
            boxOL.effectColor    = new Color(borderColour.r / 255f, borderColour.g / 255f, borderColour.b / 255f, 0.85f);
            boxOL.effectDistance = new Vector2(1.5f, -1.5f);

            // Inner dashed border
            var dashGO = new GameObject("DashBorder");
            var dashRT = dashGO.AddComponent<RectTransform>();
            dashRT.SetParent(boxGO.transform, false);
            dashRT.anchorMin = new Vector2(0.05f, 0.05f);
            dashRT.anchorMax = new Vector2(0.95f, 0.95f);
            dashRT.offsetMin = Vector2.zero;
            dashRT.offsetMax = Vector2.zero;
            var dashImg = dashGO.AddComponent<Image>();
            dashImg.color         = new Color(1f, 1f, 1f, 0f);
            dashImg.raycastTarget = false;
            var dashOL = dashGO.AddComponent<Outline>();
            dashOL.effectColor    = new Color(borderColour.r / 255f, borderColour.g / 255f, borderColour.b / 255f, 0.3f);
            dashOL.effectDistance = new Vector2(1f, -1f);

            // Centered item icon
            var iconGO = new GameObject("ItemIcon");
            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.SetParent(boxGO.transform, false);
            iconRT.anchorMin = new Vector2(0.2f, 0.2f);
            iconRT.anchorMax = new Vector2(0.8f, 0.8f);
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.color         = new Color(1f, 1f, 1f, 0f);

            // Label below box
            var lblGO = new GameObject("Label");
            lblGO.AddComponent<RectTransform>().SetParent(containerRT, false);
            lblGO.AddComponent<LayoutElement>().preferredHeight = 12;
            var lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text      = label;
            lbl.fontSize  = 8;
            lbl.fontStyle = FontStyles.Bold;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color     = new Color(borderColour.r / 255f, borderColour.g / 255f, borderColour.b / 255f, 0.9f);
        }

        private static void WireDropZone(GameObject sidebar, string slotName, System.Type markerType)
        {
            var iconT = sidebar.transform.Find($"{slotName}/DropBox/ItemIcon");
            if (iconT == null)
            {
                Debug.LogWarning($"[PlayerInvUI] Could not find ItemIcon for {slotName}");
                return;
            }

            var iconGO = iconT.gameObject;
            iconGO.tag = "ItemSlot";

            var img = iconGO.GetComponent<Image>() ?? iconGO.AddComponent<Image>();
            img.raycastTarget = false;
            img.color         = new Color(1f, 1f, 1f, 0f);

            var col = iconGO.GetComponent<BoxCollider2D>() ?? iconGO.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            var rb = iconGO.GetComponent<Rigidbody2D>() ?? iconGO.AddComponent<Rigidbody2D>();
            rb.bodyType                = RigidbodyType2D.Kinematic;
            rb.gravityScale            = 0f;
            rb.useFullKinematicContacts = true;

            var icon = iconGO.GetComponent<ItemIcon>() ?? iconGO.AddComponent<ItemIcon>();
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

            if (iconGO.GetComponent(markerType) == null)
                iconGO.AddComponent(markerType);
        }

        private static void MakeDivider(Transform parent)
        {
            var go = new GameObject("Divider");
            go.AddComponent<RectTransform>().SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 1;
            var img = go.AddComponent<Image>();
            img.color = new Color(45f/255f, 49f/255f, 57f/255f, 1f);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public sync — called by Harmony patches
        // ─────────────────────────────────────────────────────────────────────
        public static void SyncSidebarVisibility()
        {
            if (_sidebar == null)
            {
                // Try to find it in case reference was lost after scene reload
                var playerInv = GameObject.Find(PlayerInvName);
                if (playerInv != null)
                    _sidebar = playerInv.transform
                        .Find($"InvWindow/{SidebarName}")?.gameObject // via InvWindow child path
                        ?? GameObject.Find(SidebarName);
            }

            if (_sidebar == null) return;

            var invWindow = GameData.PlayerInv?.InvWindow;
            if (invWindow == null) return;

            _sidebar.SetActive(invWindow.activeSelf);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Harmony patches — sync sidebar to inventory visibility
    // Patch Update so we catch the direct SetActive call from the keyboard shortcut,
    // plus ForceOpenInv/ForceCloseInv for all other callers.
    // ─────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(Inventory), "Update")]
    public static class Inventory_Update_Patch
    {
        public static void Postfix(Inventory __instance)
        {
            if (!__instance.isPlayer) return;
            PlayerInvUI.SyncSidebarVisibility();
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.ForceOpenInv))]
    public static class Inventory_ForceOpenInv_Patch
    {
        public static void Postfix() => PlayerInvUI.SyncSidebarVisibility();
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.ForceCloseInv))]
    public static class Inventory_ForceCloseInv_Patch
    {
        public static void Postfix() => PlayerInvUI.SyncSidebarVisibility();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Drop zone markers
    // ─────────────────────────────────────────────────────────────────────────
    public class BlacklistDropZoneMarker   : MonoBehaviour { }
    public class BanklistDropZoneMarker    : MonoBehaviour { }
    public class JunklistDropZoneMarker    : MonoBehaviour { }
    public class AuctionlistDropZoneMarker : MonoBehaviour { }
}