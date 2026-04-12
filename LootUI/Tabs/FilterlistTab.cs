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
            float actW   = 50f  * scale;
            float checkW = 54f  * scale;
            float editW  = 40f  * scale;
            float delW   = 40f  * scale;

            // ── Table ─────────────────────────────────────────────────────────
            float listH = ImGui.GetContentRegionAvail().Y - 4f;
            ImGui.BeginChild("##fl_scroll", new Vector2(-1f, listH), false, ImGuiWindowFlags.None);

            var tableFlags = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV;
            if (ImGui.BeginTable("##fl_table", 6, tableFlags))
            {
                // Col 0: Active checkbox  (fixed)
                // Col 1: Name             (stretch)
                // Col 2: BL toggle        (fixed)
                // Col 3: WL toggle        (fixed)
                // Col 4: Bank toggle      (fixed)
                // Col 5: Edit / Del       (fixed)
                ImGui.TableSetupColumn("Active", ImGuiTableColumnFlags.WidthFixed,  actW);
                ImGui.TableSetupColumn("Name",   ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("BL",     ImGuiTableColumnFlags.WidthFixed,  checkW);
                ImGui.TableSetupColumn("WL",     ImGuiTableColumnFlags.WidthFixed,  checkW);
                ImGui.TableSetupColumn("Bank",   ImGuiTableColumnFlags.WidthFixed,  checkW);
                ImGui.TableSetupColumn("##act",  ImGuiTableColumnFlags.WidthFixed,  editW + delW + 6f * scale);

                // Header row
                ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
                ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
                for (int col = 0; col < 6; col++)
                {
                    ImGui.TableSetColumnIndex(col);
                    string hdr = col == 0 ? "Active"
                               : col == 1 ? "Name"
                               : col == 2 ? "BL"
                               : col == 3 ? "WL"
                               : col == 4 ? "Bank"
                               : "";
                    ImGui.TextUnformatted(hdr);
                }
                ImGui.PopStyleColor();

                // Data rows
                foreach (string cat in _sortedKeys)
                {
                    ImGui.TableNextRow();
                    ImGui.PushID(cat);

                    // Col 0: Active
                    ImGui.TableSetColumnIndex(0);
                    bool active = Plugin.EnabledFilterCategories.Contains(cat);
                    if (ImGui.Checkbox("##active", ref active))
                        LootFilterlist.SetSectionEnabled(cat, active);

                    // Col 1: Name
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted(cat);

                    // Col 2: BL
                    ImGui.TableSetColumnIndex(2);
                    bool applyBL = Plugin.FilterAppliedToBlacklist.Contains(cat);
                    if (ImGui.Checkbox("##bl", ref applyBL))
                        LootFilterlist.SetAppliedTo(cat, "Blacklist", applyBL);

                    // Col 3: WL
                    ImGui.TableSetColumnIndex(3);
                    bool applyWL = Plugin.FilterAppliedToWhitelist.Contains(cat);
                    if (ImGui.Checkbox("##wl", ref applyWL))
                        LootFilterlist.SetAppliedTo(cat, "Whitelist", applyWL);

                    // Col 4: Bank
                    ImGui.TableSetColumnIndex(4);
                    bool applyBK = Plugin.FilterAppliedToBanklist.Contains(cat);
                    if (ImGui.Checkbox("##bk", ref applyBK))
                        LootFilterlist.SetAppliedTo(cat, "Banklist", applyBK);

                    // Col 5: Edit / Del
                    ImGui.TableSetColumnIndex(5);
                    if (ImGui.Button("Edit##edit", new Vector2(editW, 0f)))
                        openEditlist?.Invoke(cat);

                    ImGui.SameLine();

                    if (_deleteConfirm == cat)
                    {
                        if (LootManagerWindow.DangerButton("Sure?##del", new Vector2(delW + 10f * scale, 0f)))
                        {
                            DeleteCategory(cat);
                            _deleteConfirm = null;
                            ImGui.PopID();
                            break;
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

                ImGui.EndTable();
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