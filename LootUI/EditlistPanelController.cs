using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LootManager
{
    public sealed class EditlistPanelController
    {
        private readonly GameObject _root;
        private GameObject _panelBGeditlist;
        private readonly RectTransform _containerRect;

        private Transform _edititemContent;
        private Transform _editlistContent;
        private Button _editlistItemTemplate;
        private TMP_InputField _editfilterInput;
        private Button _editaddBtn;
        private Button _editremoveBtn;
        private Button _closeBtn;
        //private GameObject _dragHandle;

        private UIVirtualList _leftList;
        private UIVirtualList _rightList;

        private List<string> _leftData = new List<string>();
        private List<string> _rightData = new List<string>();  

        private readonly HashSet<string> _selectedNames = new HashSet<string>(System.StringComparer.Ordinal);
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

        public EditlistPanelController(GameObject root, RectTransform containerRect)
        {
            _root = root;
            _containerRect = containerRect;
        }

        public void Init()
        {
            _panelBGeditlist      = UICommon.Find(_root, "panelBGeditlist")?.gameObject;
            _edititemContent      = UICommon.Find(_root, "panelBGeditlist/editPanel/edititemView/Viewport/edititemContent");
            _editlistContent      = UICommon.Find(_root, "panelBGeditlist/editPanel/editlistView/Viewport/editlistContent");
            _editlistItemTemplate = UICommon.Find(_root, "panelBGeditlist/editPanel/edititemView/Viewport/edititemContent/editlistItem")?.GetComponent<Button>();
            _editfilterInput      = UICommon.Find(_root, "panelBGeditlist/editPanel/editlistFilter")?.GetComponent<TMP_InputField>();
            _editaddBtn           = UICommon.Find(_root, "panelBGeditlist/editPanel/editaddBtn")?.GetComponent<Button>();
            _editremoveBtn        = UICommon.Find(_root, "panelBGeditlist/editPanel/editremoveBtn")?.GetComponent<Button>();
            _closeBtn             = UICommon.Find(_root, "panelBGeditlist/editPanel/closeBtn")?.GetComponent<Button>();
            //_dragHandle            = UICommon.Find(_root, "panelBGeditlist/lootUIDragHandle")?.gameObject;

            /* if (_dragHandle != null && _containerRect != null)
            {
                var dh = _dragHandle.GetComponent<DragHandler>() ?? _dragHandle.AddComponent<DragHandler>();
                dh.PanelToMove = _containerRect;
            } */

            if (_editaddBtn != null)
            {
                _editaddBtn.onClick.RemoveAllListeners();
                _editaddBtn.onClick.AddListener(AddSelected);
            }

            if (_editremoveBtn != null)
            {
                _editremoveBtn.onClick.RemoveAllListeners();
                _editremoveBtn.onClick.AddListener(RemoveSelected);
            }
            
            if (_closeBtn != null)
            {
                _closeBtn.onClick.RemoveAllListeners();
                _closeBtn.onClick.AddListener(Hide);
            }

            if (_editlistItemTemplate != null)
                _editlistItemTemplate.gameObject.SetActive(false);

            if (_editfilterInput != null)
            {
                var mute = _editfilterInput.gameObject.GetComponent<TypingInputMute>()
                           ?? _editfilterInput.gameObject.AddComponent<TypingInputMute>();

                mute.input      = _editfilterInput;
                mute.windowRoot = UICommon.Find(_root, "panelBGeditlist")?.gameObject;
                mute.log        = true;
            }

            ItemLookup.EnsureBuilt();

            _debounce = DebounceInvoker.Attach(_root);

            BuildVirtualLists();
        }

        private void BuildVirtualLists()
        {
            if (_editlistItemTemplate == null) return;

            var leftScroll  = _edititemContent  ? _edititemContent.GetComponentInParent<ScrollRect>()  : null;
            var rightScroll = _editlistContent ? _editlistContent.GetComponentInParent<ScrollRect>() : null;

            float rowHeight = (_editlistItemTemplate.transform as RectTransform)?.sizeDelta.y ?? 24f;

            _leftList  = new UIVirtualList(leftScroll,  (RectTransform)_edititemContent,  _editlistItemTemplate.gameObject, rowHeight, bufferRows: 8);
            _rightList = new UIVirtualList(rightScroll, (RectTransform)_editlistContent, _editlistItemTemplate.gameObject, rowHeight, bufferRows: 8);

            _leftList.Enable(true);
            _rightList.Enable(true);
        }

        /// <summary>
        /// Opens the editor for the given LootFilterlist category.
        /// </summary>
        public void Show(string categoryName)
        {
            _currentCategory = categoryName?.Trim();
            Plugin.Editlist.Clear();
            Plugin.Editlist.UnionWith(LootFilterlist.ReadSectionItems(_currentCategory));

            if (string.IsNullOrEmpty(_currentCategory))
            {
                Debug.LogError("[LootUI] EditlistPanelController.Show called with empty category.");
                return;
            }

            if (_edititemContent == null || _editlistContent == null || _editlistItemTemplate == null)
            {
                Debug.LogError("[LootUI] Edit list content/template not found.");
                return;
            }
            
            if (_panelBGeditlist != null) _panelBGeditlist.SetActive(true);

            if (_editfilterInput != null)
            {
                _editfilterInput.onValueChanged.RemoveAllListeners();
                _editfilterInput.onValueChanged.AddListener(_ => _debounce.Schedule(RefreshUI, 0.15f));
                _editfilterInput.text = string.Empty; // fresh filter each open
            }

            RefreshUI();

            _leftList?.RecalculateAndRefresh();
            _rightList?.RecalculateAndRefresh();

            if (_panelBGeditlist != null) _panelBGeditlist.SetActive(true);

            // kick layout finalize like other panels
            _root.GetComponent<MonoBehaviour>().StartCoroutine(UIVirtualList.DeferredFinalize(_edititemContent));
            _root.GetComponent<MonoBehaviour>().StartCoroutine(UIVirtualList.DeferredFinalize(_editlistContent));
        }

        /// <summary>
        /// Optional: hide panel (call from external close/back button if desired)
        /// </summary>
        public void Hide()
        {
            if (_panelBGeditlist != null) _panelBGeditlist.SetActive(false);
        }

        private void RefreshUI()
        {
            _selectedNames.Clear();

            string filter = _editfilterInput != null && _editfilterInput.text != null
                ? _editfilterInput.text.ToLowerInvariant()
                : string.Empty;

            var source = ItemLookup.AllItems ?? new List<string>();

            _rightData = Plugin.Editlist
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

            UIVirtualList.FinalizeListLayout(_edititemContent);
            UIVirtualList.FinalizeListLayout(_editlistContent);
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
            BindRowCommon(row, itemName, isInCategory: false);
        }

        private void BindRightRow(GameObject row, int index)
        {
            if (index < 0 || index >= _rightData.Count) { row.SetActive(false); return; }
            string itemName = _rightData[index];
            BindRowCommon(row, itemName, isInCategory: true);
        }

        private void BindRowCommon(GameObject row, string itemName, bool isInCategory)
        {
            var btn = row.GetComponent<Button>() ?? row.AddComponent<Button>();
            var rootImg = EnsureClickTargetGraphic(row);
            btn.targetGraphic = rootImg;
            btn.transition = Selectable.Transition.None;
            btn.interactable = true;

            var iconTr  = row.transform.Find("Icon");
            var labelTr = row.transform.Find("Label");

            var icon  = iconTr  ? iconTr.GetComponent<Image>()     : null;
            var label = labelTr ? labelTr.GetComponent<TMP_Text>() : null;

            if (label != null)
            {
                label.text = itemName;
                label.raycastTarget = false;
                // same visual language as your other panels: left=red, right=white
                label.color = isInCategory ? Color.white : Color.red;
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
                // normal/hover/pressed
                new Color(1f, 1f, 1f, 0f),
                new Color(1f, 1f, 1f, 0.12f),
                new Color(1f, 1f, 1f, 0.20f),
                // selected normal/hover/pressed
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
                    if (isInCategory)
                    {
                        Plugin.Editlist.Remove(itemName);
                        LootFilterlist.SaveSectionItems(_currentCategory, Plugin.Editlist);
                        UpdateSocialLog.LogAdd($"[LootUI] Removed from {_currentCategory}: {itemName}", "yellow");
                    }
                    else
                    {
                        Plugin.Editlist.Add(itemName);
                        LootFilterlist.SaveSectionItems(_currentCategory, Plugin.Editlist);
                        UpdateSocialLog.LogAdd($"[LootUI] Added to {_currentCategory}: {itemName}", "yellow");
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

            if (changed)
            {
                LootFilterlist.SaveSectionItems(_currentCategory, Plugin.Editlist);
                RefreshUI();
                UpdateSocialLog.LogAdd($"[LootUI] Added selected items to {_currentCategory}.", "yellow");
            }
            else
            {
                UpdateSocialLog.LogAdd("[LootUI] No valid items selected to add.", "red");
            }
        }

        private void RemoveSelected()
        {
            if (string.IsNullOrEmpty(_currentCategory)) return;

            bool changed = false;
            foreach (var name in _selectedNames)
                if (Plugin.Editlist.Remove(name)) changed = true;

            if (changed)
            {
                LootFilterlist.SaveSectionItems(_currentCategory, Plugin.Editlist);
                RefreshUI();
                UpdateSocialLog.LogAdd($"[LootUI] Removed selected items from {_currentCategory}.", "yellow");
            }
            else
            {
                UpdateSocialLog.LogAdd("[LootUI] No valid items selected to remove.", "red");
            }
        }
    }
}
