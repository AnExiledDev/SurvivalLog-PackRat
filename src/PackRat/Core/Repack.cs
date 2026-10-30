using System;
using System.Collections.Generic;

namespace PackRat.Core;

/// <summary>A cell in a grid: X is the column, Y the row, both from 0 at the top left.</summary>
public readonly record struct Cell(int X, int Y);

/// <summary>An item as it sits in its bag.</summary>
public readonly record struct Placed(long Id, Size Size, Cell At);

/// <summary>Where the items that had to move go, and the ones with no room left.</summary>
public sealed record RepackPlan(IReadOnlyList<(long Id, Cell At)> Moves, IReadOnlyList<long> NoRoom)
{
    public static readonly RepackPlan Nothing = new(Array.Empty<(long, Cell)>(), Array.Empty<long>());

    public bool IsEmpty => Moves.Count == 0 && NoRoom.Count == 0;
}

/// <summary>
/// After a grid or an item size changes mid-game, some items may hang over the edge or sit on top of each other.
/// Every item that still fits where it is stays there, in the bag's own order; the rest go into the first free spot,
/// biggest first, row by row from the top left. Whatever finds no spot is left for the shopping bags.
/// </summary>
public static class Repack
{
    public static RepackPlan Plan(Size grid, IReadOnlyList<Placed> items)
    {
        if (grid.W <= 0 || grid.H <= 0 || items.Count == 0) return RepackPlan.Nothing;

        var taken = new bool[grid.W, grid.H];
        var homeless = new List<Placed>();

        foreach (var item in items)
        {
            if (Fits(taken, grid, item.At, item.Size)) Take(taken, item.At, item.Size);
            else homeless.Add(item);
        }

        if (homeless.Count == 0) return RepackPlan.Nothing;

        // Biggest first leaves the small ones for the gaps; equal sizes keep the bag's order.
        var order = new List<(Placed Item, int Index)>(homeless.Count);
        for (var i = 0; i < homeless.Count; i++) order.Add((homeless[i], i));
        order.Sort((a, b) =>
        {
            var byArea = (b.Item.Size.W * b.Item.Size.H).CompareTo(a.Item.Size.W * a.Item.Size.H);

            return byArea != 0 ? byArea : a.Index.CompareTo(b.Index);
        });

        var moves = new List<(long, Cell)>();
        var noRoom = new List<long>();

        foreach (var (item, _) in order)
        {
            if (FirstFree(taken, grid, item.Size) is Cell at)
            {
                Take(taken, at, item.Size);
                moves.Add((item.Id, at));
            }
            else
            {
                noRoom.Add(item.Id);
            }
        }

        return new RepackPlan(moves, noRoom);
    }

    private static Cell? FirstFree(bool[,] taken, Size grid, Size size)
    {
        for (var y = 0; y + size.H <= grid.H; y++)
        {
            for (var x = 0; x + size.W <= grid.W; x++)
            {
                var at = new Cell(x, y);

                if (Fits(taken, grid, at, size)) return at;
            }
        }

        return null;
    }

    private static bool Fits(bool[,] taken, Size grid, Cell at, Size size)
    {
        if (size.W <= 0 || size.H <= 0 || at.X < 0 || at.Y < 0 || at.X + size.W > grid.W || at.Y + size.H > grid.H) return false;

        for (var x = at.X; x < at.X + size.W; x++)
        {
            for (var y = at.Y; y < at.Y + size.H; y++)
            {
                if (taken[x, y]) return false;
            }
        }

        return true;
    }

    private static void Take(bool[,] taken, Cell at, Size size)
    {
        for (var x = at.X; x < at.X + size.W; x++)
        {
            for (var y = at.Y; y < at.Y + size.H; y++) taken[x, y] = true;
        }
    }
}
