Name: Pack Rat
Summary: Carry more weight in your backpack, shopping cart, car and storage, twice as much by default, anywhere from 0.1x to 1000x. Every bag and vehicle with a weight limit gets the new limit, so you can haul home more from each trip and slow down less under heavy loads. Talents that raise carry weight still add on top. Set it below 1 for a harder game.

Description (BBCode):
[center][img]https://sldb.core-directive.com/media/save/1-save-safe.png[/img][/center]

Heavy loads slow you down fast in Survival Log. Pack Rat raises the weight limit of every bag, storage piece and vehicle by the amount you set, so each trip brings home more.

[size=5]Main features[/size]
[list]
[*][b]More carry weight everywhere[/b]: your backpack, the hiking pack, the warehouse bag, the shopping cart, the car and storage with a weight limit all get the new limit. 2 (the default) means twice the game's limit. Any value from 0.1 to 1000 works.[/*]
[*][b]Talents still count[/b]: talents that raise your carry weight add on top of the new limit.[/*]
[*][b]Less weight too[/b]: a value below 1 makes everything hold less, for a harder game.[/*]
[*][b]Every save[/b]: the limits are set each time you load a save.[/*]
[/list]
Pack Rat writes nothing into your save and does not edit game files. You can remove it at any time. If you carry more than the game's own limit when you remove it, you will be overweight until you put some of it down.

It shows no text of its own, so it works with the game in any language.

[size=5]Known issues[/size]
[list]
[*]A new value takes effect the next time you load a save.[/*]
[*]Very large values are capped so the game's numbers stay valid. At 1000x the hiking pack, warehouse bag, shopping cart and car stop at about 833x, 801x, 556x and 208x.[/*]
[*]This version changes weight only, not how many slots a bag has.[/*]
[/list]

[center][img]https://sldb.core-directive.com/img/mods-banner.jpg[/img]
[size=4][url=https://www.nexusmods.com/survivallog/mods/73]Get Mod Manager[/url] to set up all your mods in game  |  [url=https://sldb.core-directive.com/en/]Open the Survival Log wiki[/url] for items, recipes and maps[/size][/center]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.19611. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[*]Optional: [url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 or newer. With it, settings changed on the Mod Manager's Mods screen take effect without restarting the game. Without it, they take effect at the next game start.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Optional, to change settings without a restart: install Exiled's Toolkit the same way. Download its zip and extract it into the game folder.[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\PackRat.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]PackRat.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder.

[size=5]Configuration[/size]
To change the multiplier, edit [font=Courier New]CarryWeight[/font] in [font=Courier New]BepInEx\config\com.dev.survivallog.packrat.cfg[/font] while the game is closed, or do it in game on the Mod Manager's Mods screen (see [url=https://github.com/AnExiledDev/SurvivalLog-PackRat/blob/main/CONFIG.md]CONFIG.md[/url] for details). A new value takes effect the next time you load a save.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord; you can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-PackRat]github.com/AnExiledDev/SurvivalLog-PackRat[/url]

[line]
[size=5]简体中文[/size]

在 Survival Log 里，背得太重很快就会拖慢你。Pack Rat 会按你设置的倍数提高所有背包、储物家具和载具的负重上限，每次出门都能多带些东西回家。

[size=5]主要功能[/size]
[list]
[*][b]处处负重更高[/b]：普通背包、野外登山包、仓管背包、购物车、轿车以及有负重上限的储物家具都会使用新的上限。默认值 2 表示游戏原有上限的两倍。可设为 0.1 到 1000 之间的任意数值。[/*]
[*][b]天赋照样生效[/b]：提升负重的天赋会在新上限的基础上继续叠加。[/*]
[*][b]也能降低[/b]：设为小于 1 的数值，所有东西能装的重量都会变少，游戏更有挑战。[/*]
[*][b]所有存档通用[/b]：每次读取存档时都会设置负重上限。[/*]
[/list]
Pack Rat 不会往存档里写入任何内容，也不会修改游戏文件，随时可以移除。移除时如果身上的重量超过游戏原有上限，在放下一些东西之前你会处于超重状态。

模组本身不显示任何文字，所以游戏设为任何语言都能用。

[size=5]已知问题[/size]
[list]
[*]新的数值会在下次读取存档时生效。[/*]
[*]数值过大时会被限制，以保证游戏数值有效。设为 1000 倍时，野外登山包、仓管背包、购物车和轿车分别约为 833、801、556 和 208 倍。[/*]
[*]这个版本只改负重，不改背包的格子数量。[/*]
[/list]

[url=https://www.nexusmods.com/survivallog/mods/73]获取 Mod Manager[/url]，在游戏里设置所有模组。[url=https://sldb.core-directive.com/zh/]打开 Survival Log 维基[/url]，物品、配方、地图一查就有。

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.19611。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[*]可选：[url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 或更新版本。装了它，在 Mod Manager 的「模组」界面改动的设置无需重启游戏即可生效；没装的话，改动在下次启动游戏时生效。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]可选（改动设置无需重启游戏）：用同样的方式安装 Exiled's Toolkit，下载它的压缩包并解压到游戏目录。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\PackRat.dll[/code][/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]PackRat.dll[/font]。

[size=5]配置[/size]
如需修改倍数，可以在游戏内 Mod Manager 的「模组」界面修改，也可以在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.packrat.cfg[/font] 中的 [font=Courier New]CarryWeight[/font]。配置文件中的设置有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-PackRat/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。新的数值会在下次读取存档时生效。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在本页的「Posts」标签里发帖。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-PackRat]github.com/AnExiledDev/SurvivalLog-PackRat[/url]
