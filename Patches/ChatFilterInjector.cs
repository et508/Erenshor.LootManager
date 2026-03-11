// ChatFilterInjector.cs
// Sends Loot Manager messages to a specific chat window and tab chosen
// by the player in the Settings panel. No longer injects into AdjustWindowFilters.

using UnityEngine;

namespace LootManager
{
    public static class ChatFilterInjector
    {
        public const ChatLogLine.LogType LootManagerLogType = (ChatLogLine.LogType)8388608;

        // Applies LootManagerLogType to the player's chosen window+tab so
        // messages appear there. Called by SettingsPanelController when the
        // selection changes, and once on scene load.
        public static void ApplyChatMask()
        {
            foreach (var win in UpdateSocialLog.ChatWindows)
            {
                for (int t = 0; t < win.FilterMasks.Length; t++)
                    win.FilterMasks[t] &= ~LootManagerLogType;
            }

            var target = GetTargetWindow();
            if (target == null) return;

            int tab = Mathf.Clamp(Plugin.ChatOutputTab.Value, 0, target.activeTabs - 1);
            target.FilterMasks[tab] |= LootManagerLogType;
        }

        public static void SendLootMessage(string message, string color = "orange")
        {
            if (Plugin.ChatOutputEnabled?.Value == false) return;
            UpdateSocialLog.LogAdd(new ChatLogLine(message, LootManagerLogType, color));
        }

        public static IDLog GetTargetWindow()
        {
            string name = Plugin.ChatOutputWindow.Value;
            foreach (var win in UpdateSocialLog.ChatWindows)
            {
                if (win.WindowName == name) return win;
            }
            // Fall back to first registered window
            if (UpdateSocialLog.ChatWindows.Count > 0)
                return UpdateSocialLog.ChatWindows[0];
            return null;
        }
    }
}