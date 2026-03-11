// SettingsPanelController.cs
// Builds the Settings panel UI entirely in code.
// Constructor receives the empty panel root GameObject (built by LootUIController).

using BepInEx.Configuration;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public sealed class SettingsPanelController
    {
        private readonly GameObject      _panelRoot;
        private readonly RectTransform   _containerRect;
        private readonly System.Action   _visibilityChanged;

        // Hotkey binders
        private HotkeyBindControl _toggleUIBinder;
        private HotkeyBindControl _autoLootBinder;

        // Cached widget refs
        private Toggle               _autoLootToggle;
        private Slider               _autoDistanceSlider;
        private TextMeshProUGUI      _autoDistanceText;
        private Toggle               _autoLootDelayToggle;
        private Slider               _autoLootDelaySlider;
        private TextMeshProUGUI      _autoLootDelayText;
        private GameObject           _autoLootDelayRow;
        private TMP_Dropdown         _lootMethodDropdown;
        private Toggle               _bankLootToggle;
        private TMP_Dropdown         _bankMethodDropdown;
        private TMP_Dropdown         _bankPageDropdown;
        private Slider               _bankPageFirstSlider;
        private TextMeshProUGUI      _pageFirstText;
        private Slider               _bankPageLastSlider;
        private TextMeshProUGUI      _pageLastText;

        private static SettingsPanelController s_instance;

        private readonly List<string> _lootMethodOptions = new List<string> { "Blacklist", "Whitelist", "Standard" };
        private readonly List<string> _bankMethodOptions = new List<string> { "All", "Filtered" };
        private readonly List<string> _bankPageOptions   = new List<string> { "First Empty", "Page Range" };

        public SettingsPanelController(GameObject panelRoot, RectTransform containerRect, System.Action onVisibilityChanged)
        {
            _panelRoot         = panelRoot;
            _containerRect     = containerRect;
            _visibilityChanged = onVisibilityChanged;
        }

        public void Init()
        {
            s_instance = this;
            BuildSettingsUI();
        }

        public void Show() { /* nothing to reload */ }

        // ─────────────────────────────────────────────────────────────────────
        // Build
        // ─────────────────────────────────────────────────────────────────────
        private void BuildSettingsUI()
        {
            if (_panelRoot == null) return;

            var rt = _panelRoot.GetComponent<RectTransform>();

            // Vertical layout inside the panel
            var body = new GameObject("settingsBody");
            var bodyRT = body.AddComponent<RectTransform>();
            bodyRT.SetParent(_panelRoot.transform, false);
            LootUIController.StretchFull(bodyRT);

            var vl = body.AddComponent<VerticalLayoutGroup>();
            vl.padding                = new RectOffset(12, 12, 10, 10);
            vl.spacing                = 6;
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;
            vl.childControlWidth      = true;
            vl.childControlHeight     = true;

            // ── Toggle UI Hotkey ────────────────────────────────────────────
            var uiHkRow = MakeRow(body.transform);
            var uiHkLbl = LootUIController.MakeTMP("uiHkLabel", uiHkRow.transform);
            uiHkLbl.text  = "Toggle UI Hotkey:";
            uiHkLbl.color = LootUIController.C_TextMuted;
            uiHkLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 130;

            TextMeshProUGUI uiBindingTMP;
            Image           uiHkBgImg;
            var uiHkBtn = LootUIController.MakeHotkeyButton("toggleUIHotkeyBtn", uiHkRow.transform,
                LootUIController.C_BtnNormal, 11, out uiBindingTMP, out uiHkBgImg);
            uiHkBtn.gameObject.AddComponent<LayoutElement>().preferredWidth  = 120;
            uiHkBtn.gameObject.GetComponent<LayoutElement>().preferredHeight = 22;
            // Outline must be on the BG child (same GO as the Image graphic)
            var uiHkOL = uiHkBgImg.gameObject.AddComponent<Outline>();
            uiHkOL.effectColor    = LootUIController.C_AccentBlue;
            uiHkOL.effectDistance = new Vector2(1, -1);
            uiHkOL.enabled = false;

            LootUIController.MakeDivider(body.transform);

            // ── Section: Autoloot ───────────────────────────────────────────
            AddSectionHeader(body.transform, "Autoloot");

            var autoRow = MakeRow(body.transform);
            _autoLootToggle = LootUIController.MakeToggle("autoLootToggle", autoRow.transform, "Enable Autoloot");
            var autoLE = _autoLootToggle.gameObject.AddComponent<LayoutElement>();
            autoLE.preferredHeight = 22;

            var distRow = MakeRow(body.transform);
            var distLbl = LootUIController.MakeTMP("distLabel", distRow.transform);
            distLbl.text  = "Autoloot Distance:";
            distLbl.color = LootUIController.C_TextMuted;
            var distLblLE = distLbl.gameObject.AddComponent<LayoutElement>();
            distLblLE.preferredWidth  = 130;
            distLblLE.preferredHeight = 20;

            _autoDistanceSlider = LootUIController.MakeSlider("autoDistance", distRow.transform);
            var sliderLE = _autoDistanceSlider.gameObject.AddComponent<LayoutElement>();
            sliderLE.flexibleWidth  = 1;
            sliderLE.preferredHeight = 20;

            _autoDistanceText = LootUIController.MakeTMP("autoText", distRow.transform);
            _autoDistanceText.text = "0";
            _autoDistanceText.alignment = TextAlignmentOptions.MidlineRight;
            var distValLE = _autoDistanceText.gameObject.AddComponent<LayoutElement>();
            distValLE.preferredWidth  = 32;
            distValLE.preferredHeight = 20;

            // Autoloot delay toggle
            var autoDelayToggleRow = MakeRow(body.transform);
            _autoLootDelayToggle = LootUIController.MakeToggle(
                "autoLootDelayToggle", autoDelayToggleRow.transform, "Out-of-Combat Autoloot Delay");
            _autoLootDelayToggle.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;

            // Autoloot delay slider (only visible when delay is enabled)
            _autoLootDelayRow = MakeRow(body.transform);
            var delayLbl = LootUIController.MakeTMP("delayLabel", _autoLootDelayRow.transform);
            delayLbl.text  = "Grace Period (sec):";
            delayLbl.color = LootUIController.C_TextMuted;
            var delayLblLE = delayLbl.gameObject.AddComponent<LayoutElement>();
            delayLblLE.preferredWidth  = 130;
            delayLblLE.preferredHeight = 20;

            _autoLootDelaySlider = LootUIController.MakeSlider("autoDelay", _autoLootDelayRow.transform);
            var delaySliderLE = _autoLootDelaySlider.gameObject.AddComponent<LayoutElement>();
            delaySliderLE.flexibleWidth   = 1;
            delaySliderLE.preferredHeight = 20;

            _autoLootDelayText = LootUIController.MakeTMP("delayText", _autoLootDelayRow.transform);
            _autoLootDelayText.text      = "0";
            _autoLootDelayText.alignment = TextAlignmentOptions.MidlineRight;
            var delayValLE = _autoLootDelayText.gameObject.AddComponent<LayoutElement>();
            delayValLE.preferredWidth  = 32;
            delayValLE.preferredHeight = 20;

            // Autoloot hotkey
            var autoHkRow = MakeRow(body.transform);
            var autoHkLbl = LootUIController.MakeTMP("autoHkLabel", autoHkRow.transform);
            autoHkLbl.text  = "Autoloot Hotkey:";
            autoHkLbl.color = LootUIController.C_TextMuted;
            autoHkLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 130;

            TextMeshProUGUI autoBindingTMP;
            Image           autoHkBgImg;
            var autoHkBtn = LootUIController.MakeHotkeyButton("autoLootHotkeyBtn", autoHkRow.transform,
                LootUIController.C_BtnNormal, 11, out autoBindingTMP, out autoHkBgImg);
            autoHkBtn.gameObject.AddComponent<LayoutElement>().preferredWidth  = 120;
            autoHkBtn.gameObject.GetComponent<LayoutElement>().preferredHeight = 22;
            // Outline must be on the BG child (same GO as the Image graphic)
            var autoHkOL = autoHkBgImg.gameObject.AddComponent<Outline>();
            autoHkOL.effectColor    = LootUIController.C_AccentBlue;
            autoHkOL.effectDistance = new Vector2(1, -1);
            autoHkOL.enabled = false;

            LootUIController.MakeDivider(body.transform);

            // ── Section: Loot Method ────────────────────────────────────────
            AddSectionHeader(body.transform, "Loot Method");

            var methodRow = MakeRow(body.transform);
            var methodLbl = LootUIController.MakeTMP("methodLabel", methodRow.transform);
            methodLbl.text  = "Method:";
            methodLbl.color = LootUIController.C_TextMuted;
            methodLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 80;
            _lootMethodDropdown = LootUIController.MakeDropdown("lootMethod", methodRow.transform);
            var dropLE = _lootMethodDropdown.transform.parent.gameObject.AddComponent<LayoutElement>();
            dropLE.flexibleWidth  = 1;
            dropLE.preferredHeight = 22;

            LootUIController.MakeDivider(body.transform);

            // ── Section: Bank Loot ──────────────────────────────────────────
            AddSectionHeader(body.transform, "Bank Loot");

            var bankToggleRow = MakeRow(body.transform);
            _bankLootToggle = LootUIController.MakeToggle("bankLootToggle", bankToggleRow.transform, "Enable Bank Loot");
            _bankLootToggle.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;

            var bankMethodRow = MakeRow(body.transform);
            var bankMethodLbl = LootUIController.MakeTMP("bankMethodLabel", bankMethodRow.transform);
            bankMethodLbl.text  = "Bank Method:";
            bankMethodLbl.color = LootUIController.C_TextMuted;
            bankMethodLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 100;
            _bankMethodDropdown = LootUIController.MakeDropdown("bankMethodDrop", bankMethodRow.transform);
            _bankMethodDropdown.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;
            _bankMethodDropdown.transform.parent.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;

            var bankPageRow = MakeRow(body.transform);
            var bankPageLbl = LootUIController.MakeTMP("bankPageLabel", bankPageRow.transform);
            bankPageLbl.text  = "Page Mode:";
            bankPageLbl.color = LootUIController.C_TextMuted;
            bankPageLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 100;
            _bankPageDropdown = LootUIController.MakeDropdown("bankPageDrop", bankPageRow.transform);
            _bankPageDropdown.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;
            _bankPageDropdown.transform.parent.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;

            // Page range sliders
            var pageFirstRow = MakeRow(body.transform);
            var pfLbl = LootUIController.MakeTMP("pfLabel", pageFirstRow.transform);
            pfLbl.text  = "First Page:";
            pfLbl.color = LootUIController.C_TextMuted;
            pfLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 80;
            _bankPageFirstSlider = LootUIController.MakeSlider("bankPageFirst", pageFirstRow.transform);
            _bankPageFirstSlider.gameObject.AddComponent<LayoutElement>().flexibleWidth   = 1;
            _bankPageFirstSlider.gameObject.GetComponent<LayoutElement>().preferredHeight = 20;
            _pageFirstText = LootUIController.MakeTMP("pageFirstText", pageFirstRow.transform);
            _pageFirstText.alignment = TextAlignmentOptions.MidlineRight;
            _pageFirstText.gameObject.AddComponent<LayoutElement>().preferredWidth = 32;

            var pageLastRow = MakeRow(body.transform);
            var plLbl = LootUIController.MakeTMP("plLabel", pageLastRow.transform);
            plLbl.text  = "Last Page:";
            plLbl.color = LootUIController.C_TextMuted;
            plLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 80;
            _bankPageLastSlider = LootUIController.MakeSlider("bankPageLast", pageLastRow.transform);
            _bankPageLastSlider.gameObject.AddComponent<LayoutElement>().flexibleWidth   = 1;
            _bankPageLastSlider.gameObject.GetComponent<LayoutElement>().preferredHeight = 20;
            _pageLastText = LootUIController.MakeTMP("pageLastText", pageLastRow.transform);
            _pageLastText.alignment = TextAlignmentOptions.MidlineRight;
            _pageLastText.gameObject.AddComponent<LayoutElement>().preferredWidth = 32;

            // ── Wire up logic ───────────────────────────────────────────────
            SetupAutoLootToggle();
            SetupAutoLootDistance();
            SetupAutoLootDelay();
            SetupLootMethod();
            SetupBankLootToggle();
            SetupBankMethod();
            SetupBankPageMode();
            SetupPageRange();
            UpdatePageRangeInteractable();

            SetupHotkeyBinder(uiHkBtn, uiBindingTMP, uiHkOL, Plugin.ToggleLootUIHotkey, out _toggleUIBinder);
            SetupHotkeyBinder(autoHkBtn, autoBindingTMP, autoHkOL, Plugin.ToggleAutoLootHotkey, out _autoLootBinder);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper — labelled horizontal row
        // ─────────────────────────────────────────────────────────────────────
        private static GameObject MakeRow(Transform parent)
        {
            var go = new GameObject("Row");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            var hl = go.AddComponent<HorizontalLayoutGroup>();
            hl.spacing                = 6;
            hl.childForceExpandWidth  = false;
            hl.childForceExpandHeight = false;
            hl.childControlWidth      = true;
            hl.childControlHeight     = true;
            go.AddComponent<LayoutElement>().preferredHeight = 24;
            return go;
        }

        private static void AddSectionHeader(Transform parent, string text)
        {
            var hdr = LootUIController.MakeTMP("Header_" + text, parent);
            hdr.text      = text.ToUpper();
            hdr.color     = LootUIController.C_TextMuted;
            hdr.fontSize  = 10;
            hdr.fontStyle = FontStyles.Bold;
            hdr.gameObject.AddComponent<LayoutElement>().preferredHeight = 16;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Logic (identical to original)
        // ─────────────────────────────────────────────────────────────────────
        private void SetupAutoLootToggle()
        {
            if (_autoLootToggle == null) return;
            _autoLootToggle.SetIsOnWithoutNotify(Plugin.AutoLootEnabled.Value);
            _autoLootToggle.onValueChanged.RemoveAllListeners();
            _autoLootToggle.onValueChanged.AddListener(v =>
            {
                Plugin.AutoLootEnabled.Value = v;
                UpdateAutoDistanceInteractable();
            });
            UpdateAutoDistanceInteractable();
        }

        public static void ApplyAutoLootFromExternal(bool value)
        {
            Plugin.AutoLootEnabled.Value = value;
            if (s_instance?._autoLootToggle != null)
            {
                s_instance._autoLootToggle.SetIsOnWithoutNotify(value);
                s_instance.UpdateAutoDistanceInteractable();
            }
        }

        private void SetupAutoLootDistance()
        {
            if (_autoDistanceSlider == null || _autoDistanceText == null) return;
            _autoDistanceSlider.minValue     = 0f;
            _autoDistanceSlider.maxValue     = 200f;
            _autoDistanceSlider.wholeNumbers = true;
            float current = Plugin.AutoLootDistance.Value;
            _autoDistanceSlider.SetValueWithoutNotify(current);
            _autoDistanceText.text = ((int)current).ToString();
            _autoDistanceSlider.onValueChanged.RemoveAllListeners();
            _autoDistanceSlider.onValueChanged.AddListener(v =>
            {
                Plugin.AutoLootDistance.Value = v;
                _autoDistanceText.text = ((int)v).ToString();
            });
        }

        private void SetupAutoLootDelay()
        {
            if (_autoLootDelayToggle == null) return;
            _autoLootDelayToggle.SetIsOnWithoutNotify(Plugin.AutoLootDelayEnabled.Value);
            _autoLootDelayToggle.onValueChanged.RemoveAllListeners();
            _autoLootDelayToggle.onValueChanged.AddListener(v =>
            {
                Plugin.AutoLootDelayEnabled.Value = v;
                UpdateAutoDelayInteractable();
            });

            if (_autoLootDelaySlider == null || _autoLootDelayText == null) return;
            _autoLootDelaySlider.minValue     = 0.5f;
            _autoLootDelaySlider.maxValue     = 10f;
            _autoLootDelaySlider.wholeNumbers = false;
            float current = Mathf.Clamp(Plugin.AutoLootDelay.Value, 0.5f, 10f);
            _autoLootDelaySlider.SetValueWithoutNotify(current);
            _autoLootDelayText.text = current.ToString("F1");
            _autoLootDelaySlider.onValueChanged.RemoveAllListeners();
            _autoLootDelaySlider.onValueChanged.AddListener(v =>
            {
                Plugin.AutoLootDelay.Value  = v;
                _autoLootDelayText.text     = v.ToString("F1");
            });

            UpdateAutoDelayInteractable();
        }

        private void UpdateAutoDelayInteractable()
        {
            bool enabled = Plugin.AutoLootEnabled.Value && Plugin.AutoLootDelayEnabled.Value;
            if (_autoLootDelaySlider != null) _autoLootDelaySlider.interactable = enabled;
            if (_autoLootDelayRow    != null) _autoLootDelayRow.SetActive(Plugin.AutoLootDelayEnabled.Value);
        }

        private void UpdateAutoDistanceInteractable()
        {
            if (_autoDistanceSlider != null)
                _autoDistanceSlider.interactable = Plugin.AutoLootEnabled.Value;
            UpdateAutoDelayInteractable();
        }

        private void SetupLootMethod()
        {
            if (_lootMethodDropdown == null) return;
            _lootMethodDropdown.ClearOptions();
            _lootMethodDropdown.AddOptions(_lootMethodOptions);
            int idx = _lootMethodOptions.IndexOf(Plugin.LootMethod.Value);
            _lootMethodDropdown.SetValueWithoutNotify(idx < 0 ? 0 : idx);
            _lootMethodDropdown.onValueChanged.RemoveAllListeners();
            _lootMethodDropdown.onValueChanged.AddListener(i =>
            {
                if (i < 0 || i >= _lootMethodOptions.Count) return;
                Plugin.LootMethod.Value = _lootMethodOptions[i];
                _visibilityChanged?.Invoke();
            });
            _visibilityChanged?.Invoke();
        }

        private void SetupBankLootToggle()
        {
            if (_bankLootToggle == null) return;
            _bankLootToggle.SetIsOnWithoutNotify(Plugin.BankLootEnabled.Value);
            _bankLootToggle.onValueChanged.RemoveAllListeners();
            _bankLootToggle.onValueChanged.AddListener(v =>
            {
                Plugin.BankLootEnabled.Value = v;
                UpdateBankControlsInteractable();
                _visibilityChanged?.Invoke();
            });
            UpdateBankControlsInteractable();
        }

        private void SetupBankMethod()
        {
            if (_bankMethodDropdown == null) return;
            _bankMethodDropdown.ClearOptions();
            _bankMethodDropdown.AddOptions(_bankMethodOptions);
            int idx = _bankMethodOptions.IndexOf(Plugin.BankLootMethod.Value);
            _bankMethodDropdown.SetValueWithoutNotify(idx < 0 ? 0 : idx);
            _bankMethodDropdown.onValueChanged.RemoveAllListeners();
            _bankMethodDropdown.onValueChanged.AddListener(i =>
            {
                if (i < 0 || i >= _bankMethodOptions.Count) return;
                Plugin.BankLootMethod.Value = _bankMethodOptions[i];
            });
        }

        private void SetupBankPageMode()
        {
            if (_bankPageDropdown == null) return;
            _bankPageDropdown.ClearOptions();
            _bankPageDropdown.AddOptions(_bankPageOptions);
            int idx = _bankPageOptions.IndexOf(Plugin.BankLootPageMode.Value);
            _bankPageDropdown.SetValueWithoutNotify(idx < 0 ? 0 : idx);
            _bankPageDropdown.onValueChanged.RemoveAllListeners();
            _bankPageDropdown.onValueChanged.AddListener(i =>
            {
                if (i < 0 || i >= _bankPageOptions.Count) return;
                Plugin.BankLootPageMode.Value = _bankPageOptions[i];
                UpdatePageRangeInteractable();
            });
        }

        private void SetupPageRange()
        {
            if (_bankPageFirstSlider == null || _pageFirstText == null ||
                _bankPageLastSlider  == null || _pageLastText  == null) return;

            _bankPageFirstSlider.wholeNumbers = true;
            _bankPageLastSlider.wholeNumbers  = true;
            _bankPageFirstSlider.minValue = 1;
            _bankPageFirstSlider.maxValue = 98;
            _bankPageLastSlider.minValue  = 1;
            _bankPageLastSlider.maxValue  = 98;

            _bankPageFirstSlider.SetValueWithoutNotify(Plugin.BankPageFirst.Value);
            _bankPageLastSlider.SetValueWithoutNotify(Plugin.BankPageLast.Value);
            _pageFirstText.text = Plugin.BankPageFirst.Value.ToString();
            _pageLastText.text  = Plugin.BankPageLast.Value.ToString();

            _bankPageFirstSlider.onValueChanged.RemoveAllListeners();
            _bankPageFirstSlider.onValueChanged.AddListener(v =>
            {
                int val = (int)v;
                Plugin.BankPageFirst.Value = val;
                _pageFirstText.text = val.ToString();
            });

            _bankPageLastSlider.onValueChanged.RemoveAllListeners();
            _bankPageLastSlider.onValueChanged.AddListener(v =>
            {
                int val = (int)v;
                Plugin.BankPageLast.Value = val;
                _pageLastText.text = val.ToString();
            });
        }

        // Sets interactable on a dropdown AND greys its wrapper visually.
        // Transition.None means Unity won't do this automatically.
        private static void SetDropdownInteractable(TMP_Dropdown drop, bool on)
        {
            if (drop == null) return;
            drop.interactable = on;
            // Wrapper is the direct parent — apply alpha via CanvasGroup
            var wrapper = drop.transform.parent?.gameObject;
            if (wrapper == null) return;
            var cg = wrapper.GetComponent<CanvasGroup>();
            if (cg == null) cg = wrapper.AddComponent<CanvasGroup>();
            cg.alpha          = on ? 1f : 0.4f;
            cg.interactable   = on;
            cg.blocksRaycasts = on;
        }

        private void UpdateBankControlsInteractable()
        {
            bool on = Plugin.BankLootEnabled.Value;
            SetDropdownInteractable(_bankMethodDropdown, on);
            SetDropdownInteractable(_bankPageDropdown,   on);
            UpdatePageRangeInteractable();
        }

        private void UpdatePageRangeInteractable()
        {
            bool on = Plugin.BankLootEnabled.Value && Plugin.BankLootPageMode.Value == "Page Range";
            if (_bankPageFirstSlider != null) _bankPageFirstSlider.interactable = on;
            if (_bankPageLastSlider  != null) _bankPageLastSlider.interactable  = on;
        }

        private void SetupHotkeyBinder(Button btn, TextMeshProUGUI label, Outline outline,
            ConfigEntry<KeyboardShortcut> configEntry, out HotkeyBindControl binderOut)
        {
            binderOut = null;
            if (btn == null || label == null) return;

            var binder = btn.gameObject.GetComponent<HotkeyBindControl>()
                         ?? btn.gameObject.AddComponent<HotkeyBindControl>();

            binder.SetLabel(label);
            if (outline != null)
            {
                binder.SetListeningHighlight(outline);
                outline.enabled = false;
            }

            binder.Configure(
                getter: () => configEntry.Value,
                setter: v => configEntry.Value = v,
                saver : () => configEntry.ConfigFile.Save()
            );

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(binder.BeginListening);


            binderOut = binder;
        }
    }
}