using System.Numerics;
using ImGuiNET;

namespace LootManager
{
    internal sealed class LootManagerWindow
    {
        // ── Public surface ────────────────────────────────────────────────────

        public float Scale { get; set; } = 1f;

        public void Toggle() { _visible = !_visible; }
        public void Show()   { _visible = true; }
        public void Hide()   { _visible = false; }

        // ── Tab instances ─────────────────────────────────────────────────────

        private readonly SettingsTab    _settings    = new SettingsTab();
        private readonly BlacklistTab   _blacklist   = new BlacklistTab();
        private readonly WhitelistTab   _whitelist   = new WhitelistTab();
        private readonly BanklistTab    _banklist    = new BanklistTab();
        private readonly JunklistTab    _junklist    = new JunklistTab();
        private readonly AuctionlistTab _auctionlist = new AuctionlistTab();
        private readonly FilterlistTab  _filterlist  = new FilterlistTab();
        private readonly EditlistTab    _editlist    = new EditlistTab();

        // ── State ─────────────────────────────────────────────────────────────

        private bool _visible    = false;
        private bool _editMode      = false;   // true = editlist is shown instead of tab bar
        private int  _activeTab     = 0;       // index into the visible tab list
        private bool _initialized   = false;   // first-open OnShow fired

        // Tab IDs — kept stable so ImGui tab bar state doesn't flicker
        private const int TAB_SETTINGS    = 0;
        private const int TAB_BLACKLIST   = 1;
        private const int TAB_WHITELIST   = 2;
        private const int TAB_BANKLIST    = 3;
        private const int TAB_JUNKLIST    = 4;
        private const int TAB_AUCTIONLIST = 5;
        private const int TAB_FILTERLIST  = 6;

        // ── Colors ───────────────────────────────────────────────────────────────

        // Stored as packed uint for ImGui: 0xAABBGGRR
        internal static uint Col(byte r, byte g, byte b, byte a = 255)
            => (uint)((a << 24) | (b << 16) | (g << 8) | r);

        internal static readonly uint C_WindowBg   = Col(0x0F, 0x10, 0x14, 245);
        internal static readonly uint C_TitleBg    = Col(0x1A, 0x1D, 0x23, 255);
        internal static readonly uint C_PanelBg    = Col(0x13, 0x16, 0x1B, 255);
        internal static readonly uint C_Border      = Col(0x2D, 0x31, 0x39, 255);
        internal static readonly uint C_AccentBlue  = Col(0x00, 0x8D, 0xFD, 255);
        internal static readonly uint C_BtnNormal   = Col(0x00, 0x00, 0x00, 255);
        internal static readonly uint C_BtnHover    = Col(0x1A, 0x3A, 0x5C, 255);
        internal static readonly uint C_BtnActive   = Col(0x0D, 0x24, 0x40, 255);
        internal static readonly uint C_TextPri     = Col(0xF1, 0xF5, 0xF9, 255);
        internal static readonly uint C_TextMuted   = Col(0x64, 0x74, 0x8B, 255);
        internal static readonly uint C_TextSecond  = Col(0x94, 0xA3, 0xB8, 255);
        internal static readonly uint C_InputBg     = Col(0x0A, 0x0C, 0x10, 255);
        internal static readonly uint C_RowOdd      = Col(0x11, 0x13, 0x18, 255);
        internal static readonly uint C_Danger      = Col(0xEF, 0x44, 0x44, 255);
        internal static readonly uint C_Success     = Col(0x10, 0xB9, 0x81, 255);
        internal static readonly uint C_Warning     = Col(0xF5, 0x9E, 0x0B, 255);

