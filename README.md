# Pack Rat

Lets every bag, storage piece and vehicle carry more weight. Twice as much by default.

Survival Log limits how much weight your backpack, your storage furniture, the shopping cart and the car can hold. Pack Rat multiplies all of those limits by the amount you set, so you can carry home more from each trip and slow down less from heavy loads.

## Features

- **More carry weight everywhere**: set `CarryWeight` to how many times more weight everything can hold. 2 (the default) means twice the game's limit. Any value from 0.1 to 1000 works.
- **Less weight too**: a value below 1 makes everything hold less, for a harder game.
- **Talents still count**: talents that raise your carry weight add on top of the new limit.
- **Every save**: the limits are set each time you load a save.

Pack Rat writes nothing into your save and does not edit game files. You can remove it at any time; the next save you load uses the game's own limits. If you carry more than the game's own limit when you remove it, you will be overweight until you put some of it down.

It shows no text of its own, so it works with the game set to any language.

## Known issues and limits

- **Changes apply at the next save load.** Bags already loaded keep their limit until you load a save.
- **Very large values are capped.** At 1000x the hiking pack, warehouse bag, shopping cart and car stop at about 833x, 801x, 556x and 208x so the game's numbers stay valid.
- **Grid size is not changed yet.** This version changes weight only, not how many slots a bag has.

## Requirements

- Survival Log 1.1.19611. Built and tested on this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- Optional: [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.4.0 or newer. With it, settings changed on the Mod Manager's Mods screen take effect without restarting the game. Without it, they take effect at the next game start.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Optional, to change settings without a restart: install Exiled's Toolkit. Download `ExiledToolkit-x.y.z.zip` from [its latest release](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/61), and extract it into the game folder (the folder with `SurvivalLog.exe`).
3. Download `PackRat-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-PackRat/releases/latest).
4. Extract the zip into the game folder. It holds a `BepInEx` folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
   `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\PackRat.dll`

## Uninstall

Delete `PackRat.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.packrat.cfg`.

## Keys

None. Pack Rat has no hotkeys and no buttons.

## Configuration

See [CONFIG.md](CONFIG.md) for every setting.

## Troubleshooting

Pack Rat writes to `BepInEx\LogOutput.log`. Check it for these lines:

- `Pack Rat 0.1.0 started` when the game starts, then `carry weight will be Nx from the next save load`.
- `hooked the world start` right after. A line starting with `could not hook` instead means a game update broke the mod, and everything keeps the game's limits.
- `carry weight Nx: A of B bag rows changed, backpack limit X -> Y` each time you load a save.

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
