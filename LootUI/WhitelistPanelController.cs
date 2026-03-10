// WhitelistPanelController.cs
// All logic is identical to the original; Init() now builds its UI in code.

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
        private readonly GameObject    _panelRoot;
        private readonly RectTransform _containerRect;

        private Transform      _leftContent;
        private Transform      _rightContent;
        private GameObject     _rowTemplate;
        private TMP_InputField _filterInput;
        private Button         _addBtn;
        private Button         _removeBtn;
        private Toggle         _lootEquipToggle;
        private TMP_Dropdown   _equipmentTierDropdown;

        // Category filter list widgets
        private Transform  _filterlistContent;
        private Toggle     _filterCategoryTemplate;
        private TMP_InputField _newCategoryInput;
        private Button         _newCategoryAddBtn;

        private UIVirtualList _leftList;
        private UIVirtualList _rightList;

        private List<string> _leftData  = new List<string>();
        private List<string> _rightData = new List<string>();

        private readonly HashSet<string>           _selectedNames = new HashSet<string>(StringComparer.Ordinal);
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

        public WhitelistPanelController(GameObject panelRoot, RectTransform containerRect)
        {
            _panelRoot     = panelRoot;
            _containerRect = containerRect;
        }

        public void Init()
        {
            var refs = DualListPanelBuilder.Build(
                _panelRoot,
                leftTitle:  "All Items",
                rightTitle: "Whitelisted",
                filterPlaceholder: "Filter items...",
                extraBuilder: BuildExtraControls
            );

            _leftContent  = refs.LeftContent;
            _rightContent = refs.RightContent;
            _rowTemplate  = refs.RowTemplate;
            _filterInput  = refs.FilterInput;
            _addBtn       = refs.AddBtn;
            _removeBtn    = refs.RemoveBtn;

            if (_addBtn    != null) { _addBtn.onClick.RemoveAllListeners();    _addBtn.onClick.AddListener(AddSelected);    }
            if (_removeBtn != null) { _removeBtn.onClick.RemoveAllListeners(); _removeBtn.onClick.AddListener(RemoveSelected); }

            if (_filterInput != null)
            {
                var mute = _filterInput.gameObject.GetComponent<TypingInputMute>()
                           ?? _filterInput.gameObject.AddComponent<TypingInputMute>();
                mute.input      = _filterInput;
                mute.windowRoot = _panelRoot;
            }

            if (_newCategoryInput != null)
            {
                var mute2 = _newCategoryInput.gameObject.GetComponent<TypingInputMute>()
                            ?? _newCategoryInput.gameObject.AddComponent<TypingInputMute>();
                mute2.input      = _newCategoryInput;
                mute2.windowRoot = _panelRoot;
            }

            if (_newCategoryAddBtn != null)
            {
                _newCategoryAddBtn.onClick.RemoveAllListeners();
                _newCategoryAddBtn.onClick.AddListener(TryAddCategoryFromInput);
                _newCategoryAddBtn.interactable = !string.IsNullOrEmpty(_newCategoryInput?.text);
            }

            if (_newCategoryInput != null && _newCategoryAddBtn != null)
            {
                _newCategoryInput.onValueChanged.RemoveAllListeners();
                _newCategoryInput.onValueChanged.AddListener(v =>
                    _newCategoryAddBtn.interactable = !string.IsNullOrWhiteSpace(v));
            }

            ItemLookup.EnsureBuilt();
            SetupLootEquipToggle();
            SetupEquipmentTierDropdown();
            RebuildFilterToggles();

            _debounce = DebounceInvoker.Attach(_panelRoot);
            BuildVirtualLists();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Extra controls: equipment toggles + filter category list
        // ─────────────────────────────────────────────────────────────────────
        private void BuildExtraControls(Transform parent)
        {
            // Equipment loot toggle
            _lootEquipToggle = LootUIController.MakeToggle("lootequipToggle", parent, "Loot Equipment");
            _lootEquipToggle.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;

            // Equipment tier dropdown row
            var tierRow = new GameObject("TierRow");
            var trRT = tierRow.AddComponent<RectTransform>();
            trRT.SetParent(parent, false);
            var trHL = tierRow.AddComponent<HorizontalLayoutGroup>();
            trHL.spacing = 6;
            trHL.childForceExpandWidth  = false;
            trHL.childForceExpandHeight = false;
            trHL.childControlWidth      = true;
            trHL.childControlHeight     = true;
            tierRow.AddComponent<LayoutElement>().preferredHeight = 24;

            var tierLbl = LootUIController.MakeTMP("tierLabel", tierRow.transform);
            tierLbl.text  = "Equipment Tier:";
            tierLbl.color = LootUIController.C_TextMuted;
            tierLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 110;
            _equipmentTierDropdown = LootUIController.MakeDropdown("equipmenttierDropdown", tierRow.transform);
            _equipmentTierDropdown.gameObject.AddComponent<LayoutElement>().flexibleWidth   = 1;
            _equipmentTierDropdown.gameObject.GetComponent<LayoutElement>().preferredHeight = 22;

            LootUIController.MakeDivider(parent);

            // Filter category list section header
            var catHdr = LootUIController.MakeTMP("CatHeader", parent);
            catHdr.text      = "FILTER CATEGORIES";
            catHdr.color     = LootUIController.C_TextMuted;
            catHdr.fontSize  = 10;
            catHdr.fontStyle = FontStyles.Bold;
            catHdr.gameObject.AddComponent<LayoutElement>().preferredHeight = 16;

            // Filter category scroll list
            RectTransform flVP, flContent;
            var flScroll = LootUIController.MakeScrollView("filterlistView", parent, out flVP, out flContent);
            flScroll.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
            _filterlistContent = flContent;

            // Hidden toggle template
            var tplGO = new GameObject("filterCategoryToggle");
            tplGO.AddComponent<RectTransform>().SetParent(_filterlistContent, false);
            tplGO.AddComponent<LayoutElement>().preferredHeight = 22;

            var tplHL = tplGO.AddComponent<HorizontalLayoutGroup>();
            tplHL.spacing = 4;
            tplHL.childForceExpandWidth  = false;
            tplHL.childForceExpandHeight = true;
            tplHL.childControlWidth  = false;
            tplHL.childControlHeight = true;
            tplHL.padding = new RectOffset(4, 4, 2, 2);

            // Toggle check
            var checkGO = new GameObject("Background");
            var checkRT = checkGO.AddComponent<RectTransform>();
            checkRT.SetParent(tplGO.transform, false);
            checkRT.sizeDelta = new Vector2(16, 16);
            var checkImg = checkGO.AddComponent<Image>();
            checkImg.color = LootUIController.C_InputBg;

            var markGO = new GameObject("Checkmark");
            var markRT = markGO.AddComponent<RectTransform>();
            markRT.SetParent(checkRT, false);
            LootUIController.StretchFull(markRT);
            markRT.offsetMin = new Vector2(3, 3);
            markRT.offsetMax = new Vector2(-3, -3);
            var markImg = markGO.AddComponent<Image>();
            markImg.color = LootUIController.C_AccentBlue;

            // Label (using legacy Text for Toggle compatibility with original code)
            var lblGO = new GameObject("Label");
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.SetParent(tplGO.transform, false);
            var legacyTxt = lblGO.AddComponent<UnityEngine.UI.Text>();
            legacyTxt.color    = Color.white;
            legacyTxt.fontSize = 11;
            lblGO.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Edit + Delete buttons
            var editBtnGO = new GameObject("filterCategoryEditBtn");
            editBtnGO.AddComponent<RectTransform>().SetParent(tplGO.transform, false);
            var editImg = editBtnGO.AddComponent<Image>();
            editImg.color = LootUIController.C_BtnNormal;
            var editBtn = editBtnGO.AddComponent<Button>();
            editBtnGO.AddComponent<LayoutElement>().preferredWidth = 36;
            var editLblGO = new GameObject("Label");
            editLblGO.AddComponent<RectTransform>().SetParent(editBtnGO.transform, false);
            LootUIController.StretchFull(editLblGO.GetComponent<RectTransform>());
            var editLbl = editLblGO.AddComponent<TextMeshProUGUI>();
            editLbl.text = "Edit"; editLbl.fontSize = 9; editLbl.alignment = TextAlignmentOptions.Center;
            editLbl.color = LootUIController.C_TextPri;

            var delBtnGO = new GameObject("filterCategoryDeleteBtn");
            delBtnGO.AddComponent<RectTransform>().SetParent(tplGO.transform, false);
            var delImg = delBtnGO.AddComponent<Image>();
            delImg.color = LootUIController.C_BtnNormal;
            var delBtn = delBtnGO.AddComponent<Button>();
            delBtnGO.AddComponent<LayoutElement>().preferredWidth = 36;
            var delLblGO = new GameObject("Label");
            delLblGO.AddComponent<RectTransform>().SetParent(delBtnGO.transform, false);
            LootUIController.StretchFull(delLblGO.GetComponent<RectTransform>());
            var delLbl = delLblGO.AddComponent<TextMeshProUGUI>();
            delLbl.text = "Del"; delLbl.fontSize = 9; delLbl.alignment = TextAlignmentOptions.Center;
            delLbl.color = LootUIController.C_Danger;

            _filterCategoryTemplate = tplGO.AddComponent<Toggle>();
            _filterCategoryTemplate.targetGraphic = checkImg;
            _filterCategoryTemplate.graphic       = markImg;
            tplGO.SetActive(false);

            // New category row
            var newCatRow = new GameObject("NewCategoryRow");
            var ncRT = newCatRow.AddComponent<RectTransform>();
            ncRT.SetParent(parent, false);
            var ncHL = newCatRow.AddComponent<HorizontalLayoutGroup>();
            ncHL.spacing = 4;
            ncHL.childForceExpandWidth  = false;
            ncHL.childForceExpandHeight = true;
            ncHL.childControlWidth  = true;
            ncHL.childControlHeight = true;
            newCatRow.AddComponent<LayoutElement>().preferredHeight = 26;

            _newCategoryInput = LootUIController.MakeInputField("filterlistNewName", newCatRow.transform, "New group name...");
            _newCategoryInput.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            _newCategoryAddBtn = LootUIController.MakeButton("filterlistNewAddBtn", newCatRow.transform,
                "+ Add", LootUIController.C_TextPri, LootUIController.C_BtnNormal);
            _newCategoryAddBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 60;

            LootUIController.MakeDivider(parent);
        }

        private void BuildVirtualLists()
        {
            if (_rowTemplate == null) return;
            var leftSR  = _leftContent  ? _leftContent.GetComponentInParent<ScrollRect>()  : null;
            var rightSR = _rightContent ? _rightContent.GetComponentInParent<ScrollRect>() : null;
            _leftList  = new UIVirtualList(leftSR,  (RectTransform)_leftContent,  _rowTemplate, 24f, bufferRows: 8);
            _rightList = new UIVirtualList(rightSR, (RectTransform)_rightContent, _rowTemplate, 24f, bufferRows: 8);
            _leftList.Enable(true);
            _rightList.Enable(true);
        }

        public void Show()
        {
            if (_leftContent == null || _rightContent == null || _rowTemplate == null)
            {
                Debug.LogError("[LootUI] Whitelist content/template missing.");
                return;
            }
            if (_filterInput != null)
            {
                _filterInput.onValueChanged.RemoveAllListeners();
                _filterInput.onValueChanged.AddListener(_ => _debounce.Schedule(RefreshUI, 0.15f));
                _filterInput.text = string.Empty;
            }
            RefreshUI();
            _leftList?.RecalculateAndRefresh();
            _rightList?.RecalculateAndRefresh();
            _panelRoot.GetComponent<MonoBehaviour>().StartCoroutine(UIVirtualList.DeferredFinalize(_leftContent));
            _panelRoot.GetComponent<MonoBehaviour>().StartCoroutine(UIVirtualList.DeferredFinalize(_rightContent));
        }

        private void RefreshUI()
        {
            _selectedNames.Clear();
            string filter = _filterInput?.text?.ToLowerInvariant() ?? string.Empty;
            var source    = ItemLookup.AllItems;

            _rightData = Plugin.Whitelist
                .Where(i => string.IsNullOrEmpty(filter) || i.ToLowerInvariant().Contains(filter))
                .Distinct().OrderBy(i => i).ToList();

            _leftData = string.IsNullOrEmpty(filter)
                ? new List<string>(source)
                : source.Where(i => i.ToLowerInvariant().Contains(filter)).ToList();

            if (_rightData.Count > 0)
            {
                var mask = new HashSet<string>(_rightData, StringComparer.Ordinal);
                _leftData.RemoveAll(mask.Contains);
            }

            _leftList?.SetData(_leftData.Count, BindLeftRow);
            _rightList?.SetData(_rightData.Count, BindRightRow);
            UIVirtualList.FinalizeListLayout(_leftContent);
            UIVirtualList.FinalizeListLayout(_rightContent);
        }

        private static Image EnsureClickTargetGraphic(GameObject go)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.sprite = GetWhite1x1();
            img.type   = Image.Type.Simple;
            img.color  = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;
            return img;
        }

        private void BindLeftRow(GameObject row, int index)
        {
            if (index < 0 || index >= _leftData.Count) { row.SetActive(false); return; }
            BindRowCommon(row, _leftData[index], isInWhitelist: false);
        }

        private void BindRightRow(GameObject row, int index)
        {
            if (index < 0 || index >= _rightData.Count) { row.SetActive(false); return; }
            BindRowCommon(row, _rightData[index], isInWhitelist: true);
        }

        private void BindRowCommon(GameObject row, string itemName, bool isInWhitelist)
        {
            var btn     = row.GetComponent<Button>() ?? row.AddComponent<Button>();
            var rootImg = EnsureClickTargetGraphic(row);
            btn.targetGraphic = rootImg;
            btn.transition    = Selectable.Transition.None;
            btn.interactable  = true;

            var icon  = row.transform.Find("Icon")?.GetComponent<Image>();
            var label = row.transform.Find("Label")?.GetComponent<TMP_Text>();

            if (label != null)
            {
                label.text          = itemName;
                label.raycastTarget = false;
                label.color         = isInWhitelist ? Color.white : Color.red;
            }
            if (icon != null)
            {
                icon.sprite         = ItemLookup.GetIcon(itemName);
                icon.preserveAspect = true;
                icon.raycastTarget  = false;
            }

            var hover = row.GetComponent<CheatManager.RowHover>() ?? row.AddComponent<CheatManager.RowHover>();
            hover.Init(rootImg,
                new Color(1f,1f,1f,0f), new Color(1f,1f,1f,0.12f), new Color(1f,1f,1f,0.20f),
                new Color(1f,1f,1f,0.10f), new Color(1f,1f,1f,0.18f), new Color(1f,1f,1f,0.26f));
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
                        ChatFilterInjector.SendLootMessage("[LootUI] Removed from whitelist: " + itemName, "yellow");
                    }
                    else
                    {
                        Plugin.Whitelist.Add(itemName);
                        LootWhitelist.SaveWhitelist();
                        ChatFilterInjector.SendLootMessage("[LootUI] Added to whitelist: " + itemName, "yellow");
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
                if (!Plugin.Whitelist.Contains(name)) { Plugin.Whitelist.Add(name); changed = true; }
            if (changed) { LootWhitelist.SaveWhitelist(); RefreshUI(); ChatFilterInjector.SendLootMessage("[LootUI] Added selected items to whitelist.", "yellow"); }
            else ChatFilterInjector.SendLootMessage("[LootUI] No valid items selected to add.", "red");
        }

        private void RemoveSelected()
        {
            bool changed = false;
            foreach (var name in _selectedNames.ToArray())
                if (Plugin.Whitelist.Contains(name)) { Plugin.Whitelist.Remove(name); changed = true; }
            if (changed) { LootWhitelist.SaveWhitelist(); RefreshUI(); ChatFilterInjector.SendLootMessage("[LootUI] Removed selected items from whitelist.", "yellow"); }
            else ChatFilterInjector.SendLootMessage("[LootUI] No valid items selected to remove.", "red");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Equipment / Filter Category logic (unchanged from original)
        // ─────────────────────────────────────────────────────────────────────
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
            var options = new List<string> { "All", "Normal Only", "Blessed Only", "Godly Only", "Blessed and up" };
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
            if (_filterlistContent == null || _filterCategoryTemplate == null) return;

            for (int i = _filterlistContent.childCount - 1; i >= 0; i--)
            {
                Transform child = _filterlistContent.GetChild(i);
                if (child.gameObject == _filterCategoryTemplate.gameObject) continue;
                GameObject.Destroy(child.gameObject);
            }

            LootFilterlist.ReadAll(out var sections, out var enabledSet);
            var sorted = sections.Keys.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (string category in sorted)
            {
                Toggle toggle = GameObject.Instantiate(_filterCategoryTemplate, _filterlistContent);
                toggle.gameObject.name = "Toggle_" + category;
                toggle.gameObject.SetActive(true);

                var legacyLbl = toggle.GetComponentInChildren<UnityEngine.UI.Text>();
                if (legacyLbl != null) legacyLbl.text = category;

                toggle.isOn = enabledSet.Contains(category);
                string cat = category;
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener((bool isOn) => { LootFilterlist.SetSectionEnabled(cat, isOn); });

                var editBtn = toggle.transform.Find("filterCategoryEditBtn")?.GetComponent<Button>();
                if (editBtn != null)
                {
                    editBtn.onClick.RemoveAllListeners();
                    string captured = cat;
                    editBtn.onClick.AddListener(() => LootUIController.ShowEditCategory(captured));
                }

                var delBtn = toggle.transform.Find("filterCategoryDeleteBtn")?.GetComponent<Button>();
                if (delBtn != null)
                {
                    delBtn.onClick.RemoveAllListeners();
                    string captured = cat;
                    delBtn.onClick.AddListener(() => TryDeleteCategory(captured));
                }
            }

            Canvas.ForceUpdateCanvases();
            var rt = _filterlistContent.GetComponent<RectTransform>();
            if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        private void TryAddCategoryFromInput()
        {
            string name = _newCategoryInput?.text?.Trim();
            if (string.IsNullOrEmpty(name)) { ChatFilterInjector.SendLootMessage("[LootUI] Group name is empty.", "red"); return; }
            if (Plugin.FilterList.ContainsKey(name)) { ChatFilterInjector.SendLootMessage($"[LootUI] Group {name} already exists.", "red"); return; }
            Plugin.FilterList[name] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            LootFilterlist.SaveFilterlist();
            ChatFilterInjector.SendLootMessage($"[LootUI] Created new group {name}.", "yellow");
            if (_newCategoryInput != null) _newCategoryInput.text = string.Empty;
            RebuildFilterToggles();
        }

        private void TryDeleteCategory(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (!Plugin.FilterList.ContainsKey(name)) { ChatFilterInjector.SendLootMessage($"[LootUI] Group {name} not found.", "red"); return; }
            Plugin.FilterList.Remove(name);
            Plugin.EnabledFilterCategories.Remove(name);
            LootFilterlist.SaveFilterlist();
            ChatFilterInjector.SendLootMessage($"[LootUI] Deleted group {name}.", "yellow");
            RebuildFilterToggles();
        }
    }
}