// FilterlistPanelController.cs
// Manages the Filter Categories panel — moved from WhitelistPanelController.

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public sealed class FilterlistPanelController
    {
        private readonly GameObject    _panelRoot;
        private readonly RectTransform _containerRect;

        private Transform      _filterlistContent;
        private Toggle         _filterCategoryTemplate;
        private TMP_InputField _newCategoryInput;
        private Button         _newCategoryAddBtn;

        private DebounceInvoker _debounce;

        public FilterlistPanelController(GameObject panelRoot, RectTransform containerRect)
        {
            _panelRoot     = panelRoot;
            _containerRect = containerRect;
        }

        public void Init()
        {
            // Outer vertical layout for the whole panel
            var vl = _panelRoot.GetComponent<VerticalLayoutGroup>()
                     ?? _panelRoot.AddComponent<VerticalLayoutGroup>();
            vl.padding            = new RectOffset(8, 8, 8, 8);
            vl.spacing            = 6;
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;
            vl.childControlWidth  = true;
            vl.childControlHeight = true;

            var parent = _panelRoot.transform;

            // Section header
            var catHdr = LootUIController.MakeTMP("CatHeader", parent);
            catHdr.text      = "FILTER CATEGORIES";
            catHdr.color     = LootUIController.C_TextMuted;
            catHdr.fontSize  = 10;
            catHdr.fontStyle = FontStyles.Bold;
            catHdr.gameObject.AddComponent<LayoutElement>().preferredHeight = 16;

            // Category scroll list
            RectTransform flVP, flContent;
            var flScroll = LootUIController.MakeScrollView("filterlistView", parent, out flVP, out flContent);
            flScroll.gameObject.AddComponent<LayoutElement>().preferredHeight = 200;
            _filterlistContent = flContent;

            // Hidden toggle template
            // Row GO — holds HLG, is the template root, Toggle goes on this GO
            // Structure: [rowGO(Toggle+HLG)] -> [toggleInner(check+label)] [editBtn] [delBtn]
            // Edit/Del are siblings of toggleInner so the Toggle doesn't swallow their clicks.
            var tplGO = new GameObject("filterCategoryToggle");
            tplGO.AddComponent<RectTransform>().SetParent(_filterlistContent, false);
            tplGO.AddComponent<LayoutElement>().preferredHeight = 22;

            var tplHL = tplGO.AddComponent<HorizontalLayoutGroup>();
            tplHL.spacing = 4;
            tplHL.childForceExpandWidth  = false;
            tplHL.childForceExpandHeight = false;
            tplHL.childControlWidth  = true;
            tplHL.childControlHeight = true;
            tplHL.childAlignment     = TextAnchor.MiddleLeft;
            tplHL.padding = new RectOffset(4, 4, 2, 2);

            // Inner toggle GO — only checkbox + label live here so Toggle only
            // intercepts clicks on this sub-region, not the whole row.
            var toggleInner = new GameObject("toggleInner");
            toggleInner.AddComponent<RectTransform>().SetParent(tplGO.transform, false);
            var tiHL = toggleInner.AddComponent<HorizontalLayoutGroup>();
            tiHL.spacing = 4;
            tiHL.childForceExpandWidth  = false;
            tiHL.childForceExpandHeight = false;
            tiHL.childControlWidth  = true;
            tiHL.childControlHeight = true;
            tiHL.childAlignment     = TextAnchor.MiddleLeft;
            toggleInner.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Checkbox — child of toggleInner
            var checkGO  = new GameObject("Background");
            var checkRT  = checkGO.AddComponent<RectTransform>();
            checkRT.SetParent(toggleInner.transform, false);
            var checkLE = checkGO.AddComponent<LayoutElement>();
            checkLE.minWidth        = 14;
            checkLE.preferredWidth  = 14;
            checkLE.minHeight       = 14;
            checkLE.preferredHeight = 14;
            checkLE.flexibleWidth   = 0;
            checkLE.flexibleHeight  = 0;
            var checkImg = checkGO.AddComponent<Image>();
            checkImg.color = LootUIController.C_InputBg;
            var checkOL = checkGO.AddComponent<Outline>();
            checkOL.effectColor    = LootUIController.C_Border;
            checkOL.effectDistance = new Vector2(1, -1);

            // Checkmark — child of checkbox
            var markGO  = new GameObject("Checkmark");
            var markRT  = markGO.AddComponent<RectTransform>();
            markRT.SetParent(checkRT, false);
            LootUIController.StretchFull(markRT);
            markRT.offsetMin = new Vector2(3, 3);
            markRT.offsetMax = new Vector2(-3, -3);
            var markImg = markGO.AddComponent<Image>();
            markImg.color = LootUIController.C_AccentBlue;

            // Label — child of toggleInner
            var lblGO  = new GameObject("Label");
            var lblRT  = lblGO.AddComponent<RectTransform>();
            lblRT.SetParent(toggleInner.transform, false);
            var lblTxt = lblGO.AddComponent<TextMeshProUGUI>();
            lblTxt.color     = LootUIController.C_TextPri;
            lblTxt.fontSize  = 11;
            lblTxt.alignment = TextAlignmentOptions.MidlineLeft;
            lblTxt.raycastTarget = false;
            var lblLE = lblGO.AddComponent<LayoutElement>();
            lblLE.flexibleWidth   = 1;
            lblLE.preferredHeight = 18;

            // Edit button — sibling of toggleInner, not a Toggle child
            var editBtnGO = new GameObject("filterCategoryEditBtn");
            editBtnGO.AddComponent<RectTransform>().SetParent(tplGO.transform, false);
            var editImg = editBtnGO.AddComponent<Image>();
            editImg.color = LootUIController.C_BtnNormal;
            var editBtn = editBtnGO.AddComponent<Button>();
            editBtn.targetGraphic = editImg;
            var editBtnLE = editBtnGO.AddComponent<LayoutElement>();
            editBtnLE.preferredWidth  = 36;
            editBtnLE.preferredHeight = 18;
            var editLblGO = new GameObject("Label");
            editLblGO.AddComponent<RectTransform>().SetParent(editBtnGO.transform, false);
            LootUIController.StretchFull(editLblGO.GetComponent<RectTransform>());
            var editLbl = editLblGO.AddComponent<TextMeshProUGUI>();
            editLbl.text         = "Edit";
            editLbl.fontSize     = 9;
            editLbl.alignment    = TextAlignmentOptions.Center;
            editLbl.color        = LootUIController.C_TextPri;
            editLbl.raycastTarget = false;

            // Del button — sibling of toggleInner, not a Toggle child
            var delBtnGO = new GameObject("filterCategoryDeleteBtn");
            delBtnGO.AddComponent<RectTransform>().SetParent(tplGO.transform, false);
            var delImg = delBtnGO.AddComponent<Image>();
            delImg.color = LootUIController.C_BtnNormal;
            var delBtn = delBtnGO.AddComponent<Button>();
            delBtn.targetGraphic = delImg;
            var delBtnLE = delBtnGO.AddComponent<LayoutElement>();
            delBtnLE.preferredWidth  = 36;
            delBtnLE.preferredHeight = 18;
            var delLblGO = new GameObject("Label");
            delLblGO.AddComponent<RectTransform>().SetParent(delBtnGO.transform, false);
            LootUIController.StretchFull(delLblGO.GetComponent<RectTransform>());
            var delLbl = delLblGO.AddComponent<TextMeshProUGUI>();
            delLbl.text         = "Del";
            delLbl.fontSize     = 9;
            delLbl.alignment    = TextAlignmentOptions.Center;
            delLbl.color        = LootUIController.C_Danger;
            delLbl.raycastTarget = false;

            // Toggle on the row GO — targets the checkbox inside toggleInner
            _filterCategoryTemplate = tplGO.AddComponent<Toggle>();
            _filterCategoryTemplate.targetGraphic = checkImg;
            _filterCategoryTemplate.graphic       = markImg;
            tplGO.SetActive(false);

            LootUIController.MakeDivider(parent);

            // New category row
            var newCatRow = new GameObject("NewCategoryRow");
            newCatRow.AddComponent<RectTransform>().SetParent(parent, false);
            var ncHL = newCatRow.AddComponent<HorizontalLayoutGroup>();
            ncHL.spacing = 4;
            ncHL.childForceExpandWidth  = false;
            ncHL.childForceExpandHeight = true;
            ncHL.childControlWidth  = true;
            ncHL.childControlHeight = true;
            newCatRow.AddComponent<LayoutElement>().preferredHeight = 24;

            _newCategoryInput = LootUIController.MakeInputField(
                "filterlistNewName", newCatRow.transform, "New group name...");
            var inputLE = _newCategoryInput.gameObject.AddComponent<LayoutElement>();
            inputLE.flexibleWidth   = 1;
            inputLE.preferredHeight = 22;

            _newCategoryAddBtn = LootUIController.MakeButton(
                "filterlistNewAddBtn", newCatRow.transform,
                "+ Add", LootUIController.C_TextPri, LootUIController.C_BtnNormal);
            var addBtnLE = _newCategoryAddBtn.gameObject.AddComponent<LayoutElement>();
            addBtnLE.preferredWidth  = 60;
            addBtnLE.preferredHeight = 22;

            // Wire up input mute on both text fields
            var mute = _newCategoryInput.gameObject.GetComponent<TypingInputMute>()
                       ?? _newCategoryInput.gameObject.AddComponent<TypingInputMute>();
            mute.input      = _newCategoryInput;
            mute.windowRoot = _panelRoot;

            // Wire up add button
            _newCategoryAddBtn.interactable = false;
            _newCategoryInput.onValueChanged.RemoveAllListeners();
            _newCategoryInput.onValueChanged.AddListener(v =>
                _newCategoryAddBtn.interactable = !string.IsNullOrWhiteSpace(v));

            _newCategoryAddBtn.onClick.RemoveAllListeners();
            _newCategoryAddBtn.onClick.AddListener(TryAddCategoryFromInput);

            _debounce = DebounceInvoker.Attach(_panelRoot);

            RebuildFilterToggles();
        }

        public void Show()
        {
            RebuildFilterToggles();
            if (_newCategoryInput != null) _newCategoryInput.text = string.Empty;
            if (_newCategoryAddBtn != null) _newCategoryAddBtn.interactable = false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Filter category list (identical logic from WhitelistPanelController)
        // ─────────────────────────────────────────────────────────────────────
        private void RebuildFilterToggles()
        {
            if (_filterlistContent == null || _filterCategoryTemplate == null) return;

            for (int i = _filterlistContent.childCount - 1; i >= 0; i--)
            {
                var child = _filterlistContent.GetChild(i);
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

                var tmpLbl = toggle.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpLbl != null) tmpLbl.text = category;

                toggle.isOn = enabledSet.Contains(category);
                string cat  = category;
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener((bool isOn) =>
                    LootFilterlist.SetSectionEnabled(cat, isOn));

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
            if (string.IsNullOrEmpty(name))
            {
                ChatFilterInjector.SendLootMessage("[LootUI] Group name is empty.", "red");
                return;
            }
            if (Plugin.FilterList.ContainsKey(name))
            {
                ChatFilterInjector.SendLootMessage($"[LootUI] Group '{name}' already exists.", "red");
                return;
            }
            Plugin.FilterList[name] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            LootFilterlist.SaveFilterlist();
            ChatFilterInjector.SendLootMessage($"[LootUI] Created new group '{name}'.", "yellow");
            if (_newCategoryInput != null) _newCategoryInput.text = string.Empty;
            RebuildFilterToggles();
        }

        private void TryDeleteCategory(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (!Plugin.FilterList.ContainsKey(name))
            {
                ChatFilterInjector.SendLootMessage($"[LootUI] Group '{name}' not found.", "red");
                return;
            }
            Plugin.FilterList.Remove(name);
            Plugin.EnabledFilterCategories.Remove(name);
            LootFilterlist.SaveFilterlist();
            ChatFilterInjector.SendLootMessage($"[LootUI] Deleted group '{name}'.", "yellow");
            RebuildFilterToggles();
        }
    }
}