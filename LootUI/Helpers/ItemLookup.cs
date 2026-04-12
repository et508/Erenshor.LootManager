using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace LootManager
{
    public static class ItemLookup
    {
        private static readonly Dictionary<string, Sprite> _iconByName =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);

        private static readonly List<string> _allItemNames = new List<string>();
        private static bool _built;

        public static void EnsureBuilt()
        {
            if (_built) return;

            var db = GameData.ItemDB?.ItemDB;
            if (db == null) return;

            _allItemNames.Clear();
            foreach (var it in db)
            {
                if (it == null) continue;
                var name = it.ItemName;
                if (string.IsNullOrWhiteSpace(name)) continue;

                if (!_iconByName.ContainsKey(name))
                    _iconByName[name] = it.ItemIcon;

                _allItemNames.Add(name);
            }

            var distinct = _allItemNames.Distinct().OrderBy(x => x).ToList();
            _allItemNames.Clear();
            _allItemNames.AddRange(distinct);

            // Register every unique texture with the ImGui renderer so
            // ImGui.Image() calls can draw them correctly.
            var renderer = Plugin.Instance?._imgui;
            if (renderer != null)
            {
                var seenTextures = new HashSet<IntPtr>();
                foreach (var kv in _iconByName)
                {
                    var sprite = kv.Value;
                    if (sprite == null || sprite.texture == null) continue;
                    var ptr = sprite.texture.GetNativeTexturePtr();
                    if (ptr != IntPtr.Zero && seenTextures.Add(ptr))
                        renderer.RegisterTexture(ptr, sprite.texture);
                }
            }

            _built = true;
        }

        public static IReadOnlyList<string> AllItems
        {
            get { EnsureBuilt(); return _allItemNames; }
        }

        public static Sprite GetIcon(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            EnsureBuilt();
            _iconByName.TryGetValue(itemName, out var s);
            return s;
        }

        /// <summary>
        /// Returns the native texture pointer for use with ImGui.Image().
        /// Returns IntPtr.Zero if the sprite or texture is null.
        /// </summary>
        public static IntPtr GetIconPtr(string itemName)
        {
            var sprite = GetIcon(itemName);
            if (sprite == null || sprite.texture == null) return IntPtr.Zero;
            return sprite.texture.GetNativeTexturePtr();
        }

        /// <summary>
        /// Returns the UV coordinates (uv0, uv1) for the sprite within its texture,
        /// accounting for Unity's bottom-left origin vs ImGui's top-left origin.
        /// uv0 = top-left corner, uv1 = bottom-right corner in ImGui space.
        /// </summary>
        public static void GetIconUVs(string itemName, out Vector2 uv0, out Vector2 uv1)
        {
            var sprite = GetIcon(itemName);
            if (sprite == null || sprite.texture == null)
            {
                uv0 = Vector2.Zero;
                uv1 = Vector2.One;
                return;
            }

            var tex  = sprite.texture;
            var rect = sprite.textureRect;

            float u0 =  rect.x                  / tex.width;
            float u1 = (rect.x + rect.width)    / tex.width;
            // Unity UV origin is bottom-left; ImGui is top-left — flip V
            float v0 = 1f - (rect.y + rect.height) / tex.height;
            float v1 = 1f -  rect.y                / tex.height;

            uv0 = new Vector2(u0, v0);
            uv1 = new Vector2(u1, v1);
        }
    }
}