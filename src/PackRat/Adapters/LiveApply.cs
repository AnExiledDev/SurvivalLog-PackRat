using System;
using System.Collections.Generic;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using PackRat.Core;
using UnityEngine;
using Size = PackRat.Core.Size;

namespace PackRat.Adapters;

/// <summary>
/// Applies a change made in the middle of a game right away. Writing the tables is enough for a bag's grid (the game
/// reads it from the table each time a bag opens) and for an item's weight (read each time it is weighed). The rest
/// the game copies when a bag or item is made, so it is brought up to date here: every bag's weight limit, every item's
/// size, every bag's weight total, and the items a smaller grid or a bigger item no longer fits, which move to a free
/// spot in their bag or, with no room left, into the shopping bags.
/// </summary>
internal static class LiveApply
{
    /// <summary>A short wait, so a run of changes (a held button, a typed number) is applied once.</summary>
    private const float WaitSeconds = 0.3f;

    /// <summary>The game's shopping bags by the door use this bag table row for their limit and slot count.</summary>
    private const int DoorBoxBagId = 1023;

    private static readonly Dictionary<long, int> WeightBefore = new();
    private static readonly HashSet<long> Touched = new();
    private static readonly List<long> Owners = new();
    private static readonly List<Placed> Layout = new();
    private static readonly Dictionary<long, ItemData> ById = new();
    private static readonly List<ItemData> NoRoom = new();

    private static volatile bool _isWanted;
    private static float _dueAt = -1;

    internal static Func<Settings?> Settings = () => null;
    internal static Action<string> Log = _ => { };

    /// <summary>Items that had no room after a change went into the shopping bags or on the floor.</summary>
    public static event Action<Overflow.Placed>? Moved;

    /// <summary>Asks for the current settings to be applied on the game's thread. Safe from any thread.</summary>
    public static void Request() => _isWanted = true;

    public static void Tick(float now)
    {
        if (!_isWanted) return;

        if (_dueAt < 0) _dueAt = now + WaitSeconds;
        if (now < _dueAt) return;

        _isWanted = false;
        _dueAt = -1;
        Run();
    }

    private static void Run()
    {
        var settings = Settings();

        if (settings == null || settings.IsUninstallPrep) return;

        var world = GameSingleton.Existing<BattleLogicWorld>();
        var player = world?._AgentManager?.GetLeadingRole();
        var items = world?._ItemManager;

        // Outside a game, or while one loads, the load writes the tables itself.
        if (world == null || player == null || items == null || !Overflow.IsIdle) return;

        var cache = items._Cache_k__BackingField;

        if (cache == null) return;

        WeightBefore.Clear();
        foreach (var item in cache.Values)
        {
            if (item != null) WeightBefore[item.InstanceId] = items.GetItemActualWeight(item);
        }

        var line = GameTables.Write(settings.Current());

        if (line == null) return;

        Touched.Clear();

        var resized = ResizeItems(cache);
        var limits = RefreshLimits(world._AgentManager);
        var reweighed = Reweigh(items, cache);
        var moved = GameTables.ChangedGrids > 0 || resized > 0 ? Repack(items) : 0;

        foreach (var owner in Touched)
        {
            if (owner > 0) items.SyncSingleBag(owner);
        }

        Log($"applied in game: {line}; {resized} items resized, {limits} weight limits refreshed, {reweighed} item weights updated, {moved} items moved, {NoRoom.Count} had no room");

        if (NoRoom.Count == 0) return;

        var placed = Overflow.Place(world, player, NoRoom);

        NoRoom.Clear();
        Moved?.Invoke(placed);
    }

    /// <summary>An item keeps the size it was made with; one that has its item's old size gets the new one.</summary>
    private static int ResizeItems(Il2CppSystem.Collections.Generic.Dictionary<long, ItemData> cache)
    {
        var resized = GameTables.ResizedItems;

        if (resized.Count == 0) return 0;

        var count = 0;

        foreach (var item in cache.Values)
        {
            if (item == null || !resized.TryGetValue(item._ItemConfigId_k__BackingField, out var change)) continue;

            var size = item._ItemSize_k__BackingField;

            if (size.x != change.Was.W || size.y != change.Was.H) continue;

            item._ItemSize_k__BackingField = new Vector2Int(change.Now.W, change.Now.H);
            Touched.Add(item._OwnerId_k__BackingField);
            count++;
        }

        return count;
    }

