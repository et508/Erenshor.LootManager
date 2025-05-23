using System;
using HarmonyLib;

namespace LootManager
{
    [HarmonyPatch(typeof(TypeText), "CheckCommands")]
    public static class LootCommands
    {
        public static bool Prefix()
        {
            string command = GameData.TextInput.typed.text;
            if (string.IsNullOrWhiteSpace(command))
                return true;

            if (command.StartsWith("/addloot ", StringComparison.OrdinalIgnoreCase))
            {
                string itemName = command.Substring(9).Trim();
                if (!string.IsNullOrEmpty(itemName))
                {
                    Plugin.Blacklist.Add(itemName);
                    LootBlacklist.SaveBlacklist();
                    UpdateSocialLog.LogAdd("[Loot Manager] Added to blacklist: " + itemName, "yellow");
                }
                ClearInput();
                return false;
            }

            if (command.StartsWith("/removeloot ", StringComparison.OrdinalIgnoreCase))
            {
                string itemName = command.Substring(12).Trim();
                if (Plugin.Blacklist.Remove(itemName))
                {
                    LootBlacklist.SaveBlacklist();
                    UpdateSocialLog.LogAdd("[Loot Manager] Removed from blacklist: " + itemName, "yellow");
                }
                else
                {
                    UpdateSocialLog.LogAdd("[Loot Manager] Item not found in blacklist: " + itemName, "yellow");
                }
                ClearInput();
                return false;
            }
            
            if (command.Equals("/showloot", StringComparison.OrdinalIgnoreCase))
            {
                UpdateSocialLog.LogAdd("[Loot Manager] Blacklisted items:", "yellow");
                foreach (var item in Plugin.Blacklist)
                {
                    UpdateSocialLog.LogAdd(" - " + item, "orange");
                }
                ClearInput();
                return false;
            }

            return true;
        }

        private static void ClearInput()
        {
            GameData.TextInput.typed.text = "";
            GameData.TextInput.CDFrames = 10f;
            GameData.TextInput.InputBox.SetActive(false);
            GameData.PlayerTyping = false;
        }
    }
}