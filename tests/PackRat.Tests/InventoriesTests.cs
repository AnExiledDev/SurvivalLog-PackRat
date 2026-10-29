using PackRat.Core;
using Xunit;

namespace PackRat.Tests;

public class InventoriesTests
{
    private const int Backpack = 1;
    private const int Car = 3;
    private const int Fridge = 1004;

    private static Inventories Parse(string json)
    {
        Assert.True(Inventories.TryParse(json, out var settings, out var error), error);

        return settings;
    }

    [Theory]
    [InlineData(1, BagGroup.Bag)]
    [InlineData(999, BagGroup.Bag)]
    [InlineData(3, BagGroup.Vehicle)]
    [InlineData(4, BagGroup.Vehicle)]
    [InlineData(1000, BagGroup.Storage)]
    public void Rows_fall_into_the_game_groups(int id, BagGroup group)
    {
        Assert.Equal(group, BagGroups.Of(id));
    }

    [Fact]
    public void No_file_means_the_game_grids()
    {
        Assert.True(Inventories.TryParse(null, out var settings, out _));
        Assert.Equal(new Size(10, 8), settings.GridOf(Backpack, new Size(10, 8)));
    }

    [Fact]
    public void A_group_multiplier_beats_all()
    {
        var settings = Parse("""{"scale":{"all":1.5,"vehicle":2}}""");

        Assert.Equal(new Size(20, 16), settings.GridOf(Car, new Size(10, 8)));
        Assert.Equal(new Size(15, 12), settings.GridOf(Backpack, new Size(10, 8)));
    }

    [Fact]
    public void A_row_own_size_beats_the_multiplier_side_by_side()
    {
        var settings = Parse("""{"scale":{"all":2},"rows":{"1":{"w":12}}}""");

        Assert.Equal(new Size(12, 16), settings.GridOf(Backpack, new Size(10, 8)));
    }

    [Fact]
    public void A_shrunk_grid_keeps_at_least_one_cell()
    {
        var settings = Parse("""{"scale":{"all":0.5}}""");

        Assert.Equal(new Size(1, 2), settings.GridOf(Backpack, new Size(1, 3)));
    }

    [Fact]
    public void A_row_own_kg_beats_carry_weight()
    {
        var settings = Parse("""{"rows":{"1004":{"kg":40}}}""");

        Assert.Equal(40_000, settings.LimitOf(Fridge, 12_000, 2));
        Assert.Equal(19_200, settings.LimitOf(Backpack, 9_600, 2));
    }

    [Fact]
    public void Out_of_range_values_are_pulled_into_range()
    {
        var settings = Parse("""{"scale":{"all":50},"rows":{"1":{"w":999,"h":-3,"kg":0}}}""");

        Assert.Equal(Inventories.MaxScale, settings.All);
        Assert.Equal(new GridRow(Inventories.MaxCells, Inventories.MinCells, Inventories.MinKg), settings.Rows[Backpack]);
    }

    [Fact]
    public void Entries_that_are_not_numbers_are_left_out()
    {
        var settings = Parse("""{"scale":{"all":"big"},"rows":{"x":{"w":4},"1":{"w":"wide"}}}""");

        Assert.Equal(1, settings.All);
        Assert.Empty(settings.Rows);
    }

    [Fact]
    public void Text_that_is_not_an_object_fails_with_a_reason()
    {
        Assert.False(Inventories.TryParse("{not json", out _, out var error));
        Assert.NotEmpty(error);
        Assert.False(Inventories.TryParse("[1,2]", out _, out _));
    }

    [Fact]
    public void The_file_round_trips()
    {
        var json = """{"scale":{"all":1.25,"vehicle":1.5},"rows":{"1":{"w":10,"h":9},"1004":{"kg":40}}}""";

        Assert.Equal(json, Parse(json).ToJson());
    }
}
