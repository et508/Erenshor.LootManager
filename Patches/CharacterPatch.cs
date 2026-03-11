using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace LootManager
{
    [HarmonyPatch(typeof(Character), "DoDeath")]
    public class Autoloot
    {
        private static void Postfix(Character __instance)
        {
            if (!Plugin.AutoLootEnabled.Value)
                return;
            
            if (__instance == null || !__instance.isNPC || __instance.MyNPC == null)
                return;

            // Respect vanilla: if the game would destroy/rot this NPC on death, don't loot it.
            // (Vanilla: if (this.DestroyOnDeath && GetComponent<NPC>() != null) NPC.ExpediteRot(50f); )
            // We mirror that check here to preserve the intended “no loot” behavior.
            var npcComp = __instance.GetComponent<NPC>();
            if (__instance.DestroyOnDeath && npcComp != null)
            {
                ChatFilterInjector.SendLootMessage("[Loot Manager] Skipping autoloot (DestroyOnDeath).", "yellow");
                return;
            }
            
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
            if (lootTable == null) return;

            if (Plugin.AutoLootDelayEnabled.Value && Plugin.AutoLootDelay.Value > 0f)
            {
                var npc = __instance.MyNPC;
                Plugin.Instance.StartCoroutine(DelayedLoot(npc, lootTable));
            }
            else
            {
                ChatFilterInjector.SendLootMessage(
                    "[Loot Manager] Looting NPC: " + __instance.MyNPC.name, "yellow");
                lootTable.LoadLootTable();
                GameData.LootWindow.LootAll();
            }
        }

        private static IEnumerator DelayedLoot(NPC npc, LootTable lootTable)
        {
            float delay = Mathf.Clamp(Plugin.AutoLootDelay.Value, 0.5f, 10f);
            yield return new WaitForSeconds(delay);

            // Re-validate after delay — player or NPC may have moved/died
            if (!Plugin.AutoLootEnabled.Value) yield break;
            if (npc == null || GameData.PlayerControl?.Myself == null) yield break;
            if (!GameData.PlayerControl.Myself.Alive) yield break;

            float dist = Vector3.Distance(
                GameData.PlayerControl.Myself.transform.position,
                npc.transform.position
            );
            if (dist >= Plugin.AutoLootDistance.Value) yield break;

            ChatFilterInjector.SendLootMessage(
                "[Loot Manager] Looting NPC: " + npc.name, "yellow");
            lootTable.LoadLootTable();
            GameData.LootWindow.LootAll();
        }
    }
}