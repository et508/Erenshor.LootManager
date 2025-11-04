using UnityEngine;

namespace LootManager
{
    public sealed class AutoLootHotkeyListener : MonoBehaviour
    {
        private void Update()
        {
            if (Plugin.ToggleAutoLootHotkey == null) return;
            
            if (GameData.PlayerTyping || TypingInputMute.IsAnyActive) return;

            if (Plugin.ToggleAutoLootHotkey.Value.IsDown())
            {
                bool newValue = !Plugin.AutoLootEnabled.Value;
                SettingsPanelController.ApplyAutoLootFromExternal(newValue);
                
                UpdateSocialLog.LogAdd(
                    newValue ? "[Loot Manager] Autoloot ON" : "[Loot Manager] Autoloot OFF",
                    newValue ? "green" : "red"
                );
            }
        }
    }
}