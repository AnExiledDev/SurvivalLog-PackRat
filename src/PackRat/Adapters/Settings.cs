using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using PackRat.Core;

namespace PackRat.Adapters;

/// <summary>
/// The config entries and the two tab files. Everything is read when a save loads, and again when a setting or a tab
/// changes in the middle of a game, so a change applies right away (see <see cref="LiveApply"/>).
/// </summary>
internal sealed class Settings
{
    private readonly Action<string> _log;

    public Settings(ConfigFile config, string configDir, Action<string> log)
    {
        _log = log;
        GridsPath = Path.Combine(configDir, "PackRat.Inventories.json");
        ItemsPath = Path.Combine(configDir, "PackRat.Items.json");
        CarryWeightEntry = Bind.CarryWeight(config);
        ItemWeightEntry = Bind.ItemWeight(config);
        OverweightEntry = Bind.Overweight(config);
        SlowFromEntry = Bind.SlowFrom(config);
        SlowByEntry = Bind.SlowBy(config);
        UninstallPrepEntry = Bind.UninstallPrep(config);
        ScrollBarsEntry = Bind.ScrollBars(config);

        CarryWeightEntry.SettingChanged += Changed;
        ItemWeightEntry.SettingChanged += Changed;
        OverweightEntry.SettingChanged += Changed;
        SlowFromEntry.SettingChanged += Changed;
        SlowByEntry.SettingChanged += Changed;
    }

    private static void Changed(object? sender, EventArgs e) => LiveApply.Request();

    public string GridsPath { get; }
    public string ItemsPath { get; }
    public ConfigEntry<double> CarryWeightEntry { get; }
    public ConfigEntry<double> ItemWeightEntry { get; }
    public ConfigEntry<string> OverweightEntry { get; }
    public ConfigEntry<int> SlowFromEntry { get; }
    public ConfigEntry<int> SlowByEntry { get; }
    public ConfigEntry<bool> UninstallPrepEntry { get; }
    public ConfigEntry<bool> ScrollBarsEntry { get; }

    public bool IsUninstallPrep => UninstallPrepEntry.Value;

    /// <summary>What goes into the game's tables at this load. Uninstall prep puts the game's values back.</summary>
    public TableSettings Current()
    {
        if (IsUninstallPrep) return TableSettings.GameDefaults;

        var problems = new List<string>();
        var grids = ReadGrids(problems) ?? Inventories.Default;
        var items = ReadItems(problems) ?? Items.Default;

        foreach (var problem in problems) _log(problem);

        return new TableSettings(
            CarryWeight.ClampMultiplier(CarryWeightEntry.Value),
            ItemWeight.Clamp(ItemWeightEntry.Value),
            Overweight.Parse(OverweightEntry.Value),
            SlowFromEntry.Value,
            SlowByEntry.Value,
            grids,
            items);
    }

    /// <summary>The Inventories file, its defaults when there is none, or null (with the reason added) when it cannot be read.</summary>
    public Inventories? ReadGrids(List<string> problems) =>
        Read(GridsPath, problems, (string? text, out Inventories value, out string error) => Inventories.TryParse(text, out value, out error));

    public Items? ReadItems(List<string> problems) =>
        Read(ItemsPath, problems, (string? text, out Items value, out string error) => Items.TryParse(text, out value, out error));

    private delegate bool Parser<T>(string? text, out T value, out string error);

    private static T? Read<T>(string path, List<string> problems, Parser<T> parse) where T : class
    {
        string? text;

        try
        {
            text = File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception ex)
        {
            problems.Add($"could not read {Path.GetFileName(path)}, its changes are left out: {ex.Message}");

            return null;
        }

        if (parse(text, out var value, out var error)) return value;

        problems.Add($"{Path.GetFileName(path)} is not valid JSON, its changes are left out: {error}");

        return null;
    }

    private static class Bind
    {
        private const string Weight = "Weight";
        private const string Uninstall = "Uninstall";
        private const string Screens = "Screens";
        private const string ShowWhenCustom = "ModManager.ShowWhen=Overweight=" + Core.Overweight.CustomName;
        private const string Advanced = "ModManager.Advanced";

        // The Mod Manager shows these in place of the choice in Chinese; the value written to the file stays English.
        private const string ChoiceZh = "ModManager.ChoiceZh=";

        public static ConfigEntry<double> CarryWeight(ConfigFile config) => config.Bind(Weight, "CarryWeight", Core.CarryWeight.DefaultMultiplier,
            $"How many times more weight every bag, storage piece and vehicle can hold. 2 means twice the game's limit, and 1 (the default) keeps the game's limit. Anything that has no weight limit in the game still has none. The Inventories tab can set a limit for a single bag or storage piece. From {Core.CarryWeight.MinMultiplier} to {Core.CarryWeight.MaxMultiplier}. Applies right away.\n" +
            $"所有背包、储物家具和载具的负重上限倍数。2 表示游戏原有负重上限的两倍，1（默认）为游戏原值。原本没有负重上限的地方仍然没有上限。「库存」页面可以为单个背包或家具设置上限。范围 {Core.CarryWeight.MinMultiplier} 到 {Core.CarryWeight.MaxMultiplier}。立即生效。");

