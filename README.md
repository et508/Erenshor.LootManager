![GitHub Release](https://img.shields.io/github/v/release/et508/Erenshor.LootManager) ![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/et508/Erenshor.LootManager/total)


# Loot Manager
Loot using Blacklist or Whitelist filtering. Send all your loot directly to your bank, or setup a bank filter and only send the loot you want to the bank. Manage your Junklist for quick selling, and autolist items on the Auction House.

## Installation
- Install [BepInEx](https://github.com/et508/Erenshor.BepInEx/releases/tag/e1)
- [Download the latest release](https://github.com/et508/Erenshor.LootManager/releases/latest)
- Extract the folder from Erenshor.LootManager.zip into `Erenshor\BepInEx\plugins\` folder.

### It is highly recommended to use BepInEx 5.4.23.x

## How It Works
- Open the Loot Manager window by pressing `F6`.
  - You can configure a custom keybind in the settings tab.
- Choose your settings.
- Setup your Blacklist/Whitelist, Banklist, Junklist, Auctionlist, and Filter Lists.

---

## Settings

### Hotkeys
- **Open/Close Window** — Toggle the Loot Manager window. `F6` by default.
- **Toggle Autoloot** — Toggle autolooting on or off. `F10` by default.

### Autoloot
- **Enable Autoloot** — Toggle autoloot on or off.
- **Autoloot Distance** — Set the distance to autoloot. 0 to 200 units.
- **Out-of-Combat Delay** — When enabled, adds a short delay before autolooting so you don't accidentally loot mid-fight.

### Loot Method
- **Blacklist** — Loot everything except items on your Blacklist. Blacklisted items are destroyed.
- **Whitelist** — Destroy everything. Only loot what you have explicitly chosen to keep.
- **Standard** — Default game looting behaviour. No filtering applied.

### Fishing & Mining
- **Apply Loot Filters to Fishing** — When enabled, fish catches are processed through your active loot filters (Blacklist/Whitelist/Standard), Banklist, and Auctionlist. Useful for AFK fishing to discard junk catches automatically. Off by default.
- **Apply Loot Filters to Mining** — When enabled, mining yields are processed through the same loot filters. Useful for discarding unwanted ore or automatically banking materials. Off by default.

### Bank Loot
- **Enable Bank Loot** — Toggle bank looting on or off.
- **Bank Loot Method** — `All` or `Filtered`.
  - `All` — Everything you loot goes directly to the bank.
  - `Filtered` — Only items on your Banklist are sent to the bank.
- **Bank Loot Page** — `First Empty` or `Page Range`.
  - `First Empty` — Items are deposited into the first available bank slot.
  - `Page Range` — Items are deposited within a specific range of bank pages (1–98). Set both sliders to the same page to target a single page.

### Chat Output
- **Enable Chat Output** — Toggle loot activity messages in chat on or off.
- **Chat Window / Tab** — Choose which chat window and tab loot messages are sent to. Detected automatically from your open chat windows.

---

## Blacklist
Items on the Blacklist are **destroyed** when looted.

- The left column lists every item in the game. The right column (red) is your Blacklist.
- Double-click any item to move it between columns.
- Use the search bar to filter by name. Not case-sensitive.
- **Always Loot Rare** — When enabled, equipment items are always looted regardless of the Blacklist.

## Whitelist
Only items on the Whitelist are **kept** when looting. Everything else is destroyed.

- Items with the `NoTradeNoDestroy` flag are always preserved regardless of your Whitelist (quest items, event rewards, etc.).
- The left column (red) lists every item in the game. The right column (white) contains items you will keep.
- Items that pass the Whitelist still follow any Banklist rules you have set.
- **Loot Equipment** — When enabled, equippable items are looted regardless of the Whitelist. A tier filter is available:
  - `All` — All tiers of equipment.
  - `Normal Only` — White tier only.
  - `Blessed Only` — Blue tier only.
  - `Godly Only` — Purple tier only.
  - `Blessed and Up` — Blue and purple tier.
- **Filter Groups** — Togglable sets of items you wish to loot. See [Filter Lists](#filter-lists) below.

## Banklist
Items on the Banklist are **sent directly to your bank** when looted, instead of going to your inventory.

- Works the same as the Blacklist/Whitelist transfer panel.
- Double-click to add or remove items.

## Junklist
Items on the Junklist are marked for **quick selling** at a vendor. The Junklist does not affect looting — it is purely a sell list.

- Double-click to add or remove items.
- Items can also be added by dragging them from your inventory onto the **Junklist** drop zone below your inventory window.
  - Dragging an item to the Junklist zone adds it to the list and **returns the item to your inventory**.

## Auctionlist
Items on the Auctionlist are **automatically listed on the Auction House** when looted.

- Double-click to add or remove items.
- Items can also be added by dragging them from your inventory onto the **Auctionlist** drop zone below your inventory window.
  - If the item can be listed (has a sell value, is not no-trade, is not Blessed or Godly tier), it will be listed immediately and removed from your inventory.
  - If listing fails, the item is returned to your inventory and a message is shown.
- **Blessed and Godly items cannot be listed**, matching the game's own Auction House restriction.

## Filter Lists
Filter Lists are togglable groups of items that work alongside the Whitelist loot method.

- A few pre-made groups are provided the first time you run Loot Manager. These can be edited or removed.
- Create a new group by entering a name and clicking `+ Add`. New groups start empty and disabled.
- Click `Edit` next to a group to open a transfer list for that group. Works the same as the other list panels.
- Click `Del` (then confirm) to permanently delete a group.
- Each group has three toggles: **BL** (apply to Blacklist), **WL** (apply to Whitelist), **Bank** (apply to Banklist).
- The **Active** toggle enables or disables the group entirely.

---

## Inventory Drop Zones
A panel of drop zones is attached below your inventory window. Drag items from your inventory directly onto these zones for quick list management.

| Zone | Behaviour |
|------|-----------|
| **Blacklist** | Adds item to Blacklist and **destroys** it from inventory |
| **Banklist** | Adds item to Banklist and **deposits** it to the bank |
| **Junklist** | Adds item to Junklist and **returns** it to inventory |
| **Auctionlist** | Adds item to Auctionlist and **lists** it on the AH (returns to inventory if listing fails) |