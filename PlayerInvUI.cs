using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private IEnumerator PlayerInvUICoroutine()
        {
            while (GameObject.Find(PlayerInvName) == null)
                yield return null;

            GameObject playerInv = GameObject.Find(PlayerInvName);

            yield return null;
            Canvas.ForceUpdateCanvases();

            // Move existing tabs (anchor-safe)
            MoveButtonRelative(playerInv, "Button (1)", "Button (1)", new Vector2(-65f, 0f));
            MoveButtonRelative(playerInv, "Button",     "Button",     new Vector2(-55f, 0f));

            // Build a brand-new Button (2) and wire both Button.onClick + Proxy
            EnsureFreshManagerButton(playerInv);

            // Hook other tabs to hide ManagerPanel
            var btn0 = playerInv.transform.Find("Button")?.GetComponent<Button>();
            if (btn0 != null) AttachOtherTabProxy(playerInv.transform, "Button");

            var btn1 = playerInv.transform.Find("Button (1)")?.GetComponent<Button>();
            if (btn1 != null) AttachOtherTabProxy(playerInv.transform, "Button (1)");

            // Clone AAScreen -> ManagerPanel (strip children & clean root scripts)
            var managerPanel = ClonePanelStripChildren(
                parent: playerInv,
                sourceName: SourcePanelName,
                cloneName: ManagerPanelName,
                keepChildNames: new string[] { "Image", "Text (TMP)" }
            );

            if (managerPanel != null)
            {
                // Title text: "Loot Manager" (no wrap)
                var textTr = managerPanel.transform.Find("Text (TMP)");
                if (textTr != null && textTr.TryGetComponent<TextMeshProUGUI>(out var tmp))
                {
                    tmp.text = "Loot Manager";
                    tmp.enableWordWrapping = false;
                    tmp.overflowMode = TextOverflowModes.Overflow;
                    tmp.enableAutoSizing = false;

                    tmp.ForceMeshUpdate();
                    float preferred = tmp.preferredWidth;
                    if (textTr is RectTransform rtTitle)
                    {
                        float padding = 12f;
                        rtTitle.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferred + padding);
                    }
                }

                // Attach managerSlotPanel prefab once
                var managerPanelTr = playerInv.transform.Find(ManagerPanelName);
                var slotPrefab = LootUI.Instance?.ManagerSlotPrefab;
                if (managerPanelTr != null && slotPrefab != null && managerPanelTr.Find("managerSlotPanel") == null)
                {
                    var slot = Instantiate(slotPrefab, managerPanelTr, false);
                    slot.name = "managerSlotPanel";

                    ManagerSlotController.Initialize(slot);

                    if (slot.TryGetComponent<RectTransform>(out var rt))
                    {
                        rt.anchorMin = new Vector2(0f, 1f);
                        rt.anchorMax = new Vector2(0f, 1f);
                        rt.pivot     = new Vector2(0f, 1f);
                        rt.anchoredPosition = new Vector2(-160f, 300f); // tweak as needed
                        slot.transform.localScale = Vector3.one;
                    }
                }

                // Start hidden until clicked
                managerPanel.SetActive(false);
            }
        }

        // Build/rebuild "Button (2)" with robust click handling
        private GameObject EnsureFreshManagerButton(GameObject playerInv)
        {
            // If a stale Button (2) exists (from previous scene), destroy it and rebuild
            var existing = playerInv.transform.Find("Button (2)")?.gameObject;
            if (existing != null)
            {
                Destroy(existing);
            }

            // Use "Button" as a visual template only
            var sourceBtnTr = playerInv.transform.Find("Button");
            TextMeshProUGUI sourceTMP = null;
            Image sourceImg = null;
            Button sourceBtn = null;
            RectTransform srcRT = null;

            if (sourceBtnTr != null)
            {
                sourceBtn = sourceBtnTr.GetComponent<Button>();
                sourceImg = sourceBtnTr.GetComponent<Image>();
                srcRT     = sourceBtnTr.GetComponent<RectTransform>();
                var sourceTextTr = sourceBtnTr.Find("Text (TMP)");
                if (sourceTextTr != null) sourceTextTr.TryGetComponent(out sourceTMP);
            }

            // Create GO + components
            var go = new GameObject("Button (2)", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(playerInv.transform, false);

            // Copy RectTransform layout from source (do NOT overwrite later)
            var dstRT = go.GetComponent<RectTransform>();
            if (srcRT != null)
            {
                dstRT.anchorMin = srcRT.anchorMin;
                dstRT.anchorMax = srcRT.anchorMax;
                dstRT.pivot     = srcRT.pivot;
                dstRT.sizeDelta = srcRT.sizeDelta;
                dstRT.anchoredPosition = srcRT.anchoredPosition; // base position same as template
            }

            // Make layout independent so moves stick
            var le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            // Image styling and raycast
            var img = go.GetComponent<Image>();
            if (sourceImg != null)
            {
                img.sprite   = sourceImg.sprite;
                img.type     = sourceImg.type;
                img.color    = sourceImg.color;
                img.material = sourceImg.material;
                img.pixelsPerUnitMultiplier = sourceImg.pixelsPerUnitMultiplier;
            }
            img.raycastTarget = true; // ensure clickable

            // Configure Button with ONLY our listener
            var btn2 = go.GetComponent<Button>();
            if (sourceBtn != null)
            {
                btn2.transition        = sourceBtn.transition;
                btn2.colors            = sourceBtn.colors;
                btn2.spriteState       = sourceBtn.spriteState;
                btn2.animationTriggers = sourceBtn.animationTriggers;
            }
            var nav2 = btn2.navigation; nav2.mode = Navigation.Mode.None; btn2.navigation = nav2;
            btn2.onClick = new Button.ButtonClickedEvent();
            btn2.onClick.AddListener(OnManagerButtonClicked);

            // Add child TMP text (raycast OFF so text never eats clicks)
            var textGO = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0, 0);
            textRT.anchorMax = new Vector2(1, 1);
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "Manager";
            tmp.color = TabInactiveColor;
            tmp.alignment = sourceTMP != null ? sourceTMP.alignment : TextAlignmentOptions.Center;
            tmp.fontSize  = sourceTMP != null ? sourceTMP.fontSize  : 20f;
            tmp.enableAutoSizing   = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode       = TextOverflowModes.Overflow;
            if (sourceTMP != null)
            {
                tmp.font            = sourceTMP.font;
                tmp.fontStyle       = sourceTMP.fontStyle;
                tmp.lineSpacing     = sourceTMP.lineSpacing;
                tmp.characterSpacing= sourceTMP.characterSpacing;
                tmp.extraPadding    = sourceTMP.extraPadding;
                tmp.richText        = sourceTMP.richText;
            }
            tmp.raycastTarget = false;

            // Add proxy click handler to belt-and-suspenders the click
            var proxy = go.AddComponent<ManagerButtonProxy>();
            proxy.Bind(() => OnManagerButtonClicked());

            // Make sure parent chain allows interaction
            EnsureInteractiveChain(go.transform);

            // Final position tweak RELATIVE TO TEMPLATE (no anchor change)
            MoveButtonRelative(playerInv, "Button (2)", "Button", new Vector2(115f, 0f));

            // Sibling ordering next to source
            if (sourceBtnTr != null)
                go.transform.SetSiblingIndex(sourceBtnTr.GetSiblingIndex() + 1);
            else
                go.transform.SetSiblingIndex(playerInv.transform.childCount - 1);

            return go;
        }

        private void EnsureInteractiveChain(Transform leaf)
        {
            // Walk up to root, enabling interaction on any CanvasGroup we find
            var t = leaf;
            while (t != null)
            {
                var cg = t.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                t = t.parent;
            }
        }

        // NEW: anchor-safe mover — never force anchorMin/Max to 0.5
        private void MoveButtonRelative(GameObject parent, string targetName, string templateName, Vector2 delta)
        {
            var targetRT   = parent.transform.Find(targetName)?.GetComponent<RectTransform>();
            var templateRT = parent.transform.Find(templateName)?.GetComponent<RectTransform>();
            if (targetRT == null || templateRT == null) return;

            // Mirror template anchors/pivot/size so layout math stays consistent
            targetRT.anchorMin = templateRT.anchorMin;
            targetRT.anchorMax = templateRT.anchorMax;
            targetRT.pivot     = templateRT.pivot;
            targetRT.sizeDelta = templateRT.sizeDelta;

            // Ensure layout system won’t override us
            var le = targetRT.GetComponent<LayoutElement>();
            if (le != null) le.ignoreLayout = true;

            // Position relative to template
            targetRT.anchoredPosition = templateRT.anchoredPosition + delta;
        }

        private void UpdateButtonText(GameObject parent, string buttonName, string newText, Color newColor)
        {
            GameObject button = parent.transform.Find(buttonName)?.gameObject;
            if (button == null) return;

            var textTransform = button.transform.Find("Text (TMP)");
            if (textTransform != null && textTransform.TryGetComponent(out TextMeshProUGUI tmp))
            {
                if (!string.IsNullOrEmpty(newText))
                    tmp.text = newText;
                tmp.color = newColor;
            }
        }

        private void OnManagerButtonClicked()
        {
            var playerInv = GameObject.Find(PlayerInvName);
            if (playerInv == null) return;

            var panelTr = playerInv.transform.Find(ManagerPanelName);
            if (panelTr == null) return;

            var panel = panelTr.gameObject;

            // Ensure visible and interactive
            panel.SetActive(true);
            panel.transform.SetSiblingIndex(27);

            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            cg.interactable = true;
            cg.blocksRaycasts = true;

            UpdateButtonText(playerInv, "Button (2)", "Manager", TabActiveColor);
            UpdateButtonText(playerInv, "Button",     "Ascension", TabInactiveColor);
            UpdateButtonText(playerInv, "Button (1)", "Equipment", TabInactiveColor);
        }

        private void OnOtherTabClicked()
        {
            var playerInv = GameObject.Find(PlayerInvName);
            if (playerInv == null) return;

            var panel = playerInv.transform.Find(ManagerPanelName)?.gameObject;
            if (panel != null) panel.SetActive(false);

            UpdateButtonText(playerInv, "Button (2)", "Manager", TabInactiveColor);
        }

        private GameObject ClonePanelStripChildren(GameObject parent, string sourceName, string cloneName, string[] keepChildNames)
        {
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

                if (child.name == "Text (TMP)") continue;
                if (child.name == "Image" && child.childCount == 0) continue;

                toDelete.Add(child.gameObject);
            }

            for (int i = 0; i < toDelete.Count; i++)
                Destroy(toDelete[i]);
        }

        private void CleanRootComponents(GameObject go)
        {
            var evt = go.GetComponent<EventTrigger>();
            if (evt) Destroy(evt);

            System.Type[] keepTypes =
            {
                typeof(Image),
                typeof(RawImage),
                typeof(Mask),
                typeof(RectMask2D),
                typeof(LayoutElement),
                typeof(ContentSizeFitter),
                typeof(HorizontalLayoutGroup),
                typeof(VerticalLayoutGroup),
                typeof(GridLayoutGroup),
                typeof(CanvasGroup)
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

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(RewireAfterSceneLoad());
        }

        private IEnumerator RewireAfterSceneLoad()
        {
            while (GameObject.Find(PlayerInvName) == null)
                yield return null;

            var playerInv = GameObject.Find(PlayerInvName);

            // Let the game finish wiring its UI first
            yield return null;
            yield return new WaitForEndOfFrame();

            // Recreate Button (2) fresh and bind it (Button + Proxy)
            EnsureFreshManagerButton(playerInv);
            AttachOtherTabProxy(playerInv.transform, "Button");
            AttachOtherTabProxy(playerInv.transform, "Button (1)");
            UpdateButtonText(playerInv, "Button (2)", "Manager", TabInactiveColor);

            // Re-hook other tabs to hide ManagerPanel
            var btn0 = playerInv.transform.Find("Button")?.GetComponent<Button>();
            var btn1 = playerInv.transform.Find("Button (1)")?.GetComponent<Button>();
            if (btn0 != null) btn0.onClick.AddListener(OnOtherTabClicked);
            if (btn1 != null) btn1.onClick.AddListener(OnOtherTabClicked);

            // Ensure ManagerPanel exists and is hidden on new scene
            var managerPanelTr = playerInv.transform.Find(ManagerPanelName);
            if (managerPanelTr == null)
            {
                var recreated = ClonePanelStripChildren(playerInv, SourcePanelName, ManagerPanelName, new string[] { "Image", "Text (TMP)" });
                managerPanelTr = playerInv.transform.Find(ManagerPanelName);
                if (recreated != null) recreated.SetActive(false);
            }
            else
            {
                managerPanelTr.gameObject.SetActive(false);
            }

            // Ensure managerSlotPanel exists and is initialized
            if (managerPanelTr != null)
            {
                var slot = managerPanelTr.Find("managerSlotPanel")?.gameObject;
                if (slot == null && LootUI.Instance?.ManagerSlotPrefab != null)
                {
                    slot = Instantiate(LootUI.Instance.ManagerSlotPrefab, managerPanelTr, false);
                    slot.name = "managerSlotPanel";
                    if (slot.TryGetComponent<RectTransform>(out var rt))
                    {
                        rt.anchorMin = new Vector2(0f, 1f);
                        rt.anchorMax = new Vector2(0f, 1f);
                        rt.pivot     = new Vector2(0f, 1f);
                        rt.anchoredPosition = new Vector2(-160f, 300f);
                        slot.transform.localScale = Vector3.one;
                    }
                }
                if (slot != null)
                    ManagerSlotController.Initialize(slot);
            }

            // After layout settles, re-assert position relative to template
            MoveButtonRelative(playerInv, "Button (2)", "Button", new Vector2(115f, 0f));
            Canvas.ForceUpdateCanvases();
        }
        
        private void AttachOtherTabProxy(Transform parent, string childName)
        {
            var tr = parent.Find(childName);
            if (tr == null) return;

            // Ensure there is a Button (we don't rely on its onClick, but it's fine either way)
            var btn = tr.GetComponent<Button>();
            if (btn != null)
            {
                // (Optional) also bind onClick as a secondary path
                btn.onClick.AddListener(OnOtherTabClicked);
            }

            // Add or reuse our proxy so clicks always call our handler
            var proxy = tr.GetComponent<OtherTabProxy>();
            if (proxy == null) proxy = tr.gameObject.AddComponent<OtherTabProxy>();
            proxy.OnClicked = OnOtherTabClicked;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    public class ManagerButtonProxy : MonoBehaviour, IPointerClickHandler
    {
        private System.Action _onClicked;

        public void Bind(System.Action onClicked)
        {
            _onClicked = onClicked;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onClicked?.Invoke();
            eventData.Use();
        }
    }
    
    public sealed class OtherTabProxy : MonoBehaviour, IPointerClickHandler
    {
        public System.Action OnClicked;

        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            OnClicked?.Invoke();
            // let the game's tab system also handle it
        }
    }
}
