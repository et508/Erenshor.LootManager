using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace LootManager
{
    public class PlayerInvUI : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(PlayerInvUICoroutine());
        }

        private IEnumerator PlayerInvUICoroutine()
        {
            // Wait until PlayerInv exists
            while (GameObject.Find("PlayerInv") == null)
                yield return null;

            GameObject playerInv = GameObject.Find("PlayerInv");

            // Wait one more frame so Unity’s layout system finishes
            yield return null;
            Canvas.ForceUpdateCanvases();

            // === Move Button (1) ===
            MoveButton(playerInv, "Button (1)", -65f, 0f);

            // === Move Button ===
            MoveButton(playerInv, "Button", -55f, 0f);

            // === Clone Button into Button (2) ===
            CloneButton(playerInv, "Button", "Button (2)");
        }

        private void MoveButton(GameObject parent, string childName, float offsetX, float offsetY)
        {
            GameObject go = parent.transform.Find(childName)?.gameObject;
            if (go != null && go.TryGetComponent(out RectTransform rt))
            {
                // If a layout group controls this button, prevent it from snapping back
                var layoutElement = go.GetComponent<LayoutElement>();
                if (layoutElement != null)
                    layoutElement.ignoreLayout = true;

                // Preserve anchors
                var saved = rt.anchoredPosition;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = saved;

                // Apply offset
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x + offsetX, rt.anchoredPosition.y + offsetY);
            }
        }

        private void CloneButton(GameObject parent, string sourceName, string cloneName)
        {
            GameObject source = parent.transform.Find(sourceName)?.gameObject;
            if (source == null) return;

            // Instantiate a copy under the same parent
            GameObject clone = GameObject.Instantiate(source, parent.transform);

            // Rename it so we can find it later
            clone.name = cloneName;

            // Remove any existing listeners so it does nothing
            if (clone.TryGetComponent<Button>(out Button btn))
            {
                btn.onClick.RemoveAllListeners();
            }
        }
    }
}
