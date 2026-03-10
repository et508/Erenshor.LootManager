// LootUI.cs
// uGUI implementation — no AssetBundle required.
// Builds the entire UI hierarchy in code and delegates to LootUIController.

using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public class LootUI : MonoBehaviour
    {
        public static LootUI Instance { get; private set; }

        private GameObject _uiRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildUI();
        }

        private void BuildUI()
        {
            Plugin.Log.LogInfo("[Loot Manager] Creating Loot UI (uGUI)...");

            // Ensure an EventSystem exists.
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("LootManager_EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Object.DontDestroyOnLoad(es);
            }

            // Canvas — Screen Space Overlay, high sort order so it renders on top.
            var canvasGO = new GameObject("LootManager_Canvas");
            canvasGO.transform.SetParent(transform, false);
            DontDestroyOnLoad(canvasGO);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var cs = canvasGO.AddComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.matchWidthOrHeight  = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Root window object that houses the entire UI.
            _uiRoot = new GameObject("LootManager_Root");
            _uiRoot.transform.SetParent(canvasGO.transform, false);

            var rootRT = _uiRoot.AddComponent<RectTransform>();
            rootRT.anchorMin        = new Vector2(0.5f, 0.5f);
            rootRT.anchorMax        = new Vector2(0.5f, 0.5f);
            rootRT.pivot            = new Vector2(0.5f, 0.5f);
            rootRT.anchoredPosition = Vector2.zero;
            rootRT.sizeDelta        = Vector2.zero;

            LootUIController.Initialize(_uiRoot);

            DontDestroyOnLoad(_uiRoot);

            _uiRoot.SetActive(false);

            Plugin.Log.LogInfo("[Loot Manager] Loot UI initialized successfully (uGUI).");
        }

        private void Update()
        {
            if (Plugin.ToggleLootUIHotkey != null &&
                Plugin.ToggleLootUIHotkey.Value.IsDown() &&
                !GameData.PlayerTyping &&
                !TypingInputMute.IsAnyActive &&
                Instance == this)
            {
                ToggleUI();
            }
        }

        public void ToggleUI()
        {
            if (_uiRoot == null) return;
            _uiRoot.SetActive(!_uiRoot.activeSelf);
        }

        private void OnDestroy()
        {
            if (_uiRoot != null)
            {
                Destroy(_uiRoot);
                _uiRoot = null;
            }
            Instance = null;
        }
    }
}