using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public sealed class EditlistPanelController
    {
        private readonly GameObject    _editViewRoot;  
        private readonly RectTransform _containerRect;

        private TMP_Text       _groupNameTMP;
        private Transform      _leftContent;
        private Transform      _rightContent;
        private GameObject     _rowTemplate;
        private TMP_InputField _filterInput;
        private UIVirtualList _leftList;
        private UIVirtualList _rightList;

        private List<string> _leftData  = new List<string>();
        private List<string> _rightData = new List<string>();

        private readonly HashSet<string>           _selectedNames = new HashSet<string>(StringComparer.Ordinal);
        private readonly UICommon.DoubleClickTracker _doubleClick = new UICommon.DoubleClickTracker(0.25f);
        private DebounceInvoker _debounce;
        private string _currentCategory;

        private static Sprite _white1x1;
        private static Sprite GetWhite1x1()
        {
            if (_white1x1 != null) return _white1x1;
            var tex = Texture2D.whiteTexture;
            _white1x1 = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            _white1x1.name = "LootUI_White1x1";
            return _white1x1;
        }

        public EditlistPanelController(GameObject editViewRoot, RectTransform containerRect)
        {
            _editViewRoot  = editViewRoot;
            _containerRect = containerRect;
        }

        public void Init()
        {
            if (_editViewRoot == null) return;
            
            var titleBar = new GameObject("editTitleBar");
            var tbRT = titleBar.AddComponent<RectTransform>();
            tbRT.SetParent(_editViewRoot.transform, false);
            tbRT.anchorMin = new Vector2(0, 1);
            tbRT.anchorMax = new Vector2(1, 1);
            tbRT.pivot     = new Vector2(0.5f, 1);
            tbRT.sizeDelta = new Vector2(0, 28);
            tbRT.anchoredPosition = Vector2.zero;
            titleBar.AddComponent<Image>().color = LootUIController.C_TitleBg;

            _groupNameTMP = LootUIController.MakeTMP("groupName", tbRT);
            var gnRT = _groupNameTMP.GetComponent<RectTransform>();
            gnRT.anchorMin = Vector2.zero;
            gnRT.anchorMax = Vector2.one;
            gnRT.offsetMin = new Vector2(10, 0);
            gnRT.offsetMax = new Vector2(-36, 0);
            _groupNameTMP.fontSize  = 13;
            _groupNameTMP.fontStyle = FontStyles.Bold;
            _groupNameTMP.alignment = TextAlignmentOptions.MidlineLeft;
            _groupNameTMP.color     = LootUIController.C_TextPri;

            var closeBtn = LootUIController.MakeButton("closeBtn", tbRT, "X",
                LootUIController.C_Danger, LootUIController.C_TitleBg, 11);
            var closeBtnOL = closeBtn.GetComponent<Outline>();
            if (closeBtnOL != null) UnityEngine.Object.Destroy(closeBtnOL);
            var closeBtnBHO = closeBtn.GetComponent<ButtonHoverOutline>();
            if (closeBtnBHO != null) UnityEngine.Object.Destroy(closeBtnBHO);
            var cbHover = closeBtn.gameObject.AddComponent<LootUIController.CloseButtonHover>();
            cbHover.bg        = closeBtn.GetComponent<Image>();
            cbHover.lbl       = closeBtn.GetComponentInChildren<TextMeshProUGUI>();
            cbHover.normalBg   = LootUIController.C_TitleBg;
            cbHover.normalText = LootUIController.C_Danger;
            var closeBtnRT = closeBtn.GetComponent<RectTransform>();
            closeBtnRT.anchorMin = new Vector2(1, 0);
            closeBtnRT.anchorMax = new Vector2(1, 1);
            closeBtnRT.pivot     = new Vector2(1, 0.5f);
            closeBtnRT.sizeDelta = new Vector2(28, 0);
            closeBtnRT.anchoredPosition = Vector2.zero;
            closeBtn.onClick.AddListener(Hide);
            
            var body = new GameObject("editBody");
            var bodyRT = body.AddComponent<RectTransform>();
            bodyRT.SetParent(_editViewRoot.transform, false);
            bodyRT.anchorMin = Vector2.zero;
            bodyRT.anchorMax = Vector2.one;
            bodyRT.offsetMin = Vector2.zero;
            bodyRT.offsetMax = new Vector2(0, -28);
            
            var refs = DualListPanelBuilder.Build(
                body,
                leftTitle:  "All Items",
                rightTitle: "In Category",
                filterPlaceholder: "Filter items..."
            );

            _leftContent  = refs.LeftContent;
            _rightContent = refs.RightContent;
            _rowTemplate  = refs.RowTemplate;
            _filterInput  = refs.FilterInput;

            if (_filterInput != null)
            {
                var mute = _filterInput.gameObject.GetComponent<TypingInputMute>()
                           ?? _filterInput.gameObject.AddComponent<TypingInputMute>();
                mute.input      = _filterInput;
                mute.windowRoot = _editViewRoot;
            }

            ItemLookup.EnsureBuilt();
            _debounce = DebounceInvoker.Attach(_editViewRoot);
            BuildVirtualLists();
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

        public void Show(string categoryName)
        {
            _currentCategory = categoryName?.Trim();
            if (string.IsNullOrEmpty(_currentCategory))
            {
                Debug.LogError("[LootUI] EditlistPanelController.Show called with empty category.");
                return;
            }

            SetTitle(_currentCategory);

            Plugin.Editlist.Clear();
            Plugin.Editlist.UnionWith(LootFilterlist.ReadSectionItems(_currentCategory));

            if (_leftContent == null || _rightContent == null || _rowTemplate == null)
            {
                Debug.LogError("[LootUI] Edit list content/template not found.");
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
            _editViewRoot.GetComponent<MonoBehaviour>().StartCoroutine(UIVirtualList.DeferredFinalize(_leftContent));
            _editViewRoot.GetComponent<MonoBehaviour>().StartCoroutine(UIVirtualList.DeferredFinalize(_rightContent));
        }

        public void Hide()
        {
            LootUIController.HideEditView();
            _selectedNames.Clear();
        }

        private void RefreshUI()
        {
            _selectedNames.Clear();
            string filter = _filterInput?.text?.ToLowerInvariant() ?? string.Empty;
            var source    = ItemLookup.AllItems ?? new List<string>();

            _rightData = Plugin.Editlist
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
            BindRowCommon(row, _leftData[index], isInCategory: false);
        }

        private void BindRightRow(GameObject row, int index)
        {
            if (index < 0 || index >= _rightData.Count) { row.SetActive(false); return; }
            BindRowCommon(row, _rightData[index], isInCategory: true);
        }

        private void BindRowCommon(GameObject row, string itemName, bool isInCategory)
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
                label.color         = isInCategory ? Color.white : Color.red;
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
                    if (isInCategory)
                    {
                        Plugin.Editlist.Remove(itemName);
                        LootFilterlist.SaveSectionItems(_currentCategory, Plugin.Editlist);
                        ChatFilterInjector.SendLootMessage($"[LootUI] Removed from {_currentCategory}: {itemName}", "yellow");
                    }
                    else
                    {
                        Plugin.Editlist.Add(itemName);
                        LootFilterlist.SaveSectionItems(_currentCategory, Plugin.Editlist);
                        ChatFilterInjector.SendLootMessage($"[LootUI] Added to {_currentCategory}: {itemName}", "yellow");
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
            if (string.IsNullOrEmpty(_currentCategory)) return;
            bool changed = false;
            foreach (var name in _selectedNames)
                if (Plugin.Editlist.Add(name)) changed = true;
            if (changed) { LootFilterlist.SaveSectionItems(_currentCategory, Plugin.Editlist); RefreshUI(); ChatFilterInjector.SendLootMessage($"[LootUI] Added selected items to {_currentCategory}.", "yellow"); }
            else ChatFilterInjector.SendLootMessage("[LootUI] No valid items selected to add.", "red");
        }

        private void RemoveSelected()
        {
            if (string.IsNullOrEmpty(_currentCategory)) return;
            bool changed = false;
            foreach (var name in _selectedNames)
                if (Plugin.Editlist.Remove(name)) changed = true;
            if (changed) { LootFilterlist.SaveSectionItems(_currentCategory, Plugin.Editlist); RefreshUI(); ChatFilterInjector.SendLootMessage($"[LootUI] Removed selected items from {_currentCategory}.", "yellow"); }
            else ChatFilterInjector.SendLootMessage("[LootUI] No valid items selected to remove.", "red");
        }

        private void SetTitle(string category)
        {
            if (_groupNameTMP == null) return;
            _groupNameTMP.text = string.IsNullOrEmpty(category)
                ? "Edit Group"
                : $"Editing: {category}";
        }
    }
}