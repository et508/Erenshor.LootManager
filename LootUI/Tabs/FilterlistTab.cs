using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace LootManager
{
    internal sealed class FilterlistTab
    {
        private List<string>    _sortedKeys    = new List<string>();
        private byte[]          _newNameBuf    = new byte[128];
        private string          _newName       = "";
        private string          _deleteConfirm = null; // category name pending confirm

        public void OnShow()
        {
            RebuildKeys();
            Array.Clear(_newNameBuf, 0, _newNameBuf.Length);
            _newName       = "";
            _deleteConfirm = null;
        }

        public void Draw(float scale, Action<string> openEditlist)
        {
            LootManagerWindow.PushWidgetStyle();
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6f * scale, 5f * scale));

            // ── New group row ─────────────────────────────────────────────────
            ImGui.SetNextItemWidth(-80f * scale);
            if (ImGui.InputText("##fl_newname", _newNameBuf, (uint)_newNameBuf.Length))
                _newName = System.Text.Encoding.UTF8.GetString(_newNameBuf).TrimEnd('\0');

            ImGui.SameLine();
            bool canAdd = !string.IsNullOrWhiteSpace(_newName)
                       && !Plugin.FilterList.ContainsKey(_newName.Trim());

            if (!canAdd) ImGui.BeginDisabled();
            if (LootManagerWindow.AccentButton("+ Add##fl_add", new Vector2(-1f, 0f)))
            {
                string key = _newName.Trim();
                Plugin.FilterList[key] = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                LootFilterlist.SaveFilterlist();
                ChatFilterInjector.SendLootMessage($"[LootUI] Created group '{key}'.", "yellow");
                Array.Clear(_newNameBuf, 0, _newNameBuf.Length);
                _newName = "";
                RebuildKeys();
            }
            if (!canAdd) ImGui.EndDisabled();

            ImGui.Separator();

            // ── Column headers ────────────────────────────────────────────────
            float totalW  = ImGui.GetContentRegionAvail().X;
            float actW    = 44f  * scale;
            float nameW   = 0f;            // flexible
            float checkW  = 64f  * scale;  // per toggle column
            float editW   = 38f  * scale;
            float delW    = 38f  * scale;
            int   numCheckCols = 3;        // Blacklist, Whitelist, Banklist
            float fixedW  = actW + (checkW * numCheckCols) + editW + delW + 20f * scale;
            nameW = Math.Max(80f * scale, totalW - fixedW);

            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("Active"); ImGui.SameLine(actW);
            ImGui.TextUnformatted("Name");   ImGui.SameLine(actW + nameW);
            ImGui.TextUnformatted("BL");     ImGui.SameLine(actW + nameW + checkW * 1);
            ImGui.TextUnformatted("WL");     ImGui.SameLine(actW + nameW + checkW * 2);
            ImGui.TextUnformatted("Bank");
            ImGui.PopStyleColor();
            ImGui.Separator();

            // ── Rows ──────────────────────────────────────────────────────────
            float listH = ImGui.GetContentRegionAvail().Y - 4f;
            ImGui.BeginChild("##fl_scroll", new Vector2(-1f, listH), false, ImGuiWindowFlags.None);

            foreach (string cat in _sortedKeys)
            {
                ImGui.PushID(cat);

                // Active toggle
                bool active = Plugin.EnabledFilterCategories.Contains(cat);
                if (ImGui.Checkbox("##active", ref active))
                    LootFilterlist.SetSectionEnabled(cat, active);
                ImGui.SameLine(actW);

                // Name
                ImGui.TextUnformatted(cat);
                ImGui.SameLine(actW + nameW);

                // Blacklist toggle
                bool applyBL = Plugin.FilterAppliedToBlacklist.Contains(cat);
                if (ImGui.Checkbox("##bl", ref applyBL))
                    LootFilterlist.SetAppliedTo(cat, "Blacklist", applyBL);
                ImGui.SameLine(actW + nameW + checkW * 1);

                // Whitelist toggle
                bool applyWL = Plugin.FilterAppliedToWhitelist.Contains(cat);
                if (ImGui.Checkbox("##wl", ref applyWL))
                    LootFilterlist.SetAppliedTo(cat, "Whitelist", applyWL);
                ImGui.SameLine(actW + nameW + checkW * 2);

                // Banklist toggle
                bool applyBK = Plugin.FilterAppliedToBanklist.Contains(cat);
                if (ImGui.Checkbox("##bk", ref applyBK))
                    LootFilterlist.SetAppliedTo(cat, "Banklist", applyBK);
                ImGui.SameLine();

                // Spacer to push edit/del to right side
                ImGui.SetCursorPosX(totalW - editW - delW - 8f * scale);

                if (ImGui.Button("Edit##edit", new Vector2(editW, 0f)))
                    openEditlist?.Invoke(cat);

                ImGui.SameLine();

                // Delete — with inline confirmation
                if (_deleteConfirm == cat)
                {
                    if (LootManagerWindow.DangerButton("Sure?##del", new Vector2(delW + 10f * scale, 0f)))
                    {
                        DeleteCategory(cat);
                        _deleteConfirm = null;
                        ImGui.PopID();
                        break; // list changed, stop iterating
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("No##delno"))
                        _deleteConfirm = null;
                }
                else
                {
                    if (LootManagerWindow.DangerButton("Del##del", new Vector2(delW, 0f)))
                        _deleteConfirm = cat;
                }

                ImGui.PopID();
            }

            ImGui.EndChild();

            ImGui.PopStyleVar();
            LootManagerWindow.PopWidgetStyle();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void RebuildKeys()
        {
            _sortedKeys = Plugin.FilterList.Keys
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void DeleteCategory(string cat)
        {
            Plugin.FilterList.Remove(cat);
            Plugin.EnabledFilterCategories.Remove(cat);
            Plugin.FilterAppliedToBlacklist.Remove(cat);
            Plugin.FilterAppliedToWhitelist.Remove(cat);
            Plugin.FilterAppliedToBanklist.Remove(cat);
            LootFilterlist.SaveFilterlist();
            ChatFilterInjector.SendLootMessage($"[LootUI] Deleted group '{cat}'.", "yellow");
            RebuildKeys();
        }
    }
}