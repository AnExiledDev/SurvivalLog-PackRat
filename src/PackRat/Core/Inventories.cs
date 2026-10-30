using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PackRat.Core;

/// <summary>Columns and rows of one grid, or the size of one item, in cells.</summary>
public readonly record struct Size(int W, int H)
{
    public override string ToString() => W + "x" + H;
}

/// <summary>
/// The groups the Inventories tab sets together. Each holds inventories of about the same size, so one size or one
/// multiplier fits the whole group.
/// </summary>
public enum Category
{
    Bags,
    Vehicles,
    SmallStorage,
    MediumStorage,
    LargeStorage,
    ExtraLargeStorage,
}

public static class Categories
{
    public const int SmallMaxCells = 24;
    public const int MediumMaxCells = 60;
    public const int LargeMaxCells = 140;

    public static readonly Category[] All =
    {
        Category.Bags, Category.Vehicles, Category.SmallStorage, Category.MediumStorage, Category.LargeStorage, Category.ExtraLargeStorage,
    };

    public static readonly Category[] Storage = { Category.SmallStorage, Category.MediumStorage, Category.LargeStorage, Category.ExtraLargeStorage };

    /// <summary>
    /// The game's bag table numbers player bags below 1000 and storage furniture from 1000 up. Rows 3 and 4 (the cart
    /// and the car) and the car trunk, SUV trunk and truck trailer rows are vehicles. Storage is sorted by the game's
    /// own cell count, so a row a game update adds finds its group by itself.
    /// </summary>
    public static Category Of(int id, Size game)
    {
        if (id is 3 or 4 or 1022 or 1023 or 115003) return Category.Vehicles;
        if (id < 1000) return Category.Bags;

        var cells = game.W * game.H;

        return cells <= SmallMaxCells ? Category.SmallStorage
            : cells <= MediumMaxCells ? Category.MediumStorage
            : cells <= LargeMaxCells ? Category.LargeStorage
            : Category.ExtraLargeStorage;
    }

    /// <summary>The name a category has in the file and on the page.</summary>
    public static string Key(Category category) => category switch
    {
        Category.Bags => "bags",
        Category.Vehicles => "vehicles",
        Category.SmallStorage => "small",
        Category.MediumStorage => "medium",
        Category.LargeStorage => "large",
        _ => "xlarge",
    };

    public static bool TryParse(string key, out Category category)
    {
        foreach (var each in All)
        {
            if (Key(each) != key) continue;

            category = each;

            return true;
        }

        category = default;

        return false;
    }
}

/// <summary>
/// A category's own multiplier and its own columns and rows. A multiplier of 1 means the All multiplier applies; a
/// null side follows the multiplier.
/// </summary>
public sealed record CategoryRule(double Scale, int? W, int? H)
{
    public static readonly CategoryRule None = new(1, null, null);

    public bool IsEmpty => Scale == 1 && W is null && H is null;
}

/// <summary>One bag table row's own columns, rows and weight limit. Null means the row follows its category.</summary>
public sealed record GridRow(int? W, int? H, double? Kg)
{
    public bool IsEmpty => W is null && H is null && Kg is null;
}

/// <summary>What the Inventories tab saves to PackRat.Inventories.json.</summary>
public sealed class Inventories
{
    public const double MinScale = 0.5;
    public const double MaxScale = 3;
    public const int MinCells = 1;
    public const int MaxCells = 30;
    public const double MinKg = 0.5;
    public const double MaxKg = 9999;

    public static readonly Inventories Default = new(1, new Dictionary<Category, CategoryRule>(), new Dictionary<int, GridRow>());

    public Inventories(double all, IReadOnlyDictionary<Category, CategoryRule> cats, IReadOnlyDictionary<int, GridRow> rows)
    {
        All = ClampScale(all);
        Cats = cats;
        Rows = rows;
    }

    public double All { get; }
    public IReadOnlyDictionary<Category, CategoryRule> Cats { get; }
    public IReadOnlyDictionary<int, GridRow> Rows { get; }

    public CategoryRule RuleOf(Category category) => Cats.TryGetValue(category, out var rule) ? rule : CategoryRule.None;

    /// <summary>A category's own multiplier wins when it is not 1, otherwise All applies.</summary>
    public double ScaleOf(Category category)
    {
        var own = RuleOf(category).Scale;

        return own != 1 ? own : All;
    }

    /// <summary>
    /// The grid row <paramref name="id"/> gets, each side on its own: the row's own columns or rows, then its
    /// category's, then the game's times the multiplier.
    /// </summary>
    public Size GridOf(int id, Size game)
    {
        if (game.W <= 0 || game.H <= 0) return game;

        Rows.TryGetValue(id, out var own);
        var category = Categories.Of(id, game);
        var rule = RuleOf(category);
        var scale = ScaleOf(category);

        return new Size(own?.W ?? rule.W ?? Scaled(game.W, scale), own?.H ?? rule.H ?? Scaled(game.H, scale));
    }

    /// <summary>
    /// The weight limit row <paramref name="id"/> gets, in grams: its own kg when set, otherwise the game's limit
    /// times <paramref name="carryWeight"/>. 0 is the game's "no limit".
    /// </summary>
    public int LimitOf(int id, int gameLimit, double carryWeight)
    {
        if (Rows.TryGetValue(id, out var own) && own.Kg is double kg)
        {
            return (int)Math.Clamp(Math.Round(kg * 1000, MidpointRounding.AwayFromZero), 1, CarryWeight.MaxLimit);
        }

        return CarryWeight.Scaled(gameLimit, carryWeight);
    }

