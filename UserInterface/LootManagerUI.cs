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

        private const string BundleFileName  = "lootui"; // change to new asset file name 
        private const string PrefabAssetName = "lootui"; // change to new prefab name

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
            LootUIController.Initialize(_uiRoot);

            DontDestroyOnLoad(_uiRoot);
            
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
