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

        private readonly List<(TMP_Text text, bool isBlacklist)> _selected = new List<(TMP_Text text, bool isBlacklist)>();
        private readonly UICommon.DoubleClickTracker _doubleClick = new UICommon.DoubleClickTracker(0.25f);
        private DebounceInvoker _debounce;

        public BlacklistPanelController(GameObject root, RectTransform containerRect)
        {
            _root = root;
            _containerRect = containerRect;
        }

        public void Init()
        {
            _blackitemContent      = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blackitemView/Viewport/blackitemContent");
            _blacklistContent      = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blacklistView/Viewport/blacklistContent");
            _blacklistItemTemplate = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blackitemView/Viewport/blackitemContent/blacklistItem")?.GetComponent<Button>();
            _blackfilterInput      = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blacklistFilter")?.GetComponent<TMP_InputField>();
            _blackaddBtn           = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blackaddBtn")?.GetComponent<Button>();
            _blackremoveBtn        = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blackremoveBtn")?.GetComponent<Button>();
            _dragHandle            = UICommon.Find(_root, "container/panelBGblacklist/lootUIDragHandle")?.gameObject;

            // Drag handler
            if (_dragHandle != null && _containerRect != null)
            {
                var dh = _dragHandle.GetComponent<DragHandler>() ?? _dragHandle.AddComponent<DragHandler>();
                dh.PanelToMove = _containerRect;
            }

            // Buttons
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

            // Keep template hidden
            if (_blacklistItemTemplate != null)
                _blacklistItemTemplate.gameObject.SetActive(false);

            // Build shared caches once
            ItemLookup.EnsureBuilt();

            // Debouncer for filter input
            _debounce = DebounceInvoker.Attach(_root);
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
                _blackfilterInput.onValueChanged.AddListener(_ => _debounce.Schedule(RefreshUI, 0.15f));
            }

            RefreshUI();
        }

        // ---------- Core UI ----------

        private void RefreshUI()
        {
            // Preserve the template under left list
            UICommon.ClearListExceptTemplate(_blackitemContent, _blacklistItemTemplate != null ? _blacklistItemTemplate.gameObject : null);
            UICommon.ClearList(_blacklistContent);
            _selected.Clear();

            string filter = _blackfilterInput != null && _blackfilterInput.text != null
                ? _blackfilterInput.text.ToLowerInvariant()
                : string.Empty;

            var source = ItemLookup.AllItems;

            // Left list (available items not in blacklist)
            var filteredItems = string.IsNullOrEmpty(filter)
                ? (source as List<string> ?? source.ToList())
                : source.Where(i => i.ToLowerInvariant().Contains(filter)).ToList();

            // Right list (current blacklist)
            var filteredBlacklist = Plugin.Blacklist
                .Where(i => string.IsNullOrEmpty(filter) || i.ToLowerInvariant().Contains(filter))
                .OrderBy(i => i)
                .ToList();

            UILayoutBatch.WithLayoutSuspended(_blackitemContent, () =>
            {
                foreach (var item in filteredItems)
                {
                    if (!filteredBlacklist.Contains(item))
                        CreateRow(_blackitemContent, item, false);
                }
            });

            UILayoutBatch.WithLayoutSuspended(_blacklistContent, () =>
            {
                foreach (var item in filteredBlacklist)
                    CreateRow(_blacklistContent, item, true);
            });
        }

        private static Image EnsureClickTargetGraphic(GameObject go)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f); // fully transparent but raycastable
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

        // ---------- Actions ----------

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
