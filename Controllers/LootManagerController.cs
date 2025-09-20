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
            while (GameObject.Find("TrashSlot") == null)
                yield return null;

            // BlacklistSlot init
            var go = new GameObject("BlacklistSlotObject");
            go.AddComponent<BlacklistSlot>().InitSlotAsync();

            // TrashSlotPatch init to reposition the existing TrashSlot
            var mover = new GameObject("TrashSlotMover");
            mover.AddComponent<TrashSlotPatch>();
        }
    }
}