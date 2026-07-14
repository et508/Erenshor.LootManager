using System.Collections.Generic;
using ImGuiNET;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace LootManager
{
    internal sealed class SettingsTab
    {
        // ── Hotkey binding state ──────────────────────────────────────────────
        private bool   _bindingUIHotkey;
        private bool   _bindingAutoHotkey;

        // ── Slider backing fields (ImGui needs refs) ──────────────────────────
        private float _autoDistance;
        private float _autoDelay;
        private float _bankPageFirst;
        private float _bankPageLast;

        // ── Loot method / bank option indices ────────────────────────────────
        private int _lootMethodIdx;
        private int _bankMethodIdx;
        private int _bankPageModeIdx;

        // ── Chat dropdowns ────────────────────────────────────────────────────
        private int              _chatWindowIdx;
        private int              _chatTabIdx;
        private List<IDLog>      _chatWindows    = new List<IDLog>();
        private List<string>     _chatWindowNames = new List<string>();
        private List<string>     _chatTabNames   = new List<string>();

        private static readonly string[] LootMethodOptions  = { "Blacklist", "Whitelist", "Standard" };
        private static readonly string[] BankMethodOptions  = { "All", "Filtered" };
        private static readonly string[] BankPageOptions    = { "First Empty", "Page Range" };
        private static readonly string[] EquipTierOptions   = { "All", "Normal Only", "Blessed Only", "Ascended Only", "Blessed and Up", "Improved Only", "Improved and Up" };

        private System.Action _onVisibilityChanged;

        public void OnShow()
        {
            SyncFromPlugin();
            RefreshChatWindows();
        }

        public void Draw(float scale, System.Action onVisibilityChanged)
        {
            _onVisibilityChanged = onVisibilityChanged;

            LootManagerWindow.PushWidgetStyle();
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6f * scale, 6f * scale));

            // Scrollable content
            ImGui.BeginChild("##settings_scroll", Vector2.Zero, false, ImGuiWindowFlags.None);

            DrawHotkeySection(scale);
            ImGui.Spacing();
            DrawAutolootSection(scale);
            ImGui.Spacing();
            DrawLootMethodSection(scale);
            ImGui.Spacing();
            DrawFishingSection(scale);
            ImGui.Spacing();
            DrawBankLootSection(scale);
            ImGui.Spacing();
            DrawAuctionLootSection(scale);
            ImGui.Spacing();
            DrawChatOutputSection(scale);

            ImGui.EndChild();

            ImGui.PopStyleVar();
            LootManagerWindow.PopWidgetStyle();
        }

        // ── Hotkeys ───────────────────────────────────────────────────────────

        private void DrawHotkeySection(float s)
        {
            LootManagerWindow.SectionHeader("Hotkeys");

            float labelW = 160f * s;
            float btnW   = 140f * s;

            // Toggle UI hotkey
            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("Toggle UI Hotkey:");
            ImGui.PopStyleColor();
            ImGui.SameLine(labelW);
            DrawHotkeyButton("##hk_ui", Plugin.GetToggleLootUIHotkey, Plugin.SetToggleLootUIHotkey, ref _bindingUIHotkey, btnW);

            ImGui.Spacing();

            // Autoloot hotkey
            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("Autoloot Hotkey:");
            ImGui.PopStyleColor();
            ImGui.SameLine(labelW);
            DrawHotkeyButton("##hk_auto", Plugin.GetToggleAutoLootHotkey, Plugin.SetToggleAutoLootHotkey, ref _bindingAutoHotkey, btnW);
        }

        private void DrawHotkeyButton(string id, System.Func<KeyCode> getter,
            System.Action<KeyCode> setter, ref bool binding, float width)
        {
            if (binding)
            {
                ImGui.PushStyleColor(ImGuiCol.Button,        LootManagerWindow.V4AccentBlue);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, LootManagerWindow.V4AccentBlue);
                if (ImGui.Button("Press a key..." + id, new Vector2(width, 0f)))
                    binding = false;
                ImGui.PopStyleColor(2);

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    binding = false;
                }
                else
                {
                    foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
                    {
                        if (kc == KeyCode.Escape || kc == KeyCode.Mouse0 ||
                            kc == KeyCode.Mouse1  || kc == KeyCode.Mouse2) continue;
                        if (Input.GetKeyDown(kc))
                        {
                            setter(kc);
                            binding = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                KeyCode cur = getter();
                string label = cur == KeyCode.None ? "(none)" : cur.ToString();
                if (ImGui.Button(label + id, new Vector2(width, 0f)))
                    binding = true;
            }
        }

        // ── Autoloot ──────────────────────────────────────────────────────────

        private void DrawAutolootSection(float s)
        {
            LootManagerWindow.SectionHeader("Autoloot");

            float labelW = 160f * s;

            // Enable toggle
            bool autoEnabled = Plugin.GetAutoLootEnabled();
            if (ImGui.Checkbox("Enable Autoloot##auto_en", ref autoEnabled))
            {
                Plugin.SetAutoLootEnabled(autoEnabled);
            }

            ImGui.Spacing();

            // Distance slider
            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("Autoloot Distance:");
            ImGui.PopStyleColor();
            ImGui.SameLine(labelW);
            ImGui.SetNextItemWidth(-60f * s);
            if (ImGui.SliderFloat("##auto_dist", ref _autoDistance, 0f, 200f, "%.0f"))
                Plugin.SetAutoLootDistance(_autoDistance);
            ImGui.SameLine();
            ImGui.TextUnformatted(((int)_autoDistance).ToString());

            ImGui.Spacing();

            // Delay toggle
            bool delayEnabled = Plugin.GetAutoLootDelayEnabled();
            if (ImGui.Checkbox("Out-of-Combat Delay##delay_en", ref delayEnabled))
            {
                Plugin.SetAutoLootDelayEnabled(delayEnabled);
            }

            // Delay slider — only shown when delay is enabled
            if (delayEnabled)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
                ImGui.TextUnformatted("Grace Period (sec):");
                ImGui.PopStyleColor();
                ImGui.SameLine(labelW);
                ImGui.SetNextItemWidth(-60f * s);
                if (ImGui.SliderFloat("##auto_delay", ref _autoDelay, 0.5f, 10f, "%.1f"))
                    Plugin.SetAutoLootDelay(_autoDelay);
                ImGui.SameLine();
                ImGui.TextUnformatted(_autoDelay.ToString("F1"));
            }
        }

        // ── Loot method ───────────────────────────────────────────────────────

        private void DrawLootMethodSection(float s)
        {
            LootManagerWindow.SectionHeader("Loot Method");

            float labelW = 100f * s;

            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("Method:");
            ImGui.PopStyleColor();
            ImGui.SameLine(labelW);
            ImGui.SetNextItemWidth(160f * s);
            if (ImGui.Combo("##loot_method", ref _lootMethodIdx, LootMethodOptions, LootMethodOptions.Length))
            {
                Plugin.SetLootMethod(LootMethodOptions[_lootMethodIdx]);
                _onVisibilityChanged?.Invoke();
            }
        }

        // ── Bank loot ─────────────────────────────────────────────────────────

        private void DrawFishingSection(float s)
        {
            LootManagerWindow.SectionHeader("Fishing & Mining");

            bool fishOn = Plugin.GetFishingFilterEnabled();
            if (ImGui.Checkbox("Apply Loot Filters to Fishing##fish_en", ref fishOn))
                Plugin.SetFishingFilterEnabled(fishOn);

            bool mineOn = Plugin.GetMiningFilterEnabled();
            if (ImGui.Checkbox("Apply Loot Filters to Mining##mine_en", ref mineOn))
                Plugin.SetMiningFilterEnabled(mineOn);
        }

        private void DrawBankLootSection(float s)
        {
            LootManagerWindow.SectionHeader("Bank Loot");

            float labelW  = 120f * s;
            bool bankOn   = Plugin.GetBankLootEnabled();

            if (ImGui.Checkbox("Enable Bank Loot##bank_en", ref bankOn))
            {
                Plugin.SetBankLootEnabled(bankOn);
                _onVisibilityChanged?.Invoke();
            }

            // Dim controls when bank loot is off
            if (!bankOn) ImGui.BeginDisabled();

            ImGui.Spacing();

            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("Bank Method:");
            ImGui.PopStyleColor();
            ImGui.SameLine(labelW);
            ImGui.SetNextItemWidth(160f * s);
            if (ImGui.Combo("##bank_method", ref _bankMethodIdx, BankMethodOptions, BankMethodOptions.Length))
                Plugin.SetBankLootMethod(BankMethodOptions[_bankMethodIdx]);

            ImGui.Spacing();

            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("Page Mode:");
            ImGui.PopStyleColor();
            ImGui.SameLine(labelW);
            ImGui.SetNextItemWidth(160f * s);
            if (ImGui.Combo("##bank_pagemode", ref _bankPageModeIdx, BankPageOptions, BankPageOptions.Length))
                Plugin.SetBankLootPageMode(BankPageOptions[_bankPageModeIdx]);

            bool pageRange = Plugin.GetBankLootPageMode() == "Page Range";
            if (pageRange)
            {
                ImGui.Spacing();

                ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
                ImGui.TextUnformatted("First Page:");
                ImGui.PopStyleColor();
                ImGui.SameLine(labelW);
                ImGui.SetNextItemWidth(-60f * s);
                if (ImGui.SliderFloat("##bank_pfirst", ref _bankPageFirst, 1f, 98f, "%.0f"))
                    Plugin.SetBankPageFirst((int)_bankPageFirst);
                ImGui.SameLine();
                ImGui.TextUnformatted(((int)_bankPageFirst).ToString());

                ImGui.Spacing();

                ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
                ImGui.TextUnformatted("Last Page:");
                ImGui.PopStyleColor();
                ImGui.SameLine(labelW);
                ImGui.SetNextItemWidth(-60f * s);
                if (ImGui.SliderFloat("##bank_plast", ref _bankPageLast, 1f, 98f, "%.0f"))
                    Plugin.SetBankPageLast((int)_bankPageLast);
                ImGui.SameLine();
                ImGui.TextUnformatted(((int)_bankPageLast).ToString());
            }

            if (!bankOn) ImGui.EndDisabled();
        }

        // ── Auction loot ──────────────────────────────────────────────────────

        private void DrawAuctionLootSection(float s)
        {
            LootManagerWindow.SectionHeader("Auction Loot");

            bool auctionOn = Plugin.GetAuctionLootEnabled();
            if (ImGui.Checkbox("Enable Auction Loot##auction_en", ref auctionOn))
            {
                Plugin.SetAuctionLootEnabled(auctionOn);
                _onVisibilityChanged?.Invoke();
            }
        }

        // ── Chat output ───────────────────────────────────────────────────────

        private void DrawChatOutputSection(float s)
        {
            LootManagerWindow.SectionHeader("Chat Output");

            float labelW = 80f * s;

            bool chatOn = Plugin.GetChatOutputEnabled();
            if (ImGui.Checkbox("Enable Chat Output##chat_en", ref chatOn))
            {
                Plugin.SetChatOutputEnabled(chatOn);
            }

            if (!chatOn) ImGui.BeginDisabled();

            ImGui.Spacing();

            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("Window:");
            ImGui.PopStyleColor();
            ImGui.SameLine(labelW);
            ImGui.SetNextItemWidth(-1f);
            if (_chatWindowNames.Count > 0)
            {
                if (ImGui.Combo("##chat_win", ref _chatWindowIdx, _chatWindowNames.ToArray(), _chatWindowNames.Count))
                {
                    if (_chatWindowIdx < _chatWindows.Count)
                    {
                        Plugin.SetChatOutputWindow(_chatWindows[_chatWindowIdx].WindowName);
                        RefreshChatTabs(_chatWindows[_chatWindowIdx]);
                        ChatFilterInjector.ApplyChatMask();
                        
                    }
                }
            }
            else
            {
                ImGui.TextDisabled("(no chat windows found)");
            }

            ImGui.Spacing();

            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("Tab:");
            ImGui.PopStyleColor();
            ImGui.SameLine(labelW);
            ImGui.SetNextItemWidth(-1f);
            if (_chatTabNames.Count > 0)
            {
                if (ImGui.Combo("##chat_tab", ref _chatTabIdx, _chatTabNames.ToArray(), _chatTabNames.Count))
                {
                    Plugin.SetChatOutputTab(_chatTabIdx);
                    ChatFilterInjector.ApplyChatMask();
                    
                }
            }
            else
            {
                ImGui.TextDisabled("(no tabs)");
            }

            if (!chatOn) ImGui.EndDisabled();
        }

        // ── Sync helpers ──────────────────────────────────────────────────────

        private void SyncFromPlugin()
        {
            _autoDistance   = Plugin.GetAutoLootDistance();
            _autoDelay      = UnityEngine.Mathf.Clamp(Plugin.GetAutoLootDelay(), 0.5f, 10f);
            _bankPageFirst  = Plugin.GetBankPageFirst();
            _bankPageLast   = Plugin.GetBankPageLast();

            _lootMethodIdx  = IndexOf(LootMethodOptions, Plugin.GetLootMethod());
            _bankMethodIdx  = IndexOf(BankMethodOptions,  Plugin.GetBankLootMethod());
            _bankPageModeIdx = IndexOf(BankPageOptions,   Plugin.GetBankLootPageMode());
        }

        private void RefreshChatWindows()
        {
            _chatWindows.Clear();
            _chatWindowNames.Clear();

            foreach (var win in UpdateSocialLog.ChatWindows)
            {
                _chatWindows.Add(win);
                _chatWindowNames.Add(string.IsNullOrEmpty(win.WindowName) ? "(unnamed)" : win.WindowName);
            }

            _chatWindowIdx = 0;
            for (int i = 0; i < _chatWindows.Count; i++)
            {
                if (_chatWindows[i].WindowName == Plugin.GetChatOutputWindow())
                {
                    _chatWindowIdx = i;
                    break;
                }
            }

            if (_chatWindowIdx < _chatWindows.Count)
                RefreshChatTabs(_chatWindows[_chatWindowIdx]);
        }

        private void RefreshChatTabs(IDLog win)
        {
            _chatTabNames.Clear();
            if (win == null) return;

            int count = UnityEngine.Mathf.Clamp(win.activeTabs, 1, win.TabDisplayName.Length);
            for (int i = 0; i < count; i++)
            {
                string name = win.TabDisplayName[i];
                _chatTabNames.Add(string.IsNullOrEmpty(name) ? $"Tab {i + 1}" : name);
            }

            _chatTabIdx = UnityEngine.Mathf.Clamp(Plugin.GetChatOutputTab(), 0, _chatTabNames.Count - 1);
        }

        private static int IndexOf(string[] arr, string value)
        {
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] == value) return i;
            return 0;
        }
    }
}