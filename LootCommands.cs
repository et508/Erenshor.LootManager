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

            if (command.Equals("/lootui", StringComparison.OrdinalIgnoreCase))
            {
                LootManagerUI.Instance?.ToggleUI();
                ClearInput();
                return false;
            }

            return true; // continue with default behavior
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