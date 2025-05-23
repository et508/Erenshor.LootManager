# Loot Manager
Create a blacklist of items you want to stop looting. Use the Loot All button and never worry about the items you don't want again.

## Installation
- Install [BepInEx Mod Pack](https://thunderstore.io/c/erenshor/p/BepInEx/BepInExPack/)
- Download the latest [release](https://github.com/et508/Erenshor.LootManager)
- Extract files from Erenshor.LootManager.zip into Erenshor\BepInEx\plugins\ folder.

## Configuration
- Run Erenshor so the config file will be automatically created
- Open *et508.erenshor.lootmanager* in your Erenshor\BepInEx\config
- Change values to your liking.
- I recommend using a config manager like [BepInExConfigManager](https://github.com/sinai-dev/BepInExConfigManager) for easier config changes from in-game.

## Loot Manager Commands
- /addloot - Adds a new item to the blacklist. (Item names are case sensitive and must be spelled exactly how they are in-game.)
- /removeloot - Removes a item from the blacklist. (Same as addloot item names are case sensitive and must be spelled exactly how they are in-game.)
- /showloot - Shows a list off all items currently on the blacklist. 

## Compatibility
Loot Manager does have a small compatibility issue with ErenshorQOL. To get the intended function of Loot Manager, I had to unpatch ErenshorQOL's LootAll to ensure Loot Manager's was applied. Autoloot still works as intended. I have tested this with both mods running and did not run into any issues. That is not to say there may not be. If you do have any problems, please try each mod separately before reporting any bugs to the respective mod authors. Also please let me know if you have any problems using both mods. 

