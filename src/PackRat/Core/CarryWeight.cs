using System;
using System.Collections.Generic;

namespace PackRat.Core;

/// <summary>
/// Every bag, storage piece and vehicle takes its carry limit from one row of the game's bag table, in the game's
/// weight units (a backpack is 960000). A limit of 0 means the game sets no limit there.
/// </summary>
public static class CarryWeight
{
    public const double MinMultiplier = 0.1;
    public const double MaxMultiplier = 1000;

    /// <summary>
    /// The game adds talent bonuses on top and keeps the result in a whole number that stops near 2.1 billion, so a
    /// limit is never raised past this. It still leaves room for the bonuses to double it.
    /// </summary>
    public const int MaxLimit = 1_000_000_000;

    public static double ClampMultiplier(double multiplier) =>
        double.IsNaN(multiplier) ? 1 : Math.Clamp(multiplier, MinMultiplier, MaxMultiplier);

    /// <summary>The game's limit times <paramref name="multiplier"/>. No limit stays no limit, and a limit never drops to 0, since 0 would remove it.</summary>
    public static int Scaled(int gameLimit, double multiplier)
    {
        if (gameLimit <= 0) return gameLimit;

        var scaled = Math.Round(gameLimit * ClampMultiplier(multiplier), MidpointRounding.AwayFromZero);

        return (int)Math.Clamp(scaled, 1, MaxLimit);
    }
}

/// <summary>
/// Keeps each row's own limit. A row whose value is not the one this mod last wrote holds the game's own limit (the
/// table was loaded again, or this is the first look), so a new multiplier is always worked out from the game's
/// number, never from an already raised one.
/// </summary>
public sealed class LimitTracker
{
    private readonly Dictionary<int, int> _gameLimits = new();
    private readonly Dictionary<int, int> _written = new();

    /// <summary>The value to write for row <paramref name="id"/>, or null when it already holds it.</summary>
    public int? Next(int id, int current, double multiplier)
    {
        if (!_written.TryGetValue(id, out var written) || written != current) _gameLimits[id] = current;

        var scaled = CarryWeight.Scaled(_gameLimits[id], multiplier);

        _written[id] = scaled;

        return scaled == current ? null : scaled;
    }

    /// <summary>The game's own limit for row <paramref name="id"/>, as last seen before this mod changed it.</summary>
    public int GameLimit(int id) => _gameLimits.TryGetValue(id, out var limit) ? limit : 0;
}
