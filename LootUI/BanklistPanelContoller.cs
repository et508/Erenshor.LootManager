using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LootManager
{
    public sealed class BanklistPanelController
    {
        private readonly GameObject _root;
        private readonly RectTransform _containerRect;

        private Transform _bankitemContent;
        private Transform _banklistContent;
        private Button _banklistItemTemplate;
        private TMP_InputField _bankfilterInput;
        private Button _addBtn;
        private Button _removeBtn;
        private GameObject _dragHandle;

        private readonly List<string> _allItems = new List<string>();
        private readonly List<(TMP_Text text, bool isBanklist)> _selected = new List<(TMP_Text text, bool isBanklist)>();
        private readonly UICommon.DoubleClickTracker _doubleClick = new UICommon.DoubleClickTracker(0.25f);
        private readonly Dictionary<string, Sprite> _iconByName = new Dictionary<string, Sprite>();

        public BanklistPanelController(GameObject root, RectTransform containerRect)
        {
            _root = root;
            _containerRect = containerRect;
        }

        public void Init()
        {
            _bankitemContent      = UICommon.Find(_root, "container/panelBGbanklist/banklistPanel/bankitemView/Viewport/bankitemContent");
            _banklistContent      = UICommon.Find(_root, "container/panelBGbanklist/banklistPanel/banklistView/Viewport/banklistContent");
            _banklistItemTemplate = UICommon.Find(_root, "container/panelBGbanklist/banklistPanel/bankitemView/Viewport/bankitemContent/banklistItem")?.GetComponent<Button>();
            _bankfilterInput      = UICommon.Find(_root, "container/panelBGbanklist/banklistPanel/banklistFilter")?.GetComponent<TMP_InputField>();
            _addBtn               = UICommon.Find(_root, "container/panelBGbanklist/banklistPanel/bankaddBtn")?.GetComponent<Button>();
            _removeBtn            = UICommon.Find(_root, "container/panelBGbanklist/banklistPanel/bankremoveBtn")?.GetComponent<Button>();
            _dragHandle           = UICommon.Find(_root, "container/panelBGbanklist/lootUIDragHandle")?.gameObject;

            if (_dragHandle != null && _containerRect != null)
            {
                var dh = _dragHandle.GetComponent<DragHandler>() ?? _dragHandle.AddComponent<DragHandler>();
                dh.PanelToMove = _containerRect;
            }

            if (_addBtn != null)
            {
                _addBtn.onClick.RemoveAllListeners();
                _addBtn.onClick.AddListener(AddSelected);
            }

            if (_removeBtn != null)
            {
                _removeBtn.onClick.RemoveAllListeners();
                _removeBtn.onClick.AddListener(RemoveSelected);
            }

            if (_banklistItemTemplate != null)
                _banklistItemTemplate.gameObject.SetActive(false); // keep as hidden prefab

            RebuildAllItems();
            BuildIconCache();
        }

        public void Show()
        {
            if (_bankitemContent == null || _banklistContent == null || _banklistItemTemplate == null)
            {
                Debug.LogError("[LootUI] Banklist content/template not found.");
                return;
            }

            if (_bankfilterInput != null)
            {
                _bankfilterInput.onValueChanged.RemoveAllListeners();
                _bankfilterInput.onValueChanged.AddListener(delegate { RefreshUI(); });
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

            foreach (var item in db)
            {
                if (item == null) continue;
                var name = item.ItemName;
                if (string.IsNullOrWhiteSpace(name)) continue;

                if (!_iconByName.ContainsKey(name))
                    _iconByName[name] = item.ItemIcon; // may be null; still cached
            }
        }

        private Sprite GetIcon(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            if (_iconByName.TryGetValue(itemName, out var s)) return s;

            var itm = GameData.ItemDB?.ItemDB?.FirstOrDefault(i => i != null && i.ItemName == itemName);
            s = itm?.ItemIcon;
            _iconByName[itemName] = s;
            return s;
        }

        private void RefreshUI()
        {
            // IMPORTANT: preserve the template under _bankitemContent
            UICommon.ClearListExceptTemplate(_bankitemContent, _banklistItemTemplate != null ? _banklistItemTemplate.gameObject : null);
            UICommon.ClearList(_banklistContent);
            _selected.Clear();

            string filter = _bankfilterInput != null && _bankfilterInput.text != null
                ? _bankfilterInput.text.ToLowerInvariant()
                : string.Empty;

            var filteredItems = string.IsNullOrEmpty(filter)
                ? _allItems
                : _allItems.Where(item => item.ToLowerInvariant().Contains(filter)).ToList();

            var filteredBanklist = Plugin.Banklist
                .Where(item => string.IsNullOrEmpty(filter) || item.ToLowerInvariant().Contains(filter))
                .OrderBy(item => item)
                .ToList();

            foreach (var item in filteredItems)
            {
                if (!filteredBanklist.Contains(item))
                    CreateRow(_bankitemContent, item, false);
            }

            foreach (var item in filteredBanklist)
            {
                CreateRow(_banklistContent, item, true);
            }

            Canvas.ForceUpdateCanvases();
            var a = _bankitemContent?.GetComponent<RectTransform>();
            if (a != null) LayoutRebuilder.ForceRebuildLayoutImmediate(a);
            var b = _banklistContent?.GetComponent<RectTransform>();
            if (b != null) LayoutRebuilder.ForceRebuildLayoutImmediate(b);
        }

        private static Image EnsureClickTargetGraphic(GameObject go)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f); // fully transparent but raycastable
            img.raycastTarget = true;
            return img;
        }

        private void CreateRow(Transform parent, string itemName, bool isBanklist)
        {
            var go = GameObject.Instantiate(_banklistItemTemplate.gameObject, parent);
            go.name = "banklistItem_" + itemName;
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
                label.color = isBanklist ? Color.blue : Color.white;
                label.raycastTarget = false; // let root handle clicks
            }

            if (icon != null)
            {
                var sprite = GetIcon(itemName);
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false; // let root handle clicks
                // icon.enabled = (sprite != null); // optional
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(delegate
            {
                if (_doubleClick.IsDoubleClick(itemName))
                {
                    if (isBanklist)
                    {
                        Plugin.Banklist.Remove(itemName);
                        LootBanklist.SaveBanklist();
                        UpdateSocialLog.LogAdd("[LootUI] Removed from banklist: " + itemName, "yellow");
                    }
                    else
                    {
                        Plugin.Banklist.Add(itemName);
                        LootBanklist.SaveBanklist();
                        UpdateSocialLog.LogAdd("[LootUI] Added to banklist: " + itemName, "yellow");
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
                        label.color = isBanklist ? Color.blue : Color.white;
                        _selected.RemoveAll(e => e.text == label);
                    }
                    else
                    {
                        label.color = Color.green;
                        _selected.Add((label, isBanklist));
                    }
                }
                else
                {
                    foreach (var entry in _selected.ToArray())
                    {
                        var t = entry.text;
                        bool wasBank = entry.isBanklist;
                        if (t != null) t.color = wasBank ? Color.blue : Color.white;
                    }
                    _selected.Clear();

                    label.color = Color.green;
                    _selected.Add((label, isBanklist));
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

                if (!Plugin.Banklist.Contains(t.text))
                {
                    Plugin.Banklist.Add(t.text);
                    changed = true;
                }
            }
            if (changed)
            {
                LootBanklist.SaveBanklist();
                RefreshUI();
                UpdateSocialLog.LogAdd("[LootUI] Added selected items to banklist.", "yellow");
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

                if (Plugin.Banklist.Contains(t.text))
                {
                    Plugin.Banklist.Remove(t.text);
                    changed = true;
                }
            }
            if (changed)
            {
                LootBanklist.SaveBanklist();
                RefreshUI();
                UpdateSocialLog.LogAdd("[LootUI] Removed selected items from banklist.", "yellow");
            }
            else
            {
                UpdateSocialLog.LogAdd("[LootUI] No valid items selected to remove.", "red");
            }
        }
    }
}
