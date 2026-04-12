using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace LootManager
{
    internal sealed class EditlistTab
    {
        private string       _category   = "";
        private List<string> _leftData   = new List<string>();
        private List<string> _rightData  = new List<string>();

        private string _filter    = "";
        private byte[] _filterBuf = new byte[256];

        private string _lastSingleClick = null;
        private float  _lastClickTime   = -1f;
        private const float DoubleClickInterval = 0.3f;

        public void Open(string categoryName)
        {
            _category = categoryName?.Trim() ?? "";
            Array.Clear(_filterBuf, 0, _filterBuf.Length);
            _filter = "";

            Plugin.Editlist.Clear();
            Plugin.Editlist.UnionWith(LootFilterlist.ReadSectionItems(_category));

            ItemLookup.EnsureBuilt();
            Refresh();
        }

        /// <summary>Returns false when the user closes the editor.</summary>
        public bool Draw(float scale)
        {
            if (string.IsNullOrEmpty(_category)) return false;

            float s = scale;

            ImGui.SetNextWindowSize(new Vector2(700f * s, 520f * s), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(
                new Vector2(500f * s, 360f * s),
                new Vector2(1200f * s, 900f * s));
            ImGui.SetNextWindowPos(
                new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f),
                ImGuiCond.FirstUseEver,
                new Vector2(0.5f, 0.5f));

            // Window style
            ImGui.PushStyleColor(ImGuiCol.WindowBg,      LootManagerWindow.V4WindowBg);
            ImGui.PushStyleColor(ImGuiCol.TitleBg,       LootManagerWindow.V4TitleBg);
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, LootManagerWindow.V4TitleBg);
            ImGui.PushStyleColor(ImGuiCol.Border,        LootManagerWindow.V4Border);
            ImGui.PushStyleColor(ImGuiCol.Text,          LootManagerWindow.V4TextPri);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10f * s, 8f * s));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing,   new Vector2(6f  * s, 4f * s));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 3f * s);

            bool open = true;
            string title = $"Editing: {_category}##editlist";
            bool expanded = ImGui.Begin(title, ref open,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

            ImGui.PopStyleVar(3);
            ImGui.PopStyleColor(5);

            if (!open || !expanded)
            {
                if (expanded) ImGui.End();
                return false;
            }

            DrawContents(s);

            ImGui.End();
            return true;
        }

        private void DrawContents(float s)
        {
            LootManagerWindow.PushWidgetStyle();
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6f * s, 4f * s));

            // Filter bar
            ImGui.SetNextItemWidth(-1f);
            if (ImGui.InputText("##el_filter", _filterBuf, (uint)_filterBuf.Length))
            {
                _filter = System.Text.Encoding.UTF8.GetString(_filterBuf).TrimEnd('\0');
                Refresh();
            }

            ImGui.Spacing();

            // Column headers
            float colW = (ImGui.GetContentRegionAvail().X - 8f * s) * 0.5f;

            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("All Items");
            ImGui.SameLine(colW + 8f * s);
            ImGui.TextUnformatted("In Category");
            ImGui.PopStyleColor();

            ImGui.Separator();

            float listH = ImGui.GetContentRegionAvail().Y - 22f * s;
            if (listH < 60f) listH = 60f;

            DrawListColumn("##el_left",  colW, listH, s, _leftData,  false);
            ImGui.SameLine(0f, 8f * s);
            DrawListColumn("##el_right", colW, listH, s, _rightData, true);

            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("Double-click an item to move it");
            ImGui.PopStyleColor();

            ImGui.PopStyleVar();
            LootManagerWindow.PopWidgetStyle();
        }

        private void DrawListColumn(string id, float width, float height, float scale,
            List<string> data, bool isRight)
        {
            ImGui.PushStyleColor(ImGuiCol.ChildBg, LootManagerWindow.V4InputBg);
            ImGui.BeginChild(id, new Vector2(width, height), false, ImGuiWindowFlags.None);
            ImGui.PopStyleColor();

            uint textColor = isRight ? LootManagerWindow.C_AccentBlue : LootManagerWindow.C_TextPri;

            for (int i = 0; i < data.Count; i++)
            {
                string item = data[i];

                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertU32ToFloat4(textColor));
                bool selected = ImGui.Selectable(item + "##" + (isRight ? "r" : "l") + i,
                    false, ImGuiSelectableFlags.None);
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

        private void OnDoubleClick(string itemName, bool isInCategory)
        {
            if (isInCategory)
            {
                Plugin.Editlist.Remove(itemName);
                ChatFilterInjector.SendLootMessage(
                    $"[LootUI] Removed from {_category}: {itemName}", "yellow");
            }
            else
            {
                Plugin.Editlist.Add(itemName);
                ChatFilterInjector.SendLootMessage(
                    $"[LootUI] Added to {_category}: {itemName}", "yellow");
            }
            LootFilterlist.SaveSectionItems(_category, Plugin.Editlist);
            Refresh();
        }

        private void Refresh()
        {
            var source = ItemLookup.AllItems;
            string f   = _filter.ToLowerInvariant();

            _rightData = Plugin.Editlist
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