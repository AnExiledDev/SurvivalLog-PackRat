using PackRat.Core;
using Xunit;

namespace PackRat.Tests;

public class OverweightTests
{
    [Theory]
    [InlineData("Default", OverweightMode.Default)]
    [InlineData("Custom", OverweightMode.Custom)]
    [InlineData("Hard limit", OverweightMode.HardLimit)]
    [InlineData("something else", OverweightMode.Default)]
    [InlineData(null, OverweightMode.Default)]
    public void The_config_choice_names_a_mode(string? name, OverweightMode mode)
    {
        Assert.Equal(mode, Overweight.Parse(name));
    }

    [Fact]
    public void Default_gives_back_the_game_values()
    {
        Assert.Equal((0.8f, -0.4f), Overweight.Values(OverweightMode.Default, 50, 10, 0.8f, -0.4f));
    }

    [Fact]
    public void Custom_turns_percents_into_the_table_values()
    {
        Assert.Equal((0.5f, -0.1f), Overweight.Values(OverweightMode.Custom, 50, 10, 0.8f, -0.4f));
    }

    [Fact]
    public void Custom_is_held_to_its_ranges()
    {
        Assert.Equal((1f, -0.9f), Overweight.Values(OverweightMode.Custom, 500, 500, 0.8f, -0.4f));
        Assert.Equal((0.1f, 0f), Overweight.Values(OverweightMode.Custom, 0, -5, 0.8f, -0.4f));
    }

    [Fact]
    public void Hard_limit_moves_the_start_to_the_limit_with_no_slowdown()
    {
        Assert.Equal((1f, 0f), Overweight.Values(OverweightMode.HardLimit, 50, 10, 0.8f, -0.4f));
    }
}

public class GameValuesTests
{
    [Fact]
    public void The_first_value_seen_is_the_game_value()
    {
        var values = new GameValues<int, int>();

        Assert.Equal(9_600, values.Game(1, 9_600));
    }

    [Fact]
    public void The_value_this_mod_wrote_is_never_taken_for_the_game_value()
    {
        var values = new GameValues<int, int>();

        values.Game(1, 9_600);
        values.Wrote(1, 19_200);

        Assert.Equal(9_600, values.Game(1, 19_200));
    }

    [Fact]
    public void A_table_loaded_again_by_the_game_is_read_again()
    {
        var values = new GameValues<int, int>();

        values.Game(1, 9_600);
        values.Wrote(1, 19_200);

        Assert.Equal(12_000, values.Game(1, 12_000));
    }

    [Fact]
    public void Each_key_keeps_its_own_game_value()
    {
        var values = new GameValues<int, Size>();

        values.Game(1, new Size(10, 8));
        values.Game(4, new Size(6, 6));
        values.Wrote(1, new Size(20, 16));

        Assert.Equal(new Size(10, 8), values.Game(1, new Size(20, 16)));
        Assert.Equal(new Size(6, 6), values.Game(4, new Size(6, 6)));
    }
}
