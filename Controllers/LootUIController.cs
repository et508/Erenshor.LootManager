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

        public static void Initialize(GameObject uiRoot)
        {
            _uiRoot = uiRoot;

            _settingsPanel    = Find("panelBG/settingsPanel")?.gameObject;
            _blacklistPanel   = Find("panelBG/blacklistPanel")?.gameObject;
            _menuBar          = Find("panelBG/menuBar")?.gameObject;
            _titleImage       = Find("panelBG/titleImage")?.gameObject;
            _filterInput      = Find("panelBG/blacklistPanel/blacklistFilter")?.GetComponent<TMP_InputField>();

            SetupMenuBarButtons();

            // Initial panel state
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
            // Always visible
            _menuBar?.SetActive(true);
            _titleImage?.SetActive(true);

            // Toggle panel visibility
            _settingsPanel?.SetActive(activePanel == _settingsPanel);
            _blacklistPanel?.SetActive(activePanel == _blacklistPanel);

            // Set up panel-specific UI
            if (activePanel == _blacklistPanel)
                SetupBlacklistPanel();
        }

        private static void SetupBlacklistPanel()
        {
            _itemContent      = Find("panelBG/blacklistPanel/itemView/Viewport/itemContent");
            _blacklistContent = Find("panelBG/blacklistPanel/blacklistView/Viewport/blacklistContent");

            Debug.Log("[LootUI] itemContent: " + _itemContent);
            Debug.Log("[LootUI] blacklistContent: " + _blacklistContent);

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

            string filter = _filterInput?.text?.ToLowerInvariant() ?? string.Empty;

            var filteredItems = string.IsNullOrEmpty(filter)
                ? _allItems
                : _allItems.Where(item => item.ToLowerInvariant().Contains(filter)).ToList();

            var filteredBlacklist = _blacklist
                .Where(item => string.IsNullOrEmpty(filter) || item.ToLowerInvariant().Contains(filter))
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
        }

        private static Transform Find(string path)
        {
            return _uiRoot?.transform.Find(path);
        }
    }
}