    private static int Scaled(int cells, double scale) => Math.Max(1, (int)Math.Round(cells * scale, MidpointRounding.AwayFromZero));

    private static double ClampScale(double scale) => double.IsFinite(scale) ? Math.Clamp(scale, MinScale, MaxScale) : 1;

    private static int? Cells(JsonNode? node) => Json.Int(node) is int cells ? Math.Clamp(cells, MinCells, MaxCells) : null;

    /// <summary>
    /// Reads the file's text. Values out of range are pulled into range and entries that are not numbers are left out;
    /// only text that is not a JSON object fails, with the reason in <paramref name="error"/>. Files from before the
    /// categories had a bag, vehicle and storage multiplier; those become the multipliers of the matching categories.
    /// </summary>
    public static bool TryParse(string? json, out Inventories settings, out string error)
    {
        settings = Default;
        error = "";

        if (string.IsNullOrWhiteSpace(json)) return true;

        JsonObject? root;

        try
        {
            root = JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException ex)
        {
            error = ex.Message;

            return false;
        }

        if (root is null)
        {
            error = "the file is not a JSON object";

            return false;
        }

        var scale = root["scale"] as JsonObject;
        var cats = new Dictionary<Category, CategoryRule>();
        var rows = new Dictionary<int, GridRow>();

        if (Json.Number(scale?["bag"]) is double bag) cats[Category.Bags] = new CategoryRule(ClampScale(bag), null, null);
        if (Json.Number(scale?["vehicle"]) is double vehicle) cats[Category.Vehicles] = new CategoryRule(ClampScale(vehicle), null, null);

        if (Json.Number(scale?["storage"]) is double storage)
        {
            foreach (var category in Categories.Storage) cats[category] = new CategoryRule(ClampScale(storage), null, null);
        }

        if (root["cats"] is JsonObject catNodes)
        {
            foreach (var (key, node) in catNodes)
            {
                if (!Categories.TryParse(key, out var category) || node is not JsonObject cat) continue;

                cats[category] = new CategoryRule(Json.Number(cat["x"]) is double x ? ClampScale(x) : 1, Cells(cat["w"]), Cells(cat["h"]));
            }
        }

        foreach (var category in Categories.All)
        {
            if (cats.TryGetValue(category, out var rule) && rule.IsEmpty) cats.Remove(category);
        }

        if (root["rows"] is JsonObject rowNodes)
        {
            foreach (var (key, node) in rowNodes)
            {
                if (!Json.TryId(key, out var id) || node is not JsonObject row) continue;

                var parsed = new GridRow(Cells(row["w"]), Cells(row["h"]), Json.Number(row["kg"]) is double kg ? Math.Clamp(kg, MinKg, MaxKg) : null);

                if (!parsed.IsEmpty) rows[id] = parsed;
            }
        }

        settings = new Inventories(Json.Number(scale?["all"]) ?? 1, cats, rows);

        return true;
    }

    /// <summary>The file's text, with only what differs from the game.</summary>
    public string ToJson()
    {
        var scale = new JsonObject();

        if (All != 1) scale["all"] = All;

        var cats = new JsonObject();

        foreach (var category in Categories.All)
        {
            var rule = RuleOf(category);

            if (rule.IsEmpty) continue;

            var node = new JsonObject();
            if (rule.Scale != 1) node["x"] = rule.Scale;
            if (rule.W is int w) node["w"] = w;
            if (rule.H is int h) node["h"] = h;
            cats[Categories.Key(category)] = node;
        }

        var rows = new JsonObject();

        foreach (var (id, row) in Rows.OrderBy(r => r.Key))
        {
            var node = new JsonObject();
            if (row.W is int w) node["w"] = w;
            if (row.H is int h) node["h"] = h;
            if (row.Kg is double kg) node["kg"] = kg;
            rows[id.ToString(CultureInfo.InvariantCulture)] = node;
        }

        return new JsonObject { ["scale"] = scale, ["cats"] = cats, ["rows"] = rows }.ToJsonString();
    }
}

internal static class Json
{
    public static bool TryId(string key, out int id) => int.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out id);

    public static double? Number(JsonNode? node)
    {
        if (node is not JsonValue value) return null;
        if (value.TryGetValue<double>(out var number) && double.IsFinite(number)) return number;

        return null;
    }

    public static int? Int(JsonNode? node) => Number(node) is double number ? (int)Math.Round(Math.Clamp(number, int.MinValue, int.MaxValue)) : null;

    public static bool Bool(JsonNode? node) => node is JsonValue value && value.TryGetValue<bool>(out var flag) && flag;

    /// <summary>A [w, h] pair with both sides from <paramref name="min"/> to <paramref name="max"/>, or null.</summary>
    public static Size? Size(JsonNode? node, int min, int max)
    {
        if (node is not JsonArray pair || pair.Count != 2) return null;
        if (Int(pair[0]) is not int w || Int(pair[1]) is not int h) return null;

        return new Size(Math.Clamp(w, min, max), Math.Clamp(h, min, max));
    }

    public static JsonArray Pair(Size size) => new(size.W, size.H);
}
