using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public sealed class WhitelistPanelController
    {
        private readonly GameObject _root;
        private readonly RectTransform _containerRect;

        // View refs
        private Transform _whiteitemContent;
        private Transform _whitelistContent;
        private Button _whitelistItemTemplate;
        private TMP_InputField _filterInput;
        private Button _addBtn;
        private Button _removeBtn;
        private Toggle _lootEquipToggle;
        private TMP_Dropdown _equipmentTierDropdown;
        private Transform _filterlistContent;
        private Toggle _filterCategoryTemplate;
        private GameObject _dragHandle;

        // Local state
        private readonly List<(TMP_Text text, bool isWhitelist)> _selected = new List<(TMP_Text text, bool isWhitelist)>();
        private readonly UICommon.DoubleClickTracker _doubleClick = new UICommon.DoubleClickTracker(0.25f);
        private DebounceInvoker _debounce;

        public WhitelistPanelController(GameObject root, RectTransform containerRect)
        {
            _root = root;
            _containerRect = containerRect;
        }

        public void Init()
        {
            // Lookups
            _whiteitemContent        = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/whiteitemView/Viewport/whiteitemContent");
            _whitelistContent        = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/whitelistView/Viewport/whitelistContent");
            _whitelistItemTemplate   = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/whiteitemView/Viewport/whiteitemContent/whitelistItem")?.GetComponent<Button>();
            _filterInput             = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/whitelistFilter")?.GetComponent<TMP_InputField>();
            _addBtn                  = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/whiteaddBtn")?.GetComponent<Button>();
            _removeBtn               = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/whiteremoveBtn")?.GetComponent<Button>();
            _lootEquipToggle         = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/lootequipToggle")?.GetComponent<Toggle>();
            _equipmentTierDropdown   = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/equipmenttierDropdown")?.GetComponent<TMP_Dropdown>();
            _filterlistContent       = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/filterlistView/Viewport/filterlistContent");
            _filterCategoryTemplate  = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/filterlistView/Viewport/filterlistContent/filterCategoryToggle")?.GetComponent<Toggle>();
            _dragHandle              = UICommon.Find(_root, "container/panelBGwhitelist/lootUIDragHandle")?.gameObject;

            // Drag
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

            // Template inactive
            if (_whitelistItemTemplate != null)
                _whitelistItemTemplate.gameObject.SetActive(false);

            // Shared caches built once
            ItemLookup.EnsureBuilt();

            // Toggles & dropdowns
            SetupLootEquipToggle();
            SetupEquipmentTierDropdown();
            RebuildFilterToggles();

            // Debouncer (for filter input)
            _debounce = DebounceInvoker.Attach(_root);
        }

        public void Show()
        {
            if (_whiteitemContent == null || _whitelistContent == null || _whitelistItemTemplate == null)
            {
                Debug.LogError("[LootUI] Whitelist content/template missing.");
                return;
            }

            if (_filterInput != null)
            {
                _filterInput.onValueChanged.RemoveAllListeners();
                _filterInput.onValueChanged.AddListener(_ => _debounce.Schedule(RefreshUI, 0.15f));
            }

            RefreshUI();
        }

        // ---------- Core UI ----------

        private void RefreshUI()
        {
            UICommon.ClearListExceptTemplate(_whiteitemContent, _whitelistItemTemplate != null ? _whitelistItemTemplate.gameObject : null);
            UICommon.ClearList(_whitelistContent);
            _selected.Clear();

            string filter = _filterInput != null && _filterInput.text != null
                ? _filterInput.text.ToLowerInvariant()
                : string.Empty;

            var source = ItemLookup.AllItems;

            // Left list (available items)
            List<string> filteredItems = string.IsNullOrEmpty(filter)
                ? (source as List<string> ?? source.ToList())
                : source.Where(item => item.ToLowerInvariant().Contains(filter)).ToList();

            // Right list (in whitelist)
            List<string> filteredWhitelist = Plugin.Whitelist
                .Where(item => string.IsNullOrEmpty(filter) || item.ToLowerInvariant().Contains(filter))
                .OrderBy(item => item)
                .ToList();

            UILayoutBatch.WithLayoutSuspended(_whiteitemContent, () =>
            {
                foreach (string item in filteredItems)
                {
                    if (!filteredWhitelist.Contains(item))
                        CreateRow(_whiteitemContent, item, false);
                }
            });

            UILayoutBatch.WithLayoutSuspended(_whitelistContent, () =>
            {
                foreach (string item in filteredWhitelist)
                    CreateRow(_whitelistContent, item, true);
            });
        }

        private static Image EnsureClickTargetGraphic(GameObject go)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f); // fully transparent but raycastable
            img.raycastTarget = true;
            return img;
        }

        private void CreateRow(Transform parent, string itemName, bool isWhitelist)
        {
            GameObject go = GameObject.Instantiate(_whitelistItemTemplate.gameObject, parent);
            go.name = "whitelistItem_" + itemName;
            go.SetActive(true);

            Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            btn.targetGraphic = EnsureClickTargetGraphic(go);

            Transform iconTr  = go.transform.Find("Icon");
            Transform labelTr = go.transform.Find("Label");

            Image icon      = iconTr  ? iconTr.GetComponent<Image>()     : null;
            TMP_Text label  = labelTr ? labelTr.GetComponent<TMP_Text>() : null;

            if (label != null)
            {
                label.text = itemName;
                label.color = isWhitelist ? Color.white : Color.red;
                label.raycastTarget = false; // let root handle clicks
            }

            if (icon != null)
            {
                Sprite sprite = ItemLookup.GetIcon(itemName);
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false; // let root handle clicks
                // icon.enabled = (sprite != null); // optional: hide placeholder if missing
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(delegate
            {
                if (_doubleClick.IsDoubleClick(itemName))
                {
                    if (isWhitelist)
                    {
                        Plugin.Whitelist.Remove(itemName);
                        LootWhitelist.SaveWhitelist();
                        UpdateSocialLog.LogAdd("[LootUI] Removed from whitelist: " + itemName, "yellow");
                    }
                    else
                    {
                        Plugin.Whitelist.Add(itemName);
                        LootWhitelist.SaveWhitelist();
                        UpdateSocialLog.LogAdd("[LootUI] Added to whitelist: " + itemName, "yellow");
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
                        label.color = isWhitelist ? Color.white : Color.red;
                        _selected.RemoveAll(e => e.text == label);
                    }
                    else
                    {
                        label.color = Color.green;
                        _selected.Add((label, isWhitelist));
                    }
                }
                else
                {
                    foreach (var entry in _selected.ToArray())
                    {
                        TMP_Text t = entry.text;
                        bool wasWL = entry.isWhitelist;
                        if (t != null) t.color = wasWL ? Color.white : Color.red;
                    }
                    _selected.Clear();

                    label.color = Color.green;
                    _selected.Add((label, isWhitelist));
                }
            });
        }

        // ---------- Actions ----------

        private void AddSelected()
        {
            bool changed = false;
            foreach (var entry in _selected.ToArray())
            {
                TMP_Text text = entry.text;
                if (text == null) continue;
                if (!Plugin.Whitelist.Contains(text.text))
                {
                    Plugin.Whitelist.Add(text.text);
                    changed = true;
                }
            }
            if (changed)
            {
                LootWhitelist.SaveWhitelist();
                RefreshUI();
                UpdateSocialLog.LogAdd("[LootUI] Added selected items to whitelist.", "yellow");
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
                TMP_Text text = entry.text;
                if (text == null) continue;
                if (Plugin.Whitelist.Contains(text.text))
                {
                    Plugin.Whitelist.Remove(text.text);
                    changed = true;
                }
            }
            if (changed)
            {
                LootWhitelist.SaveWhitelist();
                RefreshUI();
                UpdateSocialLog.LogAdd("[LootUI] Removed selected items from whitelist.", "yellow");
            }
            else
            {
                UpdateSocialLog.LogAdd("[LootUI] No valid items selected to remove.", "red");
            }
        }

        // ---------- Settings UI ----------

        private void SetupLootEquipToggle()
        {
            if (_lootEquipToggle == null) return;
            _lootEquipToggle.SetIsOnWithoutNotify(Plugin.LootEquipment.Value);
            _lootEquipToggle.onValueChanged.RemoveAllListeners();
            _lootEquipToggle.onValueChanged.AddListener(v =>
            {
                Plugin.LootEquipment.Value = v;
            });
        }

        private void SetupEquipmentTierDropdown()
        {
            if (_equipmentTierDropdown == null) return;

            List<string> options = new List<string> { "All", "Normal Only", "Blessed Only", "Godly Only", "Blessed and up" };
            _equipmentTierDropdown.ClearOptions();
            _equipmentTierDropdown.AddOptions(options);

            int idx = (int)Plugin.LootEquipmentTier.Value;
            if (idx < 0 || idx >= options.Count) idx = 0;

            _equipmentTierDropdown.SetValueWithoutNotify(idx);
            _equipmentTierDropdown.onValueChanged.RemoveAllListeners();
            _equipmentTierDropdown.onValueChanged.AddListener(i =>
            {
                if (i < 0 || i >= options.Count) return;
                Plugin.LootEquipmentTier.Value = (EquipmentTierSetting)i;
            });
        }

        private void RebuildFilterToggles()
        {
            if (_filterlistContent == null || _filterCategoryTemplate == null)
                return;

            // Clear all children except the template
            for (int i = _filterlistContent.childCount - 1; i >= 0; i--)
            {
                Transform child = _filterlistContent.GetChild(i);
                if (child.gameObject == _filterCategoryTemplate.gameObject)
                    continue;
                GameObject.Destroy(child.gameObject);
            }

            // Read the latest categories and enabled states directly from LootFilterlist.ini
            LootFilterlist.ReadAll(out var sections, out var enabledSet);

            // Sort categories alphabetically for clean, predictable display
            var sortedCategories = sections.Keys
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (string category in sortedCategories)
            {
                Toggle toggle = GameObject.Instantiate(_filterCategoryTemplate, _filterlistContent);
                toggle.gameObject.name = "Toggle_" + category;
                toggle.gameObject.SetActive(true);

                // Set the label text
                Text label = toggle.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = category;

                // Set toggle state based on the current enabled set from disk
                toggle.isOn = enabledSet.Contains(category);

                // Capture category name for closure
                string cat = category;

                // Update the file instantly when toggled
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener((bool isOn) =>
                {
                    LootFilterlist.SetSectionEnabled(cat, isOn);
                });
            }

            // Force UI layout refresh
            Canvas.ForceUpdateCanvases();
            RectTransform rt = _filterlistContent.GetComponent<RectTransform>();
            if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }
}
