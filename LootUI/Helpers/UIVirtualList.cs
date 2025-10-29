using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public sealed class UIVirtualList
    {
        public delegate void BindRowDelegate(GameObject row, int index);

        private readonly ScrollRect _scrollRect;
        private readonly RectTransform _viewport;
        private readonly RectTransform _content;
        private readonly GameObject _rowTemplate;
        private readonly float _rowHeight;
        private readonly int _bufferRows;
        private readonly List<GameObject> _pool = new List<GameObject>();

        private int _visibleCount;    
        private int _dataCount;
        private float _contentHeight;
        private Vector2 _lastAnchoredPos;
        private BindRowDelegate _bind;
        private bool _enabled;
        
        public UIVirtualList(ScrollRect scrollRect, RectTransform content, GameObject rowTemplate, float rowHeight, int bufferRows = 8)
        {
            _scrollRect   = scrollRect;
            _viewport     = scrollRect != null ? scrollRect.viewport : null;
            _content      = content;
            _rowTemplate  = rowTemplate;
            _rowHeight    = rowHeight > 0 ? rowHeight : 24f;
            _bufferRows   = Mathf.Max(2, bufferRows);

            if (_rowTemplate != null) _rowTemplate.SetActive(false);

            EnsureTopAnchors();

            if (_scrollRect != null)
                _scrollRect.onValueChanged.AddListener(_ => OnScroll());
        }
        
        public void Enable(bool enabled)
        {
            _enabled = enabled;
            if (!enabled) ClearPool();
        }
        
        public void SetData(int count, BindRowDelegate bind)
        {
            _dataCount = Mathf.Max(0, count);
            _bind = bind;
            RecalcAndEnsurePool();
            RebindVisible();
        }
        
        public void Refresh() => RebindVisible();
        
        public void RecalculateAndRefresh()
        {
            RecalcAndEnsurePool();
            RebindVisible();
        }

        public void Dispose()
        {
            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveAllListeners();
            ClearPool();
        }

        private void EnsureTopAnchors()
        {
            if (_content != null)
            {
                _content.anchorMin = new Vector2(0f, 1f);
                _content.anchorMax = new Vector2(1f, 1f);
                _content.pivot     = new Vector2(0.5f, 1f);
                
                var vlg = _content.GetComponent<VerticalLayoutGroup>();
                if (vlg) vlg.enabled = false;
                var hlg = _content.GetComponent<HorizontalLayoutGroup>();
                if (hlg) hlg.enabled = false;
                var fitter = _content.GetComponent<ContentSizeFitter>();
                if (fitter) fitter.enabled = false;
            }

            if (_rowTemplate != null)
            {
                var rt = _rowTemplate.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot     = new Vector2(0.5f, 1f);
                    rt.sizeDelta = new Vector2(0f, _rowHeight);
                }
            }
        }

        private float GetViewportHeight()
        {
            float h = _viewport != null ? _viewport.rect.height : 0f;
            if (h <= 1f)
                h = 400f;
            return h;
        }

        private void RecalcAndEnsurePool()
        {
            if (!_enabled || _content == null) return;

            float viewHeight = GetViewportHeight();
            int rowsInView = Mathf.CeilToInt(viewHeight / _rowHeight);
            _visibleCount = Mathf.Max(1, rowsInView + _bufferRows);
            
            int needed = Mathf.Min(_visibleCount, Mathf.Max(1, _dataCount));
            while (_pool.Count < needed) _pool.Add(CreateRow());
            while (_pool.Count > needed)
            {
                var go = _pool[_pool.Count - 1];
                _pool.RemoveAt(_pool.Count - 1);
                if (go != null) GameObject.Destroy(go);
            }

            _contentHeight = _dataCount * _rowHeight;
            var size = _content.sizeDelta;
            _content.sizeDelta = new Vector2(size.x, _contentHeight);
        }

        private GameObject CreateRow()
        {
            var go = GameObject.Instantiate(_rowTemplate, _content);
            go.name = "VirtualRow";
            go.SetActive(true);
            
            var rt = go.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0f, _rowHeight);
            }
            return go;
        }

        private void ClearPool()
        {
            for (int i = 0; i < _pool.Count; i++)
                if (_pool[i] != null) GameObject.Destroy(_pool[i]);
            _pool.Clear();
        }

        private void OnScroll()
        {
            if (!_enabled) return;
            var pos = _content.anchoredPosition;
            if (Mathf.Abs(pos.y - _lastAnchoredPos.y) >= (_rowHeight * 0.25f))
            {
                _lastAnchoredPos = pos;
                RebindVisible();
            }
        }

        private void RebindVisible()
        {
            if (!_enabled || _bind == null || _dataCount <= 0)
            {
                foreach (var r in _pool) if (r != null) r.SetActive(false);
                return;
            }
            
            float scrollY = Mathf.Max(0f, _content.anchoredPosition.y);

            int firstIndex = Mathf.FloorToInt(scrollY / _rowHeight) - (_bufferRows / 2);
            if (firstIndex < 0) firstIndex = 0;

            for (int i = 0; i < _pool.Count; i++)
            {
                int dataIndex = firstIndex + i;
                var row = _pool[i];

                if (dataIndex >= 0 && dataIndex < _dataCount)
                {
                    if (!row.activeSelf) row.SetActive(true);
                    
                    var rt = row.transform as RectTransform;
                    float y = -dataIndex * _rowHeight; 
                    rt.anchoredPosition = new Vector2(0f, y);

                    _bind(row, dataIndex);
                }
                else
                {
                    if (row.activeSelf) row.SetActive(false);
                }
            }
        }
        
        public static void FinalizeListLayout(Transform content)
        {
            if (content == null) return;

            Canvas.ForceUpdateCanvases();

            var rt = content as RectTransform;
            if (rt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            var scroll = rt != null ? rt.GetComponentInParent<ScrollRect>() : null;
            if (scroll != null)
            {
                var vprt = scroll.viewport as RectTransform;
                if (vprt != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(vprt);

                var cont = scroll.content as RectTransform;
                if (cont != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(cont);

                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1f;
            }

            Canvas.ForceUpdateCanvases();
        }
        
        public static System.Collections.IEnumerator DeferredFinalize(Transform content)
        {
            yield return null;
            FinalizeListLayout(content);
        }

    }
}
