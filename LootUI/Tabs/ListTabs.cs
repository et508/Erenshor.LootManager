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

        protected override void DrawExtraControls(float scale)
        {
            bool lootRare = Plugin.LootRare.Value;
            if (ImGui.Checkbox("Always Loot Rare##bl_rare", ref lootRare))
                Plugin.LootRare.Value = lootRare;

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
            { "All", "Normal Only", "Blessed Only", "Godly Only", "Blessed and Up" };

        private int _tierIdx;

        public new void OnShow()
        {
            base.OnShow();
            _tierIdx = (int)Plugin.LootEquipmentTier.Value;
        }

        protected override void DrawExtraControls(float scale)
        {
            float labelW = 130f * scale;

            bool lootEquip = Plugin.LootEquipment.Value;
            if (ImGui.Checkbox("Loot Equipment##wl_equip", ref lootEquip))
            {
                Plugin.LootEquipment.Value = lootEquip;
            }

            ImGui.SameLine(200f * scale);

            if (!lootEquip) ImGui.BeginDisabled();

            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("Tier:");
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(160f * scale);
            if (ImGui.Combo("##wl_tier", ref _tierIdx, TierOptions, TierOptions.Length))
                Plugin.LootEquipmentTier.Value = (EquipmentTierSetting)_tierIdx;

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