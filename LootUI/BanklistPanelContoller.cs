using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LootManager
{
    public sealed class BanklistPanelController
    {
        private readonly GameObject _root;
        private readonly RectTransform _containerRect;

        private Transform _bankitemContent;
        private Transform _banklistContent;
        private Button _banklistItemTemplate;
        private TMP_InputField _bankfilterInput;
        private Button _addBtn;
        private Button _removeBtn;
        private GameObject _dragHandle;

        private UIVirtualList _leftList;
        private UIVirtualList _rightList;

        private List<string> _leftData = new List<string>();
        private List<string> _rightData = new List<string>();

        private readonly HashSet<string> _selectedNames = new HashSet<string>(System.StringComparer.Ordinal);
        private readonly UICommon.DoubleClickTracker _doubleClick = new UICommon.DoubleClickTracker(0.25f);
        private DebounceInvoker _debounce;

        public BanklistPanelController(GameObject root, RectTransform containerRect)
        {
            _root = root;
            _containerRect = containerRect;
        }

        public void Init()
        {
            _bankitemContent      = UICommon.Find(_root, "container/panelBGbanklist/banklistPanel/bankitemView/Viewport/bankitemContent");
            _banklistContent      = UICommon.Find(_root, "container/panelBGbanklist/banklistPanel/banklistView/Viewport/banklistContent");
            _banklistItemTemplate = UICommon.Find(_root, "container/panelBGbanklist/banklistPanel/bankitemView/Viewport/bankitemContent/banklistItem")?.GetComponent<Button>();
            _bankfilterInput      = UICommon.Find(_root, "container/panelBGbanklist/banklistPanel/banklistFilter")?.GetComponent<TMP_InputField>();
            _addBtn               = UICommon.Find(_root, "container/panelBGbanklist/banklistPanel/bankaddBtn")?.GetComponent<Button>();
            _removeBtn            = UICommon.Find(_root, "container/panelBGbanklist/banklistPanel/bankremoveBtn")?.GetComponent<Button>();
            _dragHandle           = UICommon.Find(_root, "container/panelBGbanklist/lootUIDragHandle")?.gameObject;

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

            if (_banklistItemTemplate != null)
                _banklistItemTemplate.gameObject.SetActive(false);

            ItemLookup.EnsureBuilt();

            _debounce = DebounceInvoker.Attach(_root);

            BuildVirtualLists();
        }

        private void BuildVirtualLists()
        {
            if (_banklistItemTemplate == null) return;

            var leftScroll  = _bankitemContent  ? _bankitemContent.GetComponentInParent<ScrollRect>()  : null;
            var rightScroll = _banklistContent ? _banklistContent.GetComponentInParent<ScrollRect>() : null;

            float rowHeight = (_banklistItemTemplate.transform as RectTransform)?.sizeDelta.y ?? 24f;
            if (rowHeight <= 0f) rowHeight = 28f;

            _leftList  = new UIVirtualList(leftScroll,  (RectTransform)_bankitemContent,  _banklistItemTemplate.gameObject, rowHeight, bufferRows: 8);
            _rightList = new UIVirtualList(rightScroll, (RectTransform)_banklistContent, _banklistItemTemplate.gameObject, rowHeight, bufferRows: 8);

            _leftList.Enable(true);
            _rightList.Enable(true);

            Debug.Log($"[Banklist] virtual rowHeight={rowHeight}");
        }

        public void Show()
        {
            if (_bankitemContent == null || _banklistContent == null || _banklistItemTemplate == null)
            {
                Debug.LogError("[LootUI] Banklist content/template not found.");
                return;
            }

            if (_bankfilterInput != null)
            {
                _bankfilterInput.onValueChanged.RemoveAllListeners();
                _bankfilterInput.onValueChanged.AddListener(_ => _debounce.Schedule(RefreshUI, 0.15f));
            }

            RefreshUI();

            _leftList?.RecalculateAndRefresh();
            _rightList?.RecalculateAndRefresh();

            _root.GetComponent<MonoBehaviour>().StartCoroutine(UIVirtualList.DeferredFinalize(_bankitemContent));
            _root.GetComponent<MonoBehaviour>().StartCoroutine(UIVirtualList.DeferredFinalize(_banklistContent));
        }

        private void RefreshUI()
        {
            _selectedNames.Clear();

            string filter = _bankfilterInput != null && _bankfilterInput.text != null
                ? _bankfilterInput.text.ToLowerInvariant()
                : string.Empty;

            var source = ItemLookup.AllItems;

            _rightData = Plugin.Banklist
                .Where(i => string.IsNullOrEmpty(filter) || i.ToLowerInvariant().Contains(filter))
                .OrderBy(i => i)
                .ToList();

            _leftData = string.IsNullOrEmpty(filter)
                ? (source as List<string> ?? source.ToList())
                : source.Where(i => i.ToLowerInvariant().Contains(filter)).ToList();

            if (_rightData.Count > 0)
            {
                var mask = new HashSet<string>(_rightData);
                _leftData.RemoveAll(mask.Contains);
            }

            _leftList?.SetData(_leftData.Count, BindLeftRow);
            _rightList?.SetData(_rightData.Count, BindRightRow);

            UIVirtualList.FinalizeListLayout(_bankitemContent);
            UIVirtualList.FinalizeListLayout(_banklistContent);
        }

        private static Image EnsureClickTargetGraphic(GameObject go)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;
            return img;
        }

        private void BindLeftRow(GameObject row, int index)
        {
            if (index < 0 || index >= _leftData.Count) { row.SetActive(false); return; }
            string itemName = _leftData[index];
            BindRowCommon(row, itemName, isBanklist: false);
        }

        private void BindRightRow(GameObject row, int index)
        {
            if (index < 0 || index >= _rightData.Count) { row.SetActive(false); return; }
            string itemName = _rightData[index];
            BindRowCommon(row, itemName, isBanklist: true);
        }

        private void BindRowCommon(GameObject row, string itemName, bool isBanklist)
        {
            var btn = row.GetComponent<Button>() ?? row.AddComponent<Button>();
            var rootImg = EnsureClickTargetGraphic(row);
            btn.targetGraphic = rootImg;

            var iconTr  = row.transform.Find("Icon");
            var labelTr = row.transform.Find("Label");

            var icon  = iconTr  ? iconTr.GetComponent<Image>()     : null;
            var label = labelTr ? labelTr.GetComponent<TMP_Text>() : null;

            if (label != null)
            {
                label.text = itemName;
                label.raycastTarget = false;
                label.color = _selectedNames.Contains(itemName)
                    ? Color.green
                    : isBanklist ? Color.blue : Color.white;
            }

            if (icon != null)
            {
                var sprite = ItemLookup.GetIcon(itemName);
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (_doubleClick.IsDoubleClick(itemName))
                {
                    if (isBanklist)
                    {
                        Plugin.Banklist.Remove(itemName);
                        LootBanklist.SaveBanklist();
                        UpdateSocialLog.LogAdd("[LootUI] Removed from banklist: " + itemName, "yellow");
                    }
                    else
                    {
                        Plugin.Banklist.Add(itemName);
                        LootBanklist.SaveBanklist();
                        UpdateSocialLog.LogAdd("[LootUI] Added to banklist: " + itemName, "yellow");
                    }
                    RefreshUI();
                    return;
                }

                bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                if (ctrl)
                {
                    if (_selectedNames.Contains(itemName))
                        _selectedNames.Remove(itemName);
                    else
                        _selectedNames.Add(itemName);
                }
                else
                {
                    _selectedNames.Clear();
                    _selectedNames.Add(itemName);
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
                if (!Plugin.Banklist.Contains(name))
                {
                    Plugin.Banklist.Add(name);
                    changed = true;
                }
            }

            if (changed)
            {
                LootBanklist.SaveBanklist();
                RefreshUI();
                UpdateSocialLog.LogAdd("[LootUI] Added selected items to banklist.", "yellow");
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
                if (Plugin.Banklist.Contains(name))
                {
                    Plugin.Banklist.Remove(name);
                    changed = true;
                }
            }

            if (changed)
            {
                LootBanklist.SaveBanklist();
                RefreshUI();
                UpdateSocialLog.LogAdd("[LootUI] Removed selected items from banklist.", "yellow");
            }
            else
            {
                UpdateSocialLog.LogAdd("[LootUI] No valid items selected to remove.", "red");
            }
        }
    }
}
