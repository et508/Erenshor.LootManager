using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace LootManager
{
    public class PlayerInvUI : MonoBehaviour
    {
        private const string PlayerInvName    = "PlayerInv";
        private const string SourcePanelName  = "AAScreen";
        private const string ManagerPanelName = "ManagerPanel";
        
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

            // Position it where you want
            MoveButton(playerInv, "Button (2)", 115f, 0f);

            // Update its label and color
            UpdateButtonText(playerInv, "Button (2)", "Manager", new Color(1f, 0.9216f, 0.0157f, 1f));
            
            // Ensure Button (2) has a Button component and ONLY our click
            if (button2 != null)
            {
                var btn = button2.GetComponent<Button>();
                if (btn == null) btn = button2.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnManagerButtonClicked);
            }
            
            // Clone AAScreen -> ManagerPanel, keeping only Image & Text (TMP) as direct children
            var managerPanel = ClonePanelStripChildren(
                parent: playerInv,
                sourceName: SourcePanelName,
                cloneName: ManagerPanelName,
                keepChildNames: new[] { "Image", "Text (TMP)" }
            );
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
        
        private void OnManagerButtonClicked()
        {
            var playerInv = GameObject.Find(PlayerInvName);
            if (playerInv == null) return;

            var panel = playerInv.transform.Find(ManagerPanelName)?.gameObject;
            if (panel == null) return;

            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            // (We’re not hiding other tabs yet; we’ll revisit if needed)
        }
        
        private GameObject ClonePanelStripChildren(GameObject parent, string sourceName, string cloneName, string[] keepChildNames)
        {
            // Reuse if already present
            var existing = parent.transform.Find(cloneName)?.gameObject;
            if (existing != null) return existing;

            var source = parent.transform.Find(sourceName);
            if (source == null) return null;

            var clone = Instantiate(source.gameObject, parent.transform);
            clone.name = cloneName;

            RemoveAllChildrenExcept(clone.transform);

            return clone;
        }
        
        private void RemoveAllChildrenExcept(Transform root)
        {
            var toDelete = new List<GameObject>();

            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);

                // Keep "Text (TMP)"
                if (child.name == "Text (TMP)")
                    continue;

                // Keep an "Image" only if it has no children
                if (child.name == "Image" && child.childCount == 0)
                    continue;

                // Otherwise, delete
                toDelete.Add(child.gameObject);
            }

            // Destroy after iteration
            for (int i = 0; i < toDelete.Count; i++)
                Destroy(toDelete[i]);
        }
    }
}
