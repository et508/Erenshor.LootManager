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

        private readonly List<string> _allItems = new List<string>();
        private readonly List<(TMP_Text text, bool isWhitelist)> _selected = new List<(TMP_Text text, bool isWhitelist)>();
        private readonly Dictionary<string, Sprite> _iconCache = new Dictionary<string, Sprite>();
        private readonly UICommon.DoubleClickTracker _doubleClick = new UICommon.DoubleClickTracker(0.25f);

        public WhitelistPanelController(GameObject root, RectTransform containerRect)
        {
            _root = root;
            _containerRect = containerRect;
        }

        public void Init()
        {
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

            if (_dragHandle != null && _containerRect != null)
            {
                DragHandler dh = _dragHandle.GetComponent<DragHandler>();
                if (dh == null) dh = _dragHandle.AddComponent<DragHandler>();
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

            if (_whitelistItemTemplate != null)
                _whitelistItemTemplate.gameObject.SetActive(false);

            RebuildAllItems();
            BuildIconCache();
            SetupLootEquipToggle();
            SetupEquipmentTierDropdown();
            RebuildFilterToggles();
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
                _filterInput.onValueChanged.AddListener(delegate { RefreshUI(); });
            }

            RefreshUI();
        }

        private void RebuildAllItems()
        {
            _allItems.Clear();
            List<string> list = GameData.ItemDB.ItemDB
                .Where(i => i != null && !string.IsNullOrWhiteSpace(i.ItemName))
                .Select(i => i.ItemName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
            _allItems.AddRange(list);
        }

        private void BuildIconCache()
        {
            var db = GameData.ItemDB != null ? GameData.ItemDB.ItemDB : null;
            if (db == null) return;

            foreach (var it in db)
            {
                if (it == null) continue;
                string name = it.ItemName;
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (_iconCache.ContainsKey(name)) continue;

                _iconCache[name] = it.ItemIcon;
            }
        }

        private Sprite GetIcon(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            Sprite s;
            if (_iconCache.TryGetValue(itemName, out s)) return s;

            var item = GameData.ItemDB != null ? GameData.ItemDB.ItemDB.FirstOrDefault(i => i != null && i.ItemName == itemName) : null;
            s = item != null ? item.ItemIcon : null;
            _iconCache[itemName] = s;
            return s;
        }

        private void RefreshUI()
        {
            UICommon.ClearListExceptTemplate(_whiteitemContent, _whitelistItemTemplate != null ? _whitelistItemTemplate.gameObject : null);
            UICommon.ClearList(_whitelistContent);
            _selected.Clear();

            string filter = _filterInput != null && _filterInput.text != null
                ? _filterInput.text.ToLowerInvariant()
                : string.Empty;

            List<string> filteredItems = string.IsNullOrEmpty(filter)
                ? _allItems
                : _allItems.Where(item => item.ToLowerInvariant().Contains(filter)).ToList();

            List<string> filteredWhitelist = Plugin.Whitelist
                .Where(item => string.IsNullOrEmpty(filter) || item.ToLowerInvariant().Contains(filter))
                .OrderBy(item => item)
                .ToList();

            foreach (string item in filteredItems)
            {
                if (!filteredWhitelist.Contains(item))
                    CreateRow(_whiteitemContent, item, false);
            }

            foreach (string item in filteredWhitelist)
            {
                CreateRow(_whitelistContent, item, true);
            }

            Canvas.ForceUpdateCanvases();
            RectTransform rtA = _whiteitemContent != null ? _whiteitemContent.GetComponent<RectTransform>() : null;
            if (rtA != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rtA);
            RectTransform rtB = _whitelistContent != null ? _whitelistContent.GetComponent<RectTransform>() : null;
            if (rtB != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rtB);
        }

        private static Image EnsureClickTargetGraphic(GameObject go)
        {
            Image img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;
            return img;
        }

        private void CreateRow(Transform parent, string itemName, bool isWhitelist)
        {
            GameObject go = GameObject.Instantiate(_whitelistItemTemplate.gameObject, parent);
            go.name = "whitelistItem_" + itemName;
            go.SetActive(true);

            Button btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            btn.targetGraphic = EnsureClickTargetGraphic(go);

            Transform iconTr  = go.transform.Find("Icon");
            Transform labelTr = go.transform.Find("Label");

            Image icon  = iconTr != null ? iconTr.GetComponent<Image>() : null;
            TMP_Text label = labelTr != null ? labelTr.GetComponent<TMP_Text>() : null;

            if (label != null)
            {
                label.text = itemName;
                label.color = isWhitelist ? Color.white : Color.red;
                label.raycastTarget = false;
            }

            if (icon != null)
            {
                Sprite sprite = GetIcon(itemName);
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
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

        private void SetupLootEquipToggle()
        {
            if (_lootEquipToggle == null) return;
            _lootEquipToggle.SetIsOnWithoutNotify(Plugin.LootEquipment.Value);
            _lootEquipToggle.onValueChanged.RemoveAllListeners();
            _lootEquipToggle.onValueChanged.AddListener(delegate (bool v)
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
            _equipmentTierDropdown.onValueChanged.AddListener(delegate (int i)
            {
                if (i < 0 || i >= options.Count) return;
                Plugin.LootEquipmentTier.Value = (EquipmentTierSetting)i;
            });
        }

        private void RebuildFilterToggles()
        {
            if (_filterlistContent == null || _filterCategoryTemplate == null) return;

            for (int i = _filterlistContent.childCount - 1; i >= 0; i--)
            {
                Transform child = _filterlistContent.GetChild(i);
                if (child.gameObject == _filterCategoryTemplate.gameObject) continue;
                GameObject.Destroy(child.gameObject);
            }

            foreach (string category in Plugin.FilterList.Keys.Reverse())
            {
                Toggle toggle = GameObject.Instantiate(_filterCategoryTemplate, _filterlistContent);
                toggle.gameObject.name = "Toggle_" + category;
                toggle.gameObject.SetActive(true);

                Text label = toggle.GetComponentInChildren<Text>();
                if (label != null) label.text = category;

                bool isOn = Plugin.EnabledFilterGroups.Contains(category);
                toggle.isOn = isOn;

                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener(delegate (bool val)
                {
                    if (val)
                        Plugin.EnabledFilterGroups.Add(category);
                    else
                        Plugin.EnabledFilterGroups.Remove(category);
                });
            }

            Canvas.ForceUpdateCanvases();
            RectTransform rt = _filterlistContent.GetComponent<RectTransform>();
            if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }
}
