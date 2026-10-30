using System.Collections.Generic;
using GameCore.HotUpdate;
using PackRat.Core;
using IntList = Il2CppSystem.Collections.Generic.List<int>;

namespace PackRat.Adapters;

/// <summary>
/// Writes Pack Rat's sizes and weights into the game's tables in memory. Bags read their grid size and weight limit
/// from the bag table whenever they are used, and an item takes its size from the item table when it is added, so a
/// write before the save's items load reaches everything. A write in the middle of a game also needs the items and
/// bags that already exist brought up to date (see <see cref="LiveApply"/>). Each table value's own game number is kept, so a setting
/// is always worked out from the game's, and Uninstall prep can put every one back.
/// </summary>
internal static class GameTables
{
    /// <summary>The overweight slowdown buff. Its first value is the speed change once you pass the threshold.</summary>
    public const int OverweightEffectId = 200000004;

    private static readonly GameValues<int, int> BagLimits = new();
    private static readonly GameValues<int, Size> BagSizes = new();
    private static readonly GameValues<int, Size> ItemSizes = new();
    private static readonly GameValues<int, int> ItemGrams = new();
    private static readonly GameValues<string, float> Overweight = new();
    private static readonly Dictionary<int, (Size Was, Size Now)> Resized = new();

    /// <summary>The item sizes the last write changed, by item id: what the table had before and what it has now.</summary>
    public static IReadOnlyDictionary<int, (Size Was, Size Now)> ResizedItems => Resized;

    /// <summary>How many grid sizes the last write changed.</summary>
    public static int ChangedGrids { get; private set; }

    /// <summary>Writes every table and returns a line for the log, or null when the tables are not loaded yet.</summary>
    public static string? Write(TableSettings settings)
    {
        var config = GameSingleton.Existing<ConfigManager>();
        var bags = config?._Config_Bag_Dict;
        var items = config?._Config_Item_Dict;

        if (config == null || bags == null || items == null || bags.Count == 0 || items.Count == 0) return null;

        int limits = 0, grids = 0, sizes = 0, weights = 0;

        Resized.Clear();
        ChangedGrids = 0;

        foreach (var row in bags.Values)
        {
            if (row == null) continue;

            var id = row._ID_k__BackingField;
            var limit = settings.Grids.LimitOf(id, BagLimits.Game(id, row._Burden_k__BackingField), settings.CarryWeight);

            BagLimits.Wrote(id, limit);

            if (limit != row._Burden_k__BackingField)
            {
                row._Burden_k__BackingField = limit;
                limits++;
            }

            if (Read(row._Size_k__BackingField) is not Size current) continue;

            var grid = settings.Grids.GridOf(id, BagSizes.Game(id, current));

            BagSizes.Wrote(id, grid);

            if (grid == current) continue;

            Put(row._Size_k__BackingField, grid);
            grids++;
        }

        // The game keeps a lazy set of bag sizes for its load-time repair checks; dropping it makes the game build it
        // again from the new sizes.
        if (grids > 0) config._bagSizeSet = null;

        ChangedGrids = grids;

        foreach (var row in items.Values)
        {
            if (row == null) continue;

            var id = row._ID_k__BackingField;
            var category = row._Category_k__BackingField;
            var subCategory = row._SubCategory_k__BackingField;
            var grams = settings.Items.GramsOf(id, category, subCategory, ItemGrams.Game(id, row._weight_k__BackingField), settings.ItemWeight);

            ItemGrams.Wrote(id, grams);

            if (grams != row._weight_k__BackingField)
            {
                row._weight_k__BackingField = grams;
                weights++;
            }

            if (Read(row._Size_k__BackingField) is not Size current) continue;

            var size = settings.Items.SizeOf(id, category, subCategory, ItemSizes.Game(id, current));

            ItemSizes.Wrote(id, size);

            if (size == current) continue;

            Put(row._Size_k__BackingField, size);
            Resized[id] = (current, size);
            sizes++;
        }

        var overweight = WriteOverweight(config, settings);

        return $"tables written: {limits} weight limits, {grids} grid sizes, {sizes} item sizes and {weights} item weights changed of {bags.Count} inventories and {items.Count} items; {overweight}";
    }

    /// <summary>The game's grid size and weight limit of every bag table row: [w, h, limit in grams].</summary>
    public static Dictionary<int, int[]> GameBags()
    {
        var result = new Dictionary<int, int[]>();
        var bags = GameSingleton.Existing<ConfigManager>()?._Config_Bag_Dict;

        if (bags == null) return result;

        foreach (var row in bags.Values)
        {
            if (row == null) continue;

            var id = row._ID_k__BackingField;
            var size = Read(row._Size_k__BackingField) is Size current ? BagSizes.Game(id, current) : new Size(0, 0);

            result[id] = new[] { size.W, size.H, BagLimits.Game(id, row._Burden_k__BackingField) };
        }

        return result;
    }

    /// <summary>The game's size and weight of every item: [w, h, grams].</summary>
    public static Dictionary<int, int[]> GameItems()
    {
        var result = new Dictionary<int, int[]>();
        var items = GameSingleton.Existing<ConfigManager>()?._Config_Item_Dict;

        if (items == null) return result;

        foreach (var row in items.Values)
        {
            if (row == null) continue;

            var id = row._ID_k__BackingField;
            var size = Read(row._Size_k__BackingField) is Size current ? ItemSizes.Game(id, current) : new Size(0, 0);

            result[id] = new[] { size.W, size.H, ItemGrams.Game(id, row._weight_k__BackingField) };
        }

        return result;
    }

    private static string WriteOverweight(ConfigManager config, TableSettings settings)
    {
        var global = config._Config_GlobalSetting_Dict;
        var effects = config._Config_Effect_Dict;
        var key = GameKey.Key_GlobalSetting_BagOverweight;

        if (global == null || key == null || !global.TryGetValue(key, out var row) || row == null) return "the overweight threshold is not in the table, the slowdown is the game's";
        if (effects == null || !effects.TryGetValue(OverweightEffectId, out var effect) || effect == null) return "the overweight slowdown is not in the table, the slowdown is the game's";

        var gameThreshold = Overweight.Game("threshold", row._Params2_k__BackingField);
        var gameSpeed = Overweight.Game("speed", effect._FloatValue0_k__BackingField);
        var (threshold, speed) = PackRat.Core.Overweight.Values(settings.Overweight, settings.SlowFrom, settings.SlowBy, gameThreshold, gameSpeed);

        row._Params2_k__BackingField = threshold;
        effect._FloatValue0_k__BackingField = speed;
        Overweight.Wrote("threshold", threshold);
        Overweight.Wrote("speed", speed);

        // The game caches the threshold the first time it reads it; 0 means it has not read it yet.
        if (GameKey._GlobalSetting_BagOverweight != 0) GameKey._GlobalSetting_BagOverweight = threshold;

        return $"overweight from {threshold:0.##} of the limit, speed change {speed:0.##} (game: {gameThreshold:0.##}, {gameSpeed:0.##})";
    }

    private static Size? Read(IntList? pair) => pair != null && pair.Count >= 2 ? new Size(pair[0], pair[1]) : null;

    private static void Put(IntList pair, Size size)
    {
        pair[0] = size.W;
        pair[1] = size.H;
    }
}
