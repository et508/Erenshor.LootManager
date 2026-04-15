using System.Collections.Generic;
using BepInEx.Configuration;
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
        private static readonly string[] EquipTierOptions   = { "All", "Normal Only", "Blessed Only", "Godly Only", "Blessed and Up" };

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
            DrawHotkeyButton("##hk_ui", Plugin.ToggleLootUIHotkey, ref _bindingUIHotkey, btnW);

            ImGui.Spacing();

            // Autoloot hotkey
            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("Autoloot Hotkey:");
            ImGui.PopStyleColor();
            ImGui.SameLine(labelW);
            DrawHotkeyButton("##hk_auto", Plugin.ToggleAutoLootHotkey, ref _bindingAutoHotkey, btnW);
        }

        private void DrawHotkeyButton(string id, ConfigEntry<KeyboardShortcut> entry,
            ref bool binding, float width)
        {
            if (binding)
            {
                // Listening for input
                ImGui.PushStyleColor(ImGuiCol.Button,        LootManagerWindow.V4AccentBlue);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, LootManagerWindow.V4AccentBlue);
                if (ImGui.Button("Press a key..." + id, new Vector2(width, 0f)))
                    binding = false; // click again to cancel
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
                            entry.Value = new KeyboardShortcut(kc);
                            entry.ConfigFile.Save();
                            binding = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                string label = entry.Value.MainKey == KeyCode.None
                    ? "(none)" : entry.Value.MainKey.ToString();
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
            bool autoEnabled = Plugin.AutoLootEnabled.Value;
            if (ImGui.Checkbox("Enable Autoloot##auto_en", ref autoEnabled))
            {
                Plugin.AutoLootEnabled.Value = autoEnabled;
            }

            ImGui.Spacing();

            // Distance slider
            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("Autoloot Distance:");
            ImGui.PopStyleColor();
            ImGui.SameLine(labelW);
            ImGui.SetNextItemWidth(-60f * s);
            if (ImGui.SliderFloat("##auto_dist", ref _autoDistance, 0f, 200f, "%.0f"))
                Plugin.AutoLootDistance.Value = _autoDistance;
            ImGui.SameLine();
            ImGui.TextUnformatted(((int)_autoDistance).ToString());

            ImGui.Spacing();

            // Delay toggle
            bool delayEnabled = Plugin.AutoLootDelayEnabled.Value;
            if (ImGui.Checkbox("Out-of-Combat Delay##delay_en", ref delayEnabled))
            {
                Plugin.AutoLootDelayEnabled.Value = delayEnabled;
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
                    Plugin.AutoLootDelay.Value = _autoDelay;
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
                Plugin.LootMethod.Value = LootMethodOptions[_lootMethodIdx];
                _onVisibilityChanged?.Invoke();
            }
        }

        // ── Bank loot ─────────────────────────────────────────────────────────

        private void DrawFishingSection(float s)
        {
            LootManagerWindow.SectionHeader("Fishing & Mining");

            bool fishOn = Plugin.FishingFilterEnabled.Value;
            if (ImGui.Checkbox("Apply Loot Filters to Fishing##fish_en", ref fishOn))
                Plugin.FishingFilterEnabled.Value = fishOn;

            bool mineOn = Plugin.MiningFilterEnabled.Value;
            if (ImGui.Checkbox("Apply Loot Filters to Mining##mine_en", ref mineOn))
                Plugin.MiningFilterEnabled.Value = mineOn;
        }

        private void DrawBankLootSection(float s)
        {
            LootManagerWindow.SectionHeader("Bank Loot");

            float labelW  = 120f * s;
            bool bankOn   = Plugin.BankLootEnabled.Value;

            if (ImGui.Checkbox("Enable Bank Loot##bank_en", ref bankOn))
            {
                Plugin.BankLootEnabled.Value = bankOn;
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
                Plugin.BankLootMethod.Value = BankMethodOptions[_bankMethodIdx];

            ImGui.Spacing();

            ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
            ImGui.TextUnformatted("Page Mode:");
            ImGui.PopStyleColor();
            ImGui.SameLine(labelW);
            ImGui.SetNextItemWidth(160f * s);
            if (ImGui.Combo("##bank_pagemode", ref _bankPageModeIdx, BankPageOptions, BankPageOptions.Length))
                Plugin.BankLootPageMode.Value = BankPageOptions[_bankPageModeIdx];

            bool pageRange = Plugin.BankLootPageMode.Value == "Page Range";
            if (pageRange)
            {
                ImGui.Spacing();

                ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
                ImGui.TextUnformatted("First Page:");
                ImGui.PopStyleColor();
                ImGui.SameLine(labelW);
                ImGui.SetNextItemWidth(-60f * s);
                if (ImGui.SliderFloat("##bank_pfirst", ref _bankPageFirst, 1f, 98f, "%.0f"))
                    Plugin.BankPageFirst.Value = (int)_bankPageFirst;
                ImGui.SameLine();
                ImGui.TextUnformatted(((int)_bankPageFirst).ToString());

                ImGui.Spacing();

                ImGui.PushStyleColor(ImGuiCol.Text, LootManagerWindow.V4TextMuted);
                ImGui.TextUnformatted("Last Page:");
                ImGui.PopStyleColor();
                ImGui.SameLine(labelW);
                ImGui.SetNextItemWidth(-60f * s);
                if (ImGui.SliderFloat("##bank_plast", ref _bankPageLast, 1f, 98f, "%.0f"))
                    Plugin.BankPageLast.Value = (int)_bankPageLast;
                ImGui.SameLine();
                ImGui.TextUnformatted(((int)_bankPageLast).ToString());
            }

            if (!bankOn) ImGui.EndDisabled();
        }

        // ── Chat output ───────────────────────────────────────────────────────

        private void DrawChatOutputSection(float s)
        {
            LootManagerWindow.SectionHeader("Chat Output");

            float labelW = 80f * s;

            bool chatOn = Plugin.ChatOutputEnabled.Value;
            if (ImGui.Checkbox("Enable Chat Output##chat_en", ref chatOn))
            {
                Plugin.ChatOutputEnabled.Value = chatOn;
                Plugin.ChatOutputEnabled.ConfigFile.Save();
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
                        Plugin.ChatOutputWindow.Value = _chatWindows[_chatWindowIdx].WindowName;
                        RefreshChatTabs(_chatWindows[_chatWindowIdx]);
                        ChatFilterInjector.ApplyChatMask();
                        Plugin.ChatOutputWindow.ConfigFile.Save();
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
                    Plugin.ChatOutputTab.Value = _chatTabIdx;
                    ChatFilterInjector.ApplyChatMask();
                    Plugin.ChatOutputTab.ConfigFile.Save();
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
            _autoDistance   = Plugin.AutoLootDistance.Value;
            _autoDelay      = UnityEngine.Mathf.Clamp(Plugin.AutoLootDelay.Value, 0.5f, 10f);
            _bankPageFirst  = Plugin.BankPageFirst.Value;
            _bankPageLast   = Plugin.BankPageLast.Value;

            _lootMethodIdx  = IndexOf(LootMethodOptions, Plugin.LootMethod.Value);
            _bankMethodIdx  = IndexOf(BankMethodOptions,  Plugin.BankLootMethod.Value);
            _bankPageModeIdx = IndexOf(BankPageOptions,   Plugin.BankLootPageMode.Value);
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
                if (_chatWindows[i].WindowName == Plugin.ChatOutputWindow.Value)
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

            _chatTabIdx = UnityEngine.Mathf.Clamp(Plugin.ChatOutputTab.Value, 0, _chatTabNames.Count - 1);
        }

        private static int IndexOf(string[] arr, string value)
        {
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] == value) return i;
            return 0;
        }
    }
}