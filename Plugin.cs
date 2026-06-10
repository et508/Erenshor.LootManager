using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using ImGuiNET;
using Lunaris;
using Lunaris.Config;
using UnityEngine;

[assembly: System.Reflection.AssemblyMetadata("LunarisPluginId", "loot-manager")]

namespace LootManager
{
    [LunarisPlugin("Loot Manager", "4.0.0", "et508", "Automated loot management for Erenshor")]
    [LunarisPermission(LunarisPermission.Harmony)]
    public class Plugin : LunarisPlugin
    {
        internal static Plugin Instance;
        internal static Lunaris.ILog Log => Instance?.Logging;

        // ── List state ────────────────────────────────────────────────────────
        internal static HashSet<string> Blacklist   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal static HashSet<string> Whitelist   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal static HashSet<string> Banklist    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal static HashSet<string> Junklist    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal static HashSet<string> Auctionlist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal static HashSet<string> Editlist    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static Dictionary<string, HashSet<string>> FilterList =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> EnabledFilterCategories =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> FilterAppliedToBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> FilterAppliedToWhitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> FilterAppliedToBanklist  = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // ── Config ────────────────────────────────────────────────────────────
        private static PluginConfig _cfg;
        // Keep the raw IConfig so we can call Write<T> to persist changes
        private static IConfig _configStore;
        // Prefix used by ConfigHandle: typeof(PluginConfig).Name
        private const string CfgPrefix = "PluginConfig";

        private static void Save<T>(string fieldName, T value)
        {
            _configStore?.Write(CfgPrefix + "." + fieldName, value);
        }

        public static bool   GetAutoLootEnabled()      => _cfg?.AutoLootEnabled      ?? true;
        public static float  GetAutoLootDistance()     => _cfg?.AutoLootDistance     ?? 20f;
        public static bool   GetAutoLootDelayEnabled() => _cfg?.AutoLootDelayEnabled ?? false;
        public static float  GetAutoLootDelay()        => _cfg?.AutoLootDelay        ?? 3f;
        public static string GetLootMethod()           => _cfg?.LootMethod           ?? "Blacklist";
        public static bool   GetAuctionLootEnabled()   => _cfg?.AuctionLootEnabled   ?? true;
        public static bool   GetBankLootEnabled()      => _cfg?.BankLootEnabled      ?? false;
        public static string GetBankLootMethod()       => _cfg?.BankLootMethod       ?? "All";
        public static string GetBankLootPageMode()     => _cfg?.BankLootPageMode     ?? "First Empty";
        public static int    GetBankPageFirst()        => _cfg?.BankPageFirst        ?? 20;
        public static int    GetBankPageLast()         => _cfg?.BankPageLast         ?? 20;
        public static bool   GetBankslotAddToList()    => _cfg?.BankslotAddToList    ?? false;
        public static bool   GetLootRare()             => _cfg?.LootRare             ?? false;
        public static bool   GetLootEquipment()        => _cfg?.LootEquipment        ?? false;
        public static string GetLootEquipmentTier()    => _cfg?.LootEquipmentTier    ?? EquipmentTierSetting.All.ToString();
        public static bool   GetFishingFilterEnabled() => _cfg?.FishingFilterEnabled ?? false;
        public static bool   GetMiningFilterEnabled()  => _cfg?.MiningFilterEnabled  ?? false;
        public static string GetChatOutputWindow()     => _cfg?.ChatOutputWindow     ?? "MAINCHAT";
        public static int    GetChatOutputTab()        => _cfg?.ChatOutputTab        ?? 0;
        public static bool   GetChatOutputEnabled()    => _cfg?.ChatOutputEnabled    ?? true;
        public static KeyCode GetToggleLootUIHotkey()  => _cfg?.ToggleLootUIHotkey   ?? KeyCode.F6;
        public static KeyCode GetToggleAutoLootHotkey()=> _cfg?.ToggleAutoLootHotkey ?? KeyCode.F10;

