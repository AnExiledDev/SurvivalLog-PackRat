using System;
using System.IO;
using ExiledToolkit.Web;
using PackRat.Core;
using ToolkitPage = ExiledToolkit.Web.WebPage;

namespace PackRat.Adapters;

/// <summary>
/// The mod's half in the game's web UI: tab.js and its styles, and bags.js (scrolling grids on the game's inventory
/// screens), run in the root document the Mod Manager and every screen sit in. The web view can reload at any time and
/// lose the scripts, so Exiled's Toolkit checks for them every couple of seconds and they are sent again when the page
/// says they are missing. The item and inventory names (data.js, about half a megabyte) are sent only when a tab asks
/// for them.
/// </summary>
internal sealed class WebPage
{
    private readonly ToolkitPage _page;
    private string? _bags;
    private string? _css;
    private string? _data;

    public WebPage(Action<string> log)
    {
        _page = new ToolkitPage(typeof(WebPage).Assembly, "PackRat.tab.js", "window.__slpackrat", PageCommand.Prefix + "noscript", log) { Name = "tab" };
    }

    public void KeepInstalled(double now) => _page.KeepInstalled(now);

    /// <summary>bags.js first, then the styles tab.js reads as it starts, then tab.js itself.</summary>
    public void Install()
    {
        _page.Run(_bags ??= Resource("PackRat.bags.js"));
        _page.Run(_css ??= "window.__slpackratCss=" + JsText.Literal(Resource("PackRat.tab.css")) + ";");
        _page.Install();
    }

    public void SendNames() => _page.Run(_data ??= Resource("PackRat.data.js"));

    public void Call(string javaScript) => _page.Run(javaScript);

    private static string Resource(string name)
    {
        using var stream = typeof(WebPage).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
