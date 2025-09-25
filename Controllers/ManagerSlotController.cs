using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace LootManager
{
    public class ManagerSlotController
    {
        private static GameObject _managerSlotPrefab;
        
        //Panels
        private static GameObject _managerSlotPanel;
        private static GameObject _panelBG;
        private static GameObject _managerPan;
        
        //Slots
        private static GameObject _BlacklistSlot;
        private static GameObject _BankSlot;
        
        //Buttons
        private static Button _lootuiBtn;
        
        public static void Initialize(GameObject managerSlotPrefab)
        {
           _managerSlotPrefab = managerSlotPrefab;
            
            _managerSlotPanel  = managerSlotPrefab;
            _panelBG           = Find("panelBG")?.gameObject;
            _managerPan        = Find("panelBG/managerPan")?.gameObject;
            
            SetupLootUIButton();
        }
        
        private static void SetupLootUIButton()
        {
            _lootuiBtn = Find("panelBG/managerPan/lootuiBtn")?.GetComponent<Button>();

            if (_lootuiBtn != null)
            {
                _lootuiBtn.onClick.RemoveAllListeners();
                _lootuiBtn.onClick.AddListener(() =>
                {
                    if (LootUI.Instance != null)
                        LootUI.Instance.ToggleUI();
                });
            }
            else
            {
                Debug.LogWarning("[ManagerSlotController] lootuiBtn not found in managerSlotPanel.");
            }
        }
        
        private static Transform Find(string path)
        {
            if (_managerSlotPrefab == null) return null;
            if (string.IsNullOrEmpty(path)) return _managerSlotPrefab.transform;
            return _managerSlotPrefab.transform.Find(path);
        }
    }
}