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
        private static Button _bankBtn;
        private static Button _auctionBtn;
        
        public static void Initialize(GameObject managerSlotPrefab)
        {
           _managerSlotPrefab = managerSlotPrefab;
            
            _managerSlotPanel  = managerSlotPrefab;
            _panelBG           = Find("panelBG")?.gameObject;
            _managerPan        = Find("panelBG/managerPan")?.gameObject;
            
            SetupLootUIButton();
            SetupBankButton();
            SetupAuctionButton();
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

        private static void SetupBankButton()
        {
            _bankBtn = Find("panelBG/managerPan/bankBtn")?.GetComponent<Button>();

            if (_bankBtn != null)
            {
                _bankBtn.onClick.RemoveAllListeners();
                _bankBtn.onClick.AddListener(() =>
                {
                    if (GameData.ItemOnCursor == null || GameData.ItemOnCursor == GameData.PlayerInv.Empty)
                    {
                        GameData.BankUI.OpenBank(GameData.PlayerControl.transform.position);
                    }
                    else
                    {
                        UpdateSocialLog.LogAdd("Remove item from cursor before interacting with a vendor.", "yellow");
                    }
                });
            }
        }

        private static void SetupAuctionButton()
        {
            _auctionBtn = Find("panelBG/managerPan/auctionBtn")?.GetComponent<Button>();

            if (_auctionBtn != null)
            {
                _auctionBtn.onClick.RemoveAllListeners();
                _auctionBtn.onClick.AddListener(() =>
                {
                    if (GameData.ItemOnCursor == null || GameData.ItemOnCursor == GameData.PlayerInv.Empty)
                    {
                        GameData.AHUI.OpenAuctionHouse(GameData.PlayerControl.transform.position);
                    }
                    else
                    {
                        UpdateSocialLog.LogAdd("Remove item from cursor before interacting with a vendor.", "yellow");
                    }
                });
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