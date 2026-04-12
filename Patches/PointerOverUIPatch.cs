using HarmonyLib;
using UnityEngine.EventSystems;

namespace LootManager
{
    [HarmonyPatch(typeof(EventSystem), "IsPointerOverGameObject", new System.Type[0])]
    internal static class PointerOverUIPatch
    {
        internal static ImGuiRenderer Renderer;

        private static void Postfix(ref bool __result)
        {
            if (!__result && Renderer != null && Renderer.WantCaptureMouse)
                __result = true;
        }
    }
}