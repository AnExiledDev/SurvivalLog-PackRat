using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;

namespace PackRat.Core;

/// <summary>The scripts the plugin runs to answer the Inventories and Items tabs.</summary>
public static class TabsPage
{
    public const string GridsKind = "grids";
    public const string ItemsKind = "items";

    /// <summary>
    /// Hands the tabs the game's own sizes and limits ([w, h, grams] per row) and each row's category, the two settings they show, both files
    /// as they were read, and what went wrong reading them. A file that could not be read is sent as null, so the tab
    /// starts from the game's values.
    /// </summary>
    public static string DataCall(
        IReadOnlyDictionary<int, int[]> bags,
        IReadOnlyDictionary<int, int[]> items,
        double carryWeight,
        double itemWeight,
        Inventories? grids,
        Items? itemsFile,
        IReadOnlyList<string> problems)
    {
        var data = new JsonObject
        {
            ["bags"] = Rows(bags),
            ["cats"] = CategoryOf(bags),
            ["items"] = Rows(items),
            ["carryWeight"] = carryWeight,
            ["itemWeight"] = itemWeight,
            ["grids"] = grids == null ? null : JsonNode.Parse(grids.ToJson()),
            ["itemsFile"] = itemsFile == null ? null : JsonNode.Parse(itemsFile.ToJson()),
            ["problems"] = new JsonArray(problems.Select(p => (JsonNode?)JsonValue.Create(p)).ToArray()),
        };

        // The default encoder writes non-ASCII and '<' as \u escapes, so the JSON is safe to paste into a script.
        return "window.__slpackrat&&window.__slpackrat.data(" + data.ToJsonString() + ")";
    }

    public static string SavedCall(string kind, bool isSaved, string message) =>
        "window.__slpackrat&&window.__slpackrat.saved(" + JsText.Literal(kind) + "," + (isSaved ? "true" : "false") + "," + JsText.Literal(message) + ")";

    /// <summary>Each inventory's category by its game size, so the tab groups them the way the plugin applies them.</summary>
    private static JsonObject CategoryOf(IReadOnlyDictionary<int, int[]> bags)
    {
        var node = new JsonObject();

        foreach (var (id, values) in bags.OrderBy(r => r.Key))
        {
            if (values.Length < 2) continue;

            node[id.ToString(CultureInfo.InvariantCulture)] = Categories.Key(Categories.Of(id, new Size(values[0], values[1])));
        }

        return node;
    }

    private static JsonObject Rows(IReadOnlyDictionary<int, int[]> rows)
    {
        var node = new JsonObject();

        foreach (var (id, values) in rows.OrderBy(r => r.Key)) node[id.ToString(CultureInfo.InvariantCulture)] = new JsonArray(values.Select(v => (JsonNode?)JsonValue.Create(v)).ToArray());

        return node;
    }
}
