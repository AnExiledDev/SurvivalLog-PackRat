using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using ExiledToolkit;
using ExiledToolkit.Game;
using HarmonyLib;
using PackRat.Adapters;
using PackRat.Core;

namespace PackRat;

[BepInPlugin(Guid, "Pack Rat", Version)]
[BepInDependency(ToolkitGuid, ToolkitVersion)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.packrat";
    private const string Version = "0.5.0";
    private const string ToolkitGuid = "com.dev.survivallog.exiledtoolkit";
    private const string ToolkitVersion = ">=0.19.0";

    private Settings? _settings;

    public override void Load()
    {
        var harmony = new Harmony(Guid);
        var settings = new Settings(Config, Paths.ConfigPath, Write);
        var page = new WebPage(Write);

        _settings = settings;
        Log.LogInfo($"Pack Rat {Version} started" + (settings.IsUninstallPrep ? ", Uninstall prep is on: the next load puts the game's values back" : ""));
        LoadHooks.Apply(new PatchKit(harmony, Write), settings.Current, Write);
        Overflow.Done += AfterLoad;
        LiveApply.Settings = () => _settings;
        LiveApply.Log = Write;
        LiveApply.Moved += AfterChange;
        LiveSettings.Watch(settings.ScrollBarsEntry, page.SetScrollBars);
        Ticker.Page = page;
        Ticker.Inbox = ExiledToolkit.Web.PageInbox.Listen(PageCommand.Prefix);
        Ticker.Tabs = new Tabs(settings, page, Write);
        Ticker.Log = Write;
        AddComponent<Ticker>();
    }

    private void AfterLoad(Overflow.Placed placed) => Report("after the load", _settings?.IsUninstallPrep == true, placed);

    private void AfterChange(Overflow.Placed placed) => Report("after a change", false, placed);

    private void Report(string when, bool isPrep, Overflow.Placed placed)
    {
        if (placed.Total > 0) Write($"{when}: {placed.ToShoppingBags} items went into the shopping bags, {placed.ToFloor} on the floor, {placed.LeftInPlace} stayed where they were");

        var zh = GameLanguage.IsChinese();

        if (Texts.AfterLoad(isPrep, placed.ToShoppingBags, placed.ToFloor, placed.LeftInPlace, zh) is not { } toast) return;

        var (title, body) = toast;
        var kind = isPrep ? ToastKind.Success : placed.LeftInPlace > 0 || placed.ToFloor > 0 ? ToastKind.Warning : ToastKind.Info;

        try
        {
            if (Toasts.Show(new ToastRequest(Texts.Source(zh), title) { Body = body, Kind = kind }).IsNone) Write("a toast was not shown: " + title);
        }
        catch (Exception ex)
        {
            Write("could not show a toast: " + ex.Message);
        }
    }

    private void Write(string message)
    {
        if (HookFailure.Is(message)) Log.LogError(message);
        else Log.LogInfo(message);
    }
}
