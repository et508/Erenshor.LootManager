using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    public static class ChatFilterInjector
    {
        public const ChatLogLine.LogType LootManagerLogType = (ChatLogLine.LogType)8388608;
        
        private const string ToggleGoName = "LootManager_FilterToggle";
        
        private const string CloneSourceName = "SimReqLoot";
        
        private const string ToggleLabelText = "Loot Manager";
        
        private static readonly Dictionary<AdjustWindowFilters, Toggle> _toggleMap
            = new Dictionary<AdjustWindowFilters, Toggle>();
        
        public static void SendLootMessage(string message, string color = "orange")
        {
            UpdateSocialLog.LogAdd(new ChatLogLine(message, LootManagerLogType, color));
        }
        
        [HarmonyPatch(typeof(AdjustWindowFilters), "Start")]
        private static class Patch_Start
        {
            static void Postfix(AdjustWindowFilters __instance)
            {
                InjectToggle(__instance);
            }
        }

        [HarmonyPatch(typeof(AdjustWindowFilters), "ChangeWindowToggle")]
        private static class Patch_ChangeWindowToggle
        {
            static void Postfix(AdjustWindowFilters __instance)
            {
                if (!_toggleMap.TryGetValue(__instance, out Toggle lmToggle))
                    return;

                if (lmToggle == null)
                {
                    _toggleMap.Remove(__instance);
                    return;
                }

                int tabIndex = __instance.MyWindow.selectedTab;

                if (lmToggle.isOn)
                    __instance.MyWindow.FilterMasks[tabIndex] |= LootManagerLogType;
                else
                    __instance.MyWindow.FilterMasks[tabIndex] &= ~LootManagerLogType;
                
            }
        }

        [HarmonyPatch(typeof(AdjustWindowFilters), "LoadWindowSettings")]
        private static class Patch_LoadWindowSettings
        {
            static void Postfix(AdjustWindowFilters __instance)
            {
                if (!_toggleMap.TryGetValue(__instance, out Toggle lmToggle))
                    return;

                if (lmToggle == null)
                {
                    _toggleMap.Remove(__instance);
                    return;
                }

                ChatLogLine.LogType mask = __instance.MyWindow.FilterMasks[__instance.MyWindow.selectedTab];
                bool shouldBeOn = (mask & LootManagerLogType) > ChatLogLine.LogType.None;
                lmToggle.SetIsOnWithoutNotify(shouldBeOn);
            }
        }

        private static void InjectToggle(AdjustWindowFilters instance)
        {
            if (_toggleMap.ContainsKey(instance))
                return;
            
            Toggle cloneSource = instance.SimReqLoot;
            if (cloneSource == null)
            {
                Plugin.Log.LogWarning("[ChatFilterInjector] Could not find SimReqLoot toggle — skipping injection.");
                return;
            }
            
            Transform filterBG = cloneSource.transform.parent;
            if (filterBG == null)
            {
                Plugin.Log.LogWarning("[ChatFilterInjector] SimReqLoot has no parent — skipping injection.");
                return;
            }
            
            GameObject newToggleGO = Object.Instantiate(cloneSource.gameObject, filterBG);
            newToggleGO.name = ToggleGoName;
            
            Transform labelTransform = newToggleGO.transform.Find("Label");
            if (labelTransform != null)
            {
                Text label = labelTransform.GetComponent<Text>();
                if (label != null)
                    label.text = ToggleLabelText;
            }
            else
            {
                Plugin.Log.LogWarning("[ChatFilterInjector] Could not find 'Label' child on cloned toggle GO.");
            }
            
            RectTransform rt = newToggleGO.GetComponent<RectTransform>();
            if (rt != null)
                rt.anchoredPosition = new Vector2(-15.8813f, -12.7876f);
            
            Toggle newToggle = newToggleGO.GetComponent<Toggle>();
            if (newToggle == null)
            {
                Plugin.Log.LogWarning("[ChatFilterInjector] Cloned GO has no Toggle component — aborting.");
                Object.Destroy(newToggleGO);
                return;
            }
            
            newToggle.onValueChanged.RemoveAllListeners();
            newToggle.onValueChanged.AddListener((_) => instance.ChangeWindowToggle());
            
            ChatLogLine.LogType currentMask = instance.MyWindow.FilterMasks[instance.MyWindow.selectedTab];
            bool isOn = (currentMask & LootManagerLogType) > ChatLogLine.LogType.None;
            newToggle.SetIsOnWithoutNotify(isOn);
            
            _toggleMap[instance] = newToggle;

            Plugin.Log.LogInfo($"[ChatFilterInjector] Injected Loot Manager toggle into filter panel of window '{instance.MyWindow.WindowName}'.");
        }
    }
}