        public static void SetAutoLootEnabled(bool v)      { if (_cfg != null) { _cfg.AutoLootEnabled = v;      Save("AutoLootEnabled", v); } }
        public static void SetAutoLootDistance(float v)    { if (_cfg != null) { _cfg.AutoLootDistance = v;     Save("AutoLootDistance", v); } }
        public static void SetAutoLootDelayEnabled(bool v) { if (_cfg != null) { _cfg.AutoLootDelayEnabled = v; Save("AutoLootDelayEnabled", v); } }
        public static void SetAutoLootDelay(float v)       { if (_cfg != null) { _cfg.AutoLootDelay = v;        Save("AutoLootDelay", v); } }
        public static void SetLootMethod(string v)         { if (_cfg != null) { _cfg.LootMethod = v;           Save("LootMethod", v); } }
        public static void SetAuctionLootEnabled(bool v)   { if (_cfg != null) { _cfg.AuctionLootEnabled = v;   Save("AuctionLootEnabled", v); } }
        public static void SetBankLootEnabled(bool v)      { if (_cfg != null) { _cfg.BankLootEnabled = v;      Save("BankLootEnabled", v); } }
        public static void SetBankLootMethod(string v)     { if (_cfg != null) { _cfg.BankLootMethod = v;       Save("BankLootMethod", v); } }
        public static void SetBankLootPageMode(string v)   { if (_cfg != null) { _cfg.BankLootPageMode = v;     Save("BankLootPageMode", v); } }
        public static void SetBankPageFirst(int v)         { if (_cfg != null) { _cfg.BankPageFirst = v;        Save("BankPageFirst", v); } }
        public static void SetBankPageLast(int v)          { if (_cfg != null) { _cfg.BankPageLast = v;         Save("BankPageLast", v); } }
        public static void SetBankslotAddToList(bool v)    { if (_cfg != null) { _cfg.BankslotAddToList = v;    Save("BankslotAddToList", v); } }
        public static void SetLootRare(bool v)             { if (_cfg != null) { _cfg.LootRare = v;             Save("LootRare", v); } }
        public static void SetLootEquipment(bool v)        { if (_cfg != null) { _cfg.LootEquipment = v;        Save("LootEquipment", v); } }
        public static void SetLootEquipmentTier(string v)  { if (_cfg != null) { _cfg.LootEquipmentTier = v;    Save("LootEquipmentTier", v); } }
        public static void SetFishingFilterEnabled(bool v) { if (_cfg != null) { _cfg.FishingFilterEnabled = v; Save("FishingFilterEnabled", v); } }
        public static void SetMiningFilterEnabled(bool v)  { if (_cfg != null) { _cfg.MiningFilterEnabled = v;  Save("MiningFilterEnabled", v); } }
        public static void SetChatOutputWindow(string v)   { if (_cfg != null) { _cfg.ChatOutputWindow = v;     Save("ChatOutputWindow", v); } }
        public static void SetChatOutputTab(int v)         { if (_cfg != null) { _cfg.ChatOutputTab = v;        Save("ChatOutputTab", v); } }
        public static void SetChatOutputEnabled(bool v)    { if (_cfg != null) { _cfg.ChatOutputEnabled = v;    Save("ChatOutputEnabled", v); } }
        public static void SetToggleLootUIHotkey(KeyCode v){ if (_cfg != null) { _cfg.ToggleLootUIHotkey = v;   Save("ToggleLootUIHotkey", v); } }
        public static void SetToggleAutoLootHotkey(KeyCode v){ if (_cfg != null) { _cfg.ToggleAutoLootHotkey = v; Save("ToggleAutoLootHotkey", v); } }

        // ── ImGui ─────────────────────────────────────────────────────────────
        internal ImGuiRenderer _imgui;
        private LootManagerWindow _window;
        private static Harmony _harmony;

