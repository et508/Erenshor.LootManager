using UnityEngine;
using UnityEngine.EventSystems;

namespace LootManager
{
    public class DragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform PanelToMove;

        private bool _dragging;
        private Vector2 _offset;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || PanelToMove == null) return;

            _dragging = true;
            GameData.DraggingUIElement = true;

            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                PanelToMove, eventData.position, null, out local);
            _offset = local;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || PanelToMove == null) return;

            RectTransform parent = PanelToMove.parent as RectTransform;
            if (parent == null) return;

            Vector2 pointerPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, eventData.position, null, out pointerPos);

            PanelToMove.anchoredPosition = pointerPos - _offset;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _dragging = false;
            GameData.DraggingUIElement = false;
        }
    }
}