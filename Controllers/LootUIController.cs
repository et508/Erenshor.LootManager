using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public static class LootUIController
    {
        private static GameObject _uiRoot;

        // Panels
        private static GameObject _settingsPanel;
        private static GameObject _blacklistPanel;
        private static GameObject _menuBar;
        private static GameObject _titleImage;
        
        // Autoloot Dropdown
        private static TMP_Dropdown _autoLootDropdown;
        private static readonly List<string> _autoLootOptions = new List<string> { "On", "Off", "ErenshorQOL" };
        private static string _selectedAutoLootMode;
        
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
        private static Transform _itemContent;
        private static Transform _blacklistContent;

        // Search filter input
        private static TMP_InputField _filterInput;
        
        // Blacklist add and remove buttons
        private static Button _addBtn;
        private static Button _removeBtn;

        // Data
        private static List<string> _allItems = new List<string>();
        private static HashSet<string> _blacklist => Plugin.Blacklist;

        // Selection tracking
        private static List<(Text text, bool isBlacklist)> _selectedEntries = new List<(Text text, bool isBlacklist)>();
        
        // Double click detection
        private static Dictionary<string, float> _lastClickTime = new Dictionary<string, float>();
        private const float DoubleClickThreshold = 0.25f; // Seconds


        public static void Initialize(GameObject uiRoot)
        {
            _uiRoot = uiRoot;

            _settingsPanel    = Find("panelBG/settingsPanel")?.gameObject;
            _blacklistPanel   = Find("panelBG/blacklistPanel")?.gameObject;
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

            _settingsPanel?.SetActive(activePanel == _settingsPanel);
            _blacklistPanel?.SetActive(activePanel == _blacklistPanel);

            if (activePanel == _blacklistPanel)
                SetupBlacklistPanel();
        }

        private static void SetupMenuBarButtons()
        {
            var btnSettings  = Find("panelBG/menuBar/settingBtn")?.GetComponent<Button>();
            var btnBlacklist = Find("panelBG/menuBar/blacklistBtn")?.GetComponent<Button>();

            btnSettings?.onClick.AddListener(() => ShowPanel(_settingsPanel));
            btnBlacklist?.onClick.AddListener(() => ShowPanel(_blacklistPanel));
        }

        private static void SetupSettingsPanel()
        {
            _autoLootDropdown = Find("panelBG/settingsPanel/autoLootDrop")?.GetComponent<TMP_Dropdown>();
            _autoDistanceSlider = Find("panelBG/settingsPanel/autoDistance")?.GetComponent<Slider>();
            _autoDistanceText   = Find("panelBG/settingsPanel/autoText")?.GetComponent<TextMeshProUGUI>();
            _lootMethodDropdown = Find("panelBG/settingsPanel/lootMethod")?.GetComponent<TMP_Dropdown>();
            _bankLootToggle = Find("panelBG/settingsPanel/bankLootToggle")?.GetComponent<Toggle>();
            _bankMethodDropdown = Find("panelBG/settingsPanel/bankMethodDrop")?.GetComponent<TMP_Dropdown>();
            _bankPageDropdown = Find("panelBG/settingsPanel/bankPageDrop")?.GetComponent<TMP_Dropdown>();
            _bankPageFirstSlider = Find("panelBG/settingsPanel/bankPageFirst")?.GetComponent<Slider>();
            _pageFirstText       = Find("panelBG/settingsPanel/pageFirstText")?.GetComponent<TextMeshProUGUI>();
            _bankPageLastSlider  = Find("panelBG/settingsPanel/bankPageLast")?.GetComponent<Slider>();
            _pageLastText        = Find("panelBG/settingsPanel/pageLastText")?.GetComponent<TextMeshProUGUI>();

            
            SetupAutoLootDropdown();
            SetupAutoLootDistanceSlider();
            SetupLootMethodDropdown();
            SetupBankLootToggle();
            SetupBankMethodDropdown();
            SetupBankPageDropdown();
            SetupBankPageRangeSliders();
        }
        
        private static void SetupAutoLootDropdown()
        {
            if (_autoLootDropdown == null)
            {
                Debug.LogWarning("[LootUI] autoLootDrop dropdown not found.");
                return;
            }

            _autoLootDropdown.ClearOptions();
            _autoLootDropdown.AddOptions(_autoLootOptions);

            int defaultIndex = Plugin.AutoLootEnabled.Value ? 0 : 1;
            _autoLootDropdown.SetValueWithoutNotify(defaultIndex);
            
            UpdateAutoDistanceInteractable();

            _autoLootDropdown.onValueChanged.AddListener(OnAutoLootDropdownChanged);
            _selectedAutoLootMode = _autoLootOptions[defaultIndex];
        }

        private static void OnAutoLootDropdownChanged(int index)
        {
            if (index < 0 || index >= _autoLootOptions.Count)
                return;

            _selectedAutoLootMode = _autoLootOptions[index];
            Plugin.AutoLootEnabled.Value = (_selectedAutoLootMode == "On");
            
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
            bool enabled = _selectedAutoLootMode == "On";
    
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
        }
        
        private static void OnLootMethodDropdownChanged(int index)
        {
            if (index < 0 || index >= _lootMethodOptions.Count)
                return;

            _selectedLootMethod = _lootMethodOptions[index];
            Plugin.LootMethod.Value = _selectedLootMethod;

            UpdateSocialLog.LogAdd($"[LootUI] Loot method changed to: {_selectedLootMethod}", "blue");
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
        }
        
        private static void OnBankLootToggleChanged(bool isOn)
        {
            Plugin.BankLootEnabled.Value = isOn;
            UpdateSocialLog.LogAdd($"[LootUI] BankLoot toggle set to: {isOn}", "blue");
            
            if (_bankMethodDropdown != null)
                _bankMethodDropdown.interactable = isOn;
            
            
            if (_bankPageDropdown != null)
                _bankPageDropdown.interactable = isOn;
            
            UpdateBankPageSliderInteractable();
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

            // Optionally grey it out if bankLootToggle is off
            _bankMethodDropdown.interactable = Plugin.BankLootEnabled.Value;
        }
        
        private static void OnBankMethodDropdownChanged(int index)
        {
            if (index < 0 || index >= _bankMethodOptions.Count)
                return;

            _selectedBankMethod = _bankMethodOptions[index];
            Plugin.BankLootMethod.Value = _selectedBankMethod;

            UpdateSocialLog.LogAdd($"[LootUI] BankLoot method changed to: {_selectedBankMethod}", "blue");
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

            UpdateSocialLog.LogAdd($"[LootUI] BankLoot page mode changed to: {_selectedBankPageMode}", "cyan");
        }
        
        private static void SetupBankPageRangeSliders()
        {
            if (_bankPageFirstSlider == null || _pageFirstText == null ||
                _bankPageLastSlider == null || _pageLastText == null)
            {
                Debug.LogWarning("[LootUI] Bank page range sliders or text missing.");
                return;
            }

            // Set reasonable limits
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
                UpdateSocialLog.LogAdd($"[LootUI] BankLoot start page set to {newVal}", "cyan");
            });

            _bankPageLastSlider.onValueChanged.AddListener(val =>
            {
                int newVal = (int)val;
                Plugin.BankPageLast.Value = newVal;
                _pageLastText.text = newVal.ToString();
                UpdateSocialLog.LogAdd($"[LootUI] BankLoot end page set to {newVal}", "cyan");
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



        
        
        
        
        
        


        
        private static void SetupBlacklistPanel()
        {
            _itemContent      = Find("panelBG/blacklistPanel/itemView/Viewport/itemContent");
            _blacklistContent = Find("panelBG/blacklistPanel/blacklistView/Viewport/blacklistContent");
            _filterInput      = Find("panelBG/blacklistPanel/blacklistFilter")?.GetComponent<TMP_InputField>();
            _addBtn           = Find("panelBG/blacklistPanel/addBtn")?.GetComponent<Button>();
            _removeBtn        = Find("panelBG/blacklistPanel/removeBtn")?.GetComponent<Button>();
            
            _addBtn?.onClick.AddListener(AddSelectedToBlacklist);
            _removeBtn?.onClick.AddListener(RemoveSelectedFromBlacklist);

            if (_itemContent == null || _blacklistContent == null)
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

            if (_filterInput != null)
            {
                _filterInput.onValueChanged.RemoveAllListeners();
                _filterInput.onValueChanged.AddListener(delegate { RefreshBlacklistUI(); });
            }

            RefreshBlacklistUI();
        }

        private static void RefreshBlacklistUI()
        {
            ClearList(_itemContent);
            ClearList(_blacklistContent);
            _selectedEntries.Clear();

            string filter = _filterInput?.text?.ToLowerInvariant() ?? string.Empty;

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
                    CreateItemEntry(_itemContent, item, isBlacklist: false);
            }

            foreach (var item in filteredBlacklist)
            {
                CreateItemEntry(_blacklistContent, item, isBlacklist: true);
            }
        }

        private static void ClearList(Transform content)
        {
            foreach (Transform child in content)
                GameObject.Destroy(child.gameObject);
        }
        
        private static void CreateItemEntry(Transform parent, string itemName, bool isBlacklist)
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
                        UpdateSocialLog.LogAdd($"[LootUI] Removed from blacklist (double-click): {itemName}", "green");
                    }
                    else
                    {
                        Plugin.Blacklist.Add(itemName);
                        LootBlacklist.SaveBlacklist();
                        UpdateSocialLog.LogAdd($"[LootUI] Added to blacklist (double-click): {itemName}", "green");
                    }

                    RefreshBlacklistUI();
                    return;
                }

                bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool alreadySelected = _selectedEntries.Any(entry => entry.text == text);

                if (ctrlHeld)
                {
                    if (alreadySelected)
                    {
                        text.color = isBlacklist ? Color.red : Color.white;
                        _selectedEntries.RemoveAll(entry => entry.text == text);
                    }
                    else
                    {
                        text.color = Color.green;
                        _selectedEntries.Add((text, isBlacklist));
                    }
                }
                else
                {
                    foreach (var (selectedText, wasBlacklist) in _selectedEntries)
                    {
                        selectedText.color = wasBlacklist ? Color.red : Color.white;
                    }

                    _selectedEntries.Clear();

                    text.color = Color.green;
                    _selectedEntries.Add((text, isBlacklist));
                }

                // UpdateSocialLog.LogAdd($"[LootUI] Selected Items: {string.Join(", ", _selectedEntries.Select(s => s.text.text))}");
            });

        }


        private static void AddSelectedToBlacklist()
        {
            var added = false;
            foreach (var (text, isBlacklist) in _selectedEntries.ToList())
            {
                if (!isBlacklist && !_blacklist.Contains(text.text))
                {
                    _blacklist.Add(text.text);
                    added = true;
                }
            }

            if (added)
            {
                LootBlacklist.SaveBlacklist();
                RefreshBlacklistUI();
                UpdateSocialLog.LogAdd("[LootUI] Added selected items to blacklist.", "green");
            }
            else
            {
                UpdateSocialLog.LogAdd("[LootUI] No valid items selected to add.", "yellow");
            }
        }

        private static void RemoveSelectedFromBlacklist()
        {
            var removed = false;
            foreach (var (text, isBlacklist) in _selectedEntries.ToList())
            {
                if (isBlacklist && _blacklist.Contains(text.text))
                {
                    _blacklist.Remove(text.text);
                    removed = true;
                }
            }

            if (removed)
            {
                LootBlacklist.SaveBlacklist();
                RefreshBlacklistUI();
                UpdateSocialLog.LogAdd("[LootUI] Removed selected items from blacklist.", "green");
            }
            else
            {
                UpdateSocialLog.LogAdd("[LootUI] No valid items selected to remove.", "yellow");
            }
        }

        private static Transform Find(string path)
        {
            return _uiRoot?.transform.Find(path);
        }
    }
}
