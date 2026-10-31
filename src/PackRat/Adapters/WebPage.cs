using System;
using System.IO;
using GameCore.HotUpdate.ReduxUI;
using PackRat.Core;

namespace PackRat.Adapters;

/// <summary>
/// The mod's half in the game's web UI: tab.js and its styles, and bags.js (scrolling grids on the game's inventory
/// screens), run in the root document the Mod Manager and every screen sit in. The web view can reload at any time and
/// lose the scripts, so they are checked for every couple of seconds and sent again when the page says they are
/// missing. The item and inventory names (data.js, about half a megabyte) are sent only when a tab asks for them.
/// </summary>
internal sealed class WebPage
{
    private const float CheckSeconds = 2f;
    private const string NoScriptReply = "window.vuplex&&window.vuplex.postMessage('" + PageCommand.Prefix + "noscript')";

    private readonly Action<string> _log;
    private string? _script;
    private string? _bags;
    private string? _data;
    private float _nextCheckAt;
    private bool _hasReportedRunFailure;

    public WebPage(Action<string> log)
    {
        _log = log;
    }

    public void KeepInstalled(float now)
    {
        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;
        Run("window.__slpackrat||" + NoScriptReply);
    }

    public void Install()
    {
        _script ??= "window.__slpackratCss=" + JsText.Literal(Resource("PackRat.tab.css")) + ";\n" + Resource("PackRat.tab.js");

        Run(_bags ??= Resource("PackRat.bags.js"));

        if (Run(_script)) _log("tab script sent to the web UI");
    }

    public void SendNames() => Run(_data ??= Resource("PackRat.data.js"));

    public void Call(string javaScript) => Run(javaScript);

    private bool Run(string javaScript)
    {
        var webView = GameSingleton.Existing<ReduxUISystem>()?.GetWebUILayer()?.canvasWebViewPrefab?.WebView;

        if (webView == null) return false;

        try
        {
            webView.ExecuteJavaScript(javaScript, null);

            return true;
        }
        catch (Exception ex)
        {
            if (!_hasReportedRunFailure) _log("could not run script in the web UI (reported once): " + ex.Message);

            _hasReportedRunFailure = true;

            return false;
        }
    }

    private static string Resource(string name)
    {
        using var stream = typeof(WebPage).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
