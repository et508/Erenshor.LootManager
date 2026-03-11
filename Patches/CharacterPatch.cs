using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootManager
{
    [HarmonyPatch(typeof(Character), "DoDeath")]
    public class Autoloot
    {

        private static readonly List<PendingLoot> _pending = new List<PendingLoot>();
        private static bool _pollRunning = false;

        private struct PendingLoot
        {
            public NPC     Npc;
            public LootTable Table;
        }

        private static void Postfix(Character __instance)
        {
            if (!Plugin.AutoLootEnabled.Value)
                return;
            
            if (__instance == null || !__instance.isNPC || __instance.MyNPC == null)
                return;

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

            if (Plugin.AutoLootDelayEnabled.Value)
            {

                _pending.Add(new PendingLoot { Npc = __instance.MyNPC, Table = lootTable });
                if (!_pollRunning)
                    Plugin.Instance.StartCoroutine(PollOutOfCombat());
            }
            else
            {
                ChatFilterInjector.SendLootMessage(
                    "[Loot Manager] Looting NPC: " + __instance.MyNPC.name, "yellow");
                lootTable.LoadLootTable();
                GameData.LootWindow.LootAll();
            }
        }

        private static bool IsOutOfCombat()
        {
            if (GameData.AttackingPlayer != null && GameData.AttackingPlayer.Count > 0) return false;
            if (GameData.GroupMatesInCombat != null && GameData.GroupMatesInCombat.Count > 0) return false;
            return true;
        }

        private static IEnumerator PollOutOfCombat()
        {
            _pollRunning = true;

            while (!IsOutOfCombat())
                yield return new WaitForSeconds(0.25f);

            float grace = Mathf.Clamp(Plugin.AutoLootDelay.Value, 0f, 10f);
            if (grace > 0f)
                yield return new WaitForSeconds(grace);

            var toProcess = new List<PendingLoot>(_pending);
            _pending.Clear();
            _pollRunning = false;

            if (!Plugin.AutoLootEnabled.Value) yield break;

            var player = GameData.PlayerControl?.Myself;
            if (player == null || !player.Alive) yield break;

            foreach (var entry in toProcess)
            {
                if (entry.Npc == null || entry.Table == null) continue;

                float dist = Vector3.Distance(
                    player.transform.position,
                    entry.Npc.transform.position
                );
                if (dist >= Plugin.AutoLootDistance.Value) continue;

                ChatFilterInjector.SendLootMessage(
                    "[Loot Manager] Looting NPC: " + entry.Npc.name, "yellow");
                entry.Table.LoadLootTable();
                GameData.LootWindow.LootAll();
            }
        }
    }
}