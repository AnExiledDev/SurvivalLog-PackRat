using System;
using System.Collections.Generic;

namespace PackRat.Core;

public enum OverweightMode
{
    Default,
    Custom,
    HardLimit,
}

/// <summary>
/// The game slows you down once you carry more than a share of your limit (0.8), by a speed change (-0.4). Past the
/// limit itself it refuses to pick things up, which no table value changes, so there is no "off".
/// </summary>
public static class Overweight
{
    public const string DefaultName = "Default";
    public const string CustomName = "Custom";
    public const string HardLimitName = "Hard limit";

    public static readonly string[] Choices = { DefaultName, CustomName, HardLimitName };

    public const int MinSlowFrom = 10;
    public const int MaxSlowFrom = 100;
    public const int MinSlowBy = 0;
    public const int MaxSlowBy = 90;

    public static OverweightMode Parse(string? name) => name switch
    {
        CustomName => OverweightMode.Custom,
        HardLimitName => OverweightMode.HardLimit,
        _ => OverweightMode.Default,
    };

    /// <summary>
    /// The share of the limit where the slowdown starts and the speed change past it. Default gives back the game's
    /// own values; Hard limit moves the start to the limit, where pickup already stops, with no slowdown.
    /// </summary>
    public static (float Threshold, float Speed) Values(OverweightMode mode, int slowFrom, int slowBy, float gameThreshold, float gameSpeed) => mode switch
    {
        OverweightMode.Custom => (Math.Clamp(slowFrom, MinSlowFrom, MaxSlowFrom) / 100f, -Math.Clamp(slowBy, MinSlowBy, MaxSlowBy) / 100f),
        OverweightMode.HardLimit => (1f, 0f),
        _ => (gameThreshold, gameSpeed),
    };
}

/// <summary>
/// Keeps the game's own value for each key of a table this mod writes. A value that is not the one this mod last
/// wrote is the game's (the table was loaded again, or this is the first look), so every new setting is worked out
/// from the game's value, never from one already changed.
/// </summary>
public sealed class GameValues<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _game = new();
    private readonly Dictionary<TKey, TValue> _written = new();

    /// <summary>The game's value for <paramref name="key"/>, given what the table holds now.</summary>
    public TValue Game(TKey key, TValue current)
    {
        if (!_written.TryGetValue(key, out var written) || !EqualityComparer<TValue>.Default.Equals(written, current)) _game[key] = current;

        return _game[key];
    }

    public void Wrote(TKey key, TValue value) => _written[key] = value;
}
