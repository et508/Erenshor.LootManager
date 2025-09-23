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
        private static readonly Color TabActiveColor   = new Color(1f, 1f, 1f, 1f);
        private static readonly Color TabInactiveColor = new Color(1f, 0.9216f, 0.0157f, 1f);

        
        private void Start()
        {
            StartCoroutine(PlayerInvUICoroutine());
        }

        private IEnumerator PlayerInvUICoroutine()
        {
            while (GameObject.Find("PlayerInv") == null)
                yield return null;

            GameObject playerInv = GameObject.Find("PlayerInv");
            
            yield return null;
            Canvas.ForceUpdateCanvases();
            
            MoveButton(playerInv, "Button (1)", -65f, 0f);
            
            MoveButton(playerInv, "Button", -55f, 0f);
            
            var button2 = CloneButton(playerInv, "Button", "Button (2)");
            
            MoveButton(playerInv, "Button (2)", 115f, 0f);
            
            UpdateButtonText(playerInv, "Button (2)", "Manager", new Color(1f, 0.9216f, 0.0157f, 1f));
            
            if (button2 != null)
            {
                var btn = button2.GetComponent<Button>();
                if (btn == null) btn = button2.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnManagerButtonClicked);
            }
            
            var btn0 = playerInv.transform.Find("Button")?.GetComponent<Button>();
            if (btn0 != null)
                btn0.onClick.AddListener(OnOtherTabClicked);
            
            var btn1 = playerInv.transform.Find("Button (1)")?.GetComponent<Button>();
            if (btn1 != null)
                btn1.onClick.AddListener(OnOtherTabClicked);
            
            var managerPanel = ClonePanelStripChildren(
                parent: playerInv,
                sourceName: SourcePanelName,
                cloneName: ManagerPanelName,
                keepChildNames: new[] { "Image", "Text (TMP)" }
            );
            
            if (managerPanel != null)
            {
                var textTr = managerPanel.transform.Find("Text (TMP)");
                if (textTr != null && textTr.TryGetComponent<TextMeshProUGUI>(out var tmp))
                {
                    tmp.text = "Loot Manager";
                    
                    tmp.enableWordWrapping = false;
                    tmp.overflowMode = TextOverflowModes.Overflow;
                    
                    tmp.enableAutoSizing = false;
                    
                    tmp.ForceMeshUpdate();
                    float preferred = tmp.preferredWidth;

                    if (textTr is RectTransform rt)
                    {
                        float padding = 12f;
                        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferred + padding);
                    }
                }
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
            panel.transform.SetSiblingIndex(27);
            
            // Force Button (2) text white
            UpdateButtonText(playerInv, "Button (2)", "Manager", TabActiveColor);

            // Reset other tabs to inactive
            UpdateButtonText(playerInv, "Button", "Ascension", TabInactiveColor);
            UpdateButtonText(playerInv, "Button (1)", "Equipment", TabInactiveColor);
        }
        
        private void OnOtherTabClicked()
        {
            var playerInv = GameObject.Find(PlayerInvName);
            if (playerInv == null) return;

            var panel = playerInv.transform.Find(ManagerPanelName)?.gameObject;
            if (panel != null)
                panel.SetActive(false);
            
            UpdateButtonText(playerInv, "Button (2)", "Manager", TabInactiveColor);
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
            
            CleanRootComponents(clone);

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
        
        private void CleanRootComponents(GameObject go)
        {
            var evt = go.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (evt) Destroy(evt);
            
            System.Type[] keepTypes =
            {
                typeof(UnityEngine.UI.Image),
                typeof(UnityEngine.UI.RawImage),
                typeof(UnityEngine.UI.Mask),
                typeof(UnityEngine.UI.RectMask2D),
                typeof(UnityEngine.UI.LayoutElement),
                typeof(UnityEngine.UI.ContentSizeFitter),
                typeof(UnityEngine.UI.HorizontalLayoutGroup),
                typeof(UnityEngine.UI.VerticalLayoutGroup),
                typeof(UnityEngine.UI.GridLayoutGroup),
                typeof(UnityEngine.CanvasGroup)
            };
            
            foreach (var mb in go.GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;

                var t = mb.GetType();
                bool keep = false;
                for (int i = 0; i < keepTypes.Length; i++)
                {
                    if (t == keepTypes[i]) { keep = true; break; }
                }
                if (!keep)
                {
                    Destroy(mb);
                }
            }
        }
    }
}
