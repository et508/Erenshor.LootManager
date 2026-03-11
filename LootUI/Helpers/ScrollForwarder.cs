using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LootManager
{
    public class ScrollForwarder : MonoBehaviour,
        IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public ScrollRect Target;

        public void OnScroll(PointerEventData data)
        {
            if (Target != null) Target.OnScroll(data);
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (Target != null) Target.OnBeginDrag(data);
        }

        public void OnDrag(PointerEventData data)
        {
            if (Target != null) Target.OnDrag(data);
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (Target != null) Target.OnEndDrag(data);
        }
    }
}