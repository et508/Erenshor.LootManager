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
        
        /// <summary>
        /// Deposits the given loot entries into the bank JSON on disk,
        /// placing new items only within the specified page range (1-based).
        /// Already-existing stacks anywhere in the bank will be incremented.
        /// </summary>
        /// <param name="entries">Loot entries to deposit.</param>
        /// <param name="startPage">First bank page to place new items (inclusive).</param>
        /// <param name="endPage">Last bank page to place new items (inclusive).</param>
        public static void DepositLoot(IEnumerable<LootEntry> entries, int startPage = 1, int endPage = 98)
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
                    BankCount = Enumerable.Repeat(1,      initialSlotCount).ToList()
                };
            }

            // Compute valid page range and corresponding slot indices
            int totalSlots    = data.BankData.Count;
            int slotsPerPage  = 32;
            int maxPage       = Mathf.CeilToInt(totalSlots / (float)slotsPerPage);
            startPage = Mathf.Clamp(startPage, 1, maxPage);
            endPage   = Mathf.Clamp(endPage,   1, maxPage);
            int startIdx = (startPage - 1) * slotsPerPage;
            int endIdx   = endPage * slotsPerPage;  // exclusive upper bound

            bool anyDeposited = false;

            foreach (var entry in entries)
            {
                bool placed = false;

                // 1) Stack into any existing slot across all pages
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

                // 2) If not stacked, place into first empty slot within page range
                if (!placed)
                {
                    for (int i = startIdx; i < Mathf.Min(endIdx, totalSlots); i++)
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

                // Logging outcome
                if (placed)
                {
                    anyDeposited = true;
                    UpdateSocialLog.LogAdd($"[Loot Manager] Deposited \"{entry.Name}\" into bank (pages {startPage}-{endPage})", "grey");
                }
                else
                {
                    UpdateSocialLog.LogAdd($"[Loot Manager] Could not deposit \"{entry.Name}\" – no empty slots in pages {startPage}-{endPage}", "red");
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
