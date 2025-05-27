// LootWindowPatch.cs
using HarmonyLib;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace LootManager
{
    [HarmonyPatch(typeof(LootWindow), nameof(LootWindow.LootAll))]
    [HarmonyPriority(Priority.First)]
    public static class LootWindowPatch
    {
        public static bool Prefix(LootWindow __instance)
        {
            // ─── Prepare bank JSON data ───
            string saveDir    = Path.Combine(Application.persistentDataPath, "ESSaveData");
            string newPath    = Path.Combine(saveDir, "GBDATA");
            string legacyPath = Application.persistentDataPath + "GBDATA";

            BankSaveData data;
            bool bankAvailable = true;

            if (File.Exists(newPath))
            {
                data = JsonUtility.FromJson<BankSaveData>(File.ReadAllText(newPath));
            }
            else if (File.Exists(legacyPath))
            {
                data = JsonUtility.FromJson<BankSaveData>(File.ReadAllText(legacyPath));
            }
            else
            {
                // initialize empty bank
                data = new BankSaveData {
                    BankData  = new List<string>(),
                    BankCount = new List<int>()
                };
                for (int i = 0; i < 3168; i++)
                {
                    data.BankData.Add("");
                    data.BankCount.Add(1);
                }
            }

            bool anyDeposited = false;

            // ─── Process each loot slot ───
            foreach (ItemIcon slot in __instance.LootSlots)
            {
                var item = slot.MyItem;
                int qty   = slot.Quantity;

                if (item == GameData.PlayerInv.Empty)
                    continue;

                string name = item.ItemName;

                // 1) Blacklist?
                if (Plugin.Blacklist.Contains(name))
                {
                    UpdateSocialLog.LogAdd($"[Loot Manager] Destroyed \"{name}\"", "grey");
                }
                else if (bankAvailable)
                {
                    bool placed = false;

                    // 2a) Try stacking in bank
                    if (item.Stackable)
                    {
                        for (int i = 0; i < data.BankData.Count; i++)
                        {
                            if (data.BankData[i] == item.Id)
                            {
                                data.BankCount[i] += qty;
                                placed = true;
                                break;
                            }
                        }
                    }

                    // 2b) If not stacked, place in first empty
                    if (!placed)
                    {
                        for (int i = 0; i < data.BankData.Count; i++)
                        {
                            if (string.IsNullOrEmpty(data.BankData[i]))
                            {
                                data.BankData[i]  = item.Id;
                                data.BankCount[i] = qty;
                                placed = true;
                                break;
                            }
                        }
                    }

                    if (placed)
                    {
                        anyDeposited = true;
                        UpdateSocialLog.LogAdd($"[Loot Manager] Deposited \"{name}\" into bank", "grey");
                    }
                    else
                    {
                        // bank full → fallback to inventory
                        bool added = item.RequiredSlot == Item.SlotType.General
                                     ? GameData.PlayerInv.AddItemToInv(item)
                                     : GameData.PlayerInv.AddItemToInv(item, qty);

                        if (added)
                            UpdateSocialLog.LogAdd($"[Loot Manager] Looted \"{name}\" to inventory", "yellow");
                        else
                            UpdateSocialLog.LogAdd($"[Loot Manager] No room for \"{name}\"", "yellow");
                    }
                }
                else
                {
                    // no bank → normal loot
                    bool added = item.RequiredSlot == Item.SlotType.General
                                 ? GameData.PlayerInv.AddItemToInv(item)
                                 : GameData.PlayerInv.AddItemToInv(item, qty);

                    if (added)
                        UpdateSocialLog.LogAdd($"[Loot Manager] Looted \"{name}\" to inventory", "yellow");
                    else
                        UpdateSocialLog.LogAdd($"[Loot Manager] No room for \"{name}\"", "yellow");
                }

                // 3) Clear the loot slot
                slot.MyItem   = GameData.PlayerInv.Empty;
                slot.Quantity = 1;
                slot.UpdateSlotImage();
            }

            // ─── Save bank if we added anything ───
            if (anyDeposited)
            {
                Directory.CreateDirectory(saveDir);
                File.WriteAllText(newPath, JsonUtility.ToJson(data));
            }

            // ─── Play loot sound & close window ───
            GameData.PlayerAud.PlayOneShot(
                GameData.GM.GetComponent<Misc>().DropItem,
                GameData.PlayerAud.volume / 2f * GameData.SFXVol
            );
            __instance.CloseWindow();

            return false;  // skip original LootAll
        }
    }
}
