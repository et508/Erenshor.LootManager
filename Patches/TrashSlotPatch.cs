using UnityEngine;

namespace LootManager
{
    public class TrashSlotPatch : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(MoveTrashSlot());
        }

        private System.Collections.IEnumerator MoveTrashSlot()
        {
            while (GameObject.Find("PlayerInv") == null || GameObject.Find("TrashSlot") == null || GameObject.Find("Text (TMP) (1)") == null)
                yield return null;

            GameObject playerInv = GameObject.Find("PlayerInv");

            // Move TrashSlot
            GameObject trashSlot = playerInv.transform.Find("TrashSlot")?.gameObject;
            if (trashSlot != null && trashSlot.TryGetComponent(out RectTransform rtTrash))
            {
                rtTrash.anchoredPosition += new Vector2(-74, 0);
            }

            // Move Text (TMP) (1)
            GameObject text1 = playerInv.transform.Find("Text (TMP) (1)")?.gameObject;
            if (text1 != null && text1.TryGetComponent(out RectTransform rtText1))
            {
                rtText1.anchoredPosition += new Vector2(-71, 0);
            }

            // Move correct Text (TMP) among possible duplicates
            int count = 0;
            foreach (Transform child in playerInv.transform)
            {
                if (child.name == "Text (TMP)")
                {
                    count++;
                    if (count == 2) // ← choose which one to move
                    {
                        if (child.TryGetComponent(out RectTransform rtText))
                        {
                            rtText.anchoredPosition += new Vector2(-74, 0);
                        }
                        break;
                    }
                }
            }
        }
    }
}