        // Vector4 versions for PushStyleColor
        internal static Vector4 V4WindowBg    => ImGui.ColorConvertU32ToFloat4(C_WindowBg);
        internal static Vector4 V4TitleBg     => ImGui.ColorConvertU32ToFloat4(C_TitleBg);
        internal static Vector4 V4PanelBg     => ImGui.ColorConvertU32ToFloat4(C_PanelBg);
        internal static Vector4 V4Border      => ImGui.ColorConvertU32ToFloat4(C_Border);
        internal static Vector4 V4AccentBlue  => ImGui.ColorConvertU32ToFloat4(C_AccentBlue);
        internal static Vector4 V4BtnNormal   => ImGui.ColorConvertU32ToFloat4(C_BtnNormal);
        internal static Vector4 V4BtnHover    => ImGui.ColorConvertU32ToFloat4(C_BtnHover);
        internal static Vector4 V4BtnActive   => ImGui.ColorConvertU32ToFloat4(C_BtnActive);
        internal static Vector4 V4TextPri     => ImGui.ColorConvertU32ToFloat4(C_TextPri);
        internal static Vector4 V4TextMuted   => ImGui.ColorConvertU32ToFloat4(C_TextMuted);
        internal static Vector4 V4InputBg     => ImGui.ColorConvertU32ToFloat4(C_InputBg);
        internal static Vector4 V4Danger      => ImGui.ColorConvertU32ToFloat4(C_Danger);
        internal static Vector4 V4Success     => ImGui.ColorConvertU32ToFloat4(C_Success);
        internal static Vector4 V4Warning     => ImGui.ColorConvertU32ToFloat4(C_Warning);

        // ── Main draw entry ───────────────────────────────────────────────────

        public void Draw()
        {
            if (!_visible) return;

            // Edit mode: show the editlist window instead of the main window
            if (_editMode)
            {
                bool stillOpen = _editlist.Draw(Scale);
                if (!stillOpen)
                    _editMode = false;
                return;
            }

            DrawMainWindow();
        }

        // ── Main window ───────────────────────────────────────────────────────

