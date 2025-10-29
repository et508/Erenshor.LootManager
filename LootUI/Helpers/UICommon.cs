using System.Collections.Generic;
using UnityEngine;

namespace LootManager
{
    public static class UICommon
    {
        public static Transform Find(GameObject root, string path)
        {
            return root != null ? root.transform.Find(path) : null;
        }

        public static void ClearList(Transform content)
        {
            if (content == null) return;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Transform c = content.GetChild(i);
                GameObject.Destroy(c.gameObject);
            }
        }

        public static void ClearListExceptTemplate(Transform content, GameObject template)
        {
            if (content == null) return;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                GameObject c = content.GetChild(i).gameObject;
                if (template != null && c == template) continue;
                GameObject.Destroy(c);
            }
        }

        public sealed class DoubleClickTracker
        {
            private readonly Dictionary<string, float> _lastClick = new Dictionary<string, float>();
            private readonly float _threshold;

            public DoubleClickTracker(float thresholdSeconds)
            {
                _threshold = thresholdSeconds;
            }

            public bool IsDoubleClick(string key)
            {
                float now = Time.time;
                float last;
                if (_lastClick.TryGetValue(key, out last) && (now - last) < _threshold)
                {
                    _lastClick[key] = now;
                    return true;
                }
                _lastClick[key] = now;
                return false;
            }
        }
    }
}