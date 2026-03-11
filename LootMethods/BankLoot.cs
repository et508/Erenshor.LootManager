using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace LootManager
{
    public static class BankLoot
    {
        public class LootEntry
        {
            public string Id       { get; }
            public int    Quantity { get; }
            public string Name     { get; }

            public LootEntry(string id, int quantity, string name)
            {
                Id       = id;
                Quantity = quantity;
                Name     = name;
            }
        }

        public static void DepositLoot(IEnumerable<LootEntry> entries)
        {
            if (IsBankOpen())
                DepositLive(entries);
            else
                DepositToFile(entries);
        }
        
        private static void DepositLive(IEnumerable<LootEntry> entries)
        {
            var bank = GameData.BankUI?.Bank;
            if (bank == null)
            {
                DepositToFile(entries);
                return;
            }

            var storedItems = bank.StoredItems;
            var quantities  = bank.Quantities;
            int totalSlots  = storedItems.Count;
            
            var homeSlotField = typeof(ItemIcon).GetField("MouseHomeSlot",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var homeSlot = homeSlotField?.GetValue(GameData.MouseSlot) as ItemIcon;
            bool fromBankSlot = homeSlot != null && homeSlot.BankSlot;

            if (fromBankSlot)
            {
                bank.DisplayBankPage();
                foreach (var entry in entries)
                    ChatFilterInjector.SendLootMessage(
                        $"[Loot Manager] \"{entry.Name}\" is already in the bank.", "lightblue");
                return;
            }
            
            {
                var pageField = typeof(GlobalBank).GetField("page",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                int currentPage = pageField != null ? (int)pageField.GetValue(bank) : 1;
                int pageStart   = (currentPage - 1) * 32;

                for (int i = pageStart; i < pageStart + 32 && i < totalSlots; i++)
                {
                    if (storedItems[i] == null ||
                        storedItems[i] == GameData.PlayerInv.Empty ||
                        string.IsNullOrEmpty(storedItems[i].Id)) continue;

                    int slotPos = i - pageStart;
                    if (slotPos >= bank.BankSlots.Length) continue;

                    var visibleSlot = bank.BankSlots[slotPos];
                    bool visibleIsEmpty = visibleSlot.MyItem == null ||
                                         visibleSlot.MyItem == GameData.PlayerInv.Empty ||
                                         string.IsNullOrEmpty(visibleSlot.MyItem.Id);

                    if (visibleIsEmpty)
                    {
                        storedItems[i] = GameData.PlayerInv.Empty;
                        quantities[i]  = 1;
                    }
                }
            }

            bool anyDeposited = false;

            foreach (var entry in entries)
            {
                Item item = GameData.ItemDB.GetItemByID(entry.Id);
                if (item == null) continue;

                bool placed = false;
                
                if (item.Stackable)
                {
                    for (int i = 0; i < totalSlots; i++)
                    {
                        if (storedItems[i] != null &&
                            storedItems[i] != GameData.PlayerInv.Empty &&
                            storedItems[i].Id == entry.Id)
                        {
                            quantities[i] += entry.Quantity;
                            placed = true;
                            break;
                        }
                    }
                }
                
                if (!placed)
                {
                    for (int i = 0; i < totalSlots; i++)
                    {
                        if (storedItems[i] == null ||
                            storedItems[i] == GameData.PlayerInv.Empty ||
                            string.IsNullOrEmpty(storedItems[i].Id))
                        {
                            storedItems[i] = item;
                            quantities[i]  = entry.Quantity;
                            placed = true;
                            break;
                        }
                    }
                }

                if (placed)
                {
                    anyDeposited = true;
                    ChatFilterInjector.SendLootMessage(
                        $"[Loot Manager] Deposited \"{entry.Name}\" into bank.", "lightblue");
                }
                else
                {
                    ChatFilterInjector.SendLootMessage(
                        $"[Loot Manager] No bank space. Adding \"{entry.Name}\" to inventory instead.", "orange");
                    StandardLoot.LootToInv(item, entry.Quantity, entry.Name);
                }
            }
            
            if (anyDeposited)
                bank.DisplayBankPage();
        }
        
        private static void DepositToFile(IEnumerable<LootEntry> entries)
        {
            string saveDir    = Path.Combine(Application.persistentDataPath, "ESSaveData");
            string newPath    = Path.Combine(saveDir, "GBDATA");
            string legacyPath = Path.Combine(Application.persistentDataPath, "GBDATA");

            BankSaveData data;
            if (File.Exists(newPath))
                data = JsonUtility.FromJson<BankSaveData>(File.ReadAllText(newPath));
            else if (File.Exists(legacyPath))
                data = JsonUtility.FromJson<BankSaveData>(File.ReadAllText(legacyPath));
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
            int endPage      = Mathf.Clamp(Plugin.BankPageLast.Value,  1, maxPage);
            int startIdx     = (startPage - 1) * slotsPerPage;
            int endIdx       = endPage * slotsPerPage;

            bool anyDeposited = false;

            foreach (var entry in entries)
            {
                bool placed = false;

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

                if (!placed)
                {
                    int searchStart = method == "Page Range" ? startIdx : 0;
                    int searchEnd   = method == "Page Range" ? Mathf.Min(endIdx, totalSlots) : totalSlots;

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
                    ChatFilterInjector.SendLootMessage(
                        $"[Loot Manager] Deposited \"{entry.Name}\" into bank.", "lightblue");
                }
                else
                {
                    ChatFilterInjector.SendLootMessage(
                        $"[Loot Manager] No bank space. Adding \"{entry.Name}\" to inventory instead.", "orange");
                    var item = GameData.ItemDB.GetItemByID(entry.Id);
                    StandardLoot.LootToInv(item, entry.Quantity, entry.Name);
                }
            }

            if (anyDeposited)
            {
                Directory.CreateDirectory(saveDir);
                File.WriteAllText(newPath, JsonUtility.ToJson(data));
            }
        }
        
        private static bool IsBankOpen()
        {
            return GameData.BankUI != null &&
                   GameData.BankUI.BankWindow != null &&
                   GameData.BankUI.BankWindow.activeSelf;
        }
    }
}