using System;
using System.Collections.Generic;

namespace LootManager
{
    /// <summary>
    /// Supplies the default data used to seed LootFilterlist.json on first run.
    /// Matches LootFilterlist.Load() expectations (Newtonsoft.Json model).
    /// </summary>
    public static class LootFilterlistDefaults
    {
        /// <summary>
        /// Returns the default categories and items (all sections enabled).
        /// </summary>
        public static Dictionary<string, LootFilterCategory> GetDefaultData()
        {
            // Note: StringComparer.OrdinalIgnoreCase for user-friendly lookups later.
            var data = new Dictionary<string, LootFilterCategory>(StringComparer.OrdinalIgnoreCase)
            {
                ["CONSUMABLE"] = new LootFilterCategory
                {
                    IsEnabled = true,
                    Items = new List<string>
                    {
                        "Azure Mead",
                        "Bottomless Elixir",
                        "Brackfruit Juice",
                        "Bread",
                        "Elixir Of Enlightenment I",
                        "Elixir Of Enlightenment Ii",
                        "Elixir Of Enlightenment Iii",
                        "Elixir Of Enlightenment Iv",
                        "Droplet Of Lava",
                        "Seafruit",
                        "Seaspice",
                        "Muck Ball",
                        "Vithean Brew",
                        "Water"
                    }
                },
                ["KEYS"] = new LootFilterCategory
                {
                    IsEnabled = true,
                    Items = new List<string>
                    {
                        "Ancient Braxonian Key",
                        "Port Azure Lighthouse Key",
                        "Shivering Key (Halloween Event)",
                        "Dockhouse Key",
                        "Ghostly Key",
                        "Intricate Key",
                        "Key To Vitheo's Rest",
                        "Key To Vitheo's Tomb",
                        "Krakengard Tower Key",
                        "Krakengard Bunk Key",
                        "Old Church Key",
                        "Rockshade Key",
                        "Sableheart's Key",
                        "Shivering Step Lighthouse Key",
                        "Shrines Key",
                        "Stowaway's Step Gate Key",
                        "Stowaway's Step Lighthouse Key",
                        "Key To Traitor's Halls"
                    }
                },
                ["CHESS"] = new LootFilterCategory
                {
                    IsEnabled = true,
                    Items = new List<string>
                    {
                        "Blaze Fiend",
                        "Ember Acolyte",
                        "Kingsman",
                        "Monarch Of Flame",
                        "Peon",
                        "A Strange Figurine",
                        "The Faceless",
                        "The Necromancer"
                    }
                },
                ["SPECIAL"] = new LootFilterCategory
                {
                    IsEnabled = true,
                    Items = new List<string>
                    {
                        "Sivakrux",
                        "Planar Stone",
                        "Planar Stone Shards",
                        "Torn Treasure Map (Bottom Left)",
                        "Torn Treasure Map (Bottom Right)",
                        "Torn Treasure Map (Top Left)",
                        "Torn Treasure Map (Top Right)"
                    }
                }
            };

            return data;
        }
    }
}
