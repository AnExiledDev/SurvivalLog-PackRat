# Pack Rat settings

Settings live in `BepInEx\config\com.dev.survivallog.packrat.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, or edit the file while the game is closed. Every change takes effect the next time you load a save. Delete the file to get the defaults back.

## Weight

| Setting | Default | What it does |
| --- | --- | --- |
| `CarryWeight` | `2` | How many times more weight every bag, storage piece and vehicle can hold. From 0.1 to 1000. |
| `ItemWeight` | `1` | How many times heavier every item is. 0.5 makes everything half as heavy. From 0.1 to 3. |
| `Overweight` | `Default` | What happens when you carry close to your weight limit. `Default`, `Custom` or `Hard limit`. |
| `SlowFrom` | `80` | With `Overweight` set to `Custom`: the slowdown starts at this percent of your weight limit. From 10 to 100. |
| `SlowBy` | `40` | With `Overweight` set to `Custom`: how much slower you move once the slowdown starts, in percent. From 0 to 90. |

`CarryWeight` at `1` is the game's own carry weight. A value below 1 makes everything hold less. Places the game gives no weight limit keep no limit. Talents that raise your carry weight still add on top. A weight limit set for one bag on the Inventories tab replaces `CarryWeight` for that bag.

Very large `CarryWeight` values are capped so the game's numbers stay valid. At 1000x the backpack gets the full 1000x, but the hiking pack, warehouse bag, shopping cart and car stop at the cap (about 833x, 801x, 556x and 208x).

`Overweight` choices:

- `Default`: the game's slowdown, starting at 80% of your limit.
- `Custom`: you set where the slowdown starts (`SlowFrom`) and how strong it is (`SlowBy`). On the Mods screen these two show only when `Custom` is picked, under advanced settings.
- `Hard limit`: no slowdown at all, but you can't pick up more once you reach the limit.

## Uninstall

| Setting | Default | What it does |
| --- | --- | --- |
| `UninstallPrep` | `false` | Turn it on before you remove Pack Rat. The next save you load gets the game's own sizes and weights back. |

With `UninstallPrep` on, items that no longer fit in a smaller bag go into the shopping bags in the living room, or onto the floor if those are full. A message then says Pack Rat is safe to remove. Save the game, quit, and delete the mod.

## The Inventories and Items tabs

With Mod Manager installed, Pack Rat adds two tabs to it. They save to their own files in `BepInEx\config`:

- **Inventories** saves to `PackRat.Inventories.json`: the size multipliers for all bags, vehicles and storage, and the columns, rows and weight limit of each one you changed.
- **Items** saves to `PackRat.Items.json`: the "All items 1x1" switch, and the sizes and weights set for categories and single items.

Each tab has a reset button that puts everything on it back to the default. Deleting a file does the same. If a file can't be read, Pack Rat uses the defaults for it and says why in `BepInEx\LogOutput.log`. Each save keeps the file it replaces next to it as `.bak`.

The `Item weight` field on the Items tab is the same setting as `ItemWeight`.
