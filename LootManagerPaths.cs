using System.IO;
using System.Reflection;

namespace LootManager
{
    /// <summary>
    /// Resolves config/data paths without BepInEx.Paths dependency.
    /// Config files live next to the plugin DLL in the Lunaris plugin folder.
    /// </summary>
    internal static class LootManagerPaths
    {
        public static string ConfigDir { get; private set; }

        public static void Initialize(string assemblyLocation)
        {
            string dir = Path.GetDirectoryName(assemblyLocation);
            ConfigDir = Path.Combine(dir ?? ".", "config");
            Directory.CreateDirectory(ConfigDir);
        }
    }
}