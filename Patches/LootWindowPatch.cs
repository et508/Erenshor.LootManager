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
            string lootMethod     = Plugin.LootMethod.Value;
            bool bankLootEnabled  = Plugin.BankLootEnabled.Value;
            string bankLootMethod = Plugin.BankLootMethod.Value;

            var lootedForBank = new List<BankLoot.LootEntry>();

            foreach (ItemIcon slot in __instance.LootSlots)
            {
                var item = slot.MyItem;
                int qty  = slot.Quantity;

                if (item == GameData.PlayerInv.Empty)
                    continue;

                string name = item.ItemName;

                // Apply loot method filtering
                if (lootMethod == "Blacklist" && Plugin.Blacklist.Contains(name))
                {
                    UpdateSocialLog.LogAdd($"[Loot Manager] Destroyed \"{name}\"", "grey");
                    slot.MyItem   = GameData.PlayerInv.Empty;
                    slot.Quantity = 1;
                    slot.UpdateSlotImage();
                    continue;
                }

                if (lootMethod == "Whitelist" && !Plugin.Whitelist.Contains(name))
                {
                    UpdateSocialLog.LogAdd($"[Loot Manager] Destroyed \"{name}\"", "grey");
                    slot.MyItem   = GameData.PlayerInv.Empty;
                    slot.Quantity = 1;
                    slot.UpdateSlotImage();
                    continue;
                }

                // Decide whether to send item to bank
                bool sendToBank = false;
                if (bankLootEnabled)
                {
                    if (bankLootMethod == "All")
                    {
                        sendToBank = true;
                    }
                    else if (bankLootMethod == "Filtered" && Plugin.Banklist.Contains(name))
                    {
                        sendToBank = true;
                    }
                }

                if (sendToBank)
                {
                    lootedForBank.Add(new BankLoot.LootEntry(item.Id, qty, name));
                    // UpdateSocialLog.LogAdd($"[Loot Manager] Queued \"{name}\" for bank deposit", "grey");
                }
                else
                {
                    bool added = item.RequiredSlot == Item.SlotType.General
                        ? GameData.PlayerInv.AddItemToInv(item)
                        : GameData.PlayerInv.AddItemToInv(item, qty);

                    if (added)
                    {
                        UpdateSocialLog.LogAdd($"[Loot Manager] Looted \"{name}\" to inventory", "yellow");
                        slot.InformGroupOfLoot(item);
                    }
                    else
                    {
                        UpdateSocialLog.LogAdd($"[Loot Manager] No room for \"{name}\"", "yellow");
                        continue;
                    }
                }

                // Always clear the loot slot
                slot.MyItem   = GameData.PlayerInv.Empty;
                slot.Quantity = 1;
                slot.UpdateSlotImage();
            }

            // Deposit bank-queued items
            if (lootedForBank.Count > 0)
            {
                BankLoot.DepositLoot(lootedForBank);
            }

            GameData.PlayerAud.PlayOneShot(
                GameData.GM.GetComponent<Misc>().DropItem,
                GameData.PlayerAud.volume / 2f * GameData.SFXVol
            );
            __instance.CloseWindow();

            return false;
        }
    }
}
