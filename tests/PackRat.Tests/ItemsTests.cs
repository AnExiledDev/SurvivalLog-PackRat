using PackRat.Core;
using Xunit;

namespace PackRat.Tests;

public class ItemsTests
{
    private const int Furniture = 14;
    private const int Food = Items.Food;
    private const int Canned = 3;

    private static readonly Size Big = new(3, 2);

    private static Items Parse(string json)
    {
        Assert.True(Items.TryParse(json, out var settings, out var error), error);

        return settings;
    }

    [Fact]
    public void The_size_rules_go_item_then_food_group_then_category_then_all_one()
    {
        var settings = Parse("""{"allOne":true,"cats":{"1":{"size":[2,2]}},"subs":{"3":{"size":[1,2]}},"ids":{"10231":{"size":[4,3]}}}""");

        Assert.Equal(new Size(4, 3), settings.SizeOf(10231, Food, Canned, Big));
        Assert.Equal(new Size(1, 2), settings.SizeOf(1, Food, Canned, Big));
        Assert.Equal(new Size(2, 2), settings.SizeOf(1, Food, 9, Big));
        Assert.Equal(new Size(1, 1), settings.SizeOf(1, Furniture, 0, Big));
    }

    [Fact]
    public void A_food_group_rule_only_reaches_food()
    {
        var settings = Parse("""{"subs":{"3":{"size":[1,1],"x":0.5}}}""");

        Assert.Equal(Big, settings.SizeOf(1, Furniture, Canned, Big));
        Assert.Equal(1000, settings.GramsOf(1, Furniture, Canned, 1000, 1));
    }

    [Fact]
    public void No_rule_keeps_the_game_size()
    {
        Assert.Equal(Big, Items.Default.SizeOf(1, Furniture, 0, Big));
    }

    [Fact]
    public void The_weight_rules_go_item_grams_then_group_then_category_then_item_weight()
    {
        var settings = Parse("""{"cats":{"1":{"x":2}},"subs":{"3":{"x":0.5}},"ids":{"10231":{"g":200}}}""");

        Assert.Equal(200, settings.GramsOf(10231, Food, Canned, 1000, 3));
        Assert.Equal(500, settings.GramsOf(1, Food, Canned, 1000, 3));
        Assert.Equal(2000, settings.GramsOf(1, Food, 9, 1000, 3));
        Assert.Equal(3000, settings.GramsOf(1, Furniture, 0, 1000, 3));
    }

    [Fact]
    public void An_item_with_no_weight_stays_weightless()
    {
        Assert.Equal(0, Items.Default.GramsOf(1, Furniture, 0, 0, 3));
        Assert.Equal(-1, Items.Default.GramsOf(1, Furniture, 0, -1, 3));
    }

    [Fact]
    public void Out_of_range_item_weight_is_held_to_its_range()
    {
        Assert.Equal(3000, Items.Default.GramsOf(1, Furniture, 0, 1000, 99));
        Assert.Equal(100, Items.Default.GramsOf(1, Furniture, 0, 1000, 0));
    }

    [Fact]
    public void Sizes_are_pulled_into_range_and_bad_pairs_left_out()
    {
        var settings = Parse("""{"cats":{"14":{"size":[0,20]},"2":{"size":[1]},"3":{"size":"big"}}}""");

        Assert.Equal(new Size(Items.MinSide, Items.MaxSide), settings.Cats[14].Size);
        Assert.False(settings.Cats.ContainsKey(2));
        Assert.False(settings.Cats.ContainsKey(3));
    }

    [Fact]
    public void Text_that_is_not_an_object_fails()
    {
        Assert.False(Items.TryParse("nope", out _, out var error));
        Assert.NotEmpty(error);
    }

    [Fact]
    public void The_file_round_trips()
    {
        var json = """{"allOne":false,"cats":{"14":{"size":[2,2]}},"subs":{"3":{"x":0.5}},"ids":{"10231":{"size":[1,1],"g":200}}}""";

        Assert.Equal(json, Parse(json).ToJson());
    }
}
