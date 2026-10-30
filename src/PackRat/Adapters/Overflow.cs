using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using UnityEngine;

namespace PackRat.Adapters;

/// <summary>
/// While a save loads, an item that no longer fits its bag (the grid shrank or the item grew) would be dropped on the
/// floor wherever the player stands. Pack Rat holds it back instead: the game keeps it in its bag, out of place, and
/// once the player is in the world it goes into the living-room shopping bags, the way the game unloads the car.
/// Only if both shopping bags are full does it go on the floor after all.
/// </summary>
internal static class Overflow
{
    /// <summary>The shopping bag furniture the game puts by the door when it unloads the car.</summary>
    private const int DoorBoxFurnitureId = 1024;

    private const int DoorBoxCount = 2;
    private const float SettleSeconds = 2f;

    private static readonly List<ItemData> Held = new();
    private static bool _isLoading;
    private static bool _hasLoaded;
    private static float _readyAt = -1;

    /// <summary>A save finished loading; tells the caller how the held items were placed.</summary>
    public static event Action<Placed>? Done;

    /// <summary>False while a save loads and until the items it held back are placed.</summary>
    public static bool IsIdle => !_isLoading && !_hasLoaded;

    public static void StartLoad()
    {
        Held.Clear();
        _isLoading = true;
        _hasLoaded = false;
        _readyAt = -1;
    }

    public static void EndLoad()
    {
        _isLoading = false;
        _hasLoaded = true;
    }

    /// <summary>
    /// True when the item is held back for the shopping bags. Items already in a shopping bag go the game's way, since
    /// there is nowhere better for them.
    /// </summary>
    public static bool Hold(ItemData? item, long ownerId)
    {
        if (!_isLoading || item == null || VehicleConfig.IsBoxOwner(ownerId)) return false;

        Held.Add(item);

        return true;
    }

    /// <summary>Runs every frame; places the held items a moment after the player appears in the loaded world.</summary>
    public static void Tick(float now)
    {
        if (!_hasLoaded) return;

        var world = GameSingleton.Existing<BattleLogicWorld>();
        var player = world?._AgentManager?.GetLeadingRole();

        if (world == null || player == null)
        {
            _readyAt = -1;

            return;
        }

        if (_readyAt < 0) _readyAt = now + SettleSeconds;
        if (now < _readyAt) return;

        _hasLoaded = false;

        var placed = Place(world, player, Held);

        Held.Clear();
        Done?.Invoke(placed);
    }

    /// <summary>
    /// Puts items that no longer fit their bag into the shopping bags, or on the floor when both are full. Used after
    /// a load and when a change in the middle of a game leaves items with no room.
    /// </summary>
    public static Placed Place(BattleLogicWorld world, LeadingRole player, List<ItemData> overflow)
    {
        var items = world._ItemManager;
        var agents = world._AgentManager;
        int toBags = 0, toFloor = 0, stuck = 0;

        if (items == null || agents == null) return new Placed(0, 0, overflow.Count);

        foreach (var item in overflow)
        {
            var id = item.InstanceId;

            if (items.GetItemData(id) == null) continue;

            if (IntoShoppingBag(items, agents, player, item, id)) toBags++;
            else if (OntoFloor(world, items, item, id)) toFloor++;
            else stuck++;
        }

        if (toBags > 0) player.PushDoorBoxToRedux();

        return new Placed(toBags, toFloor, stuck);
    }

    private static bool IntoShoppingBag(ItemManager items, AgentManager agents, LeadingRole player, ItemData item, long id)
    {
        for (var box = 0; box < DoorBoxCount; box++)
        {
            if (agents.FindDoorBoxFurniture(box) == null && !CreateShoppingBag(player, box)) continue;

            if (items.TryQuickMoveItem(id, item.OwnerId, VehicleConfig.GetBoxOwnerId(box), true, false)) return true;
        }

        return false;
    }

    /// <summary>The game picks the shopping bag spot nearest the given position on the home map.</summary>
    private static bool CreateShoppingBag(LeadingRole player, int box)
    {
        var build = AgentTools.GetAgentComponent<BuildComponent>(player);
        var position = AgentTools.GetAgentComponent<TransformComponent>(player)?.Position ?? Vector3.zero;

        return build != null && build.CreateBagFurniture(DoorBoxFurnitureId, position, true, box) != 0;
    }

    /// <summary>What the game would have done during the load: take it out of the bag and drop it near the player.</summary>
    private static bool OntoFloor(BattleLogicWorld world, ItemManager items, ItemData item, long id)
    {
        var drops = world._DropItemManager;
        var ownerId = item.OwnerId;

        if (drops == null) return false;

        items.RemoveItemFromOwnerCache(ownerId, id, true);

        if (drops.TryDropOverflowItemData(item, ownerId))
        {
            items.AddItemToOwnerCache(item.OwnerId, item, true);

            return true;
        }

        items.AddItemToOwnerCache(ownerId, item, true);

        return false;
    }

    /// <summary>How the items that did not fit were placed after a load.</summary>
    public readonly record struct Placed(int ToShoppingBags, int ToFloor, int LeftInPlace)
    {
        public int Total => ToShoppingBags + ToFloor + LeftInPlace;
    }
}
