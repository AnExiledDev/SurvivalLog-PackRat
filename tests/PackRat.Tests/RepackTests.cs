using PackRat.Core;
using Xunit;

namespace PackRat.Tests;

/// <summary>
/// After a bag shrinks or an item grows mid-game, items that no longer fit must move somewhere they do fit, and the
/// ones that already sit fine must not be shuffled around under the player.
/// </summary>
public class RepackTests
{
    private static Placed Item(long id, int w, int h, int x, int y) => new(id, new Size(w, h), new Cell(x, y));

    [Fact]
    public void Items_that_still_fit_stay_where_they_are()
    {
        var plan = Repack.Plan(new Size(4, 4), new[] { Item(1, 2, 2, 0, 0), Item(2, 2, 2, 2, 2) });

        Assert.Same(RepackPlan.Nothing, plan);
    }

    [Fact]
    public void An_item_past_the_new_edge_moves_to_the_first_free_spot()
    {
        // The bag was 6 wide and is now 4: the item at column 4 hangs off the edge.
        var plan = Repack.Plan(new Size(4, 3), new[] { Item(1, 2, 1, 0, 0), Item(2, 1, 1, 4, 0) });

        Assert.Equal(new[] { (2L, new Cell(2, 0)) }, plan.Moves);
        Assert.Empty(plan.NoRoom);
    }

    [Fact]
    public void A_grown_item_that_now_overlaps_another_moves_and_the_first_one_keeps_its_place()
    {
        // Item 1 grew from 1x1 to 2x1 and now covers item 2 at (1,0).
        var plan = Repack.Plan(new Size(3, 2), new[] { Item(1, 2, 1, 0, 0), Item(2, 1, 1, 1, 0) });

        Assert.Equal(new[] { (2L, new Cell(2, 0)) }, plan.Moves);
    }

    [Fact]
    public void The_biggest_homeless_item_is_placed_first()
    {
        // Two items off the edge; only the big one fits the 2x2 hole if it goes first.
        var plan = Repack.Plan(new Size(2, 3), new[] { Item(1, 2, 1, 0, 0), Item(2, 1, 1, 5, 5), Item(3, 2, 2, 5, 0) });

        Assert.Equal(new[] { (3L, new Cell(0, 1)) }, plan.Moves);
        Assert.Equal(new[] { 2L }, plan.NoRoom);
    }

    [Fact]
    public void Items_with_no_room_left_are_named()
    {
        var plan = Repack.Plan(new Size(2, 1), new[] { Item(1, 2, 1, 0, 0), Item(2, 1, 1, 3, 0) });

        Assert.Empty(plan.Moves);
        Assert.Equal(new[] { 2L }, plan.NoRoom);
    }

    [Fact]
    public void An_item_bigger_than_the_whole_grid_has_no_room()
    {
        var plan = Repack.Plan(new Size(2, 2), new[] { Item(1, 3, 1, 0, 0) });

        Assert.Equal(new[] { 1L }, plan.NoRoom);
    }
}
