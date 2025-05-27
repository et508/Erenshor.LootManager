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
        
        // Deposits the given loot entries into the bank JSON on disk.
        public static void DepositLoot(IEnumerable<LootEntry> entries)
        {
            string saveDir    = Path.Combine(Application.persistentDataPath, "ESSaveData");
            string newPath    = Path.Combine(saveDir, "GBDATA");
            string legacyPath = Application.persistentDataPath + "GBDATA";

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
                data = new BankSaveData
                {
                    BankData  = Enumerable.Repeat(string.Empty, 3168).ToList(),
                    BankCount = Enumerable.Repeat(1,       3168).ToList()
                };
            }

            bool anyDeposited = false;

            foreach (var entry in entries)
            {
                bool placed = false;

                // Try stacking if stackable
                if (GameData.ItemDB.GetItemByID(entry.Id)?.Stackable == true)
                {
                    for (int i = 0; i < data.BankData.Count; i++)
                    {
                        if (data.BankData[i] == entry.Id)
                        {
                            data.BankCount[i] += entry.Quantity;
                            placed = true;
                            break;
                        }
                    }
                }

                // If not stacked, put in first empty slot
                if (!placed)
                {
                    for (int i = 0; i < data.BankData.Count; i++)
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
                    UpdateSocialLog.LogAdd($"[Loot Manager] Deposited \"{entry.Name}\" into bank", "grey");
                }
                else
                {
                    UpdateSocialLog.LogAdd($"[Loot Manager] Bank full! Could not deposit \"{entry.Name}\"", "red");
                }
            }

            if (anyDeposited)
            {
                Directory.CreateDirectory(saveDir);
                File.WriteAllText(newPath, JsonUtility.ToJson(data));
            }
        }
    }
}
