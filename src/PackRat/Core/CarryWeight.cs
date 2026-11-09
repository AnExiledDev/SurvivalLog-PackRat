using System;

namespace PackRat.Core;

/// <summary>
/// Every bag, storage piece and vehicle takes its carry limit from one row of the game's bag table, in the game's
/// weight units (grams: a backpack is 9600). A limit of 0 means the game sets no limit there.
/// </summary>
public static class CarryWeight
{
    // Out of the box the game's limit stands; the player raises it.
    public const double DefaultMultiplier = 1;
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
