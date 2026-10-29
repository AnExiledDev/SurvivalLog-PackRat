# Pack Rat

Bigger bags, smaller and lighter items, and more carry weight. Twice the weight by default.

Survival Log limits how much your backpack, your storage furniture, the shopping cart and the car can hold, both in weight and in grid space. Pack Rat lets you raise both, make items smaller or lighter, and change how much a heavy load slows you down.

## Features

- **More carry weight everywhere**: set `CarryWeight` to how many times more weight everything can hold. 2 (the default) means twice the game's limit. Any value from 0.1 to 1000 works, and a value below 1 makes the game harder.
- **Bigger bags and storage** (Inventories tab in Mod Manager): make every bag, vehicle or storage piece bigger at once, or set the columns, rows and weight limit of one of them. A preview shows how a full bag looks at the new size.
- **Smaller, lighter items** (Items tab in Mod Manager): make every item 1x1, or set a size or weight for a whole category, a food group or a single item.
- **Lighter or heavier items**: `ItemWeight` multiplies the weight of every item, from 0.1x to 3x.
- **Your own slowdown**: `Overweight` keeps the game's slowdown (Default), lets you choose where it starts and how strong it is (Custom), or removes it and stops you picking up more at the limit (Hard limit).
- **Nothing is lost**: when a bag gets smaller, items that no longer fit go into the shopping bags in the living room, or onto the floor if those are full. A message tells you how many moved.
- **Talents still count**: talents that raise your carry weight add on top of the new limit.

Pack Rat writes nothing into your save and does not edit game files. The new sizes and weights are set each time you load a save.

## Known issues and limits

- **Changes apply at the next save load.** Bags already loaded keep their size and limit until you load a save.
- **Very large carry weights are capped.** At 1000x the hiking pack, warehouse bag, shopping cart and car stop at about 833x, 801x, 556x and 208x so the game's numbers stay valid.
- **The shopping bags in the living room hold only so much.** When they are full, the rest goes on the floor.
- **The Items tab category names are Pack Rat's own**, since the game has no names for them.
- **The Mods screen may show the old `ItemWeight`** after you change `Item weight` on the Items tab, until you reopen Mod Manager. The saved value is the new one.

## Requirements

- Survival Log 1.1.19611. Built and tested on this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.11.0 or newer. Pack Rat uses it to show its messages.
- Optional: [Mod Manager](https://www.nexusmods.com/survivallog/mods/73), for the Inventories and Items tabs and the Mods screen. Without it, change the settings in the config file.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Install Exiled's Toolkit. Download `ExiledToolkit-x.y.z.zip` from [its latest release](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/61), and extract it into the game folder (the folder with `SurvivalLog.exe`).
3. Download `PackRat-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-PackRat/releases/latest).
4. Extract the zip into the game folder. It holds a `BepInEx` folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
   `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\PackRat.dll`

## Uninstall

If you made bags smaller or larger, or changed item sizes, do this first so nothing is lost:

1. Turn on `UninstallPrep` (on the Mods screen, or in the config file while the game is closed).
2. Load your save. A message says Pack Rat is safe to remove. Items that no longer fit are in the shopping bags in the living room.
3. Save the game and quit.

Then delete `PackRat.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `com.dev.survivallog.packrat.cfg`, `PackRat.Inventories.json` and `PackRat.Items.json` from `BepInEx\config`.

## Keys

None. Pack Rat has no hotkeys. With Mod Manager it adds the Inventories and Items tabs.

## Configuration

See [CONFIG.md](CONFIG.md) for every setting.

## Troubleshooting

Pack Rat writes to `BepInEx\LogOutput.log`. Check it for these lines:

- `Pack Rat 0.2.0 started` when the game starts.
- `hooked the world start`, `hooked the item load` and `hooked the overflow drop` right after. A line starting with `could not hook` instead means a game update broke part of the mod; the line says what keeps the game's values.
- `world start: tables written: ...` and `item load: tables written: ...` each time you load a save, with how many limits, grids, sizes and weights changed.
- `after the load: ...` when items had to move because they no longer fit.
- A line naming `PackRat.Inventories.json` or `PackRat.Items.json` if one of them could not be read.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . When reporting a bug, attach `BepInEx\LogOutput.log`.

To reach me directly, message `exiled.dev` on Discord; you can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. The .NET 8 SDK is required.

```
dotnet build src/PackRat -c Release -p:GameDir="C:\path\to\Survival Log"
```

The output DLL is at `src\PackRat\bin\Release\PackRat.dll`. The unit tests need nothing from the game:

```
dotnet test tests/PackRat.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
