using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public class LootUI : MonoBehaviour
    {
        public static LootUI Instance { get; private set; }

        private const string BundleFileName  = "lootui";
        private const string PrefabAssetName = "lootui";
        private const string ManagerSlotPrefabName = "managerSlotPanel";
        private AssetBundle _uiBundle;
        private GameObject  _uiRoot;
        private GameObject  _managerSlotPrefab;
        public GameObject  ManagerSlotPrefab => _managerSlotPrefab;

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
            
            string bundlePath = Path.Combine(assemblyDir, BundleFileName);

            if (!File.Exists(bundlePath))
            {
                Debug.LogError($"[LootUI] AssetBundle not found at: {bundlePath}");
                return;
            }

            _uiBundle = AssetBundle.LoadFromFile(bundlePath);
            if (_uiBundle == null)
            {
                Debug.LogError("[LootUI] Failed to load AssetBundle!");
                return;
            }

            GameObject prefab = _uiBundle.LoadAsset<GameObject>(PrefabAssetName);
            if (prefab == null)
            {
                Debug.LogError($"[LootUI] Prefab '{PrefabAssetName}' not found in bundle.");
                return;
            }
            
            _managerSlotPrefab = _uiBundle.LoadAsset<GameObject>(ManagerSlotPrefabName);
            if (_managerSlotPrefab == null)
            {
                Debug.LogError($"[LootUI] Prefab '{ManagerSlotPrefabName}' not found in bundle.");
            }
            
            _uiRoot = Instantiate(prefab);
            LootUIController.Initialize(_uiRoot);

            DontDestroyOnLoad(_uiRoot);
            
            _uiRoot.SetActive(false); 
            
            Plugin.Log.LogInfo("[Loot Manager] Loot UI initialized successfully.");
        }
        
        private void Update()
        {
            if (Plugin.ToggleLootUIHotkey != null &&
                Plugin.ToggleLootUIHotkey.Value.IsDown() && !GameData.PlayerTyping &&
                !TypingInputMute.IsAnyActive &&
                Instance == this)
            {
                ToggleUI();
            }
        }
        
        public void ToggleUI()
        {
            if (_uiRoot == null)
                return;
            
            bool shouldBeActive = !_uiRoot.activeSelf;
            _uiRoot.SetActive(shouldBeActive);
        }

        private void OnDestroy()
        {
            if (_uiBundle != null)
            {
                _uiBundle.Unload(false);
                _uiBundle = null;
            }

            if (_uiRoot != null)
            {
                Destroy(_uiRoot);
                _uiRoot = null;
            }

            Instance = null;
        }
    }
}
