using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public sealed class BanklistPanelController
    {
        private readonly GameObject _root;
        private readonly RectTransform _containerRect;

        private Transform _bankitemContent;
        private Transform _banklistContent;
        private TMPro.TMP_InputField _bankfilterInput;
        private Button _addBtn;
        private Button _removeBtn;
        private GameObject _dragHandle;

        private readonly List<string> _allItems = new List<string>();
        private readonly List<(Text text, bool isBanklist)> _selected = new List<(Text text, bool isBanklist)>();
        private readonly UICommon.DoubleClickTracker _doubleClick = new UICommon.DoubleClickTracker(0.25f);

        public BanklistPanelController(GameObject root, RectTransform containerRect)
        {
            _root = root;
            _containerRect = containerRect;
        }

        public void Init()
        {
            _bankitemContent  = UICommon.Find(_root, "container/panelBGbanklist/banklistPanel/bankitemView/Viewport/bankitemContent");
            _banklistContent  = UICommon.Find(_root, "container/panelBGbanklist/banklistPanel/banklistView/Viewport/banklistContent");
            _bankfilterInput  = UICommon.Find(_root, "container/panelBGbanklist/banklistPanel/banklistFilter")?.GetComponent<TMPro.TMP_InputField>();
            _addBtn           = UICommon.Find(_root, "container/panelBGbanklist/banklistPanel/bankaddBtn")?.GetComponent<Button>();
            _removeBtn        = UICommon.Find(_root, "container/panelBGbanklist/banklistPanel/bankremoveBtn")?.GetComponent<Button>();
            _dragHandle       = UICommon.Find(_root, "container/panelBGbanklist/lootUIDragHandle")?.gameObject;

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

            RebuildAllItems();
        }

        public void Show()
        {
            if (_bankitemContent == null || _banklistContent == null)
            {
                Debug.LogError("[LootUI] Banklist content panels not found.");
                return;
            }

            if (_bankfilterInput != null)
            {
                _bankfilterInput.onValueChanged.RemoveAllListeners();
                _bankfilterInput.onValueChanged.AddListener(delegate { RefreshUI(); });
            }

            RefreshUI();
        }

        private void RebuildAllItems()
        {
            _allItems.Clear();
            List<string> list = GameData.ItemDB.ItemDB
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.ItemName))
                .Select(x => x.ItemName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
            _allItems.AddRange(list);
        }

        private void RefreshUI()
        {
            UICommon.ClearList(_bankitemContent);
            UICommon.ClearList(_banklistContent);
            _selected.Clear();

            string filter = _bankfilterInput != null && _bankfilterInput.text != null
                ? _bankfilterInput.text.ToLowerInvariant()
                : string.Empty;

            List<string> filteredItems = string.IsNullOrEmpty(filter)
                ? _allItems
                : _allItems.Where(item => item.ToLowerInvariant().Contains(filter)).ToList();

            List<string> filteredBanklist = Plugin.Banklist
                .Where(item => string.IsNullOrEmpty(filter) || item.ToLowerInvariant().Contains(filter))
                .OrderBy(item => item)
                .ToList();

            foreach (string item in filteredItems)
            {
                if (!filteredBanklist.Contains(item))
                    CreateRow(_bankitemContent, item, false);
            }

            foreach (string item in filteredBanklist)
            {
                CreateRow(_banklistContent, item, true);
            }
        }

        private void CreateRow(Transform parent, string itemName, bool isBanklist)
        {
            GameObject go = new GameObject(itemName);
            go.transform.SetParent(parent, false);

            Text text = go.AddComponent<Text>();
            text.text = itemName;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.color = isBanklist ? Color.blue : Color.white;
            text.fontSize = 14;

            Button btn = go.AddComponent<Button>();
            btn.onClick.AddListener(delegate
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
                bool already = _selected.Any(e => e.text == text);

                if (ctrl)
                {
                    if (already)
                    {
                        text.color = isBanklist ? Color.blue : Color.white;
                        _selected.RemoveAll(e => e.text == text);
                    }
                    else
                    {
                        text.color = Color.green;
                        _selected.Add((text, isBanklist));
                    }
                }
                else
                {
                    foreach (var entry in _selected.ToArray())
                    {
                        Text t = entry.text;
                        bool wasBank = entry.isBanklist;
                        t.color = wasBank ? Color.blue : Color.white;
                    }
                    _selected.Clear();

                    text.color = Color.green;
                    _selected.Add((text, isBanklist));
                }
            });
        }

        private void AddSelected()
        {
            bool changed = false;
            foreach (var entry in _selected.ToArray())
            {
                Text text = entry.text;
                if (!Plugin.Banklist.Contains(text.text))
                {
                    Plugin.Banklist.Add(text.text);
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
            foreach (var entry in _selected.ToArray())
            {
                Text text = entry.text;
                if (Plugin.Banklist.Contains(text.text))
                {
                    Plugin.Banklist.Remove(text.text);
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
