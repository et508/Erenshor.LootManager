using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    // Loads the LootManagerUI prefab from an AssetBundle located in the same folder as the plugin DLL.
    public class LootManagerUI : MonoBehaviour
    {
        public static LootManagerUI Instance { get; private set; }

        private const string BundleFileName  = "lootmanagerassets"; // change to new asset file name 
        private const string PrefabAssetName = "uiRoot"; // change to new prefab name

        private AssetBundle _uiBundle;
        private GameObject  _uiRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadUIFromBundle();
        }

        private void LoadUIFromBundle()
        {
            // Determine the path of this assembly (plugin DLL)
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string assemblyDir  = Path.GetDirectoryName(assemblyPath);

            // Expect the bundle in an 'Assets' subfolder next to the DLL
            string bundlePath = Path.Combine(assemblyDir, "Assets", BundleFileName);

            if (!File.Exists(bundlePath))
            {
                Debug.LogError($"[LootManagerUI] AssetBundle not found at: {bundlePath}");
                return;
            }

            _uiBundle = AssetBundle.LoadFromFile(bundlePath);
            if (_uiBundle == null)
            {
                Debug.LogError("[LootManagerUI] Failed to load AssetBundle!");
                return;
            }

            GameObject prefab = _uiBundle.LoadAsset<GameObject>(PrefabAssetName);
            if (prefab == null)
            {
                Debug.LogError($"[LootManagerUI] Prefab '{PrefabAssetName}' not found in bundle.");
                return;
            }

            _uiRoot = Instantiate(prefab);
            DontDestroyOnLoad(_uiRoot);
            
            // 🔽 Hook up the close button by name
            var closeButtonGO = _uiRoot.transform.Find("borderGO/panelGO/footerGO/closeBtn");

            if (closeButtonGO != null && closeButtonGO.TryGetComponent<Button>(out var closeButton))
            {
                closeButton.onClick.AddListener(() => _uiRoot.SetActive(false));
                Debug.Log("[LootManagerUI] Close button hooked up successfully.");
            }
            else
            {
                Debug.LogWarning("[LootManagerUI] Close button not found or missing Button component.");
            }
            
            _uiRoot.SetActive(false); // Start hidden
            
            Debug.Log("[LootManagerUI] UI prefab instantiated successfully.");
        }
        
        private void Update()
        {
            // Toggle UI on F6 keypress
            if (Input.GetKeyDown(KeyCode.F6) && Instance == this)
            {
                ToggleUI();
            }
        }
        
        /// Toggles the visibility of the UI root.
        public void ToggleUI()
        {
            if (_uiRoot != null)
                _uiRoot.SetActive(!_uiRoot.activeSelf);
        }

        private void OnDestroy()
        {
            // Unload only the bundle, keep instantiated assets
            _uiBundle?.Unload(false);
        }
    }
}
