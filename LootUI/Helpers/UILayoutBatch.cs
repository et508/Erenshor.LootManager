using System;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public static class UILayoutBatch
    {
        public static void WithLayoutSuspended(Transform content, Action body)
        {
            if (content == null) { body?.Invoke(); return; }

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            var hlg = content.GetComponent<HorizontalLayoutGroup>();
            var fitter = content.GetComponent<ContentSizeFitter>();

            bool vlgOn = vlg && vlg.enabled;
            bool hlgOn = hlg && hlg.enabled;
            bool fitOn = fitter && fitter.enabled;

            if (vlg) vlg.enabled = false;
            if (hlg) hlg.enabled = false;
            if (fitter) fitter.enabled = false;

            try { body?.Invoke(); }
            finally
            {
                if (vlg) vlg.enabled = vlgOn;
                if (hlg) hlg.enabled = hlgOn;
                if (fitter) fitter.enabled = fitOn;

                Canvas.ForceUpdateCanvases();
                var rt = content as RectTransform ?? content.GetComponent<RectTransform>();
                if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
        }
    }
}