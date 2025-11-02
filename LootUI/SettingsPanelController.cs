using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public sealed class SettingsPanelController
    {
        private readonly GameObject _root;
        private readonly RectTransform _containerRect;
        private readonly System.Action _visibilityChanged;
        
        private Button _toggleUIHotkeyBtn;
        private TextMeshProUGUI _toggleUIBindingText;
        private Outline _toggleUIOutline;
        private HotkeyBindControl _toggleUIBinder;
        
        private Toggle _autoLootToggle;
        private Slider _autoDistanceSlider;
        private TextMeshProUGUI _autoDistanceText;

        private TMP_Dropdown _lootMethodDropdown;
        private Toggle _bankLootToggle;

        private TMP_Dropdown _bankMethodDropdown;
        private TMP_Dropdown _bankPageDropdown;

        private Slider _bankPageFirstSlider;
        private TextMeshProUGUI _pageFirstText;
        private Slider _bankPageLastSlider;
        private TextMeshProUGUI _pageLastText;

        private GameObject _dragHandle;

        private readonly List<string> _lootMethodOptions = new List<string> { "Blacklist", "Whitelist", "Standard" };
        private readonly List<string> _bankMethodOptions = new List<string> { "All", "Filtered" };
        private readonly List<string> _bankPageOptions   = new List<string> { "First Empty", "Page Range" };

        public SettingsPanelController(GameObject root, RectTransform containerRect, System.Action onVisibilityChanged)
        {
            _root = root;
            _containerRect = containerRect;
            _visibilityChanged = onVisibilityChanged;
        }

        public void Init()
        {
            _autoLootToggle      = UICommon.Find(_root, "container/panelBGsettings/settingsPanel/autoLootToggle")?.GetComponent<Toggle>();
            _autoDistanceSlider  = UICommon.Find(_root, "container/panelBGsettings/settingsPanel/autoDistance")?.GetComponent<Slider>();
            _autoDistanceText    = UICommon.Find(_root, "container/panelBGsettings/settingsPanel/autoText")?.GetComponent<TextMeshProUGUI>();
            _lootMethodDropdown  = UICommon.Find(_root, "container/panelBGsettings/settingsPanel/lootMethod")?.GetComponent<TMP_Dropdown>();
            _bankLootToggle      = UICommon.Find(_root, "container/panelBGsettings/settingsPanel/bankLootToggle")?.GetComponent<Toggle>();
            _bankMethodDropdown  = UICommon.Find(_root, "container/panelBGsettings/settingsPanel/bankMethodDrop")?.GetComponent<TMP_Dropdown>();
            _bankPageDropdown    = UICommon.Find(_root, "container/panelBGsettings/settingsPanel/bankPageDrop")?.GetComponent<TMP_Dropdown>();
            _bankPageFirstSlider = UICommon.Find(_root, "container/panelBGsettings/settingsPanel/bankPageFirst")?.GetComponent<Slider>();
            _pageFirstText       = UICommon.Find(_root, "container/panelBGsettings/settingsPanel/pageFirstText")?.GetComponent<TextMeshProUGUI>();
            _bankPageLastSlider  = UICommon.Find(_root, "container/panelBGsettings/settingsPanel/bankPageLast")?.GetComponent<Slider>();
            _pageLastText        = UICommon.Find(_root, "container/panelBGsettings/settingsPanel/pageLastText")?.GetComponent<TextMeshProUGUI>();
            _dragHandle          = UICommon.Find(_root, "container/panelBGsettings/lootUIDragHandle")?.gameObject;

            if (_dragHandle != null && _containerRect != null)
            {
                DragHandler dh = _dragHandle.GetComponent<DragHandler>();
                if (dh == null) dh = _dragHandle.AddComponent<DragHandler>();
                dh.PanelToMove = _containerRect;
            }

            SetupAutoLootToggle();
            SetupAutoLootDistance();
            SetupLootMethod();
            SetupBankLootToggle();
            SetupBankMethod();
            SetupBankPageMode();
            SetupPageRange();
            UpdatePageRangeInteractable();
            SetupToggleUIHotkey(); 
        }

        public void Show()
        {
            // nothing to load
        }

        private void SetupAutoLootToggle()
        {
            if (_autoLootToggle == null) return;
            _autoLootToggle.SetIsOnWithoutNotify(Plugin.AutoLootEnabled.Value);
            _autoLootToggle.onValueChanged.RemoveAllListeners();
            _autoLootToggle.onValueChanged.AddListener(delegate (bool v)
            {
                Plugin.AutoLootEnabled.Value = v;
                UpdateAutoDistanceInteractable();
            });
            UpdateAutoDistanceInteractable();
        }

        private void SetupAutoLootDistance()
        {
            if (_autoDistanceSlider == null || _autoDistanceText == null) return;

            _autoDistanceSlider.minValue = 0f;
            _autoDistanceSlider.maxValue = 200f;
            _autoDistanceSlider.wholeNumbers = true;

            float current = Plugin.AutoLootDistance.Value;
            _autoDistanceSlider.SetValueWithoutNotify(current);
            _autoDistanceText.text = string.Format("{0:F0}", (int)current);

            _autoDistanceSlider.onValueChanged.RemoveAllListeners();
            _autoDistanceSlider.onValueChanged.AddListener(delegate (float v)
            {
                Plugin.AutoLootDistance.Value = v;
                _autoDistanceText.text = string.Format("{0:F0}", (int)v);
            });
        }

        private void UpdateAutoDistanceInteractable()
        {
            if (_autoDistanceSlider == null) return;
            _autoDistanceSlider.interactable = Plugin.AutoLootEnabled.Value;
        }

        private void SetupLootMethod()
        {
            if (_lootMethodDropdown == null) return;

            _lootMethodDropdown.ClearOptions();
            _lootMethodDropdown.AddOptions(_lootMethodOptions);
            int idx = _lootMethodOptions.IndexOf(Plugin.LootMethod.Value);
            if (idx < 0) idx = 0;
            _lootMethodDropdown.SetValueWithoutNotify(idx);

            _lootMethodDropdown.onValueChanged.RemoveAllListeners();
            _lootMethodDropdown.onValueChanged.AddListener(delegate (int i)
            {
                if (i < 0 || i >= _lootMethodOptions.Count) return;
                Plugin.LootMethod.Value = _lootMethodOptions[i];
                if (_visibilityChanged != null) _visibilityChanged();
            });

            if (_visibilityChanged != null) _visibilityChanged();
        }

        private void SetupBankLootToggle()
        {
            if (_bankLootToggle == null) return;
            _bankLootToggle.SetIsOnWithoutNotify(Plugin.BankLootEnabled.Value);
            _bankLootToggle.onValueChanged.RemoveAllListeners();
            _bankLootToggle.onValueChanged.AddListener(delegate (bool v)
            {
                Plugin.BankLootEnabled.Value = v;
                UpdateBankControlsInteractable();
                if (_visibilityChanged != null) _visibilityChanged();
            });

            UpdateBankControlsInteractable();
        }

        private void SetupBankMethod()
        {
            if (_bankMethodDropdown == null) return;
            _bankMethodDropdown.ClearOptions();
            _bankMethodDropdown.AddOptions(_bankMethodOptions);
            int idx = _bankMethodOptions.IndexOf(Plugin.BankLootMethod.Value);
            if (idx < 0) idx = 0;

            _bankMethodDropdown.SetValueWithoutNotify(idx);
            _bankMethodDropdown.onValueChanged.RemoveAllListeners();
            _bankMethodDropdown.onValueChanged.AddListener(delegate (int i)
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
            if (idx < 0) idx = 0;

            _bankPageDropdown.SetValueWithoutNotify(idx);
            _bankPageDropdown.onValueChanged.RemoveAllListeners();
            _bankPageDropdown.onValueChanged.AddListener(delegate (int i)
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
            _bankPageFirstSlider.onValueChanged.AddListener(delegate (float v)
            {
                int val = (int)v;
                Plugin.BankPageFirst.Value = val;
                _pageFirstText.text = val.ToString();
            });

            _bankPageLastSlider.onValueChanged.RemoveAllListeners();
            _bankPageLastSlider.onValueChanged.AddListener(delegate (float v)
            {
                int val = (int)v;
                Plugin.BankPageLast.Value = val;
                _pageLastText.text = val.ToString();
            });
        }

        private void UpdateBankControlsInteractable()
        {
            bool on = Plugin.BankLootEnabled.Value;
            if (_bankMethodDropdown != null) _bankMethodDropdown.interactable = on;
            if (_bankPageDropdown  != null) _bankPageDropdown.interactable  = on;
            UpdatePageRangeInteractable();
        }

        private void UpdatePageRangeInteractable()
        {
            bool slidersOn = Plugin.BankLootEnabled.Value && Plugin.BankLootPageMode.Value == "Page Range";
            if (_bankPageFirstSlider != null) _bankPageFirstSlider.interactable = slidersOn;
            if (_bankPageLastSlider  != null) _bankPageLastSlider.interactable  = slidersOn;
        }
        
        private void SetupToggleUIHotkey()
        {
            var btnGO   = UICommon.Find(_root, "container/panelBGsettings/settingsPanel/toggleUIHotkeyBtn");
            var labelGO = UICommon.Find(_root, "container/panelBGsettings/settingsPanel/toggleUIHotkeyBtn/toggleUIBinding");

            if (btnGO == null || labelGO == null)
            {
                UpdateSocialLog.LogAdd("[LootUI] Hotkey button or label not found in prefab.", "red");
                return;
            }

            _toggleUIHotkeyBtn    = btnGO.GetComponent<Button>();
            _toggleUIBindingText  = labelGO.GetComponent<TextMeshProUGUI>();
            _toggleUIOutline      = btnGO.GetComponent<Outline>();

            if (_toggleUIHotkeyBtn == null || _toggleUIBindingText == null)
            {
                UpdateSocialLog.LogAdd("[LootUI] Missing Button or TextMeshProUGUI component for hotkey binder.", "red");
                return;
            }
            
            _toggleUIBinder = btnGO.GetComponent<HotkeyBindControl>();
            if (_toggleUIBinder == null)
                _toggleUIBinder = btnGO.gameObject.AddComponent<HotkeyBindControl>();
            
            _toggleUIBinder.SetLabel(_toggleUIBindingText);
            if (_toggleUIOutline != null)
            {
                _toggleUIBinder.SetListeningHighlight(_toggleUIOutline);
                _toggleUIBinder.SetListeningHighlight(_toggleUIOutline);
            }
            
            _toggleUIHotkeyBtn.onClick.RemoveAllListeners();
            _toggleUIHotkeyBtn.onClick.AddListener(_toggleUIBinder.BeginListening);
        }
    }
}
