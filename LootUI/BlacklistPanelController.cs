using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LootManager
{
    public sealed class BlacklistPanelController
    {
        private readonly GameObject _root;
        private readonly RectTransform _containerRect;

        private Transform _blackitemContent;
        private Transform _blacklistContent;
        private Button _blacklistItemTemplate;
        private TMP_InputField _blackfilterInput;
        private Button _blackaddBtn;
        private Button _blackremoveBtn;
        private GameObject _dragHandle;

        private UIVirtualList _leftList;
        private UIVirtualList _rightList;

        private List<string> _leftData = new List<string>();
        private List<string> _rightData = new List<string>();
        
        private readonly HashSet<string> _selectedNames = new HashSet<string>(System.StringComparer.Ordinal);
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

        public BlacklistPanelController(GameObject root, RectTransform containerRect)
        {
            _root = root;
            _containerRect = containerRect;
        }

        public void Init()
        {
            _blackitemContent      = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blackitemView/Viewport/blackitemContent");
            _blacklistContent      = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blacklistView/Viewport/blacklistContent");
            _blacklistItemTemplate = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blackitemView/Viewport/blackitemContent/blacklistItem")?.GetComponent<Button>();
            _blackfilterInput      = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blacklistFilter")?.GetComponent<TMP_InputField>();
            _blackaddBtn           = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blackaddBtn")?.GetComponent<Button>();
            _blackremoveBtn        = UICommon.Find(_root, "container/panelBGblacklist/blacklistPanel/blackremoveBtn")?.GetComponent<Button>();
            _dragHandle            = UICommon.Find(_root, "container/panelBGblacklist/lootUIDragHandle")?.gameObject;

            if (_dragHandle != null && _containerRect != null)
            {
                var dh = _dragHandle.GetComponent<DragHandler>() ?? _dragHandle.AddComponent<DragHandler>();
                dh.PanelToMove = _containerRect;
            }

            if (_blackaddBtn != null)
            {
                _blackaddBtn.onClick.RemoveAllListeners();
                _blackaddBtn.onClick.AddListener(AddSelected);
            }

            if (_blackremoveBtn != null)
            {
                _blackremoveBtn.onClick.RemoveAllListeners();
                _blackremoveBtn.onClick.AddListener(RemoveSelected);
            }

            if (_blacklistItemTemplate != null)
                _blacklistItemTemplate.gameObject.SetActive(false);
            
            if (_blackfilterInput != null)
            {
                var mute = _blackfilterInput.gameObject.GetComponent<TypingInputMute>()
                           ?? _blackfilterInput.gameObject.AddComponent<TypingInputMute>();

                mute.input      = _blackfilterInput;
                mute.windowRoot = UICommon.Find(_root, "container/panelBGblacklist")?.gameObject;
                mute.log        = true;
            }

            ItemLookup.EnsureBuilt();

            _debounce = DebounceInvoker.Attach(_root);

            BuildVirtualLists();
        }

        private void BuildVirtualLists()
        {
            if (_blacklistItemTemplate == null) return;

            var leftScroll  = _blackitemContent  ? _blackitemContent.GetComponentInParent<ScrollRect>()  : null;
            var rightScroll = _blacklistContent ? _blacklistContent.GetComponentInParent<ScrollRect>() : null;

            float rowHeight = (_blacklistItemTemplate.transform as RectTransform)?.sizeDelta.y ?? 24f;

            _leftList  = new UIVirtualList(leftScroll,  (RectTransform)_blackitemContent,  _blacklistItemTemplate.gameObject, rowHeight, bufferRows: 8);
            _rightList = new UIVirtualList(rightScroll, (RectTransform)_blacklistContent, _blacklistItemTemplate.gameObject, rowHeight, bufferRows: 8);

            _leftList.Enable(true);
            _rightList.Enable(true);
        }

        public void Show()
        {
            if (_blackitemContent == null || _blacklistContent == null || _blacklistItemTemplate == null)
            {
                Debug.LogError("[LootUI] Blacklist content/template not found.");
                return;
            }

            if (_blackfilterInput != null)
            {
                _blackfilterInput.onValueChanged.RemoveAllListeners();
                _blackfilterInput.onValueChanged.AddListener(_ => _debounce.Schedule(RefreshUI, 0.15f));
            }

            RefreshUI();

            _leftList?.RecalculateAndRefresh();
            _rightList?.RecalculateAndRefresh();
            
            _root.GetComponent<MonoBehaviour>().StartCoroutine(UIVirtualList.DeferredFinalize(_blackitemContent));
            _root.GetComponent<MonoBehaviour>().StartCoroutine(UIVirtualList.DeferredFinalize(_blacklistContent));
        }

        private void RefreshUI()
        {
            _selectedNames.Clear();

            string filter = _blackfilterInput != null && _blackfilterInput.text != null
                ? _blackfilterInput.text.ToLowerInvariant()
                : string.Empty;

            var source = ItemLookup.AllItems;

            _rightData = Plugin.Blacklist
                .Where(i => string.IsNullOrEmpty(filter) || i.ToLowerInvariant().Contains(filter))
                .Distinct() 
                .OrderBy(i => i)
                .ToList();

            
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
            
            UIVirtualList.FinalizeListLayout(_blackitemContent);
            UIVirtualList.FinalizeListLayout(_blacklistContent);
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
            BindRowCommon(row, itemName, isBlacklist: false);
        }

        private void BindRightRow(GameObject row, int index)
        {
            if (index < 0 || index >= _rightData.Count) { row.SetActive(false); return; }
            string itemName = _rightData[index];
            BindRowCommon(row, itemName, isBlacklist: true);
        }

        private void BindRowCommon(GameObject row, string itemName, bool isBlacklist)
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
                label.color = isBlacklist ? Color.red : Color.white;
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
                    if (isBlacklist)
                    {
                        Plugin.Blacklist.Remove(itemName);
                        LootBlacklist.SaveBlacklist();
                        UpdateSocialLog.LogAdd("[LootUI] Removed from blacklist: " + itemName, "yellow");
                    }
                    else
                    {
                        Plugin.Blacklist.Add(itemName);
                        LootBlacklist.SaveBlacklist();
                        UpdateSocialLog.LogAdd("[LootUI] Added to blacklist: " + itemName, "yellow");
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
                if (!Plugin.Blacklist.Contains(name))
                {
                    Plugin.Blacklist.Add(name);
                    changed = true;
                }
            }

            if (changed)
            {
                LootBlacklist.SaveBlacklist();
                RefreshUI();
                UpdateSocialLog.LogAdd("[LootUI] Added selected items to blacklist.", "yellow");
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
                if (Plugin.Blacklist.Contains(name))
                {
                    Plugin.Blacklist.Remove(name);
                    changed = true;
                }
            }

            if (changed)
            {
                LootBlacklist.SaveBlacklist();
                RefreshUI();
                UpdateSocialLog.LogAdd("[LootUI] Removed selected items from blacklist.", "yellow");
            }
            else
            {
                UpdateSocialLog.LogAdd("[LootUI] No valid items selected to remove.", "red");
            }
        }
    }
}
