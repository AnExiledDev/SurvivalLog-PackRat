using System;
using System.Collections.Generic;
using System.IO;
using ExiledToolkit.Game;
using PackRat.Core;

namespace PackRat.Adapters;

/// <summary>
/// The plugin's side of the Inventories and Items tabs: hands them the game's sizes and weights and both files,
/// and writes a file back when a tab saves, keeping the previous one as .bak. A save applies in the game right away,
/// or at the next load when no game is running.
/// </summary>
internal sealed class Tabs
{
    private readonly Settings _settings;
    private readonly WebPage _page;
    private readonly Action<string> _log;

    public Tabs(Settings settings, WebPage page, Action<string> log)
    {
        _settings = settings;
        _page = page;
        _log = log;
    }

    public void Handle(PageCommand command)
    {
        switch (command)
        {
            case PageCommand.NoScript:
                _page.Install();
                break;
            case PageCommand.Load load:
                if (load.WantsData) _page.SendNames();
                Load();
                break;
            case PageCommand.SaveGrids save:
                Save(TabsPage.GridsKind, _settings.GridsPath, save.Json, (string text, out string json, out string error) =>
                {
                    var ok = Inventories.TryParse(text, out var value, out error);
                    json = value.ToJson();

                    return ok;
                });
                break;
            case PageCommand.SaveItems save:
                Save(TabsPage.ItemsKind, _settings.ItemsPath, save.Json, (string text, out string json, out string error) =>
                {
                    var ok = Items.TryParse(text, out var value, out error);
                    json = value.ToJson();

                    return ok;
                });
                break;
            case PageCommand.Unknown unknown:
                _log("page: " + unknown.Text);
                break;
        }
    }

    private void Load()
    {
        var problems = new List<string>();
        var grids = _settings.ReadGrids(problems);
        var items = _settings.ReadItems(problems);

        foreach (var problem in problems) _log("tabs: " + problem);

        _page.Call(TabsPage.DataCall(GameTables.GameBags(), GameTables.GameItems(), CarryWeight.ClampMultiplier(_settings.CarryWeightEntry.Value),
            ItemWeight.Clamp(_settings.ItemWeightEntry.Value), grids, items, problems));
    }

    private delegate bool Normalizer(string text, out string json, out string error);

    /// <summary>The file is written as the plugin reads it back, so what the tab saved is what the next load uses.</summary>
    private void Save(string kind, string path, string text, Normalizer normalize)
    {
        var zh = GameLanguage.IsChinese();
        var name = Path.GetFileName(path);

        if (!normalize(text, out var json, out var error))
        {
            _log($"{name} not saved, the tab sent text that is not valid JSON: {error}");
            _page.Call(TabsPage.SavedCall(kind, false, Texts.NotSaved(zh) + error));

            return;
        }

        try
        {
            if (File.Exists(path)) File.Copy(path, path + ".bak", overwrite: true);

            File.WriteAllText(path, json);
            _log($"saved {name} ({json.Length} characters)");
            _page.Call(TabsPage.SavedCall(kind, true, Texts.Saved(zh)));
            LiveApply.Request();
        }
        catch (Exception ex)
        {
            _log($"could not save {name}: {ex.Message}");
            _page.Call(TabsPage.SavedCall(kind, false, Texts.NotSaved(zh) + ex.Message));
        }
    }
}
