# Loot Manager
Your Loot Your Way!!! Loot using Blacklist filtering. Send all your loot directly to your bank, or setup a bank filter and only send the loot you want to the bank. Easily modify your Blacklist and Banklist using LootUI.

## Installation
- Install [BepInEx Mod Pack](https://thunderstore.io/c/erenshor/p/BepInEx/BepInExPack/)
- Download the latest [release](https://github.com/et508/Erenshor.LootManager)
- Extract the folder from Erenshor.LootManager.zip into Erenshor\BepInEx\plugins\ folder.

## How It Works
- Open the LootUI window with `/lootui` or by pressing F6. Hotkey currently is not configuable. Will be coming in a later update. 
- Choose your settings
- Setup your Blacklist and/or Banklist

## LootUI Settings
- Autoloot - Toggle On or Off
- Autoloot Distance - Set the distance to autoloot 0 to 200 units
- Loot Method - Choose between Blacklist or Standard
    - Blacklist - Will use the blicklist filter you setup. Destroying the items you do not want to loot.
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
- Item search filtering is available. Not case sensitive.
- Double-click to add or remove a single item or select multiple with left ctrl + click then click add or remove. Currently you can not select multiple and filter at the same time. Changing your filter will clear your selections. A fix for this is coming in a later update. 

## Loot Manager Commands
- A revamp of commands is currently in the works. In the current release only `/lootui` is supported. This will open the Loot Manager UI window. 

## Add Items Directly From Inventory
- Add items to the list just by placing them in the "Blacklist" inventory slot.

## Manual Editing of the LootLists
- Manual editing of both the blacklist and banklist is supported.
- Located in your BepInEx\config folder. 
- LootBlacklist.json
- LootBanklist.json
- Your loot lists must follow the correct syntax to work properly; item names are case sensitive and must be spelled exactly how they are in-game.
```
 {
    "items": [
        "Citrine Stone",
        "Water",
        "Wolf Meat",
        "Ancient Bone",
        "Star Stone"
    ]
}
```



