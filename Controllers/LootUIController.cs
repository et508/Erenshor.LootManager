using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace LootManager
{
    public static class LootUIController
    {
        private static GameObject _uiRoot;

        // Panels
        private static GameObject _container;
        private static GameObject _panelBGsettings;
        private static GameObject _settingsPanel;
        private static GameObject _panelBGblacklist;
        private static GameObject _blacklistPanel;
        private static GameObject _panelBGwhitelist;
        private static GameObject _whitelistPanel;
        private static GameObject _panelBGbanklist;
        private static GameObject _banklistPanel;
        private static GameObject _menuBar;
        private static GameObject _titleImage;
        
        // Menu Buttons
        private static Button _menuSettingsBtn;
        private static Button _menuBlacklistBtn;
        private static Button _menuWhitelistBtn;
        private static Button _menuBanklistBtn;
        
        // Drag Handles
        private static GameObject _dragHangleSettings;
        private static GameObject _dragHangleBlacklist;
        private static GameObject _dragHangleWhitelist;
        private static GameObject _dragHangleBanklist;
        
        // Autoloot Toggle
        private static Toggle _autoLootToggle;
        
        // Autoloot Distance Slider
        private static Slider _autoDistanceSlider;
        private static TextMeshProUGUI _autoDistanceText;
        
        // Loot Method Dropdown
        private static TMP_Dropdown _lootMethodDropdown;
        private static readonly List<string> _lootMethodOptions = new List<string> { "Blacklist", "Whitelist", "Standard" };
        private static string _selectedLootMethod;
        
        // Bankloot Toggle
        private static Toggle _bankLootToggle;
        
        // Bankloot Method Dropdown
        private static TMP_Dropdown _bankMethodDropdown;
        private static readonly List<string> _bankMethodOptions = new List<string> { "All", "Filtered" };
        private static string _selectedBankMethod;
        
        // Bankloot Page Dropdown
        private static TMP_Dropdown _bankPageDropdown;
        private static readonly List<string> _bankPageOptions = new List<string> { "First Empty", "Page Range" };
        private static string _selectedBankPageMode;
        
        // Bankloot Page Range Sliders
        private static Slider _bankPageFirstSlider;
        private static TextMeshProUGUI _pageFirstText;
        private static Slider _bankPageLastSlider;
        private static TextMeshProUGUI _pageLastText;





        // Blacklist viewports
        private static Transform _blackitemContent;
        private static Transform _blacklistContent;

        // Blacklist Search filter input
        private static TMP_InputField _blackfilterInput;
        
        // Blacklist add and remove buttons
        private static Button _blackaddBtn;
        private static Button _blackremoveBtn;
        
        
        
        // Whitelist viewports
        private static Transform _whiteitemContent;
        private static Transform _whitelistContent;
        
        // Whitelist Search filter input
        private static TMP_InputField _whitefilterInput;
        
        // Whitelist add and remove buttons
        private static Button _whiteaddBtn;
        private static Button _whiteremoveBtn;
        
        
        
        // Loot equipment toggle
        private static Toggle _lootequipToggle;
        
        // Loot equipment tier dropdown
        private static TMP_Dropdown _equipmenttierDropdown;
        private static readonly List<string> _equipmenttierOptions = new List<string> { "All", "Normal Only", "Blessed Only", "Godly Only", "Blessed and up" };
        private static string _selectedequipmenttier;
        
        // Filterlist viewport
        private static Transform _filterlistContent;
        private static Toggle _filterGroupToggle;

        
        
        
        
        
        // Banklist viewports
        private static Transform _bankitemContent;
        private static Transform _banklistContent;
        
        // Banklist Search filter input
        private static TMP_InputField _bankfilterInput;
        
        // Banklist add and remove buttons
        private static Button _bankaddBtn;
        private static Button _bankremoveBtn;
        
        
        
        
        
        
        
        // Window Dragging
        private static bool dragging = false;
        private static Vector2 dragOffset;
        private static RectTransform panelRect;
        
        // Data
        private static List<string> _allItems = new List<string>();
        private static HashSet<string> _blacklist => Plugin.Blacklist;
        private static HashSet<string> _whitelist => Plugin.Whitelist;
        private static HashSet<string> _banklist => Plugin.Banklist;

        // Selection tracking
        private static List<(Text text, bool isBlacklist)> _selectedBlackEntries = new List<(Text text, bool isBlacklist)>();
        private static List<(Text text, bool isWhitelist)> _selectedWhiteEntries = new List<(Text text, bool isWhitelist)>();
        private static List<(Text text, bool isBanklist)> _selectedBankEntries = new List<(Text text, bool isBanklist)>();
        
        // Double click detection
        private static Dictionary<string, float> _lastClickTime = new Dictionary<string, float>();
        private const float DoubleClickThreshold = 0.25f; // Seconds


        public static void Initialize(GameObject uiRoot)
        {
            _uiRoot = uiRoot;
            
            _container        = Find("container")?.gameObject;
            _panelBGsettings = Find("container/panelBGsettings")?.gameObject;
            _settingsPanel    = Find("container/panelBGsettings/settingsPanel")?.gameObject;
            _panelBGblacklist = Find("container/panelBGblacklist")?.gameObject;
            _blacklistPanel   = Find("container/panelBGblacklist/blacklistPanel")?.gameObject;
            _panelBGwhitelist = Find("container/panelBGwhitelist")?.gameObject;
            _whitelistPanel   = Find("container/panelBGwhitelist/whitelistPanel")?.gameObject;
            _panelBGbanklist  = Find("container/panelBGbanklist")?.gameObject;
            _banklistPanel    = Find("container/panelBGbanklist/banklistPanel")?.gameObject;
            _menuBar          = Find("panelBG/menuBar")?.gameObject;
            _titleImage       = Find("panelBG/titleImage")?.gameObject;

            
            ShowPanel(_settingsPanel);
            SetupMenuBarButtons();
            SetupSettingsPanel();
        }
        
        private static void ShowPanel(GameObject activePanel)
        {
            _menuBar?.SetActive(true);
            _titleImage?.SetActive(true);
            
            _panelBGsettings?.SetActive(activePanel == _settingsPanel);
            _settingsPanel?.SetActive(activePanel == _settingsPanel);
            _panelBGblacklist?.SetActive(activePanel == _blacklistPanel);
            _blacklistPanel?.SetActive(activePanel == _blacklistPanel);
            _panelBGwhitelist?.SetActive(activePanel == _whitelistPanel);
            _whitelistPanel?.SetActive(activePanel == _whitelistPanel);
            _panelBGbanklist?.SetActive(activePanel == _banklistPanel);
            _banklistPanel?.SetActive(activePanel == _banklistPanel);
            
            var settingBtnOutline = Find("container/menuBar/settingBtn")?.GetComponent<Outline>();
            if (settingBtnOutline != null)
                settingBtnOutline.enabled = (activePanel == _settingsPanel);
            
            var blacklistBtnOutline = Find("container/menuBar/blacklistBtn")?.GetComponent<Outline>();
            if (blacklistBtnOutline != null)
                blacklistBtnOutline.enabled = (activePanel == _blacklistPanel);
            
            var whitelistBtnOutline = Find("container/menuBar/whitelistBtn")?.GetComponent<Outline>();
            if (whitelistBtnOutline != null)
                whitelistBtnOutline.enabled = (activePanel == _whitelistPanel);
            
            var banklistBtnOutline = Find("container/menuBar/banklistBtn")?.GetComponent<Outline>();
            if (banklistBtnOutline != null)
                banklistBtnOutline.enabled = (activePanel == _banklistPanel);

            if (activePanel == _blacklistPanel)
                SetupBlacklistPanel();
            
            if (activePanel == _whitelistPanel)
                SetupWhitelistPanel();
            
            if (activePanel == _banklistPanel)
                SetupBanklistPanel();
        }

        private static void SetupMenuBarButtons()
        {
            _menuSettingsBtn  = Find("container/menuBar/settingBtn")?.GetComponent<Button>();
            _menuBlacklistBtn = Find("container/menuBar/blacklistBtn")?.GetComponent<Button>();
            _menuWhitelistBtn = Find("container/menuBar/whitelistBtn")?.GetComponent<Button>();
            _menuBanklistBtn  = Find("container/menuBar/banklistBtn")?.GetComponent<Button>();

            _menuSettingsBtn?.onClick.AddListener(() => ShowPanel(_settingsPanel));
            _menuBlacklistBtn?.onClick.AddListener(() => ShowPanel(_blacklistPanel));
            _menuWhitelistBtn?.onClick.AddListener(() => ShowPanel(_whitelistPanel));
            _menuBanklistBtn?.onClick.AddListener(() => ShowPanel(_banklistPanel));
        }

        private static void MenuBarButtonState()
        {
            bool allowBlacklist = _selectedLootMethod == "Blacklist";
            if (_menuBlacklistBtn != null)
                _menuBlacklistBtn?.gameObject.SetActive(allowBlacklist);
            
            bool allowWhitelist = _selectedLootMethod == "Whitelist";
            if (_menuWhitelistBtn != null)
                _menuWhitelistBtn?.gameObject.SetActive(allowWhitelist);

            bool allowBanklist = Plugin.BankLootEnabled.Value;
            if (_menuBanklistBtn != null)
                _menuBanklistBtn?.gameObject.SetActive(allowBanklist);
        }
        
        // Settings Panel
        private static void SetupSettingsPanel()
        {
            _autoLootToggle      = Find("container/panelBGsettings/settingsPanel/autoLootToggle")?.GetComponent<Toggle>();
            _autoDistanceSlider  = Find("container/panelBGsettings/settingsPanel/autoDistance")?.GetComponent<Slider>();
            _autoDistanceText    = Find("container/panelBGsettings/settingsPanel/autoText")?.GetComponent<TextMeshProUGUI>();
            _lootMethodDropdown  = Find("container/panelBGsettings/settingsPanel/lootMethod")?.GetComponent<TMP_Dropdown>();
            _bankLootToggle      = Find("container/panelBGsettings/settingsPanel/bankLootToggle")?.GetComponent<Toggle>();
            _bankMethodDropdown  = Find("container/panelBGsettings/settingsPanel/bankMethodDrop")?.GetComponent<TMP_Dropdown>();
            _bankPageDropdown    = Find("container/panelBGsettings/settingsPanel/bankPageDrop")?.GetComponent<TMP_Dropdown>();
            _bankPageFirstSlider = Find("container/panelBGsettings/settingsPanel/bankPageFirst")?.GetComponent<Slider>();
            _pageFirstText       = Find("container/panelBGsettings/settingsPanel/pageFirstText")?.GetComponent<TextMeshProUGUI>();
            _bankPageLastSlider  = Find("container/panelBGsettings/settingsPanel/bankPageLast")?.GetComponent<Slider>();
            _pageLastText        = Find("container/panelBGsettings/settingsPanel/pageLastText")?.GetComponent<TextMeshProUGUI>();
            _dragHangleSettings  = Find("container/panelBGsettings/lootUIDragHandle")?.gameObject;
            
            AddDragEvents(_dragHangleSettings, _container.GetComponent<RectTransform>());
            
            var closeBtn = Find("container/panelBGsettings/settingsPanel/closeBtn")?.GetComponent<Button>();
            
            closeBtn?.onClick.AddListener(() =>
            {
                if (LootUI.Instance != null)
                    LootUI.Instance.ToggleUI();
            });

            
            SetupAutoLootToggle();
            SetupAutoLootDistanceSlider();
            SetupLootMethodDropdown();
            SetupBankLootToggle();
            SetupBankMethodDropdown();
            SetupBankPageDropdown();
            SetupBankPageRangeSliders();
        }
        
        private static void SetupAutoLootToggle()
        {
            if (_autoLootToggle == null)
            {
                Debug.LogWarning("[LootUI] autoLootToggle not found.");
                return;
            }
            
            
            _autoLootToggle.SetIsOnWithoutNotify(Plugin.AutoLootEnabled.Value);
            _autoLootToggle.onValueChanged.AddListener(OnAutoLootToggleChanged);
        }

        private static void OnAutoLootToggleChanged(bool isOn)
        {
            Plugin.AutoLootEnabled.Value = isOn;
            
            if (_bankMethodDropdown != null)
                _autoDistanceSlider.interactable = isOn;
            
            UpdateAutoDistanceInteractable();
        }
        
        private static void SetupAutoLootDistanceSlider()
        {
            if (_autoDistanceSlider == null || _autoDistanceText == null)
            {
                Debug.LogWarning("[LootUI] autoDistance slider or autoText not found.");
                return;
            }

            _autoDistanceSlider.minValue = 0f;
            _autoDistanceSlider.maxValue = 200f;
            _autoDistanceSlider.wholeNumbers = true;

            float currentValue = Plugin.AutoLootDistance.Value;
            _autoDistanceSlider.SetValueWithoutNotify(currentValue);
            _autoDistanceText.text = $"{(int)currentValue:F0}";

            _autoDistanceSlider.onValueChanged.AddListener(OnAutoDistanceChanged);
        }
        
        private static void OnAutoDistanceChanged(float newValue)
        {
            Plugin.AutoLootDistance.Value = newValue;
            if (_autoDistanceText != null)
                _autoDistanceText.text = $"{(int)newValue:F0}";
        }
        
        private static void UpdateAutoDistanceInteractable()
        {
            bool enabled = Plugin.AutoLootEnabled.Value;
    
            if (_autoDistanceSlider != null)
                _autoDistanceSlider.interactable = enabled;
        }

        
        private static void SetupLootMethodDropdown()
        {
            if (_lootMethodDropdown == null)
            {
                Debug.LogWarning("[LootUI] lootMethod dropdown not found.");
                return;
            }

            _lootMethodDropdown.ClearOptions();
            _lootMethodDropdown.AddOptions(_lootMethodOptions);

            int defaultIndex = _lootMethodOptions.IndexOf(Plugin.LootMethod.Value);
            _lootMethodDropdown.SetValueWithoutNotify(defaultIndex);
            _selectedLootMethod = _lootMethodOptions[defaultIndex];

            _lootMethodDropdown.onValueChanged.AddListener(OnLootMethodDropdownChanged);
            
            MenuBarButtonState();
        }
        
        private static void OnLootMethodDropdownChanged(int index)
        {
            if (index < 0 || index >= _lootMethodOptions.Count)
                return;

            _selectedLootMethod = _lootMethodOptions[index];
            Plugin.LootMethod.Value = _selectedLootMethod;
            
            MenuBarButtonState();
        }
        
        private static void SetupBankLootToggle()
        {
            if (_bankLootToggle == null)
            {
                Debug.LogWarning("[LootUI] bankLootToggle not found.");
                return;
            }

            _bankLootToggle.SetIsOnWithoutNotify(Plugin.BankLootEnabled.Value);
            _bankLootToggle.onValueChanged.AddListener(OnBankLootToggleChanged);
            
            MenuBarButtonState();
        }
        
        private static void OnBankLootToggleChanged(bool isOn)
        {
            Plugin.BankLootEnabled.Value = isOn;
            
            if (_bankMethodDropdown != null)
                _bankMethodDropdown.interactable = isOn;
            
            
            if (_bankPageDropdown != null)
                _bankPageDropdown.interactable = isOn;
            
            UpdateBankPageSliderInteractable();
            MenuBarButtonState();
        }
        
        private static void SetupBankMethodDropdown()
        {
            if (_bankMethodDropdown == null)
            {
                Debug.LogWarning("[LootUI] bankMethodDrop dropdown not found.");
                return;
            }

            _bankMethodDropdown.ClearOptions();
            _bankMethodDropdown.AddOptions(_bankMethodOptions);

            int defaultIndex = _bankMethodOptions.IndexOf(Plugin.BankLootMethod.Value);
            if (defaultIndex < 0) defaultIndex = 0;

            _bankMethodDropdown.SetValueWithoutNotify(defaultIndex);
            _selectedBankMethod = _bankMethodOptions[defaultIndex];

            _bankMethodDropdown.onValueChanged.AddListener(OnBankMethodDropdownChanged);
            
            _bankMethodDropdown.interactable = Plugin.BankLootEnabled.Value;
        }
        
        private static void OnBankMethodDropdownChanged(int index)
        {
            if (index < 0 || index >= _bankMethodOptions.Count)
                return;

            _selectedBankMethod = _bankMethodOptions[index];
            Plugin.BankLootMethod.Value = _selectedBankMethod;
        }
        
        private static void SetupBankPageDropdown()
        {
            if (_bankPageDropdown == null)
            {
                Debug.LogWarning("[LootUI] bankPageDrop dropdown not found.");
                return;
            }

            _bankPageDropdown.ClearOptions();
            _bankPageDropdown.AddOptions(_bankPageOptions);

            int defaultIndex = _bankPageOptions.IndexOf(Plugin.BankLootPageMode.Value);
            if (defaultIndex < 0) defaultIndex = 0;

            _bankPageDropdown.SetValueWithoutNotify(defaultIndex);
            _selectedBankPageMode = _bankPageOptions[defaultIndex];

            _bankPageDropdown.onValueChanged.AddListener(OnBankPageDropdownChanged);

            _bankPageDropdown.interactable = Plugin.BankLootEnabled.Value;
            
            UpdateBankPageSliderInteractable();
        }
        
        private static void OnBankPageDropdownChanged(int index)
        {
            if (index < 0 || index >= _bankPageOptions.Count)
                return;

            _selectedBankPageMode = _bankPageOptions[index];
            Plugin.BankLootPageMode.Value = _selectedBankPageMode;
            
            UpdateBankPageSliderInteractable();
        }
        
        private static void SetupBankPageRangeSliders()
        {
            if (_bankPageFirstSlider == null || _pageFirstText == null ||
                _bankPageLastSlider == null || _pageLastText == null)
            {
                Debug.LogWarning("[LootUI] Bank page range sliders or text missing.");
                return;
            }
            
            _bankPageFirstSlider.minValue = 1;
            _bankPageFirstSlider.maxValue = 98;
            _bankPageFirstSlider.wholeNumbers = true;

            _bankPageLastSlider.minValue = 1;
            _bankPageLastSlider.maxValue = 98;
            _bankPageLastSlider.wholeNumbers = true;

            _bankPageFirstSlider.SetValueWithoutNotify(Plugin.BankPageFirst.Value);
            _bankPageLastSlider.SetValueWithoutNotify(Plugin.BankPageLast.Value);

            _pageFirstText.text = Plugin.BankPageFirst.Value.ToString();
            _pageLastText.text  = Plugin.BankPageLast.Value.ToString();

            _bankPageFirstSlider.onValueChanged.AddListener(val =>
            {
                int newVal = (int)val;
                Plugin.BankPageFirst.Value = newVal;
                _pageFirstText.text = newVal.ToString();
            });

            _bankPageLastSlider.onValueChanged.AddListener(val =>
            {
                int newVal = (int)val;
                Plugin.BankPageLast.Value = newVal;
                _pageLastText.text = newVal.ToString();
            });

            UpdateBankPageSliderInteractable();
        }
        
        private static void UpdateBankPageSliderInteractable()
        {
            bool slidersEnabled = Plugin.BankLootEnabled.Value &&
                                  Plugin.BankLootPageMode.Value == "Page Range";

            if (_bankPageFirstSlider != null)
                _bankPageFirstSlider.interactable = slidersEnabled;

            if (_bankPageLastSlider != null)
                _bankPageLastSlider.interactable = slidersEnabled;
        }
        


        // Blacklist Panel
        private static void SetupBlacklistPanel()
        {
            _blackitemContent      = Find("container/panelBGblacklist/blacklistPanel/blackitemView/Viewport/blackitemContent");
            _blacklistContent      = Find("container/panelBGblacklist/blacklistPanel/blacklistView/Viewport/blacklistContent");
            _blackfilterInput      = Find("container/panelBGblacklist/blacklistPanel/blacklistFilter")?.GetComponent<TMP_InputField>();
            _blackaddBtn           = Find("container/panelBGblacklist/blacklistPanel/blackaddBtn")?.GetComponent<Button>();
            _blackremoveBtn        = Find("container/panelBGblacklist/blacklistPanel/blackremoveBtn")?.GetComponent<Button>();
            _dragHangleBlacklist   = Find("container/panelBGblacklist/lootUIDragHandle")?.gameObject;
            
            AddDragEvents(_dragHangleBlacklist, _container.GetComponent<RectTransform>());
            
            var closeBtn = Find("container/panelBGblacklist/blacklistPanel/closeBtn")?.GetComponent<Button>();
            
            closeBtn?.onClick.AddListener(() =>
            {
                if (LootUI.Instance != null)
                    LootUI.Instance.ToggleUI();
            });
            
            _blackaddBtn?.onClick.RemoveAllListeners();
            _blackaddBtn?.onClick.AddListener(AddSelectedToBlacklist);
            
            _blackremoveBtn?.onClick.RemoveAllListeners();
            _blackremoveBtn?.onClick.AddListener(RemoveSelectedFromBlacklist);

            if (_blackitemContent == null || _blacklistContent == null)
            {
                Debug.LogError("[LootUI] One or more content panels not found. Check prefab paths.");
                return;
            }

            _allItems = GameData.ItemDB.ItemDB
                .Where(item => !string.IsNullOrWhiteSpace(item.ItemName))
                .Select(item => item.ItemName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (_blackfilterInput != null)
            {
                _blackfilterInput.onValueChanged.RemoveAllListeners();
                _blackfilterInput.onValueChanged.AddListener(delegate { RefreshBlacklistUI(); });
            }

            RefreshBlacklistUI();
        }

        private static void RefreshBlacklistUI()
        {
            ClearList(_blackitemContent);
            ClearList(_blacklistContent);
            _selectedBlackEntries.Clear();

            string filter = _blackfilterInput?.text?.ToLowerInvariant() ?? string.Empty;

            var filteredItems = string.IsNullOrEmpty(filter)
                ? _allItems
                : _allItems.Where(item => item.ToLowerInvariant().Contains(filter)).ToList();

            var filteredBlacklist = _blacklist
                .Where(item => string.IsNullOrEmpty(filter) || item.ToLowerInvariant().Contains(filter))
                .OrderBy(item => item)
                .ToList();

            foreach (var item in filteredItems)
            {
                if (!filteredBlacklist.Contains(item))
                    CreateItemEntryBlacklist(_blackitemContent, item, isBlacklist: false);
            }

            foreach (var item in filteredBlacklist)
            {
                CreateItemEntryBlacklist(_blacklistContent, item, isBlacklist: true);
            }
        }
        
        private static void CreateItemEntryBlacklist(Transform parent, string itemName, bool isBlacklist)
        {
            GameObject go = new GameObject(itemName);
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.text = itemName;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.color = isBlacklist ? Color.red : Color.white;
            text.fontSize = 14;

            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                float time = Time.time;
                bool isDoubleClick = _lastClickTime.TryGetValue(itemName, out float lastClick) && (time - lastClick < DoubleClickThreshold);
                _lastClickTime[itemName] = time;

                if (isDoubleClick)
                {
                    if (isBlacklist)
                    {
                        Plugin.Blacklist.Remove(itemName);
                        LootBlacklist.SaveBlacklist();
                        UpdateSocialLog.LogAdd($"[LootUI] Removed from blacklist: {itemName}", "yellow");
                    }
                    else
                    {
                        Plugin.Blacklist.Add(itemName);
                        LootBlacklist.SaveBlacklist();
                        UpdateSocialLog.LogAdd($"[LootUI] Added to blacklist: {itemName}", "yellow");
                    }

                    RefreshBlacklistUI();
                    return;
                }

                bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool alreadySelected = _selectedBlackEntries.Any(entry => entry.text == text);

                if (ctrlHeld)
                {
                    if (alreadySelected)
                    {
                        text.color = isBlacklist ? Color.red : Color.white;
                        _selectedBlackEntries.RemoveAll(entry => entry.text == text);
                    }
                    else
                    {
                        text.color = Color.green;
                        _selectedBlackEntries.Add((text, isBlacklist));
                    }
                }
                else
                {
                    foreach (var (selectedText, wasBlacklist) in _selectedBlackEntries)
                    {
                        selectedText.color = wasBlacklist ? Color.red : Color.white;
                    }

                    _selectedBlackEntries.Clear();

                    text.color = Color.green;
                    _selectedBlackEntries.Add((text, isBlacklist));
                }
            });

        }
        
        private static void AddSelectedToBlacklist()
        {
            var added = false;
            foreach (var (text, _) in _selectedBlackEntries.ToList())
            {
                if (!Plugin.Blacklist.Contains(text.text))
                {
                    Plugin.Blacklist.Add(text.text);
                    added = true;
                }
            }

            if (added)
            {
                LootBlacklist.SaveBlacklist();
                RefreshBlacklistUI();
                UpdateSocialLog.LogAdd("[LootUI] Added selected items to blacklist.", "yellow");
            }
            else
            {
                UpdateSocialLog.LogAdd("[LootUI] No valid items selected to add.", "red");
            }
        }

        private static void RemoveSelectedFromBlacklist()
        {
            var removed = false;
            foreach (var (text, _) in _selectedBlackEntries.ToList())
            {
                if (Plugin.Blacklist.Contains(text.text))
                {
                    Plugin.Blacklist.Remove(text.text);
                    removed = true;
                }
            }

            if (removed)
            {
                LootBlacklist.SaveBlacklist();
                RefreshBlacklistUI();
                UpdateSocialLog.LogAdd("[LootUI] Removed selected items from blacklist.", "yellow");
            }
            else
            {
                UpdateSocialLog.LogAdd("[LootUI] No valid items selected to remove.", "red");
            }
        }
        
        
        
        
        
        
        // Whitelist Panel
        private static void SetupWhitelistPanel()
        {
            _whiteitemContent      = Find("container/panelBGwhitelist/whitelistPanel/whiteitemView/Viewport/whiteitemContent");
            _whitelistContent      = Find("container/panelBGwhitelist/whitelistPanel/whitelistView/Viewport/whitelistContent");
            _whitefilterInput      = Find("container/panelBGwhitelist/whitelistPanel/whitelistFilter")?.GetComponent<TMP_InputField>();
            _whiteaddBtn           = Find("container/panelBGwhitelist/whitelistPanel/whiteaddBtn")?.GetComponent<Button>();
            _whiteremoveBtn        = Find("container/panelBGwhitelist/whitelistPanel/whiteremoveBtn")?.GetComponent<Button>();
            _lootequipToggle       = Find("container/panelBGwhitelist/whitelistPanel/lootequipToggle")?.GetComponent<Toggle>();
            _equipmenttierDropdown = Find("container/panelBGwhitelist/whitelistPanel/equipmenttierDropdown")?.GetComponent<TMP_Dropdown>();
            _filterlistContent     = Find("container/panelBGwhitelist/whitelistPanel/filterlistView/Viewport/filterlistContent");
            _filterGroupToggle  = Find("container/panelBGwhitelist/whitelistPanel/filterlistView/Viewport/filterlistContent/filterCategoryToggle")?.GetComponent<Toggle>();
            _dragHangleWhitelist   = Find("container/panelBGwhitelist/lootUIDragHandle")?.gameObject;
            
            AddDragEvents(_dragHangleWhitelist, _container.GetComponent<RectTransform>());
            
            var closeBtn = Find("container/panelBGwhitelist/whitelistPanel/closeBtn")?.GetComponent<Button>();
            
            closeBtn?.onClick.AddListener(() =>
            {
                if (LootUI.Instance != null)
                    LootUI.Instance.ToggleUI();
            });
            
            _whiteaddBtn?.onClick.RemoveAllListeners();
            _whiteaddBtn?.onClick.AddListener(AddSelectedToWhitelist);
            
            _whiteremoveBtn?.onClick.RemoveAllListeners();
            _whiteremoveBtn?.onClick.AddListener(RemoveSelectedFromWhitelist);

            if (_whiteitemContent == null || _whitelistContent == null)
            {
                Debug.LogError("[LootUI] One or more content panels not found. Check prefab paths.");
                return;
            }

            _allItems = GameData.ItemDB.ItemDB
                .Where(item => !string.IsNullOrWhiteSpace(item.ItemName))
                .Select(item => item.ItemName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (_whitefilterInput != null)
            {
                _whitefilterInput.onValueChanged.RemoveAllListeners();
                _whitefilterInput.onValueChanged.AddListener(delegate { RefreshWhitelistUI(); });
            }

            RefreshWhitelistUI();
            SetupLootEquipToggle();
            SetupEquipmentTierDropdown();
            SetupFilterListContent();
        }

        private static void SetupLootEquipToggle()
        {
            if (_lootequipToggle == null)
            {
                Debug.LogWarning("[LootUI] lootequipToggle not found.");
                return;
            }
            
            
            _lootequipToggle.SetIsOnWithoutNotify(Plugin.LootEquipment.Value);
            _lootequipToggle.onValueChanged.AddListener(OnLootEquipToggleChanged);
            
        }
        
        private static void OnLootEquipToggleChanged(bool isOn)
        {
            Plugin.LootEquipment.Value = isOn;
            
          //  if (_bankMethodDropdown != null)
          //      _autoDistanceSlider.interactable = isOn;
            
          // UpdateAutoDistanceInteractable();
        }
        
        private static void SetupEquipmentTierDropdown()
        {
            if (_equipmenttierDropdown == null)
            {
                Debug.LogWarning("[LootUI] equipmenttier dropdown not found.");
                return;
            }

            _equipmenttierDropdown.ClearOptions();
            _equipmenttierDropdown.AddOptions(_equipmenttierOptions);

            int defaultIndex = (int)Plugin.LootEquipmentTier.Value;
            
            if (defaultIndex < 0 || defaultIndex >= _equipmenttierOptions.Count)
                defaultIndex = 0;
            
            _equipmenttierDropdown.SetValueWithoutNotify(defaultIndex);
            _selectedequipmenttier = _equipmenttierOptions[defaultIndex];

            _equipmenttierDropdown.onValueChanged.AddListener(OnEquipmentTierDropdownChanged);
        }
        
        private static void OnEquipmentTierDropdownChanged(int index)
        {
            if (index < 0 || index >= _equipmenttierOptions.Count)
                return;

            _selectedequipmenttier = _equipmenttierOptions[index];
            Plugin.LootEquipmentTier.Value = (EquipmentTierSetting)index;
        }

        
        private static void RefreshWhitelistUI()
        {
            ClearList(_whiteitemContent);
            ClearList(_whitelistContent);
            _selectedWhiteEntries.Clear();

            string filter = _whitefilterInput?.text?.ToLowerInvariant() ?? string.Empty;

            var filteredItems = string.IsNullOrEmpty(filter)
                ? _allItems
                : _allItems.Where(item => item.ToLowerInvariant().Contains(filter)).ToList();

            var filteredWhitelist = _whitelist
                .Where(item => string.IsNullOrEmpty(filter) || item.ToLowerInvariant().Contains(filter))
                .OrderBy(item => item)
                .ToList();

            foreach (var item in filteredItems)
            {
                if (!filteredWhitelist.Contains(item))
                    CreateItemEntryWhitelist(_whiteitemContent, item, isWhitelist: false);
            }

            foreach (var item in filteredWhitelist)
            {
                CreateItemEntryWhitelist(_whitelistContent, item, isWhitelist: true);
            }
        }
        
        private static void CreateItemEntryWhitelist(Transform parent, string itemName, bool isWhitelist)
        {
            GameObject go = new GameObject(itemName);
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.text = itemName;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.color = isWhitelist ? Color.white : Color.red;
            text.fontSize = 14;

            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                float time = Time.time;
                bool isDoubleClick = _lastClickTime.TryGetValue(itemName, out float lastClick) && (time - lastClick < DoubleClickThreshold);
                _lastClickTime[itemName] = time;

                if (isDoubleClick)
                {
                    if (isWhitelist)
                    {
                        Plugin.Whitelist.Remove(itemName);
                        LootWhitelist.SaveWhitelist();
                        UpdateSocialLog.LogAdd($"[LootUI] Removed from whitelist: {itemName}", "yellow");
                    }
                    else
                    {
                        Plugin.Whitelist.Add(itemName);
                        LootWhitelist.SaveWhitelist();
                        UpdateSocialLog.LogAdd($"[LootUI] Added to whitelist: {itemName}", "yellow");
                    }

                    RefreshWhitelistUI();
                    return;
                }

                bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool alreadySelected = _selectedWhiteEntries.Any(entry => entry.text == text);

                if (ctrlHeld)
                {
                    if (alreadySelected)
                    {
                        text.color = isWhitelist ? Color.white : Color.red;
                        _selectedWhiteEntries.RemoveAll(entry => entry.text == text);
                    }
                    else
                    {
                        text.color = Color.green;
                        _selectedWhiteEntries.Add((text, isWhitelist));
                    }
                }
                else
                {
                    foreach (var (selectedText, wasWhitelist) in _selectedWhiteEntries)
                    {
                        selectedText.color = wasWhitelist ? Color.white : Color.red;
                    }

                    _selectedWhiteEntries.Clear();

                    text.color = Color.green;
                    _selectedWhiteEntries.Add((text, isWhitelist));
                }
            });

        }
        
        private static void AddSelectedToWhitelist()
        {
            var added = false;
            foreach (var (text, _) in _selectedWhiteEntries.ToList())
            {
                if (!Plugin.Whitelist.Contains(text.text))
                {
                    Plugin.Whitelist.Add(text.text);
                    added = true;
                }
            }

            if (added)
            {
                LootWhitelist.SaveWhitelist();
                RefreshWhitelistUI();
                UpdateSocialLog.LogAdd("[LootUI] Added selected items to whitelist.", "yellow");
            }
            else
            {
                UpdateSocialLog.LogAdd("[LootUI] No valid items selected to add.", "red");
            }
        }
        
        private static void RemoveSelectedFromWhitelist()
        {
            var removed = false;
            foreach (var (text, _) in _selectedWhiteEntries.ToList())
            {
                if (Plugin.Whitelist.Contains(text.text))
                {
                    Plugin.Whitelist.Remove(text.text);
                    removed = true;
                }
            }

            if (removed)
            {
                LootWhitelist.SaveWhitelist();
                RefreshWhitelistUI();
                UpdateSocialLog.LogAdd("[LootUI] Removed selected items from whitelist.", "yellow");
            }
            else
            {
                UpdateSocialLog.LogAdd("[LootUI] No valid items selected to remove.", "red");
            }
        }
        
        private static void SetupFilterListContent()
        {
            if (_filterlistContent == null || _filterGroupToggle == null)
            {
                Debug.LogWarning("[LootUI] Missing filterlistContent or template.");
                return;
            }

            // Clear existing toggles
            foreach (Transform child in _filterlistContent)
            {
                if (child != _filterGroupToggle.transform)
                    GameObject.Destroy(child.gameObject);
            }

            foreach (var category in Plugin.FilterList.Keys.Reverse())
            {
                Toggle toggleInstance = GameObject.Instantiate(_filterGroupToggle, _filterlistContent);
                GameObject toggleGO = toggleInstance.gameObject;
                toggleGO.name = $"Toggle_{category}";
                toggleGO.SetActive(true);
                
                var toggle = toggleInstance;
                var label = toggleGO.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = category;

                // Set toggle state based on saved preferences
                bool isEnabled = Plugin.EnabledFilterGroups.Contains(category);
                toggle.isOn = isEnabled;

                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener((value) =>
                {
                    if (value)
                        Plugin.EnabledFilterGroups.Add(category);
                    else
                        Plugin.EnabledFilterGroups.Remove(category);
                });
            }
            
            // **CRUCIAL**: Rebuild layouts so content rect resizes to fit
            Canvas.ForceUpdateCanvases();
            RectTransform contentRT = _filterlistContent.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        // Banklist Panel
        private static void SetupBanklistPanel()
        {
            _bankitemContent      = Find("container/panelBGbanklist/banklistPanel/bankitemView/Viewport/bankitemContent");
            _banklistContent      = Find("container/panelBGbanklist/banklistPanel/banklistView/Viewport/banklistContent");
            _bankfilterInput      = Find("container/panelBGbanklist/banklistPanel/banklistFilter")?.GetComponent<TMP_InputField>();
            _bankaddBtn           = Find("container/panelBGbanklist/banklistPanel/bankaddBtn")?.GetComponent<Button>();
            _bankremoveBtn        = Find("container/panelBGbanklist/banklistPanel/bankremoveBtn")?.GetComponent<Button>();
            _dragHangleBanklist   = Find("container/panelBGbanklist/lootUIDragHandle")?.gameObject;
            
            AddDragEvents(_dragHangleBanklist, _container.GetComponent<RectTransform>());
            
            var closeBtn = Find("container/panelBGbanklist/banklistPanel/closeBtn")?.GetComponent<Button>();
            
            closeBtn?.onClick.AddListener(() =>
            {
                if (LootUI.Instance != null)
                    LootUI.Instance.ToggleUI();
            });
            
            _bankaddBtn?.onClick.RemoveAllListeners();
            _bankaddBtn?.onClick.AddListener(AddSelectedToBanklist);

            _bankremoveBtn?.onClick.RemoveAllListeners();
            _bankremoveBtn?.onClick.AddListener(RemoveSelectedFromBanklist);

            if (_bankitemContent == null || _banklistContent == null)
            {
                Debug.LogError("[LootUI] One or more content panels not found. Check prefab paths.");
                return;
            }

            _allItems = GameData.ItemDB.ItemDB
                .Where(item => !string.IsNullOrWhiteSpace(item.ItemName))
                .Select(item => item.ItemName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (_bankfilterInput != null)
            {
                _bankfilterInput.onValueChanged.RemoveAllListeners();
                _bankfilterInput.onValueChanged.AddListener(delegate { RefreshBanklistUI(); });
            }

            RefreshBanklistUI();
        }
        
        private static void RefreshBanklistUI()
        {
            ClearList(_bankitemContent);
            ClearList(_banklistContent);
            _selectedBankEntries.Clear();

            string filter = _bankfilterInput?.text?.ToLowerInvariant() ?? string.Empty;

            var filteredItems = string.IsNullOrEmpty(filter)
                ? _allItems
                : _allItems.Where(item => item.ToLowerInvariant().Contains(filter)).ToList();

            var filteredBanklist = _banklist
                .Where(item => string.IsNullOrEmpty(filter) || item.ToLowerInvariant().Contains(filter))
                .OrderBy(item => item)
                .ToList();

            foreach (var item in filteredItems)
            {
                if (!filteredBanklist.Contains(item))
                    CreateItemEntryBanklist(_bankitemContent, item, isBanklist: false);
            }

            foreach (var item in filteredBanklist)
            {
                CreateItemEntryBanklist(_banklistContent, item, isBanklist: true);
            }
        }
        
        private static void CreateItemEntryBanklist(Transform parent, string itemName, bool isBanklist)
        {
            GameObject go = new GameObject(itemName);
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.text = itemName;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.color = isBanklist ? Color.blue : Color.white;
            text.fontSize = 14;

            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                float time = Time.time;
                bool isDoubleClick = _lastClickTime.TryGetValue(itemName, out float lastClick) && (time - lastClick < DoubleClickThreshold);
                _lastClickTime[itemName] = time;

                if (isDoubleClick)
                {
                    if (isBanklist)
                    {
                        Plugin.Banklist.Remove(itemName);
                        LootBanklist.SaveBanklist();
                        UpdateSocialLog.LogAdd($"[LootUI] Removed from banklist: {itemName}", "yellow");
                    }
                    else
                    {
                        Plugin.Banklist.Add(itemName);
                        LootBanklist.SaveBanklist();
                        UpdateSocialLog.LogAdd($"[LootUI] Added to banklist: {itemName}", "yellow");
                    }

                    RefreshBanklistUI();
                    return;
                }

                bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool alreadySelected = _selectedBankEntries.Any(entry => entry.text == text);

                if (ctrlHeld)
                {
                    if (alreadySelected)
                    {
                        text.color = isBanklist ? Color.blue : Color.white;
                        _selectedBankEntries.RemoveAll(entry => entry.text == text);
                    }
                    else
                    {
                        text.color = Color.green;
                        _selectedBankEntries.Add((text, isBanklist));
                    }
                }
                else
                {
                    foreach (var (selectedText, wasBanklist) in _selectedBankEntries)
                    {
                        selectedText.color = wasBanklist ? Color.blue : Color.white;
                    }

                    _selectedBankEntries.Clear();

                    text.color = Color.green;
                    _selectedBankEntries.Add((text, isBanklist));
                }
            });

        }
        
        private static void AddSelectedToBanklist()
        {
            var added = false;
            foreach (var (text, _) in _selectedBankEntries.ToList())
            {
                if (!Plugin.Banklist.Contains(text.text))
                {
                    Plugin.Banklist.Add(text.text);
                    added = true;
                }
            }

            if (added)
            {
                LootBanklist.SaveBanklist();
                RefreshBanklistUI();
                UpdateSocialLog.LogAdd("[LootUI] Added selected items to banklist.", "yellow");
            }
            else
            {
                UpdateSocialLog.LogAdd("[LootUI] No valid items selected to add.", "red");
            }
        }
        
        private static void RemoveSelectedFromBanklist()
        {
            var removed = false;
            foreach (var (text, _) in _selectedBankEntries.ToList())
            {
                if (Plugin.Banklist.Contains(text.text))
                {
                    Plugin.Banklist.Remove(text.text);
                    removed = true;
                }
            }

            if (removed)
            {
                LootBanklist.SaveBanklist();
                RefreshBanklistUI();
                UpdateSocialLog.LogAdd("[LootUI] Removed selected items from banklist.", "yellow");
            }
            else
            {
                UpdateSocialLog.LogAdd("[LootUI] No valid items selected to remove.", "red");
            }
        }
                
                
        
        
        
        
        
        
        
        
        
        
        
        // Clear list 
        private static void ClearList(Transform content)
        {
            foreach (Transform child in content)
                GameObject.Destroy(child.gameObject);
        }
        
        // Window Dragging
        private static void AddDragEvents(GameObject dragHandle, RectTransform panelToMove)
        {
            panelRect = panelToMove;
            var trigger = dragHandle.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = dragHandle.AddComponent<EventTrigger>();
            
            var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener((data) =>
            {
                var eventData = (PointerEventData)data;
                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    dragging = true;
                    GameData.DraggingUIElement = true;

                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        panelRect, eventData.position, null, out dragOffset
                    );
                }
            });
            trigger.triggers.Add(pointerDown);
            
            var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            drag.callback.AddListener((data) =>
            {
                if (!dragging) return;

                Vector2 pointerPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    panelRect.parent as RectTransform, ((PointerEventData)data).position, null, out pointerPos
                );
                panelRect.anchoredPosition = pointerPos - dragOffset;
            });
            trigger.triggers.Add(drag);
            
            var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUp.callback.AddListener((data) =>
            {
                dragging = false;
                GameData.DraggingUIElement = false;
            });
            trigger.triggers.Add(pointerUp);
        }
        
        private static Transform Find(string path)
        {
            return _uiRoot?.transform.Find(path);
        }
    }
}
