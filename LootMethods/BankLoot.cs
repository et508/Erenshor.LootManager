using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace LootManager
{
    public static class BankLoot
    {
        // Represents a single item to deposit
        public class LootEntry
        {
            public string Id { get; }
            public int Quantity { get; }
            public string Name { get; }

            public LootEntry(string id, int quantity, string name)
            {
                Id = id;
                Quantity = quantity;
                Name = name;
            }
        }

        public static void DepositLoot(IEnumerable<LootEntry> entries)
        {
            string saveDir    = Path.Combine(Application.persistentDataPath, "ESSaveData");
            string newPath    = Path.Combine(saveDir, "GBDATA");
            string legacyPath = Application.persistentDataPath + "GBDATA";

            // Load or initialize bank data
            BankSaveData data;
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
                const int initialSlotCount = 3168;
                data = new BankSaveData
                {
                    BankData  = Enumerable.Repeat(string.Empty, initialSlotCount).ToList(),
                    BankCount = Enumerable.Repeat(1, initialSlotCount).ToList()
                };
            }

            int totalSlots   = data.BankData.Count;
            int slotsPerPage = 32;
            int maxPage      = Mathf.CeilToInt(totalSlots / (float)slotsPerPage);

            string method    = Plugin.BankLootPageMode.Value;
            int startPage    = Mathf.Clamp(Plugin.BankPageFirst.Value, 1, maxPage);
            int endPage      = Mathf.Clamp(Plugin.BankPageLast.Value, 1, maxPage);
            int startIdx     = (startPage - 1) * slotsPerPage;
            int endIdx       = endPage * slotsPerPage;

            bool anyDeposited = false;

            foreach (var entry in entries)
            {
                bool placed = false;

                // 1) Stack into any existing matching stack (all pages)
                if (GameData.ItemDB.GetItemByID(entry.Id)?.Stackable == true)
                {
                    for (int i = 0; i < totalSlots; i++)
                    {
                        if (data.BankData[i] == entry.Id)
                        {
                            data.BankCount[i] += entry.Quantity;
                            placed = true;
                            break;
                        }
                    }
                }

                // 2) If not stacked, find first empty slot based on method
                if (!placed)
                {
                    int searchStart = 0;
                    int searchEnd   = totalSlots;

                    if (method == "Page Range")
                    {
                        searchStart = startIdx;
                        searchEnd   = Mathf.Min(endIdx, totalSlots);
                    }

                    for (int i = searchStart; i < searchEnd; i++)
                    {
                        if (string.IsNullOrEmpty(data.BankData[i]))
                        {
                            data.BankData[i]  = entry.Id;
                            data.BankCount[i] = entry.Quantity;
                            placed = true;
                            break;
                        }
                    }
                }

                if (placed)
                {
                    anyDeposited = true;
                    UpdateSocialLog.LogAdd($"[Loot Manager] Deposited \"{entry.Name}\" into bank.", "lightblue");
                }
                else
                {
                    // No space found — fallback to inventory
                    UpdateSocialLog.LogAdd($"[Loot Manager] No bank space. Adding \"{entry.Name}\" to inventory instead.", "orange");

                    var item = GameData.ItemDB.GetItemByID(entry.Id);
                    StandardLoot.LootToInv(item, entry.Quantity, entry.Name);


                }
            }

            // Save if any deposits occurred
            if (anyDeposited)
            {
                Directory.CreateDirectory(saveDir);
                File.WriteAllText(newPath, JsonUtility.ToJson(data));
            }
        }
    }
}
