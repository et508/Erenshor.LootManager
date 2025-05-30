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

        // Blacklist viewports
        private static Transform _itemContent;
        private static Transform _blacklistContent;

        // Search filter input
        private static TMP_InputField _filterInput;

        // Data
        private static List<string> _allItems = new List<string>();
        private static HashSet<string> _blacklist => Plugin.Blacklist;

        // Selection tracking
        private static List<(Text text, bool isBlacklist)> _selectedEntries = new List<(Text text, bool isBlacklist)>();

        // Blacklist add and remove buttons
        private static Button _addBtn;
        private static Button _removeBtn;
        
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
            _filterInput      = Find("panelBG/blacklistPanel/blacklistFilter")?.GetComponent<TMP_InputField>();
            _addBtn           = Find("panelBG/blacklistPanel/addBtn")?.GetComponent<Button>();
            _removeBtn        = Find("panelBG/blacklistPanel/removeBtn")?.GetComponent<Button>();

            _addBtn?.onClick.AddListener(AddSelectedToBlacklist);
            _removeBtn?.onClick.AddListener(RemoveSelectedFromBlacklist);

            SetupMenuBarButtons();
            ShowPanel(_settingsPanel);
        }

        private static void SetupMenuBarButtons()
        {
            var btnSettings  = Find("panelBG/menuBar/settingBtn")?.GetComponent<Button>();
            var btnBlacklist = Find("panelBG/menuBar/blacklistBtn")?.GetComponent<Button>();

            btnSettings?.onClick.AddListener(() => ShowPanel(_settingsPanel));
            btnBlacklist?.onClick.AddListener(() => ShowPanel(_blacklistPanel));
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

        private static void SetupBlacklistPanel()
        {
            _itemContent      = Find("panelBG/blacklistPanel/itemView/Viewport/itemContent");
            _blacklistContent = Find("panelBG/blacklistPanel/blacklistView/Viewport/blacklistContent");

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

                UpdateSocialLog.LogAdd($"[LootUI] Selected Items: {string.Join(", ", _selectedEntries.Select(s => s.text.text))}");
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
