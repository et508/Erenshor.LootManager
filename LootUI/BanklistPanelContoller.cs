using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public sealed class BanklistPanelController
    {
        private readonly GameObject    _panelRoot;
        private readonly RectTransform _containerRect;

        private Transform      _leftContent;
        private Transform      _rightContent;
        private GameObject     _rowTemplate;
        private TMP_InputField _filterInput;
        private UIVirtualList _leftList;
        private UIVirtualList _rightList;

        private List<string> _leftData  = new List<string>();
        private List<string> _rightData = new List<string>();

        private readonly HashSet<string>           _selectedNames = new HashSet<string>(System.StringComparer.Ordinal);
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

        public BanklistPanelController(GameObject panelRoot, RectTransform containerRect)
        {
            _panelRoot     = panelRoot;
            _containerRect = containerRect;
        }

        public void Init()
        {
            var refs = DualListPanelBuilder.Build(
                _panelRoot,
                leftTitle:  "All Items",
                rightTitle: "Banklisted",
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
                mute.windowRoot = _panelRoot;
            }

            ItemLookup.EnsureBuilt();
            _debounce = DebounceInvoker.Attach(_panelRoot);
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

        public void Show()
        {
            if (_leftContent == null || _rightContent == null || _rowTemplate == null)
            {
                Debug.LogError("[LootUI] Banklist content/template not found.");
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

            _rightData = Plugin.Banklist
                .Where(i => string.IsNullOrEmpty(filter) || i.ToLowerInvariant().Contains(filter))
                .Distinct().OrderBy(i => i).ToList();

            _leftData = string.IsNullOrEmpty(filter)
                ? new List<string>(source)
                : source.Where(i => i.ToLowerInvariant().Contains(filter)).ToList();

            if (_rightData.Count > 0)
            {
                var mask = new HashSet<string>(_rightData, System.StringComparer.Ordinal);
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
            BindRowCommon(row, _leftData[index], isBanklist: false);
        }

        private void BindRightRow(GameObject row, int index)
        {
            if (index < 0 || index >= _rightData.Count) { row.SetActive(false); return; }
            BindRowCommon(row, _rightData[index], isBanklist: true);
        }

        private void BindRowCommon(GameObject row, string itemName, bool isBanklist)
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
                label.color         = isBanklist ? Color.blue : Color.white;
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
                    if (isBanklist)
                    {
                        Plugin.Banklist.Remove(itemName);
                        LootBanklist.SaveBanklist();
                        ChatFilterInjector.SendLootMessage("[LootUI] Removed from banklist: " + itemName, "yellow");
                    }
                    else
                    {
                        Plugin.Banklist.Add(itemName);
                        LootBanklist.SaveBanklist();
                        ChatFilterInjector.SendLootMessage("[LootUI] Added to banklist: " + itemName, "yellow");
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
                if (!Plugin.Banklist.Contains(name)) { Plugin.Banklist.Add(name); changed = true; }
            if (changed) { LootBanklist.SaveBanklist(); RefreshUI(); ChatFilterInjector.SendLootMessage("[LootUI] Added selected items to banklist.", "yellow"); }
            else ChatFilterInjector.SendLootMessage("[LootUI] No valid items selected to add.", "red");
        }

        private void RemoveSelected()
        {
            bool changed = false;
            foreach (var name in _selectedNames.ToArray())
                if (Plugin.Banklist.Contains(name)) { Plugin.Banklist.Remove(name); changed = true; }
            if (changed) { LootBanklist.SaveBanklist(); RefreshUI(); ChatFilterInjector.SendLootMessage("[LootUI] Removed selected items from banklist.", "yellow"); }
            else ChatFilterInjector.SendLootMessage("[LootUI] No valid items selected to remove.", "red");
        }
    }
}