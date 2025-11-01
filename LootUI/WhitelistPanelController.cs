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
        private const bool ENABLE_BY_DEFAULT = false;

        private readonly GameObject _root;
        private readonly RectTransform _containerRect;
        
        private Transform _whiteitemContent;
        private Transform _whitelistContent;
        private Button _whitelistItemTemplate;
        private TMP_InputField _whitefilterInput;
        private Button _addBtn;
        private Button _removeBtn;
        private Toggle _lootEquipToggle;
        private TMP_Dropdown _equipmentTierDropdown;

        private Transform _filterlistContent;
        private Toggle _filterCategoryTemplate;

        
        private TMP_InputField _newCategoryInput;
        private Button _newCategoryAddBtn;

        private GameObject _dragHandle;

        private UIVirtualList _leftList;
        private UIVirtualList _rightList;

        private List<string> _leftData = new List<string>();
        private List<string> _rightData = new List<string>();

        private readonly HashSet<string> _selectedNames = new HashSet<string>(StringComparer.Ordinal);
        private readonly UICommon.DoubleClickTracker _doubleClick = new UICommon.DoubleClickTracker(0.25f);
        private DebounceInvoker _debounce;
        
        private static Sprite _white1x1;
        private static Sprite GetWhite1x1()
        {
            if (_white1x1 != null) return _white1x1;
            var tex = Texture2D.whiteTexture;
            _white1x1 = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            _white1x1.name = "LootUI_White1x1";
            return _white1x1;
        }

        public WhitelistPanelController(GameObject root, RectTransform containerRect)
        {
            _root = root;
            _containerRect = containerRect;
        }

        public void Init()
        {
            _whiteitemContent = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/whiteitemView/Viewport/whiteitemContent");
            _whitelistContent = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/whitelistView/Viewport/whitelistContent");
            _whitelistItemTemplate = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/whiteitemView/Viewport/whiteitemContent/whitelistItem")?.GetComponent<Button>();
            _whitefilterInput = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/whitelistFilter")?.GetComponent<TMP_InputField>();
            _addBtn = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/whiteaddBtn")?.GetComponent<Button>();
            _removeBtn = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/whiteremoveBtn")?.GetComponent<Button>();
            _lootEquipToggle = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/lootequipToggle")?.GetComponent<Toggle>();
            _equipmentTierDropdown = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/equipmenttierDropdown")?.GetComponent<TMP_Dropdown>();

            _filterlistContent = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/filterlistView/Viewport/filterlistContent");
            _filterCategoryTemplate = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/filterlistView/Viewport/filterlistContent/filterCategoryToggle")?.GetComponent<Toggle>();
            
            _newCategoryInput = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/filterlistNewName")?.GetComponent<TMP_InputField>();
            _newCategoryAddBtn = UICommon.Find(_root, "container/panelBGwhitelist/whitelistPanel/filterlistNewAddBtn")?.GetComponent<Button>();

            _dragHandle = UICommon.Find(_root, "container/panelBGwhitelist/lootUIDragHandle")?.gameObject;
            
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
            
            if (_whitelistItemTemplate != null)
                _whitelistItemTemplate.gameObject.SetActive(false);
            
            if (_whitefilterInput != null)
            {
                var mute = _whitefilterInput.gameObject.GetComponent<TypingInputMute>()
                           ?? _whitefilterInput.gameObject.AddComponent<TypingInputMute>();

                mute.input = _whitefilterInput;
                mute.windowRoot = UICommon.Find(_root, "container/panelBGwhitelist")?.gameObject;
                mute.log        = true;
            }
            
            if (_newCategoryInput != null)
            {
                var mute = _newCategoryInput.gameObject.GetComponent<TypingInputMute>()
                           ?? _newCategoryInput.gameObject.AddComponent<TypingInputMute>();

                mute.input = _newCategoryInput;
                mute.windowRoot = UICommon.Find(_root, "container/panelBGwhitelist")?.gameObject;
                mute.log        = true;
            }
            
            if (_newCategoryAddBtn != null)
            {
                _newCategoryAddBtn.onClick.RemoveAllListeners();
                _newCategoryAddBtn.onClick.AddListener(TryAddCategoryFromInput);
                
                if (_newCategoryAddBtn != null)
                    _newCategoryAddBtn.interactable = !string.IsNullOrEmpty(_newCategoryInput.text);
            }
            
            if (_newCategoryInput != null && _newCategoryAddBtn != null)
            {
                _newCategoryInput.onValueChanged.RemoveAllListeners();
                _newCategoryInput.onValueChanged.AddListener(value =>
                {
                    _newCategoryAddBtn.interactable = !string.IsNullOrWhiteSpace(value);
                });
            }
            
            ItemLookup.EnsureBuilt();
            
            SetupLootEquipToggle();
            SetupEquipmentTierDropdown();

            RebuildFilterToggles();
            
            _debounce = DebounceInvoker.Attach(_root);
            
            BuildVirtualLists();
        }

        private void BuildVirtualLists()
        {
            if (_whitelistItemTemplate == null) return;

            var leftScroll = _whiteitemContent ? _whiteitemContent.GetComponentInParent<ScrollRect>() : null;
            var rightScroll = _whitelistContent ? _whitelistContent.GetComponentInParent<ScrollRect>() : null;

            float rowHeight = (_whitelistItemTemplate.transform as RectTransform)?.sizeDelta.y ?? 24f;

            _leftList = new UIVirtualList(leftScroll, (RectTransform)_whiteitemContent, _whitelistItemTemplate.gameObject, rowHeight, bufferRows: 8);
            _rightList = new UIVirtualList(rightScroll, (RectTransform)_whitelistContent, _whitelistItemTemplate.gameObject, rowHeight, bufferRows: 8);

            _leftList.Enable(true);
            _rightList.Enable(true);
        }

        public void Show()
        {
            if (_whiteitemContent == null || _whitelistContent == null || _whitelistItemTemplate == null)
            {
                Debug.LogError("[LootUI] Whitelist content/template missing.");
                return;
            }

            if (_whitefilterInput != null)
            {
                _whitefilterInput.onValueChanged.RemoveAllListeners();
                _whitefilterInput.onValueChanged.AddListener(_ => _debounce.Schedule(RefreshUI, 0.15f));
                _whitefilterInput.text = string.Empty;
            }

            RefreshUI();

            _leftList?.RecalculateAndRefresh();
            _rightList?.RecalculateAndRefresh();
            
            _root.GetComponent<MonoBehaviour>().StartCoroutine(UIVirtualList.DeferredFinalize(_whiteitemContent));
            _root.GetComponent<MonoBehaviour>().StartCoroutine(UIVirtualList.DeferredFinalize(_whitelistContent));
        }

        private void RefreshUI()
        {
            _selectedNames.Clear();

            string filter = _whitefilterInput != null && _whitefilterInput.text != null
                ? _whitefilterInput.text.ToLowerInvariant()
                : string.Empty;

            var source = ItemLookup.AllItems;

            _rightData = Plugin.Whitelist
                .Where(item => string.IsNullOrEmpty(filter) || item.ToLowerInvariant().Contains(filter))
                .Distinct()
                .OrderBy(item => item)
                .ToList();
            
            _leftData = string.IsNullOrEmpty(filter)
                ? new List<string>(source)
                : source.Where(item => item.ToLowerInvariant().Contains(filter)).ToList();

            if (_rightData.Count > 0)
            {
                var mask = new HashSet<string>(_rightData, StringComparer.Ordinal);
                _leftData.RemoveAll(mask.Contains);
            }

            _leftList?.SetData(_leftData.Count, BindLeftRow);
            _rightList?.SetData(_rightData.Count, BindRightRow);
            
            UIVirtualList.FinalizeListLayout(_whiteitemContent);
            UIVirtualList.FinalizeListLayout(_whitelistContent);
        }

        private static Image EnsureClickTargetGraphic(GameObject go)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.sprite = GetWhite1x1();
            img.type = Image.Type.Simple;
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;
            return img;
        }

        private void BindLeftRow(GameObject row, int index)
        {
            if (index < 0 || index >= _leftData.Count) { row.SetActive(false); return; }
            string itemName = _leftData[index];
            BindRowCommon(row, itemName, isInWhitelist: false);
        }

        private void BindRightRow(GameObject row, int index)
        {
            if (index < 0 || index >= _rightData.Count) { row.SetActive(false); return; }
            string itemName = _rightData[index];
            BindRowCommon(row, itemName, isInWhitelist: true);
        }

        private void BindRowCommon(GameObject row, string itemName, bool isInWhitelist)
        {
            var btn = row.GetComponent<Button>() ?? row.AddComponent<Button>();
            var rootImg = EnsureClickTargetGraphic(row);
            btn.targetGraphic = rootImg;
            btn.transition = Selectable.Transition.None;
            btn.interactable = true;

            var iconTr = row.transform.Find("Icon");
            var labelTr = row.transform.Find("Label");

            var icon  = iconTr  ? iconTr.GetComponent<Image>()     : null;
            var label = labelTr ? labelTr.GetComponent<TMP_Text>() : null;

            if (label != null)
            {
                label.text = itemName;
                label.raycastTarget = false;
                label.color = isInWhitelist ? Color.white : Color.red;
            }

            if (icon != null)
            {
                var sprite = ItemLookup.GetIcon(itemName);
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }

            var hover = row.GetComponent<CheatManager.RowHover>() ?? row.AddComponent<CheatManager.RowHover>();
            hover.Init(
                rootImg,
                // normal, highlighted, pressed
                new Color(1f, 1f, 1f, 0f),
                new Color(1f, 1f, 1f, 0.12f),
                new Color(1f, 1f, 1f, 0.20f),
                // selected normal, highlighted, pressed
                new Color(1f, 1f, 1f, 0.10f),
                new Color(1f, 1f, 1f, 0.18f),
                new Color(1f, 1f, 1f, 0.26f)
            );
            hover.SetSelected(_selectedNames.Contains(itemName));

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (_doubleClick.IsDoubleClick(itemName))
                {
                    if (isInWhitelist)
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
                _leftList?.Refresh();
                _rightList?.Refresh();
            });
        }

        private void AddSelected()
        {
            bool changed = false;
            foreach (var name in _selectedNames.ToArray())
            {
                if (!Plugin.Whitelist.Contains(name))
                {
                    Plugin.Whitelist.Add(name);
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
            foreach (var name in _selectedNames.ToArray())
            {
                if (Plugin.Whitelist.Contains(name))
                {
                    Plugin.Whitelist.Remove(name);
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
            _lootEquipToggle.onValueChanged.AddListener(v => { Plugin.LootEquipment.Value = v; });
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

            for (int i = _filterlistContent.childCount - 1; i >= 0; i--)
            {
                Transform child = _filterlistContent.GetChild(i);
                if (child.gameObject == _filterCategoryTemplate.gameObject)
                    continue;
                GameObject.Destroy(child.gameObject);
            }

            LootFilterlist.ReadAll(out var sections, out var enabledSet);

            var sortedCategories = sections.Keys
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (string category in sortedCategories)
            {
                Toggle toggle = GameObject.Instantiate(_filterCategoryTemplate, _filterlistContent);
                toggle.gameObject.name = "Toggle_" + category;
                toggle.gameObject.SetActive(true);

                Text label = toggle.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = category;

                toggle.isOn = enabledSet.Contains(category);
                string cat = category;

                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener((bool isOn) => { LootFilterlist.SetSectionEnabled(cat, isOn); });
                
                var editBtnTr = toggle.transform.Find("filterCategoryEditBtn");
                var editBtn = editBtnTr ? editBtnTr.GetComponent<Button>() : null;
                if (editBtn != null)
                {
                    editBtn.onClick.RemoveAllListeners();
                    string captured = cat;
                    editBtn.onClick.AddListener(() => LootUIController.ShowEditCategory(captured));
                }
                
                var delBtnTr = toggle.transform.Find("filterCategoryDeleteBtn");
                var delBtn = delBtnTr ? delBtnTr.GetComponent<Button>() : null;
                if (delBtn != null)
                {
                    delBtn.onClick.RemoveAllListeners();
                    string captured = cat;
                    delBtn.onClick.AddListener(() => TryDeleteCategory(captured));
                }
            }

            Canvas.ForceUpdateCanvases();
            RectTransform rt = _filterlistContent.GetComponent<RectTransform>();
            if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
        
        private void TryAddCategoryFromInput()
        {
            string raw = _newCategoryInput != null ? _newCategoryInput.text : null;
            string name = raw?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                UpdateSocialLog.LogAdd("[LootUI] Group name is empty.", "red");
                return;
            }
            
            if (Plugin.FilterList.ContainsKey(name))
            {
                UpdateSocialLog.LogAdd($"[LootUI] Group {name} already exists.", "red");
                return;
            }
            
            Plugin.FilterList[name] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (ENABLE_BY_DEFAULT)
                Plugin.EnabledFilterCategories.Add(name);

            LootFilterlist.SaveFilterlist();
            UpdateSocialLog.LogAdd($"[LootUI] Created new group {name}.", "yellow");

            if (_newCategoryInput != null) _newCategoryInput.text = string.Empty;

            RebuildFilterToggles();
        }

        private void TryDeleteCategory(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            
            if (!Plugin.FilterList.ContainsKey(name))
            {
                UpdateSocialLog.LogAdd($"[LootUI] Group {name} not found.", "red");
                return;
            }

            Plugin.FilterList.Remove(name);
            Plugin.EnabledFilterCategories.Remove(name);

            LootFilterlist.SaveFilterlist();
            UpdateSocialLog.LogAdd($"[LootUI] Deleted group {name}.", "yellow");

            RebuildFilterToggles();
        }
    }
}
