using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LootManager
{
    public sealed class BlacklistPanelController
    {
        private readonly GameObject _root;
        private readonly RectTransform _containerRect;

        private Transform _blackitemContent;
        private Transform _blacklistContent;
        private Button _blacklistItemTemplate;
        private TMP_InputField _blackfilterInput;
        private Button _blackaddBtn;
        private Button _blackremoveBtn;
        private GameObject _dragHandle;

        private readonly List<string> _allItems = new List<string>();
        private readonly List<(TMP_Text text, bool isBlacklist)> _selected = new List<(TMP_Text text, bool isBlacklist)>();
        private readonly UICommon.DoubleClickTracker _doubleClick = new UICommon.DoubleClickTracker(0.25f);
        private readonly Dictionary<string, Sprite> _iconByName = new Dictionary<string, Sprite>();

        public BlacklistPanelController(GameObject root, RectTransform containerRect)
        {
            _root = root;
            _containerRect = containerRect;
        }

        public void Init()
        {
            _blackitemContent    = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blackitemView/Viewport/blackitemContent");
            _blacklistContent    = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blacklistView/Viewport/blacklistContent");
            _blacklistItemTemplate = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blackitemView/Viewport/blackitemContent/blacklistItem")?.GetComponent<Button>();
            _blackfilterInput    = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blacklistFilter")?.GetComponent<TMP_InputField>();
            _blackaddBtn         = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blackaddBtn")?.GetComponent<Button>();
            _blackremoveBtn      = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blackremoveBtn")?.GetComponent<Button>();
            _dragHandle          = UICommon.Find(_root, "container/panelBGblacklist/lootUIDragHandle")?.gameObject;

            if (_dragHandle != null && _containerRect != null)
            {
                var dh = _dragHandle.GetComponent<DragHandler>() ?? _dragHandle.AddComponent<DragHandler>();
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
            
            if (_blacklistItemTemplate != null)
                _blacklistItemTemplate.gameObject.SetActive(false);

            RebuildAllItems();
            BuildIconCache();
        }

        public void Show()
        {
            if (_blackitemContent == null || _blacklistContent == null || _blacklistItemTemplate == null)
            {
                Debug.LogError("[LootUI] Blacklist content/template not found.");
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
            var list = GameData.ItemDB.ItemDB
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.ItemName))
                .Select(x => x.ItemName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
            _allItems.AddRange(list);
        }

        private void BuildIconCache()
        {
            var db = GameData.ItemDB?.ItemDB;
            if (db == null) return;

            foreach (var it in db)
            {
                if (it == null) continue;
                var name = it.ItemName;
                if (string.IsNullOrWhiteSpace(name)) continue;

                if (!_iconByName.ContainsKey(name))
                    _iconByName[name] = it.ItemIcon;
            }
        }

        private Sprite GetIcon(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            if (_iconByName.TryGetValue(itemName, out var s)) return s;

            var db = GameData.ItemDB?.ItemDB;
            var itm = db?.FirstOrDefault(i => i != null && i.ItemName == itemName);
            s = itm?.ItemIcon;
            _iconByName[itemName] = s;
            return s;
        }

        private void RefreshUI()
        {
            UICommon.ClearListExceptTemplate(_blackitemContent, _blacklistItemTemplate != null ? _blacklistItemTemplate.gameObject : null);
            UICommon.ClearList(_blacklistContent);
            _selected.Clear();

            string filter = _blackfilterInput != null && _blackfilterInput.text != null
                ? _blackfilterInput.text.ToLowerInvariant()
                : string.Empty;

            var filteredItems = string.IsNullOrEmpty(filter)
                ? _allItems
                : _allItems.Where(i => i.ToLowerInvariant().Contains(filter)).ToList();

            var filteredBlacklist = Plugin.Blacklist
                .Where(i => string.IsNullOrEmpty(filter) || i.ToLowerInvariant().Contains(filter))
                .OrderBy(i => i)
                .ToList();

            foreach (var item in filteredItems)
            {
                if (!filteredBlacklist.Contains(item))
                    CreateRow(_blackitemContent, item, false);
            }

            foreach (var item in filteredBlacklist)
            {
                CreateRow(_blacklistContent, item, true);
            }

            Canvas.ForceUpdateCanvases();
            var rtA = _blackitemContent?.GetComponent<RectTransform>();
            if (rtA != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rtA);
            var rtB = _blacklistContent?.GetComponent<RectTransform>();
            if (rtB != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rtB);
        }

        private static Image EnsureClickTargetGraphic(GameObject go)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;
            return img;
        }

        private void CreateRow(Transform parent, string itemName, bool isBlacklist)
        {
            var go = GameObject.Instantiate(_blacklistItemTemplate.gameObject, parent);
            go.name = "blacklistItem_" + itemName;
            go.SetActive(true);

            var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            btn.targetGraphic = EnsureClickTargetGraphic(go);

            var iconTr  = go.transform.Find("Icon");
            var labelTr = go.transform.Find("Label");

            var icon  = iconTr  ? iconTr.GetComponent<Image>()     : null;
            var label = labelTr ? labelTr.GetComponent<TMP_Text>() : null;

            if (label != null)
            {
                label.text = itemName;
                label.color = isBlacklist ? Color.red : Color.white;
                label.raycastTarget = false;
            }

            if (icon != null)
            {
                var sprite = GetIcon(itemName);
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }

            btn.onClick.RemoveAllListeners();
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

                if (label == null) return;

                bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool already = _selected.Any(e => e.text == label);

                if (ctrl)
                {
                    if (already)
                    {
                        label.color = isBlacklist ? Color.red : Color.white;
                        _selected.RemoveAll(e => e.text == label);
                    }
                    else
                    {
                        label.color = Color.green;
                        _selected.Add((label, isBlacklist));
                    }
                }
                else
                {
                    foreach (var entry in _selected.ToArray())
                    {
                        var t = entry.text;
                        bool wasBlack = entry.isBlacklist;
                        if (t != null) t.color = wasBlack ? Color.red : Color.white;
                    }
                    _selected.Clear();

                    label.color = Color.green;
                    _selected.Add((label, isBlacklist));
                }
            });
        }

        private void AddSelected()
        {
            bool changed = false;
            foreach (var entry in _selected.ToArray())
            {
                var t = entry.text;
                if (t == null) continue;

                if (!Plugin.Blacklist.Contains(t.text))
                {
                    Plugin.Blacklist.Add(t.text);
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
                var t = entry.text;
                if (t == null) continue;

                if (Plugin.Blacklist.Contains(t.text))
                {
                    Plugin.Blacklist.Remove(t.text);
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
