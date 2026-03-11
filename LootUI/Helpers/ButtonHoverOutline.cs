

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LootManager
{
    public sealed class ButtonHoverOutline : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        public bool keepActiveState;

        private Outline _outline;
        private bool    _hovered;

        private Outline Ol => _outline != null ? _outline : (_outline = GetComponent<Outline>());

        public void OnPointerEnter(PointerEventData _)
        {
            _hovered = true;
            if (Ol != null) Ol.enabled = true;
        }

        public void OnPointerExit(PointerEventData _)
        {
            _hovered = false;
            if (Ol != null && !keepActiveState)
                Ol.enabled = false;
        }

        public void SetActive(bool active)
        {
            keepActiveState = active;
            if (Ol != null)
                Ol.enabled = active || _hovered;
        }
    }
}