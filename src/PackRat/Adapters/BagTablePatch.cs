using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using PackRat.Core;

namespace PackRat.Adapters;

/// <summary>
/// Bags, storage and vehicles copy their carry limit from the game's bag table when they are made, and that happens
/// while the world starts and loads the save. Writing the table at the top of BattleLogicWorld.Initialize lands before
/// any of them, so every one of them gets the new limit without patching the bags themselves.
/// </summary>
internal static class BagTablePatch
{
    private static readonly LimitTracker Tracker = new();
    private static double _multiplier = 1;
    private static Action<string> _log = _ => { };

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;

        try
        {
            harmony.Patch(
                AccessTools.Method(typeof(BattleLogicWorld), nameof(BattleLogicWorld.Initialize)),
                prefix: new HarmonyMethod(typeof(BagTablePatch), nameof(BeforeWorldStart)));
            log("hooked the world start");
        }
        catch (Exception ex)
        {
            log($"could not hook BattleLogicWorld.Initialize, carry weight keeps the game's limits: {ex.Message}");
        }
    }

    /// <summary>Used from the next time a save loads.</summary>
    public static void UseMultiplier(double multiplier)
    {
        _multiplier = multiplier;
        _log($"carry weight will be {multiplier}x from the next save load");
    }

    private static void BeforeWorldStart()
    {
        try
        {
            var table = BaseSingleton<ConfigManager>.IsInstanceCreated ? BaseSingleton<ConfigManager>.Instance?._Config_Bag_Dict : null;

            if (table == null || table.Count == 0)
            {
                _log("the bag table is not loaded yet, carry weight keeps the game's limits for this save");

                return;
            }

            var changed = 0;

            foreach (var row in table.Values)
            {
                if (row == null) continue;

                if (Tracker.Next(row._ID_k__BackingField, row._Burden_k__BackingField, _multiplier) is not int value) continue;

                row._Burden_k__BackingField = value;
                changed++;
            }

            var backpack = table.TryGetValue(1, out var first) && first != null ? first._Burden_k__BackingField : 0;

            _log($"carry weight {_multiplier}x: {changed} of {table.Count} bag rows changed, backpack limit {Tracker.GameLimit(1)} -> {backpack}");
        }
        catch (Exception ex)
        {
            _log($"could not change carry weight: {ex.Message}");
        }
    }
}
