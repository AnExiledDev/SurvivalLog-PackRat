using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PackRat.Core;

public enum BagGroup
{
    Bag,
    Vehicle,
    Storage,
}

/// <summary>Columns and rows of one grid, or the size of one item, in cells.</summary>
public readonly record struct Size(int W, int H)
{
    public override string ToString() => W + "x" + H;
}

public static class BagGroups
{
    /// <summary>
    /// The game's bag table numbers player bags below 1000 and storage furniture from 1000 up. Rows 3 and 4 are the
    /// car's trunk and the cart, so they count as vehicles.
    /// </summary>
    public static BagGroup Of(int id) => id is 3 or 4 ? BagGroup.Vehicle : id < 1000 ? BagGroup.Bag : BagGroup.Storage;
}

/// <summary>One bag table row's own columns, rows and weight limit. Null means the row follows the multipliers.</summary>
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

    public static readonly Inventories Default = new(1, 1, 1, 1, new Dictionary<int, GridRow>());

    public Inventories(double all, double bag, double vehicle, double storage, IReadOnlyDictionary<int, GridRow> rows)
    {
        All = ClampScale(all);
        Bag = ClampScale(bag);
        Vehicle = ClampScale(vehicle);
        Storage = ClampScale(storage);
        Rows = rows;
    }

    public double All { get; }
    public double Bag { get; }
    public double Vehicle { get; }
    public double Storage { get; }
    public IReadOnlyDictionary<int, GridRow> Rows { get; }

    /// <summary>A group's own multiplier wins when it is not 1, otherwise All applies.</summary>
    public double ScaleOf(BagGroup group)
    {
        var own = group switch
        {
            BagGroup.Vehicle => Vehicle,
            BagGroup.Storage => Storage,
            _ => Bag,
        };

        return own != 1 ? own : All;
    }

    /// <summary>The grid row <paramref name="id"/> gets: its own columns and rows, then the multiplier, then the game's.</summary>
    public Size GridOf(int id, Size game)
    {
        if (game.W <= 0 || game.H <= 0) return game;

        Rows.TryGetValue(id, out var own);
        var scale = ScaleOf(BagGroups.Of(id));

        return new Size(own?.W ?? Scaled(game.W, scale), own?.H ?? Scaled(game.H, scale));
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

    /// <summary>
    /// Reads the file's text. Values out of range are pulled into range and entries that are not numbers are left out;
    /// only text that is not a JSON object fails, with the reason in <paramref name="error"/>.
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
        var rows = new Dictionary<int, GridRow>();

        if (root["rows"] is JsonObject rowNodes)
        {
            foreach (var (key, node) in rowNodes)
            {
                if (!Json.TryId(key, out var id) || node is not JsonObject row) continue;

                var parsed = new GridRow(
                    Json.Int(row["w"]) is int w ? Math.Clamp(w, MinCells, MaxCells) : null,
                    Json.Int(row["h"]) is int h ? Math.Clamp(h, MinCells, MaxCells) : null,
                    Json.Number(row["kg"]) is double kg ? Math.Clamp(kg, MinKg, MaxKg) : null);

                if (!parsed.IsEmpty) rows[id] = parsed;
            }
        }

        settings = new Inventories(
            Json.Number(scale?["all"]) ?? 1,
            Json.Number(scale?["bag"]) ?? 1,
            Json.Number(scale?["vehicle"]) ?? 1,
            Json.Number(scale?["storage"]) ?? 1,
            rows);

        return true;
    }

    /// <summary>The file's text, with only what differs from the game.</summary>
    public string ToJson()
    {
        var scale = new JsonObject();

        foreach (var (key, value) in new[] { ("all", All), ("bag", Bag), ("vehicle", Vehicle), ("storage", Storage) })
        {
            if (value != 1) scale[key] = value;
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

        return new JsonObject { ["scale"] = scale, ["rows"] = rows }.ToJsonString();
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
