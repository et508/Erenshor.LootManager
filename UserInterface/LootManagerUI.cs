using BepInEx;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public class LootManagerUI : MonoBehaviour
    {
        public static LootManagerUI Instance {get; private set;}
        
        private GameObject uiRoot;
        private Text headerText;
        private RectTransform panel;

        public void Awake()
        {
            Instance = this;
            CreateUI();
        }

        private void CreateUI()
        {
            uiRoot = new GameObject("LootManagerUI");
            DontDestroyOnLoad(uiRoot);

            Canvas canvas = uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = uiRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            uiRoot.AddComponent<GraphicRaycaster>();

            GameObject panelObject = new GameObject("Panel");
            panelObject.transform.SetParent(uiRoot.transform);
            panel = panelObject.AddComponent<RectTransform>();
            panel.sizeDelta = new Vector2(400, 300);
            panel.anchoredPosition = new Vector2(0, 0);
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.75f);

            GameObject textObject = new GameObject("HeaderText");
            textObject.transform.SetParent(panel);
            headerText = textObject.AddComponent<Text>();
            headerText.text = "Loot Manager";
            headerText.alignment = TextAnchor.UpperCenter;
            headerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            headerText.color = Color.white;
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(380, 50);
            textRect.anchoredPosition = new Vector2(0, 125);

            uiRoot.SetActive(false); // Start hidden
        }

        public void ToggleUI()
        {
            uiRoot.SetActive(!uiRoot.activeSelf);
        }
    }
}
