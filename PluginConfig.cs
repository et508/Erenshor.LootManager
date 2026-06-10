using Lunaris.Config;
using UnityEngine;

namespace LootManager
{
    /// <summary>
    /// Lunaris class-based config. Register with Config.Register&lt;PluginConfig&gt;().
    /// Fields are read/written via the returned IConfigHandle&lt;PluginConfig&gt;.Value.
    /// </summary>
    public class PluginConfig
    {
        [Config("toggle-lootui-hotkey",    "Hotkeys")]   public KeyCode ToggleLootUIHotkey   = KeyCode.F6;
        [Config("toggle-autoloot-hotkey",  "Hotkeys")]   public KeyCode ToggleAutoLootHotkey = KeyCode.F10;

        [Config("autoloot-enabled",        "Autoloot")]  public bool  AutoLootEnabled      = true;
        [Config("autoloot-distance",       "Autoloot")]  public float AutoLootDistance     = 20f;
        [Config("autoloot-delay-enabled",  "Autoloot")]  public bool  AutoLootDelayEnabled = false;
        [Config("autoloot-delay",          "Autoloot")]  public float AutoLootDelay        = 3f;

        [Config("loot-method",             "LootMethod")]public string LootMethod          = "Blacklist";

        [Config("auction-loot-enabled",    "Auction")]   public bool  AuctionLootEnabled   = true;

        [Config("bankloot-enabled",        "Bankloot")]  public bool  BankLootEnabled      = false;
        [Config("bankloot-method",         "Bankloot")]  public string BankLootMethod      = "All";
        [Config("bankloot-page-mode",      "Bankloot")]  public string BankLootPageMode    = "First Empty";
        [Config("bank-page-first",         "Bankloot")]  public int   BankPageFirst        = 20;
        [Config("bank-page-last",          "Bankloot")]  public int   BankPageLast         = 20;
        [Config("bankslot-add-to-list",    "Bankloot")]  public bool  BankslotAddToList    = false;

        [Config("loot-rare",               "Filters")]   public bool  LootRare             = false;
        [Config("loot-equipment",          "Filters")]   public bool  LootEquipment        = false;
        [Config("loot-equipment-tier",     "Filters")]   public string LootEquipmentTier   = "All";
        [Config("fishing-filter-enabled",  "Filters")]   public bool  FishingFilterEnabled = false;
        [Config("mining-filter-enabled",   "Filters")]   public bool  MiningFilterEnabled  = false;

        [Config("chat-output-window",      "Chat")]      public string ChatOutputWindow    = "MAINCHAT";
        [Config("chat-output-tab",         "Chat")]      public int   ChatOutputTab        = 0;
        [Config("chat-output-enabled",     "Chat")]      public bool  ChatOutputEnabled    = true;
    }
}