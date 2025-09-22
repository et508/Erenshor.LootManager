using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
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
            var button2 = CloneButton(playerInv, "Button", "Button (2)");

            // Make sure the clone is inert (does nothing when clicked / submitted)
            if (button2 != null)
            {
                MakeInert(button2);
                // Position it where you want
                MoveButton(playerInv, "Button (2)", 115f, 0f);

                // Update its label and color
                UpdateButtonText(playerInv, "Button (2)", "Manager", new Color(1f, 0.9216f, 0.0157f, 1f));
            }
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

        private GameObject CloneButton(GameObject parent, string sourceName, string cloneName)
        {
            GameObject source = parent.transform.Find(sourceName)?.gameObject;
            if (source == null) return null;

            // Instantiate a copy under the same parent
            GameObject clone = Instantiate(source, parent.transform);

            // Rename it so we can find it later
            clone.name = cloneName;

            // Remove any existing listeners so it does nothing (we'll also sanitize below)
            if (clone.TryGetComponent<Button>(out Button btn))
            {
                btn.onClick.RemoveAllListeners();
            }

            return clone;
        }

        /// <summary>
        /// Ensures the cloned button is visually identical but functionally inert.
        /// - Clears Button.onClick
        /// - Removes EventTrigger(s)
        /// - Disables non-visual MonoBehaviours that might handle clicks
        /// - Disables Navigation to avoid submit/keyboard triggering
        /// </summary>
        private void MakeInert(GameObject go)
        {
            // 1) Clear Button listeners & disable navigation submit
            if (go.TryGetComponent<Button>(out var btn))
            {
                btn.onClick.RemoveAllListeners();
                btn.navigation = new Navigation { mode = Navigation.Mode.None };
                // keep interactable true so hover/press visuals still work
                btn.interactable = true;
            }

            // 2) Remove any EventTrigger components (on this object or children)
            foreach (var trig in go.GetComponentsInChildren<EventTrigger>(true))
            {
                Destroy(trig);
            }

            // 3) Disable non-visual scripts that might listen for pointer events
            // Whitelist common visual/structural components; disable everything else (MonoBehaviours)
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;

                // Skip whitelisted component types we want to keep enabled
                if (mb is Button ||
                    mb is Image ||
                    mb is TextMeshProUGUI ||
                    mb is TMP_Text ||
                    mb is Outline ||
                    mb is Shadow ||
                    mb is LayoutElement ||
                    mb is ContentSizeFitter ||
                    mb is Mask ||
                    mb is RectMask2D)
                {
                    continue;
                }

                // Many custom interactive scripts implement IPointerXXX or ISubmitHandler;
                // disabling them ensures clicks do nothing.
                // We don't destroy them—just disable—so you can re-enable later if desired.
                mb.enabled = false;
            }
        }

        private void UpdateButtonText(GameObject parent, string buttonName, string newText, Color newColor)
        {
            GameObject button = parent.transform.Find(buttonName)?.gameObject;
            if (button == null) return;

            // Look for the Text (TMP) child
            var textTransform = button.transform.Find("Text (TMP)");
            if (textTransform != null && textTransform.TryGetComponent(out TextMeshProUGUI tmp))
            {
                tmp.text = newText;
                tmp.color = newColor;
            }
        }
    }
}
