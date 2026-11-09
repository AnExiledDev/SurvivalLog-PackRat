namespace PackRat.Core;

/// <summary>Everything that decides what goes into the game's tables when a save loads.</summary>
public sealed record TableSettings(
    double CarryWeight,
    double ItemWeight,
    OverweightMode Overweight,
    int SlowFrom,
    int SlowBy,
    Inventories Grids,
    Items Items)
{
    /// <summary>The game's values everywhere: what Uninstall prep writes back.</summary>
    public static readonly TableSettings GameDefaults = new(1, 1, OverweightMode.Default, 80, 40, Inventories.Default, Items.Default);
}
