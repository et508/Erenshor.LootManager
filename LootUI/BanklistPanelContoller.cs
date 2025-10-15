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

        private readonly List<(TMP_Text text, bool isBanklist)> _selected = new List<(TMP_Text text, bool isBanklist)>();
        private readonly UICommon.DoubleClickTracker _doubleClick = new UICommon.DoubleClickTracker(0.25f);
        private DebounceInvoker _debounce;

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

            // Drag handler
            if (_dragHandle != null && _containerRect != null)
            {
                var dh = _dragHandle.GetComponent<DragHandler>() ?? _dragHandle.AddComponent<DragHandler>();
                dh.PanelToMove = _containerRect;
            }

            // Buttons
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

            // Keep template hidden
            if (_banklistItemTemplate != null)
                _banklistItemTemplate.gameObject.SetActive(false);

            // Build shared caches once
            ItemLookup.EnsureBuilt();

            // Debouncer for filter input
            _debounce = DebounceInvoker.Attach(_root);
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
                _bankfilterInput.onValueChanged.AddListener(_ => _debounce.Schedule(RefreshUI, 0.15f));
            }

            RefreshUI();
        }

        // ---------- Core UI ----------

        private void RefreshUI()
        {
            // Preserve the template in the left column
            UICommon.ClearListExceptTemplate(_bankitemContent, _banklistItemTemplate != null ? _banklistItemTemplate.gameObject : null);
            UICommon.ClearList(_banklistContent);
            _selected.Clear();

            string filter = _bankfilterInput != null && _bankfilterInput.text != null
                ? _bankfilterInput.text.ToLowerInvariant()
                : string.Empty;

            var source = ItemLookup.AllItems;

            // Left list (available items not in banklist)
            var filteredItems = string.IsNullOrEmpty(filter)
                ? (source as List<string> ?? source.ToList())
                : source.Where(i => i.ToLowerInvariant().Contains(filter)).ToList();

            // Right list (current banklist)
            var filteredBanklist = Plugin.Banklist
                .Where(i => string.IsNullOrEmpty(filter) || i.ToLowerInvariant().Contains(filter))
                .OrderBy(i => i)
                .ToList();

            UILayoutBatch.WithLayoutSuspended(_bankitemContent, () =>
            {
                foreach (var item in filteredItems)
                {
                    if (!filteredBanklist.Contains(item))
                        CreateRow(_bankitemContent, item, false);
                }
            });

            UILayoutBatch.WithLayoutSuspended(_banklistContent, () =>
            {
                foreach (var item in filteredBanklist)
                    CreateRow(_banklistContent, item, true);
            });
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
                var sprite = ItemLookup.GetIcon(itemName);
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

        // ---------- Actions ----------

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
