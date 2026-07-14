using UnityEngine;

namespace LootManager
{
    public static class ChatFilterInjector
    {
        // Custom log type bit — must be injected into each window's FilterMasks
        // for the target tab so GetMatchingTabs routes our messages there.
        public const ChatLogLine.LogType LootManagerLogType = (ChatLogLine.LogType)8388608;

        private static bool _maskApplied;

        /// <summary>
        /// Injects our custom LogType bit into the target window's target tab FilterMask.
        /// Called on scene load and when the user changes chat output settings.
        /// Safe to call multiple times.
        /// </summary>
        public static void ApplyChatMask()
        {
            if (UpdateSocialLog.ChatWindows.Count == 0)
            {
                _maskApplied = false;
                return;
            }

            // Clear our bit from all windows/tabs first
            foreach (var win in UpdateSocialLog.ChatWindows)
            {
                for (int t = 0; t < win.FilterMasks.Length; t++)
                    win.FilterMasks[t] &= ~LootManagerLogType;
            }

            var target = GetTargetWindow();
            if (target == null) return;

            int tab = Mathf.Clamp(Plugin.GetChatOutputTab(), 0, target.activeTabs - 1);
            target.FilterMasks[tab] |= LootManagerLogType;

            _maskApplied = true;
        }

        /// <summary>
        /// Send a message to the configured chat window/tab.
        /// Lazily applies the mask if it hasn't been applied yet.
        /// </summary>
        public static void SendLootMessage(string message, string color = "orange")
        {
            if (!Plugin.GetChatOutputEnabled()) return;

            // Lazily apply mask if windows weren't ready at startup
            if (!_maskApplied)
                ApplyChatMask();

            // If still no windows, fall back to ForcedLineNoFilter so the
            // message appears somewhere rather than being silently dropped.
            if (!_maskApplied)
            {
                UpdateSocialLog.LogAdd(
                    new ChatLogLine(message, ChatLogLine.LogType.ForcedLineNoFilter, color));
                return;
            }

            UpdateSocialLog.LogAdd(new ChatLogLine(message, LootManagerLogType, color));
        }

        public static IDLog GetTargetWindow()
        {
            string name = Plugin.GetChatOutputWindow();
            foreach (var win in UpdateSocialLog.ChatWindows)
            {
                if (win.WindowName == name) return win;
            }

            // Fallback: first registered window
            if (UpdateSocialLog.ChatWindows.Count > 0)
                return UpdateSocialLog.ChatWindows[0];
            return null;
        }
    }
}