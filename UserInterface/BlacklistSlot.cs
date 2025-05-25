using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public class BlacklistSlot : MonoBehaviour
    {
        public static BlacklistSlot Instance;
        public ItemIcon Slot;
        private GameObject slotContainer;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        public void InitSlotAsync()
        {
            StartCoroutine(WaitAndCreateSlot());
        }

        private System.Collections.IEnumerator WaitAndCreateSlot()
        {
            GameObject templateSlot = null;

            while (templateSlot == null)
            {
                templateSlot = GameObject.Find("TrashSlot");
                yield return null;
            }

            CreateSlot(templateSlot);
        }

        public void CreateSlot(GameObject templateSlot)
        {
            if (slotContainer != null || templateSlot == null) return;

            slotContainer = Instantiate(templateSlot, templateSlot.transform.parent);
            slotContainer.name = "BlacklistSlot";

            RectTransform rt = slotContainer.GetComponent<RectTransform>();
            rt.anchoredPosition += new Vector2(73, 0);

            var text = slotContainer.GetComponentInChildren<Text>();
            if (text != null)
                text.text = "Blacklist";

            Slot = slotContainer.GetComponentInChildren<ItemIcon>();
            if (Slot == null)
            {
                Debug.LogError("[LootManager] BlacklistSlot: Failed to get ItemIcon component on cloned slot.");
                return;
            }

            Slot.TrashSlot = false;
            Slot.MouseSlot = false;
        }

        private void Update()
        {
            if (Slot == null || Slot.MyItem == null || Slot.MyItem == GameData.PlayerInv.Empty) return;

            string itemName = Slot.MyItem.ItemName;
            if (!Plugin.Blacklist.Contains(itemName))
            {
                Plugin.Blacklist.Add(itemName);
                LootBlacklist.SaveBlacklist();
                UpdateSocialLog.LogAdd("[Loot Manager] Added to blacklist: " + itemName, "yellow");
            }

            Slot.MyItem = GameData.PlayerInv.Empty;
            Slot.UpdateSlotImage();
        }
    }
}