        private void DrawMainWindow()
        {
            float s = Scale;

            // Size: 700w × 520h (a bit bigger than the old 620×460, fits better at 1080p)
            ImGui.SetNextWindowSize(new Vector2(700f * s, 520f * s), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(
                new Vector2(500f * s, 360f * s),
                new Vector2(1200f * s, 900f * s));
            ImGui.SetNextWindowPos(
                new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f),
                ImGuiCond.FirstUseEver,
                new Vector2(0.5f, 0.5f));

            PushWindowStyle();

            bool open = _visible;
            bool expanded = ImGui.Begin("##lootmanagermain", ref open,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

            PopWindowStyle();

            if (!open)
            {
                _visible = false;
                if (expanded) ImGui.End();
                return;
            }

            if (expanded)
            {
                if (!_initialized)
                {
                    _initialized = true;
                    OnTabChanged(_activeTab);
                }
                DrawColoredTitle();
                DrawTabBar();
            }

            ImGui.End();
        }

        // ── Colored title drawn into the title bar ───────────────────────────

        private void DrawColoredTitle()
        {
            // Draw "Loot" + "Manager" in separate colors into the title bar
            // using the background draw list so it appears on top of the bar.
            var dl       = ImGui.GetForegroundDrawList();
            var winPos   = ImGui.GetWindowPos();
            float titleH = ImGui.GetFrameHeight(); // ImGui title bar height

            // Vertically centre text in the title bar
            float textH  = ImGui.GetTextLineHeight();
            float y      = winPos.Y + (titleH - textH) * 0.5f;
            float x      = winPos.X + 8f * Scale;

            uint colLoot    = Col(0xF1, 0xF5, 0xF9); // white-ish
            uint colManager = Col(0x4A, 0x90, 0xD9); // readable blue (not as dark as #0D2440 on dark bg)

            dl.AddText(new System.Numerics.Vector2(x, y), colLoot, "Loot ");
            float lootW = ImGui.CalcTextSize("Loot ").X;
            dl.AddText(new System.Numerics.Vector2(x + lootW, y), colManager, "Manager");
        }

        // ── Tab bar ───────────────────────────────────────────────────────────

        private void DrawTabBar()
        {
            float s = Scale;

            PushTabStyle();

            if (ImGui.BeginTabBar("##lm_tabs", ImGuiTabBarFlags.None))
            {
                DrawTab("Settings",    TAB_SETTINGS,    true);
                DrawTab("Blacklist",   TAB_BLACKLIST,   Plugin.LootMethod.Value == "Blacklist");
                DrawTab("Whitelist",   TAB_WHITELIST,   Plugin.LootMethod.Value == "Whitelist");
                DrawTab("Banklist",    TAB_BANKLIST,    Plugin.BankLootEnabled.Value);
                DrawTab("Junklist",    TAB_JUNKLIST,    true);
                DrawTab("Auctionlist", TAB_AUCTIONLIST, true);
                DrawTab("Filterlists", TAB_FILTERLIST,  true);

                ImGui.EndTabBar();
            }

            PopTabStyle();
        }

        private void DrawTab(string label, int tabId, bool visible)
        {
            if (!visible) return;

            bool isActive = (_activeTab == tabId);

            // Highlight active tab with accent colour
            if (isActive)
                ImGui.PushStyleColor(ImGuiCol.TabActive, V4AccentBlue);

            if (ImGui.BeginTabItem(label + "##" + tabId))
            {
                if (_activeTab != tabId)
                {
                    _activeTab = tabId;
                    OnTabChanged(tabId);
                }

                // Content area — each tab draws into the remaining space
                DrawTabContent(tabId);

                ImGui.EndTabItem();
            }

            if (isActive)
                ImGui.PopStyleColor();
        }

        private void OnTabChanged(int tabId)
        {
            // Tabs that need a refresh when shown
            switch (tabId)
            {
                case TAB_SETTINGS:    _settings.OnShow();    break;
                case TAB_BLACKLIST:   _blacklist.OnShow();   break;
                case TAB_WHITELIST:   _whitelist.OnShow();   break;
                case TAB_BANKLIST:    _banklist.OnShow();    break;
                case TAB_JUNKLIST:    _junklist.OnShow();    break;
                case TAB_AUCTIONLIST: _auctionlist.OnShow(); break;
                case TAB_FILTERLIST:  _filterlist.OnShow();  break;
            }
        }

        private void DrawTabContent(int tabId)
        {
            switch (tabId)
            {
                case TAB_SETTINGS:
                    _settings.Draw(Scale, () =>
                    {
                        // When loot method or bank toggle changes, snap back to Settings
                        // tab if the current tab just became hidden
                        if (_activeTab == TAB_BLACKLIST   && Plugin.LootMethod.Value != "Blacklist") _activeTab = TAB_SETTINGS;
                        if (_activeTab == TAB_WHITELIST   && Plugin.LootMethod.Value != "Whitelist") _activeTab = TAB_SETTINGS;
                        if (_activeTab == TAB_BANKLIST    && !Plugin.BankLootEnabled.Value)          _activeTab = TAB_SETTINGS;
                    });
                    break;
                case TAB_BLACKLIST:
                    _blacklist.Draw(Scale);
                    break;
                case TAB_WHITELIST:
                    _whitelist.Draw(Scale);
                    break;
                case TAB_BANKLIST:
                    _banklist.Draw(Scale);
                    break;
                case TAB_JUNKLIST:
                    _junklist.Draw(Scale);
                    break;
                case TAB_AUCTIONLIST:
                    _auctionlist.Draw(Scale);
                    break;
                case TAB_FILTERLIST:
                    _filterlist.Draw(Scale, OpenEditlist);
                    break;
            }
        }

        // ── Edit mode (category item editor) ─────────────────────────────────

        /// <summary>Called by FilterlistTab when the user clicks Edit on a category.</summary>
        internal void OpenEditlist(string categoryName)
        {
            _editlist.Open(categoryName);
            _editMode = true;
        }

        // ── Style helpers ─────────────────────────────────────────────────────

        private void PushWindowStyle()
        {
            ImGui.PushStyleColor(ImGuiCol.WindowBg,      V4WindowBg);
            ImGui.PushStyleColor(ImGuiCol.TitleBg,       V4TitleBg);
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, V4TitleBg);
            ImGui.PushStyleColor(ImGuiCol.Border,        V4Border);
            ImGui.PushStyleColor(ImGuiCol.Text,          V4TextPri);
            ImGui.PushStyleColor(ImGuiCol.Separator,     V4Border);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding,  new Vector2(10f * Scale, 8f * Scale));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing,    new Vector2(6f  * Scale, 4f * Scale));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 3f * Scale);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding,  2f * Scale);
        }

        private void PopWindowStyle()
        {
            ImGui.PopStyleVar(4);
            ImGui.PopStyleColor(6);
        }

        private void PushTabStyle()
        {
            ImGui.PushStyleColor(ImGuiCol.Tab,              V4PanelBg);
            ImGui.PushStyleColor(ImGuiCol.TabHovered,       V4BtnHover);
            ImGui.PushStyleColor(ImGuiCol.TabActive,        V4BtnNormal);
            ImGui.PushStyleColor(ImGuiCol.TabUnfocused,     V4PanelBg);
            ImGui.PushStyleColor(ImGuiCol.TabUnfocusedActive, V4BtnNormal);
        }

        private void PopTabStyle()
        {
            ImGui.PopStyleColor(5);
        }

        // ── Shared style push/pop used by all tabs ────────────────────────────

        /// <summary>
        /// Tabs call this to push common widget colours (buttons, frames, inputs).
        /// Must be balanced with PopWidgetStyle().
        /// </summary>
        internal static void PushWidgetStyle()
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        V4BtnNormal);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, V4BtnHover);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  V4BtnActive);
            ImGui.PushStyleColor(ImGuiCol.FrameBg,       V4InputBg);
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered,V4BtnHover);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, V4BtnActive);
            ImGui.PushStyleColor(ImGuiCol.CheckMark,     V4AccentBlue);
            ImGui.PushStyleColor(ImGuiCol.SliderGrab,    V4AccentBlue);
            ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, V4AccentBlue);
            ImGui.PushStyleColor(ImGuiCol.Header,        V4BtnNormal);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, V4BtnHover);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive,  V4BtnActive);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg,   V4InputBg);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, V4BtnHover);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, V4AccentBlue);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive,  V4AccentBlue);
            ImGui.PushStyleColor(ImGuiCol.PopupBg,       V4PanelBg);
            ImGui.PushStyleColor(ImGuiCol.Text,          V4TextPri);
            ImGui.PushStyleColor(ImGuiCol.Border,        V4Border);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
        }

        internal static void PopWidgetStyle()
        {
            ImGui.PopStyleVar(1);
            ImGui.PopStyleColor(19);
        }

        // ── Shared helpers used by multiple tabs ──────────────────────────────

        /// <summary>Accent-coloured section header text.</summary>
        internal static void SectionHeader(string text)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, V4TextMuted);
            ImGui.TextUnformatted(text.ToUpper());
            ImGui.PopStyleColor();
            ImGui.Separator();
        }

        /// <summary>
        /// Danger-coloured button. Returns true if clicked.
        /// </summary>
        internal static bool DangerButton(string label, Vector2 size = default)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        V4Danger);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Lighten(V4Danger, 0.1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  Darken(V4Danger, 0.1f));
            bool clicked = size == default
                ? ImGui.Button(label)
                : ImGui.Button(label, size);
            ImGui.PopStyleColor(3);
            return clicked;
        }

        /// <summary>Accent-coloured button. Returns true if clicked.</summary>
        internal static bool AccentButton(string label, Vector2 size = default)
        {
            var v = V4AccentBlue;
            ImGui.PushStyleColor(ImGuiCol.Button,        v);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Lighten(v, 0.1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  Darken(v, 0.1f));
            bool clicked = size == default
                ? ImGui.Button(label)
                : ImGui.Button(label, size);
            ImGui.PopStyleColor(3);
            return clicked;
        }

        internal static Vector4 Lighten(Vector4 c, float amt) => new Vector4(
            System.Math.Min(c.X + amt, 1f), System.Math.Min(c.Y + amt, 1f),
            System.Math.Min(c.Z + amt, 1f), c.W);

        internal static Vector4 Darken(Vector4 c, float amt) => new Vector4(
            System.Math.Max(c.X - amt, 0f), System.Math.Max(c.Y - amt, 0f),
            System.Math.Max(c.Z - amt, 0f), c.W);
    }
}