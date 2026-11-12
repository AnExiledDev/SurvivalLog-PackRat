using System;
using ExiledToolkit.Game;
using GameCore.HotUpdate.Battle.Logic;
using PackRat.Core;

namespace PackRat.Adapters;

/// <summary>
/// Bags, storage and vehicles copy their grid and weight limit from the game's bag table when they are made, and items
/// take their size and weight from the item table as they are added. Both happen while the world starts and loads the
/// save, so the tables are written at the top of BattleLogicWorld.Initialize and again just before the save's items
/// load. While the items load, an item that does not fit is held back for the shopping bags (see <see cref="Overflow"/>).
/// </summary>
internal static class LoadHooks
{
    private static Func<TableSettings> _settings = () => TableSettings.GameDefaults;
    private static Action<string> _log = _ => { };

    public static void Apply(PatchKit hooks, Func<TableSettings> settings, Action<string> log)
    {
        _settings = settings;
        _log = log;

        hooks.Patch(typeof(LoadHooks), typeof(BattleLogicWorld), nameof(BattleLogicWorld.Initialize), prefix: nameof(BeforeWorldStart),
            done: "hooked the world start", consequence: "sizes and weights keep the game's values until the items load");
        hooks.Patch(typeof(LoadHooks), typeof(ItemManager), nameof(ItemManager.LoadItemsFromSave), prefix: nameof(BeforeItemsLoad), postfix: nameof(AfterItemsLoad), finalizer: nameof(ItemsLoadEnded),
            done: "hooked the item load", consequence: "items that no longer fit drop to the floor, as they do without Pack Rat");
        hooks.Patch(typeof(LoadHooks), typeof(DropItemManager), nameof(DropItemManager.TryDropOverflowItemData), prefix: nameof(BeforeOverflowDrop),
            done: "hooked the overflow drop", consequence: "items that no longer fit drop to the floor, as they do without Pack Rat");
    }

    private static void BeforeWorldStart() => WriteTables("world start");

    private static void BeforeItemsLoad()
    {
        WriteTables("item load");
        Overflow.StartLoad();
    }

    private static void AfterItemsLoad() => Overflow.EndLoad();

    /// <summary>Ends the load even when the game's load throws, so a later drop is never held back by mistake.</summary>
    private static Exception? ItemsLoadEnded(Exception? __exception)
    {
        if (__exception != null) Overflow.EndLoad();

        return __exception;
    }

    /// <summary>A held item stays where the save had it until the shopping bags take it; the game then keeps it in its bag.</summary>
    private static bool BeforeOverflowDrop(ItemData itemData, long intendedOwnerId, ref bool __result)
    {
        try
        {
            if (!Overflow.Hold(itemData, intendedOwnerId)) return true;

            __result = false;

            return false;
        }
        catch (Exception ex)
        {
            _log("could not hold an item back for the shopping bags, the game drops it: " + ex.Message);

            return true;
        }
    }

    private static void WriteTables(string when)
    {
        try
        {
            var line = GameTables.Write(_settings());

            _log(line == null ? $"{when}: the game's tables are not loaded yet" : $"{when}: {line}");
        }
        catch (Exception ex)
        {
            _log($"{when}: could not write the game's tables: {ex.Message}");
        }
    }
}
