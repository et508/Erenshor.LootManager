![GitHub Release](https://img.shields.io/github/v/release/et508/Erenshor.LootManager) ![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/et508/Erenshor.LootManager/total)


# Loot Manager
Loot using Blacklist or Whitelist filtering. Send all your loot directly to your bank, or setup a bank filter and only send the loot you want to the bank.

## Installation
- Install [BepInEx](https://github.com/et508/Erenshor.BepInEx/releases/tag/e1)
- [Download the latest release](https://github.com/et508/Erenshor.LootManager/releases/latest)
- Extract the folder from Erenshor.LootManager.zip into `Erenshor\BepInEx\plugins\` folder.

### It is highly recommended to use BepInEx 5.4.23.x

## How It Works
- Open the LootUI window by pressing `F6` or using the LootUI button on the manager tab in the inventory.
  - You can configure a custom keybind in the LootUI settings panel.
- Choose your settings.
- Setup your Blacklist/Whitelist, and/or Banklist.

## LootUI Settings
- Autoloot - Toggle On or Off
- Autoloot Hotkey - Set a custom keybind to toggle autolooting on or off. `F10` by default.
- Autoloot Distance - Set the distance to autoloot 0 to 200 units
- Loot Method - Choose between Blacklist, Whitelist, or Standard
    - Blacklist - Will use the blicklist filter you setup. Destroying the items you do not want to loot.
    - Whitelist - Destroy everything. Only looting what you choose to loot.
    - Standard - This the default game looting behavior.
- Bankloot - Toggle On or Off
- Bankloot Method - All or Filtered
    - All - Everything you loot goes to the bank. 
    - Filtered - Will use the banklist filter you setup. Only sending the items you want to the bank.
- Bankloot Page - First Empty or Page Range
    - First Empty - Will send items to the first empty bank slot.
    - Page Range - Will send items to the range of pages you set. Pages 1 to 98. If you want a single page, set both sliders to the same page.

## Blacklist and Banklist
- Both function the exact same.
- On the left is a list of every item in the game. 
- On the right is the blacklist(red) or banklist(blue).
- Item search filtering is available. Not case-sensitive.
- Double-click to add or remove an item.

## Whitelist
- While using the Whitelist loot method you will not loot anything. Everything will be destroyed. 
  - Items that have the `NoTradeNoDestroy` flag will be preserved. As these are typically quest items or event items that can not be gained again.
- On the Whitelist panel you will see a transfer list that works the same as the blacklist and banklist.
  - On the left(red), a list of every item in the game. 
  - On the right(white), are the items you will loot. 
    - Looting these items will follow any Banklist rules you have set. 
- Loot Equipment toggle, will allow for looting any item that is equipable.
  - A tier filter has been provided for this as well.
    - `All`: All tiers of equipment.
    - `Normal Only`: Only loot white tiered equipment.
    - `Blessed Only`: Only loot blue tiered equipment.
    - `Godly Only`: Only loot purple tiered equipment.
    - `Blessed And Up`: Loot blue and purple tiered equipment
  - This will also follow any bank rules you have set.
- Filterlist Groups are togglable sets of items you wish to loot.
  - A few pre-made groups are provided the first time you run Loot Manager. 
    - These can be edited or removed to your liking.
  - New groups can be created by entering a name and clicking `New Group`.
    - These groups start empty and disabled by default.
  - Clicking `Edit` next to each group name will open a transfer list window. This works the same as the others.
  - Click the `X` next to the group name will delete the group. This is permanent and can not be reversed. 

## Add Blacklist Items Directly From Inventory
- Add items to the Blacklist just by placing them in the "Blacklist" inventory slot.

## Send Items to Bank Directly From Inventory
- Send items to the bank just by placing them in the "Bank" inventory slot.
  - This will follow any banklist rules you have set.
  - Able to toggle banklist addition on or off.


