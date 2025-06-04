using HarmonyLib;
using UnityEngine;

namespace LootManager
{
    [HarmonyPatch(typeof(Character), "DoDeath")]
    [HarmonyPriority(50000)]
    [HarmonyAfter("Brumdail.ErenshorREL")]
    public class Autoloot
    {
        private static void Postfix(Character __instance)
        {
            if (!Plugin.AutoLootEnabled.Value)
                return;
            
            if (__instance == null || !__instance.isNPC || __instance.MyNPC == null)
                return;

            var playerChar = GameData.PlayerControl.Myself;
            if (playerChar == null || !playerChar.Alive)
                return;

            float autoLootRange = Plugin.AutoLootDistance.Value;
            float dist = Vector3.Distance(
                playerChar.transform.position,
                __instance.MyNPC.transform.position
            );
            if (dist >= autoLootRange)
                return;

            LootTable lootTable = __instance.MyNPC.GetComponent<LootTable>();
            if (lootTable != null)
            {
                UpdateSocialLog.LogAdd(
                    "[Loot Manager] Looting NPC: " + __instance.MyNPC.name,
                    "yellow"
                );
                lootTable.LoadLootTable();
                GameData.LootWindow.LootAll();
            }
        }
    }
}