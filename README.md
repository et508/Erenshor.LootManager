# Loot Manager
Create a blacklist of items you want to stop looting. Use the Loot All button and never worry about the items you don't want again.

## Installation
- Install [BepInEx Mod Pack](https://thunderstore.io/c/erenshor/p/BepInEx/BepInExPack/)
- Download the latest [release](https://github.com/et508/Erenshor.LootManager)
- Extract files from Erenshor.LootManager.zip into Erenshor\BepInEx\plugins\ folder.

## How It Works
- A new empty blacklist is created when you first load the mod. I currently do not provide any basic starting blacklist of items. You will need to add items to your blacklist as you play.
- Easy to use in-game commands have been provided for managing your blacklist.
- When using the "Loot All" button in the loot window it checks if any of the items are on your blacklist.
- If so it will destroy those items, so we are not leaving unwanted items and corpses lying around.
- It will then loot the rest. Displaying a chat message for each item looted and each item destroyed.

## Loot Manager Commands
- /addloot - Adds a new item to the blacklist. (Item names are case sensitive and must be spelled exactly how they are in-game.)
- /removeloot - Removes a item from the blacklist. (Same as addloot item names are case sensitive and must be spelled exactly how they are in-game.)
- /showloot - Shows a list off all items currently on the blacklist.

## The Blacklist
- Manual editing of the blacklist is supported.
- Located in your BepInEx\config folder. 
- Open LootBlacklist.json
- Your blacklist must follow the correct syntax to work properly, item names are case sensitive and must be spell exactly how they are in-game.
- Example LootBlacklist.json:
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

## Compatibility
- Supports the ErenshorQOL autoloot feature.


