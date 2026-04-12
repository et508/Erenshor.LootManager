using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace LootManager
{
    // ── Shared base for Blacklist / Whitelist / Banklist / Junklist / Auctionlist ──
    //
    // Layout: filter input bar at top, then two equal columns side-by-side.
    // Left  = "All Items" (items NOT in the list)
    // Right = "Listed"    (items IN the list)
    // Double-click any row to move it across.

    internal abstract class DualListTab
    {
        // ── Subclass contract ─────────────────────────────────────────────────

        protected abstract string LeftHeader  { get; }
        protected abstract string RightHeader { get; }
        protected abstract uint   RightColor  { get; }   // tint for listed items
        protected abstract HashSet<string> GetList();
        protected abstract void SaveList();
        protected abstract void DrawExtraControls(float scale); // optional extras above filter

        // ── State ─────────────────────────────────────────────────────────────

        private List<string> _leftData  = new List<string>();
        private List<string> _rightData = new List<string>();

        private string _filter         = "";
        private byte[] _filterBuf      = new byte[256];

        private string _lastSingleClick = null;
        private float  _lastClickTime   = -1f;
        private const float DoubleClickInterval = 0.3f;

        // Set by external code (e.g. drop zone handlers) to force a refresh
        // on the next Draw() without requiring a tab switch.
        private static bool _dirty;
        public static void MarkDirty() { _dirty = true; }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public void OnShow()
        {
            ItemLookup.EnsureBuilt();
            _filter = "";
            Array.Clear(_filterBuf, 0, _filterBuf.Length);
            Refresh();
        }

        public void Draw(float scale)
        {
            if (_dirty)
            {
                _dirty = false;
                Refresh();
            }
            LootManagerWindow.PushWidgetStyle();
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6f * scale, 4f * scale));

            // Extra controls (e.g. toggles specific to this tab)
            DrawExtraControls(scale);

            // Filter bar
            ImGui.SetNextItemWidth(-1f);
            if (ImGui.InputText("##filter", _filterBuf, (uint)_filterBuf.Length))
            {
                _filter = System.Text.Encoding.UTF8.GetString(_filterBuf).TrimEnd('\0');
                Refresh();
            }

            ImGui.Spacing();

            // Column headers
            float colW = (ImGui.GetContentRegionAvail().X - 8f * scale) * 0.5f;
            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted(LeftHeader);
            ImGui.SameLine(colW + 8f * scale);
            ImGui.TextUnformatted(RightHeader);
            ImGui.PopStyleColor();

            ImGui.Separator();

            // Two-column list area
            float listH = ImGui.GetContentRegionAvail().Y - 22f * scale;
            if (listH < 60f) listH = 60f;

            DrawListColumn("##left_child",  colW, listH, scale, _leftData,  false);
            ImGui.SameLine(0f, 8f * scale);
            DrawListColumn("##right_child", colW, listH, scale, _rightData, true);

            // Hint
            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("Double-click an item to move it");
            ImGui.PopStyleColor();

            ImGui.PopStyleVar();
            LootManagerWindow.PopWidgetStyle();
        }

        // ── List column ───────────────────────────────────────────────────────

        private void DrawListColumn(string id, float width, float height, float scale,
            List<string> data, bool isRight)
        {
            ImGui.PushStyleColor(ImGuiCol.ChildBg, LootManagerWindow.V4InputBg);
            ImGui.BeginChild(id, new Vector2(width, height), false, ImGuiWindowFlags.None);
            ImGui.PopStyleColor();

            float iconSize  = ImGui.GetTextLineHeight();
            float rowHeight = iconSize + 2f;
            var   iconVec   = new Vector2(iconSize, iconSize);

            uint textColor = isRight ? RightColor : LootManagerWindow.C_TextPri;

            for (int i = 0; i < data.Count; i++)
            {
                string item = data[i];

                // Full-width invisible selectable as the hit target
                ImGui.PushStyleColor(ImGuiCol.Header,        new System.Numerics.Vector4(1,1,1,0.10f));
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new System.Numerics.Vector4(1,1,1,0.15f));
                bool selected = ImGui.Selectable("##row" + (isRight ? "r" : "l") + i,
                    false, ImGuiSelectableFlags.None, new Vector2(0, rowHeight));
                ImGui.PopStyleColor(2);

                // Go back to start of the row
                ImGui.SameLine(0f, 0f);
                ImGui.SetCursorPosX(ImGui.GetCursorPosX());

                // Icon
                var texPtr = ItemLookup.GetIconPtr(item);
                if (texPtr != System.IntPtr.Zero)
                {
                    Vector2 uv0, uv1;
                    ItemLookup.GetIconUVs(item, out uv0, out uv1);
                    float curY = ImGui.GetCursorPosY();
                    ImGui.SetCursorPosY(curY + 1f);
                    ImGui.Image(texPtr, iconVec, uv0, uv1);
                    ImGui.SetCursorPosY(curY);
                    ImGui.SameLine(0f, 4f);
                }

                // Text, vertically centred in the row
                float textOffY = (rowHeight - ImGui.GetTextLineHeight()) * 0.5f;
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + textOffY);
                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertU32ToFloat4(textColor));
                ImGui.TextUnformatted(item);
                ImGui.PopStyleColor();

                if (selected)
                {
                    float now = UnityEngine.Time.unscaledTime;
                    if (_lastSingleClick == item && (now - _lastClickTime) <= DoubleClickInterval)
                    {
                        OnDoubleClick(item, isRight);
                        _lastSingleClick = null;
                        break; // list may have changed
                    }
                    else
                    {
                        _lastSingleClick = item;
                        _lastClickTime   = now;
                    }
                }
            }

            ImGui.EndChild();
        }

        // ── Double-click handler ──────────────────────────────────────────────

        private void OnDoubleClick(string itemName, bool isInList)
        {
            var list = GetList();
            if (isInList)
            {
                list.Remove(itemName);
                ChatFilterInjector.SendLootMessage(
                    $"[LootUI] Removed from {RightHeader}: {itemName}", "yellow");
            }
            else
            {
                list.Add(itemName);
                ChatFilterInjector.SendLootMessage(
                    $"[LootUI] Added to {RightHeader}: {itemName}", "yellow");
            }
            SaveList();
            Refresh();
        }

        // ── Data refresh ──────────────────────────────────────────────────────

        private void Refresh()
        {
            var list   = GetList();
            var source = ItemLookup.AllItems;
            string f   = _filter.ToLowerInvariant();

            _rightData = list
                .Where(i => string.IsNullOrEmpty(f) || i.ToLowerInvariant().Contains(f))
                .OrderBy(i => i)
                .ToList();

            var rightSet = new HashSet<string>(_rightData, StringComparer.OrdinalIgnoreCase);

            _leftData = (string.IsNullOrEmpty(f)
                    ? source.AsEnumerable()
                    : source.Where(i => i.ToLowerInvariant().Contains(f)))
                .Where(i => !rightSet.Contains(i))
                .ToList();
        }
    }
}