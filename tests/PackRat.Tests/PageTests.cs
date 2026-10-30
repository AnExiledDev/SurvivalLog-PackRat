using System.Text.Json.Nodes;
using PackRat.Core;
using Xunit;

namespace PackRat.Tests;

public class PageCommandTests
{
    [Theory]
    [InlineData("slmod|packrat|load", false)]
    [InlineData("slmod|packrat|load|data", true)]
    public void A_tab_asks_for_its_data(string message, bool wantsData)
    {
        Assert.Equal(new PageCommand.Load(wantsData), PageCommand.Parse(message));
    }

    [Fact]
    public void Saves_carry_the_whole_file()
    {
        Assert.Equal(new PageCommand.SaveGrids("""{"rows":{"a|b":1}}"""), PageCommand.Parse("""slmod|packrat|grids|save|{"rows":{"a|b":1}}"""));
        Assert.Equal(new PageCommand.SaveItems("{}"), PageCommand.Parse("slmod|packrat|items|save|{}"));
    }

    [Fact]
    public void Other_messages_are_not_acted_on()
    {
        Assert.False(PageCommand.IsOurs("slmod|newloopplus|load"));
        Assert.False(PageCommand.IsOurs(null));
        Assert.IsType<PageCommand.Unknown>(PageCommand.Parse("slmod|packrat|explode"));
        Assert.IsType<PageCommand.NoScript>(PageCommand.Parse("slmod|packrat|noscript"));
    }
}

public class TabsPageTests
{
    private static JsonObject Payload(string call)
    {
        const string head = "window.__slpackrat&&window.__slpackrat.data(";

        Assert.StartsWith(head, call);

        return JsonNode.Parse(call.Substring(head.Length, call.Length - head.Length - 1))!.AsObject();
    }

    [Fact]
    public void The_data_call_carries_the_game_values_settings_and_files()
    {
        var bags = new Dictionary<int, int[]> { [1] = new[] { 10, 8, 9600 } };
        var items = new Dictionary<int, int[]> { [10231] = new[] { 2, 2, 350 } };
        Assert.True(Inventories.TryParse("""{"rows":{"1":{"w":12}}}""", out var grids, out _));

        var data = Payload(TabsPage.DataCall(bags, items, 2, 0.5, grids, null, new[] { "PackRat.Items.json is not valid JSON" }));

        Assert.Equal(9600, (int)data["bags"]!["1"]![2]!);
        Assert.Equal(350, (int)data["items"]!["10231"]![2]!);
        Assert.Equal(2, (double)data["carryWeight"]!);
        Assert.Equal(0.5, (double)data["itemWeight"]!);
        Assert.Equal(12, (int)data["grids"]!["rows"]!["1"]!["w"]!);
        Assert.Null(data["itemsFile"]);
        Assert.Equal("PackRat.Items.json is not valid JSON", (string)data["problems"]![0]!);
    }

    /// <summary>The tab groups inventories the way the plugin sizes them, so both must agree on every row's category.</summary>
    [Fact]
    public void The_data_call_names_each_inventory_s_category_from_its_game_size()
    {
        var bags = new Dictionary<int, int[]>
        {
            [1] = new[] { 8, 7, 9600 },
            [4] = new[] { 14, 10, 48000 },
            [2003] = new[] { 5, 3, 0 },
            [1004] = new[] { 8, 7, 0 },
            [1003] = new[] { 8, 10, 24000 },
            [110009] = new[] { 20, 12, 0 },
        };

        var cats = Payload(TabsPage.DataCall(bags, new Dictionary<int, int[]>(), 1, 1, null, null, new string[0]))["cats"]!;

        Assert.Equal("bags", (string)cats["1"]!);
        Assert.Equal("vehicles", (string)cats["4"]!);
        Assert.Equal("small", (string)cats["2003"]!);
        Assert.Equal("medium", (string)cats["1004"]!);
        Assert.Equal("large", (string)cats["1003"]!);
        Assert.Equal("xlarge", (string)cats["110009"]!);
    }

    [Fact]
    public void Text_in_the_data_call_cannot_end_the_script()
    {
        var call = TabsPage.DataCall(new Dictionary<int, int[]>(), new Dictionary<int, int[]>(), 1, 1, null, null, new[] { "</script>\u2028\"x\"" });

        Assert.DoesNotContain("</script>", call);
        Assert.DoesNotContain("\u2028", call);
        Assert.Equal("</script>\u2028\"x\"", (string)Payload(call)["problems"]![0]!);
    }

    [Fact]
    public void The_saved_call_names_the_tab()
    {
        Assert.Equal("window.__slpackrat&&window.__slpackrat.saved(\"items\",false,\"Not saved: \\u003cbad\")", TabsPage.SavedCall(TabsPage.ItemsKind, false, "Not saved: <bad"));
    }
}

public class TextsTests
{
    [Fact]
    public void A_load_with_nothing_moved_says_nothing()
    {
        Assert.Null(Texts.AfterLoad(false, 0, 0, 0, zh: false));
    }

    [Fact]
    public void Uninstall_prep_always_says_it_is_safe_to_remove()
    {
        var toast = Texts.AfterLoad(true, 0, 0, 0, zh: false);

        Assert.Equal("Pack Rat is safe to remove", toast?.Title);
        Assert.Contains("Save the game", toast?.Body);
    }

    [Fact]
    public void Moved_items_are_counted_by_where_they_went()
    {
        var toast = Texts.AfterLoad(false, 3, 1, 0, zh: false);

        Assert.Equal("4 items no longer fit", toast?.Title);
        Assert.Equal("3 went into the shopping bags in the living room, 1 went on the floor.", toast?.Body);
    }

    [Fact]
    public void Chinese_counts_each_place()
    {
        var toast = Texts.AfterLoad(false, 2, 0, 1, zh: true);

        Assert.Equal("3 件物品放不下了", toast?.Title);
        Assert.Equal("2 件装进了客厅的购物袋。1 件留在了原来的背包里。", toast?.Body);
    }
}
