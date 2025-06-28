## [2.1.0] - TBD
### Added
- The Whitelist loot method has been implemented. 

### Loot UI
- Menu buttons will now be active or disabled dependent on settings.
- Whitelist UI has been added.
- Whitelist Menu
    - Equipment Toggle
    - Equipment Tier Dropdown

### Loot Method - Whitelist
- Story Items - Story items will not be destroyed.
- Equipment Toggle - Loot all equipable items.
- Equipment Tier - Filter equipment looting by tier.


## [2.0.2] - 6/23/2025
### Fixed
- Rebuilt plugin using BepInEx 5.4.21.0. Miss matching version warning should not longer appear.

### LootUI
- Made some improvements to the UI windows.
- Replaced the Autoloot dropdown with a toggle.

## [2.0.1] - 6/4/2025
### HOTFIX
- Patch to fix the Thunderstore install method not installing the proper subfolder. 

## [2.0.0] - 6/4/2025
### Added 
- LootUI - A fully functional UI window to choose all your loot settings and modify your Blacklist and Banklist.
- Bankloot - Loot directly to your bank or use a filtered banklist to only send the items you want to your bank.
- Autoloot - Now supporting its own autoloot function.

### LootUI
- Toggle UI with /lootui or by pressing F6.
- All loot config settings and modifications to the blacklist and banklist can be done through the user interface.

### Removed
- No longer supported commands. /lootadd, /lootremove, /lootshow

## [1.1.1] - 5/25/2025
### Added
- Added a new inventory slot. The Blacklist slot will add items to the blacklist.

### Removed
- Removed all blacklist functionality from the original "Destroy Item" inventory slot. Restored it to its default function.

## [1.1.0] - 5/24/2025
### Added
- You can now add and remove items to/from the blacklist using the "Destroy Item" inventory slot.

## [1.0.1] - 5/23/2025
### Fixed
- Chat command `/removeloot` would try to reply to the last whispered sim when `/r` was typed. Change all the chat commands to `/loot"command"` syntax to avoid any further chat issues.

## [1.0.0] - 5/23/2025
### Release
- Initial release