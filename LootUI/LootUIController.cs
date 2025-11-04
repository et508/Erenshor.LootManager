using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public static class LootUIController
    {
        private static GameObject _uiRoot;
        
        private static GameObject _container;
        private static GameObject _panelBGsettings;
        private static GameObject _settingsPanel;
        private static GameObject _panelBGblacklist;
        private static GameObject _blacklistPanel;
        private static GameObject _panelBGwhitelist;
        private static GameObject _whitelistPanel;
        private static GameObject _panelBGbanklist;
        private static GameObject _banklistPanel;
        private static GameObject _menuBar;
        private static GameObject _titleImage;
        private static GameObject _panelBGeditlist;
        
        private static Button _menuSettingsBtn;
        private static Button _menuBlacklistBtn;
        private static Button _menuWhitelistBtn;
        private static Button _menuBanklistBtn;
        private static Button _closeBtn;
        
        private static SettingsPanelController _settings;
        private static BlacklistPanelController _blacklist;
        private static WhitelistPanelController _whitelist;
        private static BanklistPanelController _banklist;
        private static EditlistPanelController _editlist;

        private static readonly List<string> _lootMethodOptions = new List<string> { "Blacklist", "Whitelist", "Standard" };

        public static void Initialize(GameObject uiRoot)
        {
            _uiRoot = uiRoot;

            _container        = UICommon.Find(_uiRoot, "container")?.gameObject;
            _panelBGsettings  = UICommon.Find(_uiRoot, "container/panelBGsettings")?.gameObject;
            _settingsPanel    = UICommon.Find(_uiRoot, "container/panelBGsettings/settingsPanel")?.gameObject;
            _panelBGblacklist = UICommon.Find(_uiRoot, "container/panelBGblacklist")?.gameObject;
            _blacklistPanel   = UICommon.Find(_uiRoot, "container/panelBGblacklist/blacklistPanel")?.gameObject;
            _panelBGwhitelist = UICommon.Find(_uiRoot, "container/panelBGwhitelist")?.gameObject;
            _whitelistPanel   = UICommon.Find(_uiRoot, "container/panelBGwhitelist/whitelistPanel")?.gameObject;
            _panelBGbanklist  = UICommon.Find(_uiRoot, "container/panelBGbanklist")?.gameObject;
            _banklistPanel    = UICommon.Find(_uiRoot, "container/panelBGbanklist/banklistPanel")?.gameObject;
            _menuBar          = UICommon.Find(_uiRoot, "panelBG/menuBar")?.gameObject;
            _titleImage       = UICommon.Find(_uiRoot, "panelBG/titleImage")?.gameObject;
            _panelBGeditlist  = UICommon.Find(_uiRoot, "panelBGeditlist")?.gameObject;

            SetupMenuBarButtons();

            _settings = new SettingsPanelController(
                _uiRoot,
                _container != null ? _container.GetComponent<RectTransform>() : null,
                OnUIVisibilityPossiblyChanged);
            _settings.Init();

            _blacklist = new BlacklistPanelController(
                _uiRoot,
                _container != null ? _container.GetComponent<RectTransform>() : null);
            _blacklist.Init();

            _whitelist = new WhitelistPanelController(
                _uiRoot,
                _container != null ? _container.GetComponent<RectTransform>() : null);
            _whitelist.Init();

            _banklist = new BanklistPanelController(
                _uiRoot,
                _container != null ? _container.GetComponent<RectTransform>() : null);
            _banklist.Init();
            
            _editlist = new EditlistPanelController(
                _uiRoot,
                _panelBGeditlist != null ? _panelBGeditlist.GetComponent<RectTransform>() : null);
            _editlist.Init();

            ShowPanel(_settingsPanel);
            OnUIVisibilityPossiblyChanged();
        }

        private static void SetupMenuBarButtons()
        {
            _menuSettingsBtn  = UICommon.Find(_uiRoot, "container/menuBar/settingBtn")?.GetComponent<Button>();
            _menuBlacklistBtn = UICommon.Find(_uiRoot, "container/menuBar/blacklistBtn")?.GetComponent<Button>();
            _menuWhitelistBtn = UICommon.Find(_uiRoot, "container/menuBar/whitelistBtn")?.GetComponent<Button>();
            _menuBanklistBtn  = UICommon.Find(_uiRoot, "container/menuBar/banklistBtn")?.GetComponent<Button>();
            _closeBtn         = UICommon.Find(_uiRoot, "container/menuBar/closeBtn")?.GetComponent<Button>();

            if (_menuSettingsBtn != null)
                _menuSettingsBtn.onClick.AddListener(() =>
                {
                    ShowPanel(_settingsPanel);
                    _settings.Show();
                });

            if (_menuBlacklistBtn != null)
                _menuBlacklistBtn.onClick.AddListener(() =>
                {
                    ShowPanel(_blacklistPanel);
                    _blacklist.Show();
                });

            if (_menuWhitelistBtn != null)
                _menuWhitelistBtn.onClick.AddListener(() =>
                {
                    ShowPanel(_whitelistPanel);
                    _whitelist.Show();
                });

            if (_menuBanklistBtn != null)
                _menuBanklistBtn.onClick.AddListener(() =>
                {
                    ShowPanel(_banklistPanel);
                    _banklist.Show();
                });

            if (_closeBtn != null)
                _closeBtn.onClick.AddListener(() =>
                {
                    if (LootUI.Instance != null)
                        LootUI.Instance.ToggleUI();
                });
        }

        private static void ShowPanel(GameObject activePanel)
        {
            if (_menuBar != null) _menuBar.SetActive(true);
            if (_titleImage != null) _titleImage.SetActive(true);
            if (_panelBGeditlist != null) _panelBGeditlist.SetActive(false);

            bool isSettings  = activePanel == _settingsPanel;
            bool isBlacklist = activePanel == _blacklistPanel;
            bool isWhitelist = activePanel == _whitelistPanel;
            bool isBanklist  = activePanel == _banklistPanel;

            if (_panelBGsettings != null) _panelBGsettings.SetActive(isSettings);
            if (_settingsPanel != null) _settingsPanel.SetActive(isSettings);

            if (_panelBGblacklist != null) _panelBGblacklist.SetActive(isBlacklist);
            if (_blacklistPanel != null) _blacklistPanel.SetActive(isBlacklist);

            if (_panelBGwhitelist != null) _panelBGwhitelist.SetActive(isWhitelist);
            if (_whitelistPanel != null) _whitelistPanel.SetActive(isWhitelist);

            if (_panelBGbanklist != null) _panelBGbanklist.SetActive(isBanklist);
            if (_banklistPanel != null) _banklistPanel.SetActive(isBanklist);

            Outline settingBtnOutline   = UICommon.Find(_uiRoot, "container/menuBar/settingBtn")?.GetComponent<Outline>();
            Outline blacklistBtnOutline = UICommon.Find(_uiRoot, "container/menuBar/blacklistBtn")?.GetComponent<Outline>();
            Outline whitelistBtnOutline = UICommon.Find(_uiRoot, "container/menuBar/whitelistBtn")?.GetComponent<Outline>();
            Outline banklistBtnOutline  = UICommon.Find(_uiRoot, "container/menuBar/banklistBtn")?.GetComponent<Outline>();

            if (settingBtnOutline   != null) settingBtnOutline.enabled   = isSettings;
            if (blacklistBtnOutline != null) blacklistBtnOutline.enabled = isBlacklist;
            if (whitelistBtnOutline != null) whitelistBtnOutline.enabled = isWhitelist;
            if (banklistBtnOutline  != null) banklistBtnOutline.enabled  = isBanklist;
        }

        private static void OnUIVisibilityPossiblyChanged()
        {
            if (_menuBlacklistBtn != null)
                _menuBlacklistBtn.gameObject.SetActive(Plugin.LootMethod.Value == "Blacklist");

            if (_menuWhitelistBtn != null)
                _menuWhitelistBtn.gameObject.SetActive(Plugin.LootMethod.Value == "Whitelist");

            if (_menuBanklistBtn != null)
                _menuBanklistBtn.gameObject.SetActive(Plugin.BankLootEnabled.Value);
        }
        
        public static void ShowEditCategory(string categoryName)
        {
            if (_editlist == null)
            {
                UpdateSocialLog.LogAdd("[LootUI] Edit panel not ready.", "red");
                return;
            }
            _editlist.Show(categoryName);
        }
    }
}
