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
        private GameObject     _rowTemplate;
        private TMP_InputField _newCategoryInput;
        private Button         _newCategoryAddBtn;

        private DebounceInvoker _debounce;
        
        private static readonly (string Key, string Label, float Width)[] Cols = new[]
        {
            ("Blacklist",   "Blacklist",   58f),
            ("Whitelist",   "Whitelist",   58f),
            ("Banklist",    "Banklist",    54f),
            ("Selllist",    "Junklist",    58f),
            ("Auctionlist", "Auctionlist", 68f),
        };

        public FilterlistPanelController(GameObject panelRoot, RectTransform containerRect)
        {
            _panelRoot     = panelRoot;
            _containerRect = containerRect;
        }

        public void Init()
        {
            var vl = _panelRoot.GetComponent<VerticalLayoutGroup>()
                     ?? _panelRoot.AddComponent<VerticalLayoutGroup>();
            vl.padding                = new RectOffset(8, 8, 8, 8);
            vl.spacing                = 4;
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;
            vl.childControlWidth      = true;
            vl.childControlHeight     = true;

            var parent = _panelRoot.transform;

            
            var newCatRow = new GameObject("NewCategoryRow");
            newCatRow.AddComponent<RectTransform>().SetParent(parent, false);
            var ncHL = newCatRow.AddComponent<HorizontalLayoutGroup>();
            ncHL.spacing                = 4;
            ncHL.childForceExpandWidth  = false;
            ncHL.childForceExpandHeight = false;
            ncHL.childControlWidth      = true;
            ncHL.childControlHeight     = true;
            var newCatRowLE = newCatRow.AddComponent<LayoutElement>();
            newCatRowLE.minHeight       = 22;
            newCatRowLE.preferredHeight = 22;
            newCatRowLE.flexibleHeight  = 0;

            _newCategoryInput = LootUIController.MakeInputField(
                "filterlistNewName", newCatRow.transform, "New group name...");
            var inputLE = _newCategoryInput.gameObject.AddComponent<LayoutElement>();
            inputLE.flexibleWidth   = 1;
            inputLE.minHeight       = 22;
            inputLE.preferredHeight = 22;
            inputLE.flexibleHeight  = 0;

            _newCategoryAddBtn = LootUIController.MakeButton(
                "filterlistNewAddBtn", newCatRow.transform,
                "+ Add", LootUIController.C_TextPri, LootUIController.C_BtnNormal);
            var addBtnLE = _newCategoryAddBtn.gameObject.AddComponent<LayoutElement>();
            addBtnLE.preferredWidth  = 60;
            addBtnLE.minHeight       = 22;
            addBtnLE.preferredHeight = 22;
            addBtnLE.flexibleHeight  = 0;

            LootUIController.MakeDivider(parent);

            
            BuildHeaderRow(parent);

            
            RectTransform flVP, flContent;
            var flScroll = LootUIController.MakeScrollView(
                "filterlistView", parent, out flVP, out flContent);
            flScroll.gameObject.AddComponent<LayoutElement>().preferredHeight = 200;
            _filterlistContent = flContent;

            
            _rowTemplate = BuildRowTemplate(_filterlistContent);
            _rowTemplate.SetActive(false);

            var mute = _newCategoryInput.gameObject.GetComponent<TypingInputMute>()
                       ?? _newCategoryInput.gameObject.AddComponent<TypingInputMute>();
            mute.input      = _newCategoryInput;
            mute.windowRoot = _panelRoot;

            _newCategoryInput.onValueChanged.RemoveAllListeners();
            _newCategoryInput.onValueChanged.AddListener(v =>
                _newCategoryAddBtn.interactable = !string.IsNullOrWhiteSpace(v));
            _newCategoryAddBtn.interactable = false;
            _newCategoryAddBtn.onClick.RemoveAllListeners();
            _newCategoryAddBtn.onClick.AddListener(TryAddCategoryFromInput);

            _debounce = DebounceInvoker.Attach(_panelRoot);

            RebuildRows();
        }

        public void Show()
        {
            RebuildRows();
            if (_newCategoryInput  != null) _newCategoryInput.text  = string.Empty;
            if (_newCategoryAddBtn != null) _newCategoryAddBtn.interactable = false;
        }
        
        private static void BuildHeaderRow(Transform parent)
        {
            var hdr = new GameObject("HeaderRow");
            hdr.AddComponent<RectTransform>().SetParent(parent, false);
            var hl = hdr.AddComponent<HorizontalLayoutGroup>();
            hl.spacing                = 4;
            hl.childForceExpandWidth  = false;
            hl.childForceExpandHeight = false;
            hl.childControlWidth      = true;
            hl.childControlHeight     = true;
            hl.childAlignment         = TextAnchor.MiddleLeft;
            hl.padding                = new RectOffset(4, 4, 0, 0);
            hdr.AddComponent<LayoutElement>().preferredHeight = 18;

            
            MakeHeaderCell(hdr.transform, "Active", 38);
            
            MakeHeaderCell(hdr.transform, "Name", -1);
            
            foreach (var col in Cols)
                MakeHeaderCell(hdr.transform, col.Label, col.Width);
            
            MakeHeaderCell(hdr.transform, "", 80);
        }

        private static void MakeHeaderCell(Transform parent, string text, float width)
        {
            var go  = new GameObject("Hdr_" + text);
            go.AddComponent<RectTransform>().SetParent(parent, false);
            var le  = go.AddComponent<LayoutElement>();
            if (width < 0) le.flexibleWidth = 1;
            else           le.preferredWidth = width;
            le.preferredHeight = 18;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text                = text;
            tmp.color               = LootUIController.C_TextMuted;
            tmp.fontSize            = 9;
            tmp.fontStyle           = FontStyles.Bold;
            tmp.alignment           = TextAlignmentOptions.Center;
            tmp.enableWordWrapping  = false;
            tmp.overflowMode        = TextOverflowModes.Ellipsis;
        }
        
        private static GameObject BuildRowTemplate(Transform parent)
        {
            var row = new GameObject("filterCategoryRow");
            row.AddComponent<RectTransform>().SetParent(parent, false);
            row.AddComponent<LayoutElement>().preferredHeight = 22;

            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.spacing                = 4;
            hl.childForceExpandWidth  = false;
            hl.childForceExpandHeight = false;
            hl.childControlWidth      = true;
            hl.childControlHeight     = true;
            hl.childAlignment         = TextAnchor.MiddleLeft;
            hl.padding                = new RectOffset(4, 4, 2, 2);
            
            BuildCheckCell(row.transform, "activeToggle", 38);
            
            var nameLbl = new GameObject("nameLabel");
            nameLbl.AddComponent<RectTransform>().SetParent(row.transform, false);
            var nameLE = nameLbl.AddComponent<LayoutElement>();
            nameLE.flexibleWidth   = 1;
            nameLE.preferredHeight = 18;
            var nameTMP = nameLbl.AddComponent<TextMeshProUGUI>();
            nameTMP.color        = LootUIController.C_TextPri;
            nameTMP.fontSize     = 11;
            nameTMP.alignment    = TextAlignmentOptions.Center;
            nameTMP.raycastTarget = false;
            
            foreach (var col in Cols)
                BuildCheckCell(row.transform, col.Key + "Toggle", col.Width);
            
            BuildActionButton(row.transform, "filterCategoryEditBtn", "Edit",
                LootUIController.C_TextPri, 36);
            
            BuildActionButton(row.transform, "filterCategoryDeleteBtn", "Del",
                LootUIController.C_Danger, 36);

            return row;
        }

        private static Toggle BuildCheckCell(Transform parent, string name, float width)
        {
            var cell = new GameObject(name);
            cell.AddComponent<RectTransform>().SetParent(parent, false);
            var le = cell.AddComponent<LayoutElement>();
            le.preferredWidth  = width;
            le.preferredHeight = 14;
            le.flexibleWidth   = 0;
            le.flexibleHeight  = 0;

            var hl = cell.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment         = TextAnchor.MiddleCenter;
            hl.childForceExpandWidth  = false;
            hl.childForceExpandHeight = false;
            hl.childControlWidth      = true;
            hl.childControlHeight     = true;
            
            var bgGO  = new GameObject("Background");
            bgGO.AddComponent<RectTransform>().SetParent(cell.transform, false);
            var bgLE  = bgGO.AddComponent<LayoutElement>();
            bgLE.minWidth = bgLE.preferredWidth = 14;
            bgLE.minHeight = bgLE.preferredHeight = 14;
            bgLE.flexibleWidth = bgLE.flexibleHeight = 0;
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = LootUIController.C_InputBg;
            var bgOL  = bgGO.AddComponent<Outline>();
            bgOL.effectColor    = LootUIController.C_Border;
            bgOL.effectDistance = new Vector2(1, -1);

            
            var markGO  = new GameObject("Checkmark");
            var markRT  = markGO.AddComponent<RectTransform>();
            markRT.SetParent(bgGO.transform, false);
            LootUIController.StretchFull(markRT);
            markRT.offsetMin = new Vector2(3, 3);
            markRT.offsetMax = new Vector2(-3, -3);
            var markImg = markGO.AddComponent<Image>();
            markImg.color = LootUIController.C_AccentBlue;

            var toggle = cell.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            toggle.graphic       = markImg;
            return toggle;
        }

        private static Button BuildActionButton(Transform parent, string name,
            string label, Color32 textColor, float width)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>().SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = LootUIController.C_BtnNormal;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth  = width;
            le.preferredHeight = 18;
            var ol = go.AddComponent<Outline>();
            ol.effectColor    = LootUIController.C_AccentBlue;
            ol.effectDistance = new Vector2(1, -1);
            ol.enabled        = false;
            go.AddComponent<ButtonHoverOutline>();

            var lblGO = new GameObject("Label");
            lblGO.AddComponent<RectTransform>().SetParent(go.transform, false);
            LootUIController.StretchFull(lblGO.GetComponent<RectTransform>());
            var lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text         = label;
            lbl.fontSize     = 9;
            lbl.alignment    = TextAlignmentOptions.Center;
            lbl.color        = textColor;
            lbl.raycastTarget = false;

            return btn;
        }
        
        private void RebuildRows()
        {
            if (_filterlistContent == null || _rowTemplate == null) return;

            for (int i = _filterlistContent.childCount - 1; i >= 0; i--)
            {
                var child = _filterlistContent.GetChild(i);
                if (child.gameObject == _rowTemplate) continue;
                GameObject.Destroy(child.gameObject);
            }

            LootFilterlist.ReadAll(out var sections, out var enabledSet);
            var sorted = sections.Keys.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (string category in sorted)
            {
                var row = GameObject.Instantiate(_rowTemplate, _filterlistContent);
                row.name = "Row_" + category;
                row.SetActive(true);
                string cat = category;

                
                var activeToggle = row.transform.Find("activeToggle")?.GetComponent<Toggle>();
                if (activeToggle != null)
                {
                    activeToggle.SetIsOnWithoutNotify(enabledSet.Contains(cat));
                    activeToggle.onValueChanged.RemoveAllListeners();
                    activeToggle.onValueChanged.AddListener(v =>
                        LootFilterlist.SetSectionEnabled(cat, v));
                }

                
                var nameLbl = row.transform.Find("nameLabel")?.GetComponent<TextMeshProUGUI>();
                if (nameLbl != null) nameLbl.text = cat;

                
                foreach (var col in Cols)
                {
                    var t = row.transform.Find(col.Key + "Toggle")?.GetComponent<Toggle>();
                    if (t == null) continue;
                    var appliedSet = GetAppliedSet(col.Key);
                    t.SetIsOnWithoutNotify(appliedSet != null && appliedSet.Contains(cat));
                    string capturedKey = col.Key;
                    t.onValueChanged.RemoveAllListeners();
                    t.onValueChanged.AddListener(v =>
                        LootFilterlist.SetAppliedTo(cat, capturedKey, v));
                }
                
                var editBtn = row.transform.Find("filterCategoryEditBtn")?.GetComponent<Button>();
                if (editBtn != null)
                {
                    editBtn.onClick.RemoveAllListeners();
                    editBtn.onClick.AddListener(() => LootUIController.ShowEditCategory(cat));
                }
                
                var delBtn = row.transform.Find("filterCategoryDeleteBtn")?.GetComponent<Button>();
                if (delBtn != null)
                {
                    delBtn.onClick.RemoveAllListeners();
                    delBtn.onClick.AddListener(() => TryDeleteCategory(cat));
                }
            }

            Canvas.ForceUpdateCanvases();
            var rt = _filterlistContent.GetComponent<RectTransform>();
            if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        private static HashSet<string> GetAppliedSet(string key)
        {
            switch (key)
            {
                case "Blacklist":   return Plugin.FilterAppliedToBlacklist;
                case "Whitelist":   return Plugin.FilterAppliedToWhitelist;
                case "Banklist":    return Plugin.FilterAppliedToBanklist;
                case "Selllist":    return Plugin.FilterAppliedToSelllist;
                case "Auctionlist": return Plugin.FilterAppliedToAuctionlist;
                default:            return null;
            }
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
            RebuildRows();
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
            Plugin.FilterAppliedToBlacklist.Remove(name);
            Plugin.FilterAppliedToWhitelist.Remove(name);
            Plugin.FilterAppliedToBanklist.Remove(name);
            Plugin.FilterAppliedToSelllist.Remove(name);
            Plugin.FilterAppliedToAuctionlist.Remove(name);
            LootFilterlist.SaveFilterlist();
            ChatFilterInjector.SendLootMessage($"[LootUI] Deleted group '{name}'.", "yellow");
            RebuildRows();
        }
    }
}