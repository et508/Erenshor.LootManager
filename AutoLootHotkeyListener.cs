using UnityEngine;

namespace LootManager
{
    public sealed class AutoLootHotkeyListener : MonoBehaviour
    {
        private void Update()
        {
            if (GameData.PlayerTyping) return;

            if (Input.GetKeyDown(Plugin.GetToggleAutoLootHotkey()))
            {
                bool newValue = !Plugin.GetAutoLootEnabled();
                Plugin.SetAutoLootEnabled(newValue);

                ChatFilterInjector.SendLootMessage(
                    newValue ? "[Loot Manager] Autoloot ON" : "[Loot Manager] Autoloot OFF",
                    newValue ? "green" : "red");
            }
        }
    }
}