    /// <summary>Bags, storage and the shopping bags copy their weight limit when they are made.</summary>
    private static int RefreshLimits(AgentManager? agents)
    {
        var bags = GameSingleton.Existing<ConfigManager>()?._Config_Bag_Dict;
        var list = agents?._agentList_k__BackingField;

        if (bags == null || list == null) return 0;

        var count = 0;

        foreach (var agent in list)
        {
            if (agent == null) continue;

            var bag = AgentTools.GetAgentComponent<BagComponent>(agent);

            if (bag != null && bags.TryGetValue(bag._BagConfigId_k__BackingField, out var row) && row != null && bag._Burden_k__BackingField != row._Burden_k__BackingField)
            {
                bag._Burden_k__BackingField = row._Burden_k__BackingField;
                count++;
            }

            var box = AgentTools.GetAgentComponent<BagStateComponent>(agent);

            if (box == null || !bags.TryGetValue(DoorBoxBagId, out var boxRow) || boxRow == null) continue;

            box._BagBurden_k__BackingField = boxRow._Burden_k__BackingField;

            var size = boxRow._Size_k__BackingField;

            if (size != null && size.Count >= 2) box._MaxSlotCount_k__BackingField = size[0] * size[1];
        }

        return count;
    }

    /// <summary>The game keeps each bag's weight as a running total, so a new item weight goes in as the difference.</summary>
    private static int Reweigh(ItemManager items, Il2CppSystem.Collections.Generic.Dictionary<long, ItemData> cache)
    {
        var count = 0;

        foreach (var item in cache.Values)
        {
            if (item == null || !WeightBefore.TryGetValue(item.InstanceId, out var before)) continue;

            var delta = items.GetItemActualWeight(item) - before;

            if (delta == 0) continue;

            var owner = item._OwnerId_k__BackingField;

            items.UpdateOwnerBagWeight(owner, delta, item._ItemConfigId_k__BackingField, item._ItemCount_k__BackingField);
            Touched.Add(owner);
            count++;
        }

        WeightBefore.Clear();

        return count;
    }

    /// <summary>
    /// Moves every item that now hangs over its grid's edge or sits on another to a free spot in the same bag; the
    /// ones with no spot are kept in <see cref="NoRoom"/>. Material racks have their own slot rules and are left alone.
    /// </summary>
    private static int Repack(ItemManager items)
    {
        var owners = items._OwnerCache_k__BackingField;

        if (owners == null) return 0;

        Owners.Clear();
        foreach (var owner in owners.Keys) Owners.Add(owner);

        var moved = 0;

        foreach (var owner in Owners)
        {
            if (owner <= 0 || ItemManager.IsMaterialRackOwner(owner)) continue;

            var grid = items.GetOwnerBagSize(owner);

            if (grid.x <= 0 || grid.y <= 0) continue;

            var list = items.GetItemDataList(owner);

            if (list == null || list.Count == 0) continue;

            Layout.Clear();
            ById.Clear();

            foreach (var item in list)
            {
                if (item == null) continue;

                var size = item._ItemSize_k__BackingField;
                var at = item._BagPos_k__BackingField;

                Layout.Add(new Placed(item.InstanceId, new Size(size.x, size.y), new Cell(at.x, at.y)));
                ById[item.InstanceId] = item;
            }

            var plan = Core.Repack.Plan(new Size(grid.x, grid.y), Layout);

            if (plan.IsEmpty) continue;

            Touched.Add(owner);

            foreach (var (id, at) in plan.Moves)
            {
                if (!ById.TryGetValue(id, out var item)) continue;

                if (items.SetBagPosSafe(item, new Vector2Int(at.X, at.Y), false)) moved++;
                else NoRoom.Add(item);
            }

            foreach (var id in plan.NoRoom)
            {
                if (ById.TryGetValue(id, out var item)) NoRoom.Add(item);
            }
        }

        Layout.Clear();
        ById.Clear();

        return moved;
    }
}
