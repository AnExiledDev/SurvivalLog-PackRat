# Changelog

## 0.5.1

- The search boxes in the Inventories and Items tabs now find every word you type, in any order. "backpack camping" finds Camping Backpack, and a space before or after a word no longer hides everything.
- Leaving a number box blank no longer sets it to the lowest value. A blank box, or one with no number in it, goes back to the setting. The - and + buttons next to it count from the setting too, not from the lowest value.
- Made for Survival Log 1.1.20719.

## 0.5.0

- New `ScrollBars` setting, on by default. Turn it off and big bags no longer get a scrollbar: the game shrinks the whole screen until it fits, as it does without Pack Rat. A very big bag can then get too small to use. It applies right away, even on a screen that is already open.
- Made for Survival Log 1.1.20719.

## 0.4.3

- Now uses Exiled's Toolkit for its shared parts, so it needs Exiled's Toolkit 0.19.0 or newer.
- Made for Survival Log 1.1.20719.

## 0.4.2

- The Inventories tab in Mod Manager no longer has a scrollbar of its own, and the bag preview is shown at full size instead of shrinking to a few pixels on some screens.
- Made for Survival Log 1.1.20151.

## 0.4.1

- Pack Rat now changes nothing until you set it up: `CarryWeight` starts at 1, the game's weight limit, instead of 2. If you already play with the mod, your saved setting stays as it is.
- Made for Survival Log 1.1.20151.

## 0.4.0

- Big bags now scroll on every bag screen, including the Brewing Barrel, the Cooking Station and the Juicer, and the screen looks right as soon as it opens instead of after a short flash.
- New "All items half size" switch on the Items tab: every item takes half the space, rounded down, and a long item stays long. A 3x2 item becomes 2x1 and a 2x2 item becomes 1x1. Sizes set for a category or a single item still beat it.
- The Inventories and Items tabs keep their place when you click a button, change a value or pick a category, instead of jumping back to the top.
- Fixed on the Inventories tab: a horizontal scrollbar that did nothing, and category buttons running past the edge of the window.
- Made for Survival Log 1.1.20151.

## 0.3.0

- Inventories are now sorted into six categories of about the same size: Bags, Vehicles, Small storage, Medium storage, Large storage and Extra large storage. On the Inventories tab you can set the columns, rows or size multiplier for a whole category at once.
- A size set for one inventory still beats the size of its category. The old Bags, Vehicles and Storage multipliers carry over to the new categories.
- Changes now apply right away while you are in a game: bag and storage sizes, weight limits, item sizes and item weights. Outside a game they apply when you load a save.
- When a bag gets smaller or an item gets bigger during a game, items that no longer fit move to a free spot in the same bag. If the bag is full, they go into the shopping bags in the living room.
- Very tall or wide bags now get a scrollbar on the bag screen, instead of the whole screen shrinking to fit them.
- Fixed on the Inventories tab: the weight limit box cut off big numbers, the "default" label covered the grid preview, the first inventory under a heading was hidden, and the preview could run off the bottom of the window.
- Made for Survival Log 1.1.20151.

## 0.2.0

- Bags, storage and vehicles can now be bigger, not only hold more weight. Mod Manager has a new Inventories tab: make everything bigger at once, or set the columns, rows and weight limit of one bag or storage piece, with a preview of how a full bag looks.
- Items can now take up less space and weigh less. Mod Manager has a new Items tab: make every item 1x1, set a size or weight for a whole category, or change a single item.
- New setting `ItemWeight` makes every item lighter or heavier, from 0.1x to 3x.
- New setting `Overweight` changes the slowdown from a heavy load. Default keeps the game's slowdown, Custom lets you choose where it starts and how strong it is, and Hard limit removes the slowdown but stops you picking up more at the limit.
- When a bag gets smaller, the items that no longer fit go into the shopping bags in the living room instead of being lost. If those are full, the items go on the floor.
- New setting `UninstallPrep` puts every size and weight back to the game's before you remove the mod, and tells you when it is safe to remove.
- Exiled's Toolkit 0.11.0 or newer is now required. Pack Rat uses it for its messages.
- Changes made on the Mods screen no longer need a game restart. They take effect the next time you load a save.
- Made for Survival Log 1.1.19611.

## 0.1.0

- First version.
- Every bag, storage piece and vehicle can carry more weight, twice as much by default. Set it anywhere from 0.1x to 1000x.
- Made for Survival Log 1.1.19611.
