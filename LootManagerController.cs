using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LootManager
{
    public class LootManagerController : MonoBehaviour
    {
        private static bool _initialized;
        private static LootManagerController _instance;
        private const string ControllerName   = "LootManager_Controller";
        private const string BootstrapName    = "LootManager_Loader";
        private const string TargetAnchorName = "GameManager";
        private const string LegacyInvName    = "PlayerInv";

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            EnsureBootstrapExists();

            SceneManager.sceneLoaded += OnSceneLoaded_Static;
            SceneManager.activeSceneChanged += OnActiveSceneChanged_Static;
        }

        // ---------- Static hooks / helpers ----------

        private static void OnSceneLoaded_Static(Scene scene, LoadSceneMode mode)
        {
            EnsureBootstrapExists();
            Bootstrap.Instance?.KickWaitForGameManager();
        }

        private static void OnActiveSceneChanged_Static(Scene oldScene, Scene newScene)
        {
            EnsureBootstrapExists();
            Bootstrap.Instance?.KickWaitForGameManager();
        }

        private static void EnsureBootstrapExists()
        {
            if (Bootstrap.Instance != null) return;

            var go = GameObject.Find(BootstrapName);
            if (go == null)
            {
                go = new GameObject(BootstrapName);
                Object.DontDestroyOnLoad(go);
            }

            var runner = go.GetComponent<Bootstrap>();
            if (runner == null)
            {
                runner = go.AddComponent<Bootstrap>();
            }

            runner.KickWaitForGameManager();
        }

        private static void SpawnUnderGameManager(GameObject gameManager)
        {
            if (_instance != null && _instance.gameObject != null) return;

            var existing = GameObject.Find(ControllerName);
            GameObject host;
            if (existing != null)
            {
                host = existing;
            }
            else
            {
                host = new GameObject(ControllerName);
            }

            host.hideFlags = HideFlags.None;
            host.transform.SetParent(gameManager.transform, false);

            _instance = host.GetComponent<LootManagerController>();
            if (_instance == null) _instance = host.AddComponent<LootManagerController>();

            if (host.GetComponent<LootUI>() == null)
                host.AddComponent<LootUI>();

        }

        // ---------- MonoBehaviour lifecycle ----------

        private void OnEnable()
        {
            StartCoroutine(WaitForInventoryAndInit());
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private IEnumerator WaitForInventoryAndInit()
        {
            while (GameObject.Find(LegacyInvName) == null)
                yield return null;

            var mover = GameObject.Find("PlayerInvUIAdjustment");
            if (mover == null)
            {
                mover = new GameObject("PlayerInvUIAdjustment");
                mover.transform.SetParent(transform, false);
                mover.AddComponent<PlayerInvUI>();
            }
        }

        // ---------- Loot Manager Loader ----------

        private class Bootstrap : MonoBehaviour
        {
            internal static Bootstrap Instance;
            private Coroutine _waitRoutine;
            private bool _waiting;

            private void Awake()
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject);
                    return;
                }

                Instance = this;
                name = BootstrapName;
            }

            private void OnDestroy()
            {
                if (Instance == this) Instance = null;
            }

            internal void KickWaitForGameManager()
            {
                if (_waiting) return;
                if (_waitRoutine != null)
                    StopCoroutine(_waitRoutine);
                _waitRoutine = StartCoroutine(WaitForGameManagerThenSpawn());
            }

            private IEnumerator WaitForGameManagerThenSpawn()
            {
                _waiting = true;
                GameObject gm = null;

                while (gm == null)
                {
                    gm = GameObject.Find(TargetAnchorName);
                    if (gm == null)
                    {
                        yield return null;
                    }
                }
                
                _waiting = false;
                SpawnUnderGameManager(gm);
            }
        }
    }
}
