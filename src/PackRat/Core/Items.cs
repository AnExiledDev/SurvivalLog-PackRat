using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PackRat.Core;

/// <summary>A category's or food group's size and weight multiplier. Null means it follows the next rule down.</summary>
public sealed record GroupRule(Size? Size, double? X)
{
    public bool IsEmpty => Size is null && X is null;
}

/// <summary>One item's own size and weight in grams.</summary>
public sealed record ItemRule(Size? Size, int? G)
{
    public bool IsEmpty => Size is null && G is null;
}

/// <summary>What the Items tab saves to PackRat.Items.json.</summary>
public sealed class Items
{
    /// <summary>The game splits food (category 1) into groups; other categories have one group each.</summary>
    public const int Food = 1;

    public const int MinSide = 1;
    public const int MaxSide = 8;
    public const double MinX = 0;
    public const double MaxX = 5;
    public const int MaxGrams = 500_000;

    public static readonly Items Default = new(false, false, new Dictionary<int, GroupRule>(), new Dictionary<int, GroupRule>(), new Dictionary<int, ItemRule>());

    public Items(bool allOne, bool allHalf, IReadOnlyDictionary<int, GroupRule> cats, IReadOnlyDictionary<int, GroupRule> subs, IReadOnlyDictionary<int, ItemRule> ids)
    {
        AllOne = allOne;
        AllHalf = allHalf;
        Cats = cats;
        Subs = subs;
        Ids = ids;
    }

    public bool AllOne { get; }
    public bool AllHalf { get; }
    public IReadOnlyDictionary<int, GroupRule> Cats { get; }
    public IReadOnlyDictionary<int, GroupRule> Subs { get; }
    public IReadOnlyDictionary<int, ItemRule> Ids { get; }

    /// <summary>
    /// The item's own size, then its food group, then its category, then All items 1x1, then All items half size, then
    /// the game's.
    /// </summary>
    public Size SizeOf(int id, int category, int subCategory, Size game)
    {
        if (Ids.TryGetValue(id, out var own) && own.Size is Size ownSize) return ownSize;
        if (SubOf(category, subCategory)?.Size is Size subSize) return subSize;
        if (Cats.TryGetValue(category, out var cat) && cat.Size is Size catSize) return catSize;
        if (AllOne) return new Size(1, 1);
        if (AllHalf) return Half(game);

        return game;
    }

    /// <summary>
    /// Each side halved and rounded down, never under 1. A long item whose halves would come out square rounds its long
    /// side up instead, so a 3x2 item becomes 2x1 and stays long, while a 2x1 item can only become 1x1.
    /// </summary>
    public static Size Half(Size game)
    {
        var w = Math.Max(MinSide, game.W / 2);
        var h = Math.Max(MinSide, game.H / 2);

        if (w == h && game.W > game.H) w = (game.W + 1) / 2;
        else if (w == h && game.H > game.W) h = (game.H + 1) / 2;

        return new Size(w, h);
    }

    /// <summary>The item's own grams, then the game's weight times its food group's, its category's or the overall multiplier.</summary>
    public int GramsOf(int id, int category, int subCategory, int gameGrams, double itemWeight)
    {
        if (Ids.TryGetValue(id, out var own) && own.G is int grams) return grams;
        if (gameGrams <= 0) return gameGrams;

        Cats.TryGetValue(category, out var cat);
        var scale = SubOf(category, subCategory)?.X ?? cat?.X ?? ItemWeight.Clamp(itemWeight);

        return (int)Math.Clamp(Math.Round(gameGrams * scale, MidpointRounding.AwayFromZero), 0, MaxGrams);
    }

    private GroupRule? SubOf(int category, int subCategory) =>
        category == Food && Subs.TryGetValue(subCategory, out var sub) ? sub : null;

    /// <summary>Reads the file's text the same way as <see cref="Inventories.TryParse"/>.</summary>
    public static bool TryParse(string? json, out Items settings, out string error)
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

        var ids = new Dictionary<int, ItemRule>();

        if (root["ids"] is JsonObject idNodes)
        {
            foreach (var (key, node) in idNodes)
            {
                if (!Json.TryId(key, out var id) || node is not JsonObject item) continue;

                var rule = new ItemRule(Json.Size(item["size"], MinSide, MaxSide), Json.Int(item["g"]) is int g ? Math.Clamp(g, 0, MaxGrams) : null);

                if (!rule.IsEmpty) ids[id] = rule;
            }
        }

        settings = new Items(Json.Bool(root["allOne"]), Json.Bool(root["allHalf"]), Groups(root["cats"]), Groups(root["subs"]), ids);

        return true;
    }

    private static Dictionary<int, GroupRule> Groups(JsonNode? node)
    {
        var groups = new Dictionary<int, GroupRule>();

        if (node is not JsonObject groupNodes) return groups;

        foreach (var (key, value) in groupNodes)
        {
            if (!Json.TryId(key, out var id) || value is not JsonObject group) continue;

            var rule = new GroupRule(Json.Size(group["size"], MinSide, MaxSide), Json.Number(group["x"]) is double x ? Math.Clamp(x, MinX, MaxX) : null);

            if (!rule.IsEmpty) groups[id] = rule;
        }

        return groups;
    }

    public string ToJson()
    {
        static JsonObject Group(GroupRule rule)
        {
            var node = new JsonObject();
            if (rule.Size is Size size) node["size"] = Json.Pair(size);
            if (rule.X is double x) node["x"] = x;

            return node;
        }

        static JsonObject All<T>(IReadOnlyDictionary<int, T> rules, Func<T, JsonObject> write)
        {
            var node = new JsonObject();

            foreach (var (id, rule) in rules.OrderBy(r => r.Key)) node[id.ToString(CultureInfo.InvariantCulture)] = write(rule);

            return node;
        }

        return new JsonObject
        {
            ["allOne"] = AllOne,
            ["allHalf"] = AllHalf,
            ["cats"] = All(Cats, Group),
            ["subs"] = All(Subs, Group),
            ["ids"] = All(Ids, rule =>
            {
                var node = new JsonObject();
                if (rule.Size is Size size) node["size"] = Json.Pair(size);
                if (rule.G is int g) node["g"] = g;

                return node;
            }),
        }.ToJsonString();
    }
}

/// <summary>The ItemWeight setting: how many times heavier every item is.</summary>
public static class ItemWeight
{
    public const double Min = 0.1;
    public const double Max = 3;

    public static double Clamp(double multiplier) => double.IsFinite(multiplier) ? Math.Clamp(multiplier, Min, Max) : 1;
}
