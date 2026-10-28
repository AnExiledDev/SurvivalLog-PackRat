using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using PackRat.Adapters;
using PackRat.Core;

namespace PackRat;

[BepInPlugin(Guid, "Pack Rat", Version)]
[BepInDependency(OptionalLive.ToolkitGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.packrat";
    private const string Version = "0.1.0";

    public override void Load()
    {
        if (!OptionalLive.IsToolkitInstalled) Log.LogInfo(OptionalLive.MissingNote);

        var carryWeight = Config.Bind("Weight", "CarryWeight", 2.0,
            $"How many times more weight every bag, storage piece and vehicle can hold. 2 means twice the game's limit; 1 is the game's own. Places with no weight limit stay without one. From {CarryWeight.MinMultiplier} to {CarryWeight.MaxMultiplier}. Takes effect the next time you load a save.\n" +
            $"所有背包、储物家具和载具的负重上限倍数。2 表示游戏原有负重上限的两倍，1 为游戏原值。原本没有负重上限的地方仍然没有上限。范围 {CarryWeight.MinMultiplier} 到 {CarryWeight.MaxMultiplier}。下次读取存档时生效。");

        Log.LogInfo($"Pack Rat {Version} started");
        BagTablePatch.Apply(new Harmony(Guid), Write);
        OptionalLive.Watch(carryWeight, value => BagTablePatch.UseMultiplier(CarryWeight.ClampMultiplier(value)));
    }

    private void Write(string message)
    {
        if (HookFailure.Is(message)) Log.LogError(message);
        else Log.LogInfo(message);
    }
}
