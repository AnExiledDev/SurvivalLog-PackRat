using PackRat.Core;
using Xunit;

namespace PackRat.Tests;

public class CarryWeightTests
{
    [Fact]
    public void A_multiplier_raises_the_game_limit()
    {
        Assert.Equal(1_920_000, CarryWeight.Scaled(960_000, 2));
        Assert.Equal(960_000_000, CarryWeight.Scaled(960_000, 1000));
    }

    [Fact]
    public void Out_of_the_box_every_limit_is_the_game_own()
    {
        Assert.Equal(960_000, CarryWeight.Scaled(960_000, CarryWeight.DefaultMultiplier));
    }

    [Fact]
    public void A_multiplier_below_one_lowers_it()
    {
        Assert.Equal(480_000, CarryWeight.Scaled(960_000, 0.5));
    }

    [Fact]
    public void A_place_with_no_limit_keeps_no_limit()
    {
        Assert.Equal(0, CarryWeight.Scaled(0, 1000));
    }

    [Fact]
    public void A_limit_never_drops_to_zero_which_would_remove_it()
    {
        Assert.Equal(1, CarryWeight.Scaled(3, CarryWeight.MinMultiplier));
    }

    [Fact]
    public void A_huge_limit_stops_short_of_the_game_number_overflowing()
    {
        // The car is 4,800,000; 1000x would pass the largest whole number the game can hold.
        Assert.Equal(CarryWeight.MaxLimit, CarryWeight.Scaled(4_800_000, 1000));
        Assert.True((long)CarryWeight.MaxLimit * 2 < int.MaxValue);
    }

    [Fact]
    public void Out_of_range_settings_are_held_to_the_limits()
    {
        Assert.Equal(CarryWeight.Scaled(12_000, CarryWeight.MaxMultiplier), CarryWeight.Scaled(12_000, 1_000_000));
        Assert.Equal(CarryWeight.Scaled(12_000, CarryWeight.MinMultiplier), CarryWeight.Scaled(12_000, -5));
        Assert.Equal(12_000, CarryWeight.Scaled(12_000, double.NaN));
    }
}
