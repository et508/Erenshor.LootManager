using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using BepInEx;

namespace LootManager
{
    public static class LootFilterlist
    {
        private const string FilterFileName = "LootFilterlist.ini";

        public static void Load()
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string assemblyDir  = Path.GetDirectoryName(assemblyPath);
            string path         = Path.Combine(assemblyDir, FilterFileName);

            if (!File.Exists(path))
            {
                Plugin.Log.LogWarning("[Loot Manager] LootFilterlist.ini not found.");
                Plugin.FilterList = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                return;
            }

            var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            string currentGroup = null;

            try
            {
                string[] lines = File.ReadAllLines(path);

                foreach (string raw in lines)
                {
                    string line = raw.Trim();

                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#"))
                        continue;

                    // Section headers
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentGroup = line.Substring(1, line.Length - 2).Trim().ToUpperInvariant();

                        if (!result.ContainsKey(currentGroup))
                            result[currentGroup] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        continue;
                    }

                    // Item line
                    if (currentGroup != null)
                    {
                        result[currentGroup].Add(line);
                    }
                }

                Plugin.FilterList = result;
                Plugin.Log.LogInfo($"Loaded {result.Count} filter groups.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Failed to load filterlist: " + ex);
                Plugin.FilterList = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
