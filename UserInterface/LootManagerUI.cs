using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public class LootManagerUI : MonoBehaviour
    {
        public static LootManagerUI Instance { get; private set; }

        private const string BundleFileName  = "lootui";
        private const string PrefabAssetName = "lootui";
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
            Plugin.Log.LogInfo("[Loot Manager] Creating Loot UI...");
            
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string assemblyDir  = Path.GetDirectoryName(assemblyPath);
            
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
                Debug.LogError($"[Loot Manager] Prefab '{PrefabAssetName}' not found in bundle.");
                return;
            }

            _uiRoot = Instantiate(prefab);
            LootUIController.Initialize(_uiRoot);

            DontDestroyOnLoad(_uiRoot);
            
            _uiRoot.SetActive(false); // Start hidden
            
            Plugin.Log.LogInfo("[Loot Manager] Loot UI initialized successfully.");
        }
        
        private void Update()
        {
            // Toggle UI on F6 keypress
            if (Input.GetKeyDown(KeyCode.F6) && Instance == this)
            {
                ToggleUI();
            }
        }
        
        public void ToggleUI()
        {
            if (_uiRoot == null)
                return;

            // figure out the new state and apply it
            bool shouldBeActive = !_uiRoot.activeSelf;
            _uiRoot.SetActive(shouldBeActive);

            // when UI is open we treat the player as “typing” (so movement is blocked);
            // when UI is closed we clear that flag
            GameData.PlayerTyping = shouldBeActive;
            
        }

        private void OnDestroy()
        {
            // Unload only the bundle, keep instantiated assets
            _uiBundle?.Unload(false);
        }
    }
}