        public static ConfigEntry<double> ItemWeight(ConfigFile config) => config.Bind(Weight, "ItemWeight", 1.0, new ConfigDescription(
            $"How many times heavier every item is. 1 keeps the game's weights, and 0.5 makes everything half as heavy. The Items tab can set a different multiplier for a category, or a weight for one item. From {Core.ItemWeight.Min} to {Core.ItemWeight.Max}. Applies right away.\n" +
            $"每件物品的重量倍数。1 为游戏原值，0.5 让所有物品轻一半。「物品」页面可以为某个类别设置不同的倍数，或为单件物品设置重量。范围 {Core.ItemWeight.Min} 到 {Core.ItemWeight.Max}。立即生效。",
            new AcceptableValueRange<double>(Core.ItemWeight.Min, Core.ItemWeight.Max)));

        public static ConfigEntry<string> Overweight(ConfigFile config) => config.Bind(Weight, "Overweight", Core.Overweight.DefaultName, new ConfigDescription(
            "What happens when you carry close to your weight limit. Default: the game slows you down from 80% of the limit. Custom: you choose where the slowdown starts and how strong it is. Hard limit: there is no slowdown, but you can't pick up more once you reach the limit. Applies right away, but a slowdown that already started may keep its old strength until it ends.\n" +
            "负重接近上限时的效果。「默认」(Default)：和正常游戏一样，负重达到上限的 80% 起减速。「自定义」(Custom)：自己设定从哪里开始减速以及减速多少。「硬上限」(Hard limit)：不减速，只是达到上限后无法再拾取。立即生效，但如果已经在减速，可能要等这次减速结束后才换成新的强度。",
            new AcceptableValueList<string>(Core.Overweight.Choices), ChoiceZh + Core.Overweight.DefaultName + "=默认", ChoiceZh + Core.Overweight.CustomName + "=自定义", ChoiceZh + Core.Overweight.HardLimitName + "=硬上限"));

        public static ConfigEntry<int> SlowFrom(ConfigFile config) => config.Bind(Weight, "SlowFrom", 80, new ConfigDescription(
            $"With Overweight set to Custom: the slowdown starts at this percent of your weight limit. 100 means only at the limit. From {Core.Overweight.MinSlowFrom} to {Core.Overweight.MaxSlowFrom}.\n" +
            $"当「Overweight」为「自定义」时：负重达到上限的这个百分比时开始减速。100 表示到达上限才减速。范围 {Core.Overweight.MinSlowFrom} 到 {Core.Overweight.MaxSlowFrom}。",
            new AcceptableValueRange<int>(Core.Overweight.MinSlowFrom, Core.Overweight.MaxSlowFrom), ShowWhenCustom, Advanced));

        public static ConfigEntry<int> SlowBy(ConfigFile config) => config.Bind(Weight, "SlowBy", 40, new ConfigDescription(
            $"With Overweight set to Custom: how much slower you move once the slowdown starts, in percent. 0 means no slowdown. From {Core.Overweight.MinSlowBy} to {Core.Overweight.MaxSlowBy}.\n" +
            $"当「Overweight」为「自定义」时：开始减速后移动速度降低的百分比。0 表示不减速。范围 {Core.Overweight.MinSlowBy} 到 {Core.Overweight.MaxSlowBy}。",
            new AcceptableValueRange<int>(Core.Overweight.MinSlowBy, Core.Overweight.MaxSlowBy), ShowWhenCustom, Advanced));

        public static ConfigEntry<bool> ScrollBars(ConfigFile config) => config.Bind(Screens, "ScrollBars", true,
            "A bag or storage too big for the screen gets a scrollbar, so the rest of the screen keeps its size. Turn it off and the game shrinks the whole screen until it fits, as it does without Pack Rat. A very big bag can then get too small to use. Applies right away.\n" +
            "屏幕放不下的背包或储物会显示滚动条，界面的其他部分保持原大小。关闭后由游戏把整个界面缩小到能放下为止，和没有本模组时一样；很大的背包可能会缩得太小而难以使用。立即生效。");

        public static ConfigEntry<bool> UninstallPrep(ConfigFile config) => config.Bind(Uninstall, "UninstallPrep", false,
            "Turn this on before you remove Pack Rat. The next time you load a save, bag sizes, item sizes, weights and the slowdown all go back to the game's, and anything that no longer fits goes into the shopping bags in the living room. When the message says Pack Rat is safe to remove, save the game, then remove the mod.\n" +
            "移除背包达人之前请先打开此项。下次读取存档时，背包大小、物品占格、重量和减速效果都会恢复为游戏原值，放不下的物品会装进客厅的购物袋。看到可以安全移除的提示后，先存档，再移除本模组。");
    }
}
