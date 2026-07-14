using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace LootManager
{
    // ── Blacklist ─────────────────────────────────────────────────────────────

    internal sealed class BlacklistTab : DualListTab
    {
        protected override string LeftHeader  => "All Items";
        protected override string RightHeader => "Blacklisted";
        protected override uint   RightColor  => LootManagerWindow.C_Danger;
        protected override HashSet<string> GetList()  => Plugin.Blacklist;
        protected override void            SaveList()  => LootBlacklist.SaveBlacklist();

        private static readonly string[] TierOptions =
            { "All", "Normal Only", "Improved Only", "Blessed Only", "Ascended Only", "Improved and Up", "Blessed and Up" };

        private int _tierIdx;

        public new void OnShow()
        {
            base.OnShow();
            _tierIdx = System.Array.IndexOf(TierOptions, Plugin.GetLootEquipmentTier());
        }

        protected override void DrawExtraControls(float scale)
        {
            bool lootEquip = Plugin.GetLootEquipment();
            if (ImGui.Checkbox("Loot Equipment##bl_equip", ref lootEquip))
                Plugin.SetLootEquipment(lootEquip);

            ImGui.SameLine(200f * scale);

            if (!lootEquip) ImGui.BeginDisabled();

            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("Tier:");
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(160f * scale);
            if (ImGui.Combo("##bl_tier", ref _tierIdx, TierOptions, TierOptions.Length))
                Plugin.SetLootEquipmentTier(_tierIdx >= 0 && _tierIdx < TierOptions.Length ? TierOptions[_tierIdx] : "All");

            if (!lootEquip) ImGui.EndDisabled();

            ImGui.Spacing();
        }
    }

    // ── Whitelist ─────────────────────────────────────────────────────────────

    internal sealed class WhitelistTab : DualListTab
    {
        protected override string LeftHeader  => "All Items";
        protected override string RightHeader => "Whitelisted";
        protected override uint   RightColor  => LootManagerWindow.C_Success;
        protected override HashSet<string> GetList()  => Plugin.Whitelist;
        protected override void            SaveList()  => LootWhitelist.SaveWhitelist();

        private static readonly string[] TierOptions =
            { "All", "Normal Only", "Improved Only", "Blessed Only", "Ascended Only", "Improved and Up", "Blessed and Up" };

        private int _tierIdx;

        public new void OnShow()
        {
            base.OnShow();
            _tierIdx = System.Array.IndexOf(TierOptions, Plugin.GetLootEquipmentTier());
        }

        protected override void DrawExtraControls(float scale)
        {
            float labelW = 130f * scale;

            bool lootEquip = Plugin.GetLootEquipment();
            if (ImGui.Checkbox("Loot Equipment##wl_equip", ref lootEquip))
            {
                Plugin.SetLootEquipment(lootEquip);
            }

            ImGui.SameLine(200f * scale);

            if (!lootEquip) ImGui.BeginDisabled();

            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("Tier:");
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(160f * scale);
            if (ImGui.Combo("##wl_tier", ref _tierIdx, TierOptions, TierOptions.Length))
                Plugin.SetLootEquipmentTier(_tierIdx >= 0 && _tierIdx < TierOptions.Length ? TierOptions[_tierIdx] : "All");

            if (!lootEquip) ImGui.EndDisabled();

            ImGui.Spacing();
        }
    }

    // ── Banklist ──────────────────────────────────────────────────────────────

    internal sealed class BanklistTab : DualListTab
    {
        protected override string LeftHeader  => "All Items";
        protected override string RightHeader => "Banklisted";
        protected override uint   RightColor  => LootManagerWindow.C_AccentBlue;
        protected override HashSet<string> GetList()  => Plugin.Banklist;
        protected override void            SaveList()  => LootBanklist.SaveBanklist();
        protected override void DrawExtraControls(float scale) { }
    }

    // ── Junklist ──────────────────────────────────────────────────────────────

    internal sealed class JunklistTab : DualListTab
    {
        protected override string LeftHeader  => "All Items";
        protected override string RightHeader => "Junklisted";
        protected override uint   RightColor  => LootManagerWindow.C_Warning;
        protected override HashSet<string> GetList()  => Plugin.Junklist;
        protected override void            SaveList()  => LootJunklist.SaveJunklist();
        protected override void DrawExtraControls(float scale) { }
    }

    // ── Auctionlist ───────────────────────────────────────────────────────────

    internal sealed class AuctionlistTab : DualListTab
    {
        protected override string LeftHeader  => "All Items";
        protected override string RightHeader => "Auctionlisted";

        // Gold-ish colour
        protected override uint RightColor =>
            LootManagerWindow.Col(0xFF, 0xD7, 0x00);

        protected override HashSet<string> GetList()  => Plugin.Auctionlist;
        protected override void            SaveList()  => LootAuctionlist.SaveAuctionlist();
        protected override void DrawExtraControls(float scale) { }
    }
}