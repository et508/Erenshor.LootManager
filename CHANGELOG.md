## [3.1.0] - TBD

### Loot UI
- Complete overhaul of the UI (again). The UI framework has been switched to ImGui.NET, which allows for more customization and better performance.
  - This also fixes the scrolling issues that were present in the previous version of the UI.
- Fixed some issues with the inventory drop zone window disappearing.

### Drop Zone UI
- Fixed the Junklist drop zone deleting items when they were dropped in the zone. Items will now return to their original position in the inventory.

  
## [3.0.0] - 3/11/2026
### Added
- Hooked Loot Manager messages into the chat filter system. You can now filter out Loot Manager messages in the chat settings or send them to a specific chat tab.
  - Chat output options can be found in the settings panel. Choose what window and tab you want Loot Manager messages to be sent to, or choose to hide them entirely.
- A toggle to delay autolooting has been added. When enabled, autolooting will be delayed until after combat ends.
  - This can be adjusted in the settings panel. The delay time can be set from 0 to 10 seconds.

### Fixed
- Fixed an issue where items were disappearing when they were deposited to the bank via the inventory bank slot.

### Removed
- Removed the open bank and open auction buttons from the inventory. With the addition of the Reliquary, these buttons were no longer necessary since its very easy to open the bank and auction house from the Reliquary. 

### Junklist
- Mark items as junk. When visiting a vendor, any junk items in your inventory will automatically be sold.

### Auctionlist
- Mark items to be automatically listed on the Auction House when looted. Items are listed at the recommended price (Item Value × 6 - 1) and are never added to your inventory.
- Items on the blacklist or not on the whitelist will not be looted and therefore will not be listed on the Auction House, even if they are on the Auction list.
- Auction list will take priority over the banklist. If an item is on both lists, it will be listed on the Auction House instead of being sent to the bank.
- Loot Manager Auctionlist ignores the 18 item listing limit in the Auction House. You can have as many items on the Auctionlist as you want, and they will all be listed when looted.
  - Note: The in-game Auction House UI displays a maximum of 18 player listings at a time. Items listed beyond this limit are still active and will sell normally — they simply won't appear in the listings panel until slots open up.

### Loot UI
- Complete overhaul of the UI. All functionality is the same, but the UI has been redesigned to be more user friendly and visually appealing.
- The Filter Categories have been removed from the Whitelist panel and added to its own section in the menu bar.
  - We have added the ability to assign each filter category to the blacklist, whitelist, or banklist.
  - This allows for more flexibility in how you want to use the filter categories. For example, you could have a filter category for crafting materials that is assigned to the banklist, so that all crafting materials are sent to the bank when looted.
  - Each category can be assigned to multiple lists.
- We reworked the inventory drop zones. They are now attached to the bottom of the inventory and are more visually distinct. The drop zones also have text to indicate their function.

### Known Issues
- Scrolling in the loot UI can be a bit buggy at times. This is a known issue and I am looking for a fix.

## [2.1.1] - 12/26/2025
### Added
- You can now turn on/off looting of rare item drops even if they are blacklisted.
- Updated blacklist to use NoTradeNoDestroy flag, to ensure looting of important quest items.

### Loot UI
- Added a setting to toggle looting of rare item drops even if they are blacklisted.

## [2.1.0] - 11/4/2025
### Added
- The Whitelist loot method has been implemented. 
- New Manager button added to the inventory window.
- Blacklist slot and bank slot.
- You can now open the bank and auction windows from the Manager inventory tab.
- LootUI toggle hotkey can now be customized in the settings panel.
- A hotkey to toggle autoloot on and off has been added. This can be customized in the settings panel.

### Fixed
- Flag check to prevent looting NPCs that should not normally be able to be looted.

### Removed
- Removed the Blacklist Slot from the inventory window.
- Support for ctrl+click multiple selection was removed.

### Loot UI
- Menu buttons will now be active or disabled dependent on settings.
- Moved the close button to the menu bar.
- Item icons have been added to lists.
- A hover effect and selection effect has been added in place of the green text color change.
- Whitelist UI Menu
    - Whitelist Transfer List
    - Equipment Toggle
    - Equipment Tier Dropdown
    - Filterlist Group Toggles

### Manager Slot UI
- Manager Slot UI window added to the inventory.
  - Blacklist Slot
  - Bank Slot
    - Add to Banklist by placing item in the slot. Able to toggle banklist addition on/off.
  - Button to toggle LootUI
  - Bank and Auction Buttons

### Loot Method - Whitelist
- Story Items - Story items will not be destroyed.
- Equipment Toggle - Loot all equipable items.
- Equipment Tier - Filter equipment looting by tier.
- Filterlist Groups - Groups of items to loot. 
  - Groups can be toggled on and off.
  - New groups can be created by entering a name and clicking `New Group`.
  - Groups can be edited using the edit button next to the group name.
    - This will display a transfer list to allow for adding or removing items to the category.
  - Groups can be deleted by clicking the `X` next to the group name.


### Loot Lists
- Changed how the loot list json files are handled. Now using Newtonsoft.Json to read/write. 
- Changed the location of the files. They are now located in `Erenshor/BepInEx/Config/Loot Manager/`.
  - Your current legacy loot lists should migrate over, so there should be no data loss.
- Manual editing of the files is no longer supported. If you need to edit the file manually for whatever reason. You will need exit out of the game first.
  - It is stongly recommended to use the in game UI to make changes. 

### Performance Improvments
- Fixed list loading in LootUI. Changing windows in LootUI should now be smoother and better load times for the item lists.

### Known Issues
- At times the Blacklist Slot will give the message "This item cannot go in this slot." Clicking again and/or moving item around the slot can resolve this.


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