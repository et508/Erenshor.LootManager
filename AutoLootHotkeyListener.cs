using UnityEngine;

namespace LootManager
{
    public sealed class AutoLootHotkeyListener : MonoBehaviour
    {
        private void Update()
        {
            if (Plugin.ToggleAutoLootHotkey == null) return;
            if (GameData.PlayerTyping) return;

            if (Plugin.ToggleAutoLootHotkey.Value.IsDown())
            {
                bool newValue = !Plugin.AutoLootEnabled.Value;
                Plugin.AutoLootEnabled.Value = newValue;

                ChatFilterInjector.SendLootMessage(
                    newValue ? "[Loot Manager] Autoloot ON" : "[Loot Manager] Autoloot OFF",
                    newValue ? "green" : "red");
            }
        }
    }
}