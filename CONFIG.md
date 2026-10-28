# Pack Rat settings

Settings live in `BepInEx\config\com.dev.survivallog.packrat.cfg`, created the first time the game starts with the mod. Change it in game on the Mod Manager's Mods screen, or edit the file while the game is closed. A new value takes effect the next time you load a save. Delete the file to get the defaults back. Without Exiled's Toolkit installed, changes made on the Mods screen take effect at the next game start instead.

## Weight

| Setting | Default | What it does |
| --- | --- | --- |
| `CarryWeight` | `2` | How many times more weight every bag, storage piece and vehicle can hold. From 0.1 to 1000. |

`1` is the game's own carry weight, and the mod then changes nothing. A value below 1 makes everything hold less. Places the game gives no weight limit keep no limit. Talents that raise your carry weight still add on top.

Very large values are capped so the game's numbers stay valid. At 1000x the backpack gets the full 1000x, but the hiking pack, warehouse bag, shopping cart and car stop at the cap (about 833x, 801x, 556x and 208x). That is still far more than you can fill.

Each time you load a save, `BepInEx\LogOutput.log` shows a line like `carry weight 2x: 23 of 86 bag rows changed, backpack limit 960000 -> 1920000`.
