using System.Collections;
using UnityEngine;

namespace LootManager
{
    public class LootManagerController : MonoBehaviour
    {
        private static LootManagerController _instance;
        
        public static void Initialize()
        {
            if (_instance == null)
            {
                var go = new GameObject("LootManager_Controller");
                _instance = go.AddComponent<LootManagerController>();
                Object.DontDestroyOnLoad(go);
                go.AddComponent<LootUI>();
                _instance.StartCoroutine(_instance.WaitForInventoryAndInit());
            }
        }

        private IEnumerator WaitForInventoryAndInit()
        {
            while (GameObject.Find("PlayerInv") == null)
                yield return null;

            // PLayerInvUI init to reposition existing UI elements
            var mover = new GameObject("PlayerInvUIAdjustment");
            mover.AddComponent<PlayerInvUI>();
        }
    }
}