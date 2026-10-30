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

    private static readonly Size Pack = new(8, 7);
    private static readonly Size Cupboard = new(8, 6);
    private static readonly Size Fridge7 = new(8, 7);

    [Theory]
    [InlineData(1, 8, 7, Category.Bags)]
    [InlineData(11, 9, 11, Category.Bags)]
    [InlineData(3, 12, 8, Category.Vehicles)]
    [InlineData(4, 14, 10, Category.Vehicles)]
    [InlineData(1022, 10, 8, Category.Vehicles)]
    [InlineData(1023, 12, 10, Category.Vehicles)]
    [InlineData(115003, 14, 10, Category.Vehicles)]
    [InlineData(1064, 2, 2, Category.SmallStorage)]
    [InlineData(1060, 6, 4, Category.SmallStorage)]
    [InlineData(5008, 7, 4, Category.MediumStorage)]
    [InlineData(5014, 10, 6, Category.MediumStorage)]
    [InlineData(1003, 8, 10, Category.LargeStorage)]
    [InlineData(1005, 10, 14, Category.LargeStorage)]
    [InlineData(110008, 15, 12, Category.ExtraLargeStorage)]
    public void Inventories_fall_into_categories_of_about_the_same_size(int id, int w, int h, Category category)
    {
        Assert.Equal(category, Categories.Of(id, new Size(w, h)));
    }

    [Fact]
    public void No_file_means_the_game_grids()
    {
        Assert.True(Inventories.TryParse(null, out var settings, out _));
        Assert.Equal(new Size(10, 8), settings.GridOf(Backpack, new Size(10, 8)));
    }

    [Fact]
    public void A_category_multiplier_beats_all()
    {
        var settings = Parse("""{"scale":{"all":1.5},"cats":{"vehicles":{"x":2}}}""");

        Assert.Equal(new Size(20, 16), settings.GridOf(Car, new Size(10, 8)));
        Assert.Equal(new Size(15, 12), settings.GridOf(Backpack, new Size(10, 8)));
    }

    /// <summary>Setting a category's size is the point of categories: every inventory in it gets that grid at once.</summary>
    [Fact]
    public void A_category_size_sets_every_inventory_in_it()
    {
        var settings = Parse("""{"scale":{"all":2},"cats":{"medium":{"w":10,"h":9}}}""");

        Assert.Equal(new Size(10, 9), settings.GridOf(1001, Cupboard));
        Assert.Equal(new Size(10, 9), settings.GridOf(Fridge, Fridge7));
        Assert.Equal(new Size(16, 14), settings.GridOf(Backpack, Pack));
    }

    [Fact]
    public void A_category_side_left_unset_follows_the_multiplier()
    {
        var settings = Parse("""{"cats":{"medium":{"x":2,"h":5}}}""");

        Assert.Equal(new Size(16, 5), settings.GridOf(1001, Cupboard));
    }

    [Fact]
    public void An_inventory_own_size_beats_its_category_side_by_side()
    {
        var settings = Parse("""{"cats":{"medium":{"w":10,"h":9}},"rows":{"1004":{"h":12}}}""");

        Assert.Equal(new Size(10, 12), settings.GridOf(Fridge, Fridge7));
        Assert.Equal(new Size(10, 9), settings.GridOf(1001, Cupboard));
    }

    /// <summary>A file saved before the categories keeps doing what it did for bags, vehicles and storage.</summary>
    [Fact]
    public void Old_group_multipliers_become_category_multipliers()
    {
        var settings = Parse("""{"scale":{"bag":1.5,"vehicle":2,"storage":1.25}}""");

        Assert.Equal(1.5, settings.ScaleOf(Category.Bags));
        Assert.Equal(2, settings.ScaleOf(Category.Vehicles));

        foreach (var category in Categories.Storage) Assert.Equal(1.25, settings.ScaleOf(category));

        Assert.Equal("""{"scale":{},"cats":{"bags":{"x":1.5},"vehicles":{"x":2},"small":{"x":1.25},"medium":{"x":1.25},"large":{"x":1.25},"xlarge":{"x":1.25}},"rows":{}}""", settings.ToJson());
    }

    [Fact]
    public void Unknown_categories_and_empty_rules_are_left_out()
    {
        var settings = Parse("""{"cats":{"huge":{"w":4},"small":{"x":1},"bags":{"w":"wide"}}}""");

        Assert.Empty(settings.Cats);
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
        var settings = Parse("""{"scale":{"all":50},"cats":{"large":{"x":0.1,"w":99}},"rows":{"1":{"w":999,"h":-3,"kg":0}}}""");

        Assert.Equal(Inventories.MaxScale, settings.All);
        Assert.Equal(new CategoryRule(Inventories.MinScale, Inventories.MaxCells, null), settings.Cats[Category.LargeStorage]);
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
        var json = """{"scale":{"all":1.25},"cats":{"vehicles":{"x":1.5},"large":{"w":12,"h":10}},"rows":{"1":{"w":10,"h":9},"1004":{"kg":40}}}""";

        Assert.Equal(json, Parse(json).ToJson());
    }
}