        private void Awake()
        {
            Instance = this;

            // Register Lunaris config and capture the IConfig store for persistence
            _configStore = Config;
            _cfg = Config.Register<PluginConfig>().Get();

            LootManagerPaths.Initialize(Assembly.GetExecutingAssembly().Location);
            LootBlacklist.Load();
            LootWhitelist.Load();
            LootBanklist.Load();
            LootJunklist.Load();
            LootAuctionlist.Load();
            LootFilterlist.Load();

            Logging.Log("[LootManager] Lists loaded.");

            _harmony = new Harmony("et508.erenshor.lootmanager");

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "ErenshorQoL")
                {
                    var harmonyID     = "Brumdail.ErenshorQoLMod";
                    var lootAllMethod = AccessTools.Method(typeof(LootWindow), nameof(LootWindow.LootAll));
                    var doDeathMethod = AccessTools.Method(typeof(Character), "DoDeath", new Type[0]);
                    if (lootAllMethod != null)
                    {
                        _harmony.Unpatch(lootAllMethod, HarmonyPatchType.Prefix, harmonyID);
                        Logging.LogWarning("[LootManager] Unpatched ErenshorQoL LootAll prefix.");
                    }
                    if (doDeathMethod != null)
                    {
                        _harmony.Unpatch(doDeathMethod, HarmonyPatchType.Postfix, harmonyID);
                        Logging.LogWarning("[LootManager] Unpatched ErenshorQoL DoDeath postfix.");
                    }
                    break;
                }
            }

            _harmony.PatchAll();

            LootManagerController.Initialize();

            var hotkeyGO = new GameObject("LootManager_Hotkeys");
            DontDestroyOnLoad(hotkeyGO);
            hotkeyGO.hideFlags = HideFlags.HideAndDontSave;
            hotkeyGO.AddComponent<AutoLootHotkeyListener>();

            Logging.Log("[LootManager] Loaded.");
        }

        private void Start()
        {
            InitImGui();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InitImGui()
        {
            float detectedScale = Mathf.Clamp((float)Screen.height / 1080f, 0.5f, 4f);

            _window = new LootManagerWindow();
            _imgui  = new ImGuiRenderer(Logging);
            _imgui.UiScale  = detectedScale;
            _imgui.OnLayout = () => _window.Draw();

            if (!_imgui.Init())
            {
                Logging.LogError("[LootManager] ImGui init failed — UI will not render.");
            }
            else
            {
                PointerOverUIPatch.Renderer = _imgui;
                _window.Scale = detectedScale;

                var mute = gameObject.GetComponent<ImGuiInputMute>()
                           ?? gameObject.AddComponent<ImGuiInputMute>();
                mute.Renderer = _imgui;

                Logging.Log("[LootManager] ImGui UI ready.");
            }
        }

        // OnImGuiDraw called by Lunaris from inside Bridge.OnGUI (EventType.Repaint)
        // We call our own renderer's OnGUI here so it runs in the right Unity context
        public override void OnImGuiDraw()
        {
            _imgui?.OnGUI();
        }

        public static void ToggleWindow()
        {
            Instance?._window?.Toggle();
        }

        private void Update()
        {
            if (_window == null) return;
            if (GameData.PlayerTyping || (_imgui != null && _imgui.WantTextInput)) return;

            if (Input.GetKeyDown(GetToggleLootUIHotkey()))
                _window.Toggle();
        }

        private void OnApplicationQuit()
        {
            // Signal the renderer to skip cimgui calls during shutdown
            // to prevent crashes when the native graphics context is gone
            _imgui?.OnApplicationQuit();
        }

        private void OnDestroy()
        {
            // Unpatch Harmony first so no patched methods fire during cleanup
            _harmony?.UnpatchSelf();
            _harmony = null;

            // Disable input mute and pointer patch
            PointerOverUIPatch.Renderer = null;
            var mute = GetComponent<ImGuiInputMute>();
            if (mute != null) Destroy(mute);

            // Dispose ImGui renderer — destroys context and all Unity meshes/materials
            _imgui?.Dispose();
            _imgui = null;

            // Destroy our extra GameObjects
            var hotkeys = GameObject.Find("LootManager_Hotkeys");
            if (hotkeys != null) Destroy(hotkeys);

            // Null instance last
            Instance = null;
            Logging.Log("[LootManager] Unloaded.");
        }
    }
}