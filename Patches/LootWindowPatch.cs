using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace LootManager
{
    [HarmonyPatch(typeof(LootWindow), nameof(LootWindow.LootAll))]
    [HarmonyPriority(Priority.First)]
    public static class LootWindowPatch
    {
        public static bool Prefix(LootWindow __instance)
        {
            // 1) Collect looted items for deposit
            var lootedEntries = new List<BankLoot.LootEntry>();

            foreach (ItemIcon slot in __instance.LootSlots)
            {
                var item = slot.MyItem;
                int qty  = slot.Quantity;

                if (item == GameData.PlayerInv.Empty)
                    continue;

                var name = item.ItemName;

                // Blacklist check
                if (Plugin.Blacklist.Contains(name))
                {
                    UpdateSocialLog.LogAdd($"[Loot Manager] Destroyed \"{name}\"", "grey");
                }
                else
                {
                    // Queue for bank deposit
                    lootedEntries.Add(new BankLoot.LootEntry(item.Id, qty, name));
                    UpdateSocialLog.LogAdd($"[Loot Manager] Queued \"{name}\" for bank deposit", "grey");
                }

                // Clear loot slot
                slot.MyItem   = GameData.PlayerInv.Empty;
                slot.Quantity = 1;
                slot.UpdateSlotImage();
            }

            // 2) Deposit into bank JSON
            if (lootedEntries.Count > 0)
            {
                BankLoot.DepositLoot(lootedEntries, startPage: 50, endPage: 70);

            }

            // 3) Play sound & close window
            GameData.PlayerAud.PlayOneShot(
                GameData.GM.GetComponent<Misc>().DropItem,
                GameData.PlayerAud.volume / 2f * GameData.SFXVol
            );
            __instance.CloseWindow();

            return false;  // skip original LootAll
        }
    }
}