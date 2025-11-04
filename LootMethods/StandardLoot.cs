using UnityEngine;

namespace LootManager
{
    public static class StandardLoot
    {
        public static void LootToInv(Item item, int qty, string name = null)
        {
            if (item == null)
                return;

            bool added = item.RequiredSlot == Item.SlotType.General
                ? GameData.PlayerInv.AddItemToInv(item)
                : GameData.PlayerInv.AddItemToInv(item, qty);

            if (added)
            {
                UpdateSocialLog.LogAdd($"[Loot Manager] Looted \"{name ?? item.ItemName}\" to inventory", "yellow");
            }
            else
            {
                UpdateSocialLog.LogAdd($"[Loot Manager] No room for \"{name ?? item.ItemName}\"", "yellow");
            }
        }
    }
}