// LootUIController.cs
// Builds the main Loot Manager window hierarchy in code (uGUI).
// No AssetBundle or path-based lookups — all GameObjects are created here
// and references are passed directly to panel controllers.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace LootManager
{
    public static class LootUIController
    {
        // Forces targetGraphic = null on Start, after Button.Awake() has run.
        // Prevents Button from tinting a child Image it found via GetComponentInChildren.
        private sealed class NullTargetGraphic : MonoBehaviour
        {
            private void Start()
            {
                var btn = GetComponent<Button>();
                if (btn != null) btn.targetGraphic = null;
                Destroy(this);
            }
        }

        // Attached to the dropdown Template. Runs every LateUpdate while the
        // spawned list is alive, keeping CanvasGroup.alpha pinned to 1 so
        // TMP_Dropdown's open AND close fade coroutines have no visible effect.
        // The GO is destroyed by TMP when the list closes, taking this with it.
        private sealed class DropdownNoFade : MonoBehaviour
        {
            private CanvasGroup _cg;
            private TMP_Dropdown _owner;

            private void Awake()
            {
                _cg = GetComponent<CanvasGroup>();
                if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
            }

            private void LateUpdate()
            {
                // Pin alpha to 1 every frame — overrides whatever TMP set this frame
                _cg.alpha          = 1f;
                _cg.interactable   = true;
                _cg.blocksRaycasts = true;

                // On first frame, find and stop the open-tween coroutine
                if (_owner == null)
                {
                    _owner = FindObjectOfType<TMP_Dropdown>();
                    if (_owner != null) _owner.StopAllCoroutines();
                }
            }

            private void OnDisable()
            {
                // Fires just before TMP destroys the list — stop close coroutine
                if (_owner != null) _owner.StopAllCoroutines();
            }
        }


        // Small component added to each dropdown item template row.
        // TMP_Dropdown clones the GO, so each clone gets its own instance
        // which finds its OWN Image — no stale closure captures.
        private sealed class DropdownItemHover : MonoBehaviour,
            IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
        {
            private Image _bg;
            private static readonly Color32 CNormal  = new Color32(0,   0,   0,   0);
            private static readonly Color32 CHover   = new Color32(255, 255, 255, 30);
            private static readonly Color32 CPressed = new Color32(255, 255, 255, 56);

            private void Awake() { _bg = GetComponent<Image>(); }
            public void OnPointerEnter(PointerEventData _) { if (_bg) _bg.color = CHover; }
            public void OnPointerExit (PointerEventData _) { if (_bg) _bg.color = CNormal; }
            public void OnPointerDown (PointerEventData _) { if (_bg) _bg.color = CPressed; }
            public void OnPointerUp   (PointerEventData _) { if (_bg) _bg.color = CHover; }
        }


        // ── Colours (shared palette) ──────────────────────────────────────────
        internal static readonly Color32 C_WindowBg   = Hex32("#0F1014", 245);
        internal static readonly Color32 C_TitleBg    = Hex32("#1A1D23", 255);
        internal static readonly Color32 C_PanelBg    = Hex32("#13161B", 255);
        internal static readonly Color32 C_Border     = Hex32("#2D3139", 255);
        internal static readonly Color32 C_AccentBlue = Hex32("#008DFD", 255);
        internal static readonly Color32 C_BtnNormal  = Hex32("#000000", 255);
        internal static readonly Color32 C_BtnHover   = Hex32("#1A3A5C", 255);
        internal static readonly Color32 C_BtnActive  = Hex32("#0D2440", 255);
        internal static readonly Color32 C_TextPri    = Hex32("#F1F5F9", 255);
        internal static readonly Color32 C_TextMuted  = Hex32("#64748B", 255);
        internal static readonly Color32 C_TextSecond = Hex32("#94A3B8", 255);
        internal static readonly Color32 C_InputBg    = Hex32("#0A0C10", 255);
        internal static readonly Color32 C_ScrollBg   = Hex32("#0A0C10", 255);
        internal static readonly Color32 C_RowOdd     = Hex32("#111318", 255);
        internal static readonly Color32 C_Danger     = Hex32("#EF4444", 255);
        internal static readonly Color32 C_Success    = Hex32("#10B981", 255);

        // Main window dimensions
        private const float WindowW = 620f;
        private const float WindowH = 460f;
        private const float TitleH  = 28f;
        private const float MenuH   = 30f;

        // ── Live references ───────────────────────────────────────────────────
        private static GameObject _uiRoot;
        private static RectTransform _container;   // draggable window

        private static GameObject _mainView;       // title + menubar + panels
        private static GameObject _editView;       // edit-category overlay

        private static GameObject _settingsPanelGO;
        private static GameObject _blacklistPanelGO;
        private static GameObject _whitelistPanelGO;
        private static GameObject _banklistPanelGO;
        private static GameObject _filterlistPanelGO;

        private static Button _menuSettingsBtn;
        private static Button _menuBlacklistBtn;
        private static Button _menuWhitelistBtn;
        private static Button _menuBanklistBtn;
        private static Button _menuFilterlistBtn;

        private static SettingsPanelController  _settings;
        private static BlacklistPanelController _blacklist;
        private static WhitelistPanelController _whitelist;
        private static BanklistPanelController  _banklist;
        private static EditlistPanelController    _editlist;
        private static FilterlistPanelController  _filterlist;

        // ─────────────────────────────────────────────────────────────────────
        // Entry point called from LootUI.Awake
        // ─────────────────────────────────────────────────────────────────────
        public static void Initialize(GameObject uiRoot)
        {
            _uiRoot = uiRoot;
            BuildWindow();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Build the draggable window container
        // ─────────────────────────────────────────────────────────────────────
        private static void BuildWindow()
        {
            // Draggable container
            var containerGO = new GameObject("container");
            _container = containerGO.AddComponent<RectTransform>();
            _container.SetParent(_uiRoot.transform, false);
            _container.anchorMin        = new Vector2(0.5f, 0.5f);
            _container.anchorMax        = new Vector2(0.5f, 0.5f);
            _container.pivot            = new Vector2(0.5f, 0.5f);
            _container.anchoredPosition = new Vector2(0f, 0f);
            _container.sizeDelta        = new Vector2(WindowW, WindowH);

            var bg = containerGO.AddComponent<Image>();
            bg.color = C_WindowBg;

            var outline = containerGO.AddComponent<Outline>();
            outline.effectColor    = C_Border;
            outline.effectDistance = new Vector2(1f, -1f);

            // Main view (everything except edit overlay)
            _mainView = new GameObject("mainView");
            var mainRT = _mainView.AddComponent<RectTransform>();
            mainRT.SetParent(_container, false);
            StretchFull(mainRT);

            BuildTitleBar(_mainView.transform);
            var menuBar = BuildMenuBar(_mainView.transform);
            BuildPanelArea(_mainView.transform);

            // Edit view (full-size overlay, hidden by default)
            _editView = new GameObject("editView");
            var editRT = _editView.AddComponent<RectTransform>();
            editRT.SetParent(_container, false);
            StretchFull(editRT);
            _editView.SetActive(false);

            // Wire up drag on the title bar
            var dragHandle = _mainView.transform.Find("titleBar")?.gameObject;
            if (dragHandle != null)
            {
                var dh = dragHandle.GetComponent<DragHandler>() ?? dragHandle.AddComponent<DragHandler>();
                dh.PanelToMove = _container;
            }

            // Activate all panels so Unity runs Start()/Awake() on every component
            // (including TMP_Dropdown) before we call Init() on the controllers.
            // ShowPanel() will hide the non-active ones immediately after.
            _settingsPanelGO.SetActive(true);
            _blacklistPanelGO.SetActive(true);
            _whitelistPanelGO.SetActive(true);
            _banklistPanelGO.SetActive(true);
            _filterlistPanelGO.SetActive(true);
            _editView.SetActive(true);

            // Initialise panel controllers — pass them their root GOs directly
            _settings = new SettingsPanelController(
                _settingsPanelGO,
                _container,
                OnUIVisibilityPossiblyChanged);
            _settings.Init();

            _blacklist = new BlacklistPanelController(_blacklistPanelGO, _container);
            _blacklist.Init();

            _whitelist = new WhitelistPanelController(_whitelistPanelGO, _container);
            _whitelist.Init();

            _banklist = new BanklistPanelController(_banklistPanelGO, _container);
            _banklist.Init();

            _editlist = new EditlistPanelController(_editView, _container);
            _editlist.Init();

            _filterlist = new FilterlistPanelController(_filterlistPanelGO, _container);
            _filterlist.Init();

            // ShowPanel hides all panels except the active one
            ShowPanel(_settingsPanelGO);
            OnUIVisibilityPossiblyChanged();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Title bar
        // ─────────────────────────────────────────────────────────────────────
        private static void BuildTitleBar(Transform parent)
        {
            var go = new GameObject("titleBar");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, TitleH);
            rt.anchoredPosition = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = C_TitleBg;

            // Title text
            var titleTxt = MakeTMP("titleText", rt);
            var titleRT  = titleTxt.GetComponent<RectTransform>();
            titleRT.anchorMin = Vector2.zero;
            titleRT.anchorMax = Vector2.one;
            titleRT.offsetMin = new Vector2(10, 0);
            titleRT.offsetMax = new Vector2(-30, 0);
            titleTxt.text      = "<color=#F1F5F9>LOOT</color><color=#008DFD>MANAGER</color>";
            titleTxt.fontSize  = 13;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.MidlineLeft;

            // Close button
            var closeBtn = MakeButton("closeBtn", rt, "X", C_Danger, C_TitleBg, 11);
            // No hover outline on close button
            var closeBtnOL = closeBtn.GetComponent<Outline>();
            if (closeBtnOL != null) GameObject.Destroy(closeBtnOL);
            var closeBtnBHO = closeBtn.GetComponent<ButtonHoverOutline>();
            if (closeBtnBHO != null) GameObject.Destroy(closeBtnBHO);
            var closeBtnRT = closeBtn.GetComponent<RectTransform>();
            closeBtnRT.anchorMin = new Vector2(1, 0);
            closeBtnRT.anchorMax = new Vector2(1, 1);
            closeBtnRT.pivot     = new Vector2(1, 0.5f);
            closeBtnRT.sizeDelta = new Vector2(28, 0);
            closeBtnRT.anchoredPosition = Vector2.zero;
            closeBtn.onClick.AddListener(() =>
            {
                if (LootUI.Instance != null) LootUI.Instance.ToggleUI();
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // Menu / tab bar
        // ─────────────────────────────────────────────────────────────────────
        private static GameObject BuildMenuBar(Transform parent)
        {
            var go = new GameObject("menuBar");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, MenuH);
            rt.anchoredPosition = new Vector2(0, -TitleH);

            var bg = go.AddComponent<Image>();
            bg.color = C_PanelBg;

            var hl = go.AddComponent<HorizontalLayoutGroup>();
            hl.padding                = new RectOffset(4, 4, 3, 3);
            hl.spacing                = 4;
            hl.childForceExpandWidth  = false;
            hl.childForceExpandHeight = true;
            hl.childControlWidth      = false;
            hl.childControlHeight     = true;

            _menuSettingsBtn  = MakeTabButton(rt, "settingBtn",  "Settings");
            _menuBlacklistBtn = MakeTabButton(rt, "blacklistBtn", "Blacklist");
            _menuWhitelistBtn = MakeTabButton(rt, "whitelistBtn", "Whitelist");
            _menuBanklistBtn    = MakeTabButton(rt, "banklistBtn",    "Banklist");
            _menuFilterlistBtn  = MakeTabButton(rt, "filterlistBtn",  "Filterlists");

            _menuSettingsBtn.onClick.AddListener(() =>
            {
                ShowPanel(_settingsPanelGO);
                _settings.Show();
            });
            _menuBlacklistBtn.onClick.AddListener(() =>
            {
                ShowPanel(_blacklistPanelGO);
                _blacklist.Show();
            });
            _menuWhitelistBtn.onClick.AddListener(() =>
            {
                ShowPanel(_whitelistPanelGO);
                _whitelist.Show();
            });
            _menuBanklistBtn.onClick.AddListener(() =>
            {
                ShowPanel(_banklistPanelGO);
                _banklist.Show();
            });
            _menuFilterlistBtn.onClick.AddListener(() =>
            {
                ShowPanel(_filterlistPanelGO);
                _filterlist?.Show();
            });

            return go;
        }

        private static Button MakeTabButton(RectTransform parent, string name, string label)
        {
            var go  = new GameObject(name);
            var rt  = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(90, 0);

            var img = go.AddComponent<Image>();
            img.color = C_BtnNormal;

            var btn = go.AddComponent<Button>();
            btn.transition    = Selectable.Transition.None;
            btn.targetGraphic = img;

            // Outline acts as active-tab indicator and hover indicator
            var ol = go.AddComponent<Outline>();
            ol.effectColor    = C_AccentBlue;
            ol.effectDistance = new Vector2(1, -1);
            ol.enabled        = false;
            go.AddComponent<ButtonHoverOutline>();

            var lblGO = new GameObject("Label");
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.SetParent(rt, false);
            StretchFull(lblRT);
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.color     = C_TextPri;
            tmp.fontSize  = 11;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Panel area — builds all four panel containers
        // ─────────────────────────────────────────────────────────────────────
        private static void BuildPanelArea(Transform parent)
        {
            float topOffset = TitleH + MenuH;
            float panelH    = WindowH - topOffset;

            _settingsPanelGO  = BuildPanelContainer(parent, "panelBGsettings",  topOffset, panelH);
            _blacklistPanelGO = BuildPanelContainer(parent, "panelBGblacklist", topOffset, panelH);
            _whitelistPanelGO = BuildPanelContainer(parent, "panelBGwhitelist", topOffset, panelH);
            _banklistPanelGO    = BuildPanelContainer(parent, "panelBGbanklist",    topOffset, panelH);
            _filterlistPanelGO  = BuildPanelContainer(parent, "panelBGfilterlist",  topOffset, panelH);
        }

        private static GameObject BuildPanelContainer(Transform parent, string name, float topOffset, float panelH)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin        = new Vector2(0, 0);
            rt.anchorMax        = new Vector2(1, 1);
            rt.offsetMin        = new Vector2(0, 0);
            rt.offsetMax        = new Vector2(0, -(topOffset));

            var bg = go.AddComponent<Image>();
            bg.color = C_PanelBg;

            go.SetActive(false);
            return go;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Panel switching
        // ─────────────────────────────────────────────────────────────────────
        private static void ShowPanel(GameObject activePanel)
        {
            // Hide edit view, show main view
            if (_editView != null) _editView.SetActive(false);
            if (_mainView != null) _mainView.SetActive(true);

            _settingsPanelGO?.SetActive(activePanel == _settingsPanelGO);
            _blacklistPanelGO?.SetActive(activePanel == _blacklistPanelGO);
            _whitelistPanelGO?.SetActive(activePanel == _whitelistPanelGO);
            _banklistPanelGO?.SetActive(activePanel == _banklistPanelGO);
            _filterlistPanelGO?.SetActive(activePanel == _filterlistPanelGO);

            SetTabActive(_menuSettingsBtn,  activePanel == _settingsPanelGO);
            SetTabActive(_menuBlacklistBtn, activePanel == _blacklistPanelGO);
            SetTabActive(_menuWhitelistBtn, activePanel == _whitelistPanelGO);
            SetTabActive(_menuBanklistBtn,    activePanel == _banklistPanelGO);
            SetTabActive(_menuFilterlistBtn,  activePanel == _filterlistPanelGO);
        }

        private static void SetTabActive(Button btn, bool active)
        {
            if (btn == null) return;
            var bho = btn.GetComponent<ButtonHoverOutline>();
            if (bho != null) { bho.SetActive(active); return; }
            // Fallback for buttons without ButtonHoverOutline
            var ol = btn.GetComponent<Outline>();
            if (ol != null) ol.enabled = active;
        }

        private static void OnUIVisibilityPossiblyChanged()
        {
            if (_menuBlacklistBtn != null)
                _menuBlacklistBtn.gameObject.SetActive(Plugin.LootMethod.Value == "Blacklist");
            if (_menuWhitelistBtn != null)
                _menuWhitelistBtn.gameObject.SetActive(Plugin.LootMethod.Value == "Whitelist");
            if (_menuBanklistBtn != null)
                _menuBanklistBtn.gameObject.SetActive(Plugin.BankLootEnabled.Value);
            // Filter Categories tab is always visible
            if (_menuFilterlistBtn != null)
                _menuFilterlistBtn.gameObject.SetActive(true);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API used by WhitelistPanelController to open the edit overlay
        // ─────────────────────────────────────────────────────────────────────
        public static void ShowEditCategory(string categoryName)
        {
            if (_editlist == null)
            {
                ChatFilterInjector.SendLootMessage("[LootUI] Edit panel not ready.", "red");
                return;
            }
            // Position edit view to match container then show it
            if (_mainView != null) _mainView.SetActive(false);
            if (_editView != null) _editView.SetActive(true);
            _editlist.Show(categoryName);
        }

        public static void HideEditView()
        {
            if (_editView != null) _editView.SetActive(false);
            if (_mainView != null) _mainView.SetActive(true);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Builder helpers (shared with panel controllers via internal access)
        // ─────────────────────────────────────────────────────────────────────
        internal static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        internal static Image AddImage(GameObject go, Color32 colour)
        {
            var img = go.AddComponent<Image>();
            img.color = colour;
            return img;
        }

        internal static TextMeshProUGUI MakeTMP(string name, Transform parent)
        {
            var go  = new GameObject(name);
            var rt  = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.color        = C_TextPri;
            tmp.fontSize     = 11;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        internal static Button MakeButton(string name, Transform parent, string label,
            Color32 textColour, Color32 bgColour, int fontSize = 11)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = bgColour;

            var btn = go.AddComponent<Button>();
            btn.transition    = Selectable.Transition.None;
            btn.targetGraphic = img;

            // Hover outline — disabled by default, enabled on pointer enter
            var btnOL = go.AddComponent<Outline>();
            btnOL.effectColor    = C_AccentBlue;
            btnOL.effectDistance = new Vector2(1, -1);
            btnOL.enabled        = false;
            go.AddComponent<ButtonHoverOutline>();

            var lblGO = new GameObject("Label");
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.SetParent(rt, false);
            StretchFull(lblRT);
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.color     = textColour;
            tmp.fontSize  = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        /// <summary>
        /// Creates a button whose single TMP label is returned for use with HotkeyBindControl.
        /// Unlike MakeButton, there is no separate "Label" child — the TMP IS the display surface.
        /// </summary>
        internal static Button MakeHotkeyButton(string name, Transform parent,
            Color32 bgColour, int fontSize, out TextMeshProUGUI labelOut, out Image bgImageOut)
        {
            // Image lives on a child "BG" GO, NOT on the root GO.
            // Button.Awake() searches GetComponent<Image>() on the root and
            // finds nothing, so targetGraphic stays null and no state
            // transition can ever tint the button.
            // Callers receive bgImageOut so they can add Outline to the BG
            // child (Outline needs a Graphic on the same GO to work).
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);

            var bgGO = new GameObject("BG");
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.SetParent(rt, false);
            StretchFull(bgRT);
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color         = bgColour;
            bgImg.raycastTarget = false;

            var btn = go.AddComponent<Button>();
            btn.transition    = Selectable.Transition.None;
            btn.targetGraphic = null;
            // Button.Awake() may re-assign targetGraphic via GetComponentInChildren.
            // NullTargetGraphic clears it again on Start (after Awake).
            go.AddComponent<NullTargetGraphic>();

            var lblGO = new GameObject("Label");
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.SetParent(rt, false);
            StretchFull(lblRT);
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.color        = C_TextPri;
            tmp.fontSize     = fontSize;
            tmp.fontStyle    = FontStyles.Bold;
            tmp.alignment    = TextAlignmentOptions.Center;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            bgImageOut = bgImg;
            labelOut   = tmp;
            return btn;
        }

        internal static ScrollRect MakeScrollView(string name, Transform parent,
            out RectTransform viewport, out RectTransform content)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);

            var bg = go.AddComponent<Image>();
            bg.color = C_ScrollBg;

            var sr = go.AddComponent<ScrollRect>();
            sr.horizontal        = false;
            sr.vertical          = true;
            sr.scrollSensitivity = 30f;

            // Viewport
            var vpGO = new GameObject("Viewport");
            viewport = vpGO.AddComponent<RectTransform>();
            viewport.SetParent(rt, false);
            StretchFull(viewport);
            vpGO.AddComponent<Image>().color = new Color(0, 0, 0, 1f);
            vpGO.AddComponent<Mask>().showMaskGraphic = false;

            // Content
            var cGO = new GameObject("Content");
            content = cGO.AddComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin        = new Vector2(0, 1);
            content.anchorMax        = new Vector2(1, 1);
            content.pivot            = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta        = new Vector2(0, 0);

            var vlg = cGO.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true;
            vlg.spacing                = 1;

            var csf = cGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = viewport;
            sr.content  = content;

            return sr;
        }

        internal static TMP_InputField MakeInputField(string name, Transform parent, string placeholder)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);

            var bg = go.AddComponent<Image>();
            bg.color = C_InputBg;

            // Viewport
            var vpGO = new GameObject("Text Area");
            var vpRT = vpGO.AddComponent<RectTransform>();
            vpRT.SetParent(rt, false);
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = new Vector2(4, 2);
            vpRT.offsetMax = new Vector2(-4, -2);
            vpGO.AddComponent<RectMask2D>();

            // Input text
            var textGO = new GameObject("Text");
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.SetParent(vpRT, false);
            StretchFull(textRT);
            var inputTMP = textGO.AddComponent<TextMeshProUGUI>();
            inputTMP.color               = C_TextPri;
            inputTMP.fontSize            = 11;
            inputTMP.enableWordWrapping  = false;
            inputTMP.extraPadding        = true;
            inputTMP.raycastTarget       = false;

            // Placeholder
            var phGO = new GameObject("Placeholder");
            var phRT = phGO.AddComponent<RectTransform>();
            phRT.SetParent(vpRT, false);
            StretchFull(phRT);
            var phTMP = phGO.AddComponent<TextMeshProUGUI>();
            phTMP.text               = placeholder;
            phTMP.color              = C_TextMuted;
            phTMP.fontSize           = 11;
            phTMP.fontStyle          = FontStyles.Italic;
            phTMP.enableWordWrapping = false;
            phTMP.raycastTarget      = false;

            // SetActive(false) defers TMP_InputField.Awake until after all
            // child references are assigned — prevents stale internal refs.
            go.SetActive(false);
            var field = go.AddComponent<TMP_InputField>();
            field.textViewport     = vpRT;
            field.textComponent    = inputTMP;
            field.placeholder      = phTMP;
            field.caretColor       = C_TextPri;
            field.selectionColor   = new Color(0f, 0.55f, 1f, 0.4f);
            field.caretWidth       = 1;
            field.customCaretColor = true;
            go.SetActive(true);
            field.text = string.Empty;

            return field;
        }
        internal static Toggle MakeToggle(string name, Transform parent, string label)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);

            // Layout group drives width; height is controlled per-child via LE.
            // childForceExpandHeight=false + childControlHeight=true means the
            // group respects each child's preferredHeight rather than stretching.
            var hl = go.AddComponent<HorizontalLayoutGroup>();
            hl.padding                = new RectOffset(4, 4, 0, 0);
            hl.spacing                = 6;
            hl.childForceExpandWidth  = false;
            hl.childForceExpandHeight = false;
            hl.childControlWidth      = true;
            hl.childControlHeight     = true;
            hl.childAlignment         = TextAnchor.MiddleLeft;

            // Checkbox: fixed 14x14 via LayoutElement — both axes locked
            var checkGO = new GameObject("Background");
            var checkRT = checkGO.AddComponent<RectTransform>();
            checkRT.SetParent(rt, false);
            var checkLE = checkGO.AddComponent<LayoutElement>();
            checkLE.minWidth       = 14;
            checkLE.preferredWidth = 14;
            checkLE.minHeight      = 14;
            checkLE.preferredHeight = 14;
            checkLE.flexibleWidth  = 0;
            checkLE.flexibleHeight = 0;
            var checkImg = checkGO.AddComponent<Image>();
            checkImg.color = C_InputBg;
            var checkOL = checkGO.AddComponent<Outline>();
            checkOL.effectColor    = C_Border;
            checkOL.effectDistance = new Vector2(1, -1);

            // Checkmark — inset 3 px inside the checkbox
            var markGO = new GameObject("Checkmark");
            var markRT = markGO.AddComponent<RectTransform>();
            markRT.SetParent(checkRT, false);
            markRT.anchorMin = Vector2.zero;
            markRT.anchorMax = Vector2.one;
            markRT.offsetMin = new Vector2(3, 3);
            markRT.offsetMax = new Vector2(-3, -3);
            var markImg = markGO.AddComponent<Image>();
            // Start transparent — Toggle.graphic CrossFadeColor still fires
            // but is invisible. Shown only when item is selected (isOn=true).
            markImg.color = C_AccentBlue;

            // Label — flexible width, same preferred height as checkbox
            var lblGO = new GameObject("Label");
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.SetParent(rt, false);
            var lblLE = lblGO.AddComponent<LayoutElement>();
            lblLE.flexibleWidth    = 1;
            lblLE.minHeight        = 14;
            lblLE.preferredHeight  = 14;
            lblLE.flexibleHeight   = 0;
            var lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text      = label;
            lbl.color     = C_TextPri;
            lbl.fontSize  = 11;
            lbl.alignment = TextAlignmentOptions.MidlineLeft;

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = checkImg;
            toggle.graphic       = markImg;

            return toggle;
        }

        internal static TMP_Dropdown MakeDropdown(string name, Transform parent)
        {
            // ── Wrapper: provides the visible border ──────────────────────────
            // TMP_Dropdown resets targetGraphic on Awake to its own Image, so we
            // can't use Outline on the dropdown GO itself. Instead a 1px-inset
            // border wrapper sits behind and the dropdown GO floats on top.
            var wrapGO = new GameObject(name + "_wrap");
            var wrapRT = wrapGO.AddComponent<RectTransform>();
            wrapRT.SetParent(parent, false);
            var wrapImg = wrapGO.AddComponent<Image>();
            wrapImg.color = C_Border;  // border colour shows as 1 px rim

            // ── Dropdown GO — inset 1 px inside wrapper ───────────────────────
            var go = new GameObject(name);
            go.SetActive(false);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(wrapRT, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(1, 1);
            rt.offsetMax = new Vector2(-1, -1);

            // "Background" is the name TMP_Dropdown.Awake() searches for first.
            // Giving it this name ensures Awake picks it as targetGraphic.
            var bgGO = new GameObject("Background");
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.SetParent(rt, false);
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var dropBg = bgGO.AddComponent<Image>();
            dropBg.color = C_InputBg;

            var drop = go.AddComponent<TMP_Dropdown>();

            // ── Caption label ─────────────────────────────────────────────────
            var lblGO = new GameObject("Label");
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.SetParent(rt, false);
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = new Vector2(6, 2);
            lblRT.offsetMax = new Vector2(-20, -2);
            var captionTMP = lblGO.AddComponent<TextMeshProUGUI>();
            captionTMP.color        = C_TextPri;
            captionTMP.fontSize     = 11;
            captionTMP.alignment    = TextAlignmentOptions.MidlineLeft;
            captionTMP.overflowMode = TextOverflowModes.Ellipsis;

            // ── Arrow glyph ───────────────────────────────────────────────────
            var arrowGO  = new GameObject("Arrow");
            var arrowRT  = arrowGO.AddComponent<RectTransform>();
            arrowRT.SetParent(rt, false);
            arrowRT.anchorMin        = new Vector2(1, 0.5f);
            arrowRT.anchorMax        = new Vector2(1, 0.5f);
            arrowRT.pivot            = new Vector2(1, 0.5f);
            arrowRT.sizeDelta        = new Vector2(14, 14);
            arrowRT.anchoredPosition = new Vector2(-4, 0);
            var arrowTMP = arrowGO.AddComponent<TextMeshProUGUI>();
            arrowTMP.text      = "\u25bc";
            arrowTMP.color     = C_TextMuted;
            arrowTMP.fontSize  = 8;
            arrowTMP.alignment = TextAlignmentOptions.Center;

            // ── Template (inactive — opened by TMP_Dropdown at runtime) ───────
            var templateGO = new GameObject("Template");
            var templateRT = templateGO.AddComponent<RectTransform>();
            templateRT.SetParent(rt, false);
            templateRT.anchorMin        = new Vector2(0, 0);
            templateRT.anchorMax        = new Vector2(1, 0);
            templateRT.pivot            = new Vector2(0.5f, 1f);
            templateRT.anchoredPosition = new Vector2(0, 2);
            templateRT.sizeDelta        = new Vector2(0, 150);
            templateGO.AddComponent<Image>().color = C_PanelBg;
            var templateOL = templateGO.AddComponent<Outline>();
            templateOL.effectColor    = C_Border;
            templateOL.effectDistance = new Vector2(1, -1);
            // CanvasGroup + DropdownNoFade work together to kill
            // TMP_Dropdown's alpha-fade coroutine on open.
            var templateCG = templateGO.AddComponent<CanvasGroup>();
            templateCG.alpha          = 1f;
            templateCG.blocksRaycasts = true;
            templateGO.AddComponent<DropdownNoFade>();
            templateGO.SetActive(false);

            var sr        = templateGO.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical   = true;

            // Viewport
            var vpGO = new GameObject("Viewport");
            var vpRT = vpGO.AddComponent<RectTransform>();
            vpRT.SetParent(templateRT, false);
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;
            var vpImg = vpGO.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 1f);   // alpha>0 required for Mask stencil
            var vpMask = vpGO.AddComponent<Mask>();
            vpMask.showMaskGraphic = false;
            sr.viewport = vpRT;

            // Content
            var contentGO = new GameObject("Content");
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.SetParent(vpRT, false);
            contentRT.anchorMin        = new Vector2(0, 1);
            contentRT.anchorMax        = new Vector2(1, 1);
            contentRT.pivot            = new Vector2(0.5f, 1);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta        = new Vector2(0, 28);
            sr.content = contentRT;

            // ── Item template row ─────────────────────────────────────────────
            var itemGO = new GameObject("Item");
            var itemRT = itemGO.AddComponent<RectTransform>();
            itemRT.SetParent(contentRT, false);
            itemRT.anchorMin = new Vector2(0, 0.5f);
            itemRT.anchorMax = new Vector2(1, 0.5f);
            itemRT.sizeDelta = new Vector2(0, 22);
            itemRT.pivot     = new Vector2(0.5f, 0.5f);

            var itemBg = itemGO.AddComponent<Image>();
            itemBg.color = new Color(0, 0, 0, 0);

            var markGO  = new GameObject("Item Checkmark");
            var markRT  = markGO.AddComponent<RectTransform>();
            markRT.SetParent(itemRT, false);
            markRT.anchorMin        = new Vector2(0, 0.5f);
            markRT.anchorMax        = new Vector2(0, 0.5f);
            markRT.pivot            = new Vector2(0.5f, 0.5f);
            markRT.sizeDelta        = new Vector2(10, 10);
            markRT.anchoredPosition = new Vector2(8, 0);
            var markImg = markGO.AddComponent<Image>();
            markImg.color = C_AccentBlue;

            var itemLblGO = new GameObject("Item Label");
            var itemLblRT = itemLblGO.AddComponent<RectTransform>();
            itemLblRT.SetParent(itemRT, false);
            itemLblRT.anchorMin = Vector2.zero;
            itemLblRT.anchorMax = Vector2.one;
            itemLblRT.offsetMin = new Vector2(20, 1);
            itemLblRT.offsetMax = new Vector2(-4, -1);
            var itemLbl = itemLblGO.AddComponent<TextMeshProUGUI>();
            itemLbl.color        = C_TextPri;
            itemLbl.fontSize     = 11;
            itemLbl.alignment    = TextAlignmentOptions.MidlineLeft;
            itemLbl.overflowMode = TextOverflowModes.Ellipsis;

            var itemToggle           = itemGO.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemBg;
            itemToggle.graphic       = markImg;
            itemToggle.transition    = Selectable.Transition.None;

            // DropdownItemHover finds its OWN Image via Awake() —
            // safe to clone because it never captures a stale reference.
            itemGO.AddComponent<DropdownItemHover>();

            // ── Wire all refs — full hierarchy is built ───────────────────────
            drop.template    = templateRT;
            drop.captionText = captionTMP;
            drop.itemText    = itemLbl;

            go.SetActive(true);

            // TMP_Dropdown's internal color management (RefreshShownValue,
            // AddOptions etc.) keeps resetting the ColorBlock to Unity
            // defaults. Switch to transition=None and drive hover via
            // EventTrigger on the Background image directly.
            drop.transition   = Selectable.Transition.None;
            drop.targetGraphic = null;  // nothing for Selectable to flash

            var et = go.AddComponent<EventTrigger>();

            var enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener(_ => dropBg.color = new Color32(28, 38, 56, 255));
            et.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener(_ => dropBg.color = C_InputBg);
            et.triggers.Add(exitEntry);

            var downEntry = new EventTrigger.Entry();
            downEntry.eventID = EventTriggerType.PointerDown;
            downEntry.callback.AddListener(_ => dropBg.color = new Color32(16, 24, 38, 255));
            et.triggers.Add(downEntry);

            var upEntry = new EventTrigger.Entry();
            upEntry.eventID = EventTriggerType.PointerUp;
            upEntry.callback.AddListener(_ => dropBg.color = new Color32(28, 38, 56, 255));
            et.triggers.Add(upEntry);

            // Return the wrapper so callers can size/position it normally
            // and attach LayoutElements to it.
            // The TMP_Dropdown component is still on the inner GO — callers
            // get it via the return value which is the TMP_Dropdown itself.
            return drop;
        }

        internal static Slider MakeSlider(string name, Transform parent)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(0, 16);

            // Background
            var bgGO = new GameObject("Background");
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.SetParent(rt, false);
            bgRT.anchorMin = new Vector2(0, 0.5f);
            bgRT.anchorMax = new Vector2(1, 0.5f);
            bgRT.sizeDelta = new Vector2(0, 4);
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = C_Border;

            // Fill area
            var fillAreaGO = new GameObject("Fill Area");
            var fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRT.SetParent(rt, false);
            fillAreaRT.anchorMin = new Vector2(0, 0.5f);
            fillAreaRT.anchorMax = new Vector2(1, 0.5f);
            fillAreaRT.sizeDelta = new Vector2(-10, 4);
            fillAreaRT.anchoredPosition = new Vector2(-5, 0);

            var fillGO = new GameObject("Fill");
            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.SetParent(fillAreaRT, false);
            fillRT.sizeDelta = new Vector2(10, 0);
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.color = C_AccentBlue;

            // Handle area
            var handleAreaGO = new GameObject("Handle Slide Area");
            var handleAreaRT = handleAreaGO.AddComponent<RectTransform>();
            handleAreaRT.SetParent(rt, false);
            handleAreaRT.anchorMin = new Vector2(0, 0.5f);
            handleAreaRT.anchorMax = new Vector2(1, 0.5f);
            handleAreaRT.sizeDelta = new Vector2(-10, 0);

            var handleGO = new GameObject("Handle");
            var handleRT = handleGO.AddComponent<RectTransform>();
            handleRT.SetParent(handleAreaRT, false);
            handleRT.sizeDelta = new Vector2(12, 12);
            var handleImg = handleGO.AddComponent<Image>();
            handleImg.color = C_TextPri;

            var slider = go.AddComponent<Slider>();
            slider.fillRect   = fillRT;
            slider.handleRect = handleRT;
            slider.targetGraphic = handleImg;
            slider.direction  = Slider.Direction.LeftToRight;
            slider.minValue   = 0;
            slider.maxValue   = 1;

            return slider;
        }

        /// <summary>Creates a row template GameObject suitable for UIVirtualList.</summary>
        internal static GameObject MakeRowTemplate(string name, Transform parent, float rowHeight = 24f)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(0, rowHeight);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = rowHeight;

            var hl = go.AddComponent<HorizontalLayoutGroup>();
            hl.padding                = new RectOffset(4, 4, 2, 2);
            hl.spacing                = 4;
            hl.childForceExpandWidth  = false;
            hl.childForceExpandHeight = true;
            hl.childControlWidth      = true;
            hl.childControlHeight     = true;

            // Icon
            var iconGO = new GameObject("Icon");
            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.SetParent(rt, false);
            iconRT.sizeDelta = new Vector2(rowHeight - 4f, 0);
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget  = false;
            iconGO.AddComponent<LayoutElement>().preferredWidth = rowHeight - 4f;

            // Label
            var lblGO = new GameObject("Label");
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.SetParent(rt, false);
            var lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.color         = C_TextPri;
            lbl.fontSize      = 11;
            lbl.alignment     = TextAlignmentOptions.MidlineLeft;
            lbl.overflowMode  = TextOverflowModes.Ellipsis;
            lbl.raycastTarget = false;
            var lblLE = lblGO.AddComponent<LayoutElement>();
            lblLE.flexibleWidth  = 1;
            lblLE.minWidth       = 0;

            go.SetActive(false);
            return go;
        }

        internal static void StretchFull(RectTransform rt)
        {
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.offsetMin        = Vector2.zero;
            rt.offsetMax        = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        internal static void MakeDivider(Transform parent)
        {
            var go = new GameObject("Divider");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            go.AddComponent<Image>().color = C_Border;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 1;
            le.flexibleWidth   = 1;
        }

        private static Color32 Hex32(string hex, byte alpha = 255)
        {
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            if (hex.Length != 6) return new Color32(255, 255, 255, alpha);
            byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);
            return new Color32(r, g, b, alpha);
        }
    }
}