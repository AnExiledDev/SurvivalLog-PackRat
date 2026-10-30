Name: Pack Rat
Summary: Bigger bags, smaller and lighter items, and more carry weight for your backpack, shopping cart, car and storage. Set grid sizes by category or one by one, weight limits and item sizes in two Mod Manager tabs, and see changes right away in game. Make items lighter and pick how much a heavy load slows you down. Nothing that stops fitting is lost.

Description (BBCode):
[center][img]https://sldb.core-directive.com/media/save/1-save-safe.png[/img][/center]

Survival Log limits how much your bags, storage and vehicles can hold, both in weight and in grid space. Pack Rat lets you raise both, make items smaller or lighter, and choose how much a heavy load slows you down.

[size=5]Main features[/size]
[list]
[*][b]More carry weight everywhere[/b]: your backpack, the hiking pack, the warehouse bag, the shopping cart, the car and storage with a weight limit all get the new limit. 2 (the default) means twice the game's limit. Any value from 0.1 to 1000 works, and a value below 1 makes the game harder.[/*]
[*][b]Bigger bags and storage[/b]: the Inventories tab in Mod Manager makes everything bigger at once, sets the size of a whole category, or sets the columns, rows and weight limit of one bag or storage piece. A preview shows how a full bag looks at the new size.[/*]
[*][b]Categories of about the same size[/b]: Bags, Vehicles, Small storage, Medium storage, Large storage and Extra large storage. Set a category's columns and rows and every inventory in it gets that size. A size set for one inventory beats its category.[/*]
[*][b]Changes apply right away[/b]: in a game, new sizes, weight limits, item sizes and item weights apply as soon as you change them.[/*]
[*][b]Smaller, lighter items[/b]: the Items tab makes every item 1x1, or sets a size or weight for a whole category, a food group or a single item.[/*]
[*][b]Lighter or heavier items[/b]: one setting multiplies the weight of every item, from 0.1x to 3x.[/*]
[*][b]Your own slowdown[/b]: keep the game's slowdown, choose where it starts and how strong it is, or remove it and stop picking things up at the limit instead.[/*]
[*][b]Nothing is lost[/b]: when a bag gets smaller or an item gets bigger, items that no longer fit move to a free spot in the same bag. If the bag is full, they go into the shopping bags in the living room, or onto the floor if those are full too. A message tells you how many moved.[/*]
[*][b]Big bags scroll[/b]: a bag too tall or wide for the screen gets a scrollbar on the bag screen, so the rest of the screen keeps its size.[/*]
[*][b]Talents still count[/b]: talents that raise your carry weight add on top of the new limit.[/*]
[/list]
Pack Rat writes nothing into your save and does not edit game files. Before you remove it, turn on [font=Courier New]UninstallPrep[/font] and load your save: everything goes back to the game's sizes and weights, and a message says when it is safe to remove.

[size=5]Known issues[/size]
[list]
[*]After you change the slowdown settings during a game, a slowdown that had already started can keep its old strength until it ends.[/*]
[*][font=Courier New]UninstallPrep[/font] works when you load a save, not the moment you turn it on.[/*]
[*]On a big bag that scrolls, scroll to the spot you want before you drag an item there.[/*]
[*]Very large carry weights are capped so the game's numbers stay valid. At 1000x the hiking pack, warehouse bag, shopping cart and car stop at about 833x, 801x, 556x and 208x.[/*]
[*]The shopping bags in the living room hold only so much. When they are full, the rest goes on the floor.[/*]
[/list]

[center][img]https://sldb.core-directive.com/img/mods-banner.jpg[/img]
[size=4][url=https://www.nexusmods.com/survivallog/mods/73]Get Mod Manager[/url] to set up all your mods in game  |  [url=https://sldb.core-directive.com/en/]Open the Survival Log wiki[/url] for items, recipes and maps[/size][/center]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.20151. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.11.0 or newer. Pack Rat uses it to show its messages.[/*]
[*]Optional: [url=https://www.nexusmods.com/survivallog/mods/73]Mod Manager[/url], for the Inventories and Items tabs and the Mods screen. Without it, change the settings in the config file.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] the same way: download its zip and extract it into the game folder.[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\PackRat.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
If you changed bag or item sizes, turn on [font=Courier New]UninstallPrep[/font] first and load your save. When the message says Pack Rat is safe to remove, save the game and quit. Then delete [font=Courier New]PackRat.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder.

[size=5]Configuration[/size]
Bag and item sizes are set on the Inventories and Items tabs in Mod Manager. The other settings ([font=Courier New]CarryWeight[/font], [font=Courier New]ItemWeight[/font], [font=Courier New]Overweight[/font], [font=Courier New]UninstallPrep[/font]) are on the Mods screen, or in [font=Courier New]BepInEx\config\com.dev.survivallog.packrat.cfg[/font] while the game is closed (see [url=https://github.com/AnExiledDev/SurvivalLog-PackRat/blob/main/CONFIG.md]CONFIG.md[/url] for details). In a game, changes apply right away; otherwise they apply when you load a save.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord; you can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-PackRat]github.com/AnExiledDev/SurvivalLog-PackRat[/url]

[line]
[size=5]简体中文[/size]

在 Survival Log 里，背包、储物家具和载具能装多少东西，既受负重限制，也受格子数量限制。Pack Rat 让你同时提高这两项，还能让物品更小、更轻，并决定背得太重时减速多少。

[size=5]主要功能[/size]
[list]
[*][b]处处负重更高[/b]：普通背包、野外登山包、仓管背包、购物车、轿车以及有负重上限的储物家具都会使用新的上限。默认值 2 表示游戏原有上限的两倍。可设为 0.1 到 1000 之间的任意数值，小于 1 则让游戏更有挑战。[/*]
[*][b]更大的背包和储物空间[/b]：Mod Manager 的「库存」页面可以一次放大所有库存，按类别设置大小，也可以单独设置某个背包或储物家具的列数、行数和负重上限，还能预览装满后的样子。[/*]
[*][b]按大小分类[/b]：分为背包、载具、小型储物、中型储物、大型储物和超大型储物六类，同一类的大小相近。设置某一类的列数和行数后，这一类里的所有库存都会变成这个大小。单独设置过的库存优先于所属类别。[/*]
[*][b]改动立即生效[/b]：在游戏中修改大小、负重上限、物品占格和物品重量，都会马上生效。[/*]
[*][b]更小、更轻的物品[/b]：「物品」页面可以让所有物品都只占 1x1，或者为整个类别、某类食物、单件物品设置占格和重量。[/*]
[*][b]物品轻重随你定[/b]：一个设置就能把所有物品的重量调为 0.1 到 3 倍。[/*]
[*][b]自定义减速[/b]：保持游戏原有的减速，自己设定从哪里开始减速以及减速多少，或者取消减速、改为到达上限后无法再拾取。[/*]
[*][b]东西不会丢[/b]：背包变小或物品变大后，放不下的物品会挪到同一个背包里的空位；背包满了就装进客厅的购物袋，购物袋也满了才放到地上，并有提示告诉你移动了多少件。[/*]
[*][b]大背包可以滚动[/b]：太高或太宽的背包在背包界面会出现滚动条，界面其他部分不会跟着缩小。[/*]
[*][b]天赋照样生效[/b]：提升负重的天赋会在新上限的基础上继续叠加。[/*]
[/list]
Pack Rat 不会往存档里写入任何内容，也不会修改游戏文件。移除之前，先打开 [font=Courier New]UninstallPrep[/font] 并读取存档：所有大小和重量都会恢复为游戏原值，提示可以安全移除后再移除。

[size=5]已知问题[/size]
[list]
[*]在游戏中修改减速设置后，已经开始的减速可能要等这次减速结束后才换成新的强度。[/*]
[*][font=Courier New]UninstallPrep[/font] 在读取存档时生效，而不是打开时立即生效。[/*]
[*]背包需要滚动时，请先滚动到想放的位置，再拖动物品过去。[/*]
[*]负重倍数过大时会被限制，以保证游戏数值有效。设为 1000 倍时，野外登山包、仓管背包、购物车和轿车分别约为 833、801、556 和 208 倍。[/*]
[*]客厅的购物袋容量有限，装满后其余物品会放到地上。[/*]
[/list]

[url=https://www.nexusmods.com/survivallog/mods/73]获取 Mod Manager[/url]，在游戏里设置所有模组。[url=https://sldb.core-directive.com/zh/]打开 Survival Log 维基[/url]，物品、配方、地图一查就有。

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.20151。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.11.0 或更新版本，Pack Rat 用它显示提示。[/*]
[*]可选：[url=https://www.nexusmods.com/survivallog/mods/73]Mod Manager[/url]，用于「库存」「物品」页面和「模组」界面。没装的话，请在配置文件里修改设置。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]用同样的方式安装 [url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url]：下载它的压缩包并解压到游戏目录。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\PackRat.dll[/code][/*]
[/list]

[size=5]卸载[/size]
如果改过背包或物品的大小，请先打开 [font=Courier New]UninstallPrep[/font] 并读取存档。看到可以安全移除的提示后，存档并退出游戏，再从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]PackRat.dll[/font]。

[size=5]配置[/size]
背包和物品的大小在 Mod Manager 的「库存」和「物品」页面设置。其他设置（[font=Courier New]CarryWeight[/font]、[font=Courier New]ItemWeight[/font]、[font=Courier New]Overweight[/font]、[font=Courier New]UninstallPrep[/font]）在「模组」界面修改，也可以在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.packrat.cfg[/font]。配置文件中的设置有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-PackRat/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。在游戏中改动会立即生效，否则在读取存档时生效。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在本页的「Posts」标签里发帖。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-PackRat]github.com/AnExiledDev/SurvivalLog-PackRat[/url]
