using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public sealed class BlacklistPanelController
    {
        private readonly GameObject _root;
        private readonly RectTransform _containerRect;

        private Transform _blackitemContent;
        private Transform _blacklistContent;
        private TMPro.TMP_InputField _blackfilterInput;
        private Button _blackaddBtn;
        private Button _blackremoveBtn;
        private GameObject _dragHandle;

        private readonly List<string> _allItems = new List<string>();
        private readonly List<(Text text, bool isBlacklist)> _selected = new List<(Text text, bool isBlacklist)>();

        private readonly UICommon.DoubleClickTracker _doubleClick = new UICommon.DoubleClickTracker(0.25f);

        public BlacklistPanelController(GameObject root, RectTransform containerRect)
        {
            _root = root;
            _containerRect = containerRect;
        }

        public void Init()
        {
            _blackitemContent    = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blackitemView/Viewport/blackitemContent");
            _blacklistContent    = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blacklistView/Viewport/blacklistContent");
            _blackfilterInput    = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blacklistFilter")?.GetComponent<TMPro.TMP_InputField>();
            _blackaddBtn         = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blackaddBtn")?.GetComponent<Button>();
            _blackremoveBtn      = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blackremoveBtn")?.GetComponent<Button>();
            _dragHandle          = UICommon.Find(_root, "container/panelBGblacklist/lootUIDragHandle")?.gameObject;

            if (_dragHandle != null && _containerRect != null)
            {
                DragHandler dh = _dragHandle.GetComponent<DragHandler>();
                if (dh == null) dh = _dragHandle.AddComponent<DragHandler>();
                dh.PanelToMove = _containerRect;
            }

            if (_blackaddBtn != null)
            {
                _blackaddBtn.onClick.RemoveAllListeners();
                _blackaddBtn.onClick.AddListener(AddSelected);
            }

            if (_blackremoveBtn != null)
            {
                _blackremoveBtn.onClick.RemoveAllListeners();
                _blackremoveBtn.onClick.AddListener(RemoveSelected);
            }

            RebuildAllItems();
        }

        public void Show()
        {
            if (_blackitemContent == null || _blacklistContent == null)
            {
                Debug.LogError("[LootUI] Blacklist content panels not found.");
                return;
            }

            if (_blackfilterInput != null)
            {
                _blackfilterInput.onValueChanged.RemoveAllListeners();
                _blackfilterInput.onValueChanged.AddListener(delegate { RefreshUI(); });
            }

            RefreshUI();
        }

        private void RebuildAllItems()
        {
            _allItems.Clear();
            List<string> list = GameData.ItemDB.ItemDB
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.ItemName))
                .Select(x => x.ItemName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
            _allItems.AddRange(list);
        }

        private void RefreshUI()
        {
            UICommon.ClearList(_blackitemContent);
            UICommon.ClearList(_blacklistContent);
            _selected.Clear();

            string filter = _blackfilterInput != null && _blackfilterInput.text != null
                ? _blackfilterInput.text.ToLowerInvariant()
                : string.Empty;

            List<string> filteredItems = string.IsNullOrEmpty(filter)
                ? _allItems
                : _allItems.Where(i => i.ToLowerInvariant().Contains(filter)).ToList();

            List<string> filteredBlacklist = Plugin.Blacklist
                .Where(i => string.IsNullOrEmpty(filter) || i.ToLowerInvariant().Contains(filter))
                .OrderBy(i => i)
                .ToList();

            foreach (string item in filteredItems)
            {
                if (!filteredBlacklist.Contains(item))
                    CreateRow(_blackitemContent, item, false);
            }

            foreach (string item in filteredBlacklist)
            {
                CreateRow(_blacklistContent, item, true);
            }
        }

        private void CreateRow(Transform parent, string itemName, bool isBlacklist)
        {
            GameObject go = new GameObject(itemName);
            go.transform.SetParent(parent, false);

            Text text = go.AddComponent<Text>();
            text.text = itemName;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.color = isBlacklist ? Color.red : Color.white;
            text.fontSize = 14;

            Button btn = go.AddComponent<Button>();
            btn.onClick.AddListener(delegate
            {
                if (_doubleClick.IsDoubleClick(itemName))
                {
                    if (isBlacklist)
                    {
                        Plugin.Blacklist.Remove(itemName);
                        LootBlacklist.SaveBlacklist();
                        UpdateSocialLog.LogAdd("[LootUI] Removed from blacklist: " + itemName, "yellow");
                    }
                    else
                    {
                        Plugin.Blacklist.Add(itemName);
                        LootBlacklist.SaveBlacklist();
                        UpdateSocialLog.LogAdd("[LootUI] Added to blacklist: " + itemName, "yellow");
                    }
                    RefreshUI();
                    return;
                }

                bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool already = _selected.Any(e => e.text == text);

                if (ctrl)
                {
                    if (already)
                    {
                        text.color = isBlacklist ? Color.red : Color.white;
                        _selected.RemoveAll(e => e.text == text);
                    }
                    else
                    {
                        text.color = Color.green;
                        _selected.Add((text, isBlacklist));
                    }
                }
                else
                {
                    foreach (var entry in _selected.ToArray())
                    {
                        Text t = entry.text;
                        bool wasBlack = entry.isBlacklist;
                        t.color = wasBlack ? Color.red : Color.white;
                    }
                    _selected.Clear();

                    text.color = Color.green;
                    _selected.Add((text, isBlacklist));
                }
            });
        }

        private void AddSelected()
        {
            bool changed = false;
            foreach (var entry in _selected.ToArray())
            {
                Text text = entry.text;
                if (!Plugin.Blacklist.Contains(text.text))
                {
                    Plugin.Blacklist.Add(text.text);
                    changed = true;
                }
            }
            if (changed)
            {
                LootBlacklist.SaveBlacklist();
                RefreshUI();
                UpdateSocialLog.LogAdd("[LootUI] Added selected items to blacklist.", "yellow");
            }
            else
            {
                UpdateSocialLog.LogAdd("[LootUI] No valid items selected to add.", "red");
            }
        }

        private void RemoveSelected()
        {
            bool changed = false;
            foreach (var entry in _selected.ToArray())
            {
                Text text = entry.text;
                if (Plugin.Blacklist.Contains(text.text))
                {
                    Plugin.Blacklist.Remove(text.text);
                    changed = true;
                }
            }
            if (changed)
            {
                LootBlacklist.SaveBlacklist();
                RefreshUI();
                UpdateSocialLog.LogAdd("[LootUI] Removed selected items from blacklist.", "yellow");
            }
            else
            {
                UpdateSocialLog.LogAdd("[LootUI] No valid items selected to remove.", "red");
            }
        }
    }
}
