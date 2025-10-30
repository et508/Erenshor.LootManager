using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CheatManager
{
    public sealed class RowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Image _bg;
        
        private Color _normal, _highlighted, _pressed;
       
        private Color _selNormal, _selHighlighted, _selPressed;

        private bool _isPointerOver;
        private bool _isPointerDown;
        private bool _isSelected;

        public void Init(
            Image bg,
            Color normal, Color highlighted, Color pressed,
            Color selectedNormal, Color selectedHighlighted, Color selectedPressed)
        {
            _bg = bg;

            _normal = normal;
            _highlighted = highlighted;
            _pressed = pressed;

            _selNormal = selectedNormal;
            _selHighlighted = selectedHighlighted;
            _selPressed = selectedPressed;

            Apply();
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            Apply();
        }

        private void OnEnable() => Apply();

        private void OnDisable()
        {
            if (_bg != null) _bg.color = _isSelected ? _selNormal : _normal;
            _isPointerOver = _isPointerDown = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isPointerOver = true;
            Apply();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isPointerOver = false;
            _isPointerDown = false;
            Apply();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPointerDown = true;
            Apply();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPointerDown = false;
            Apply();
        }

        private void Apply()
        {
            if (_bg == null) return;
            
            Color n   = _isSelected ? _selNormal      : _normal;
            Color hov = _isSelected ? _selHighlighted : _highlighted;
            Color prs = _isSelected ? _selPressed     : _pressed;

            if (_isPointerDown)
                _bg.color = prs;
            else if (_isPointerOver)
                _bg.color = hov;
            else
                _bg.color = n;
        }
    }
}
