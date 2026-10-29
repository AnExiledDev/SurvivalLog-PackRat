using Jint;
using Xunit;

namespace PackRat.Tests;

/// <summary>
/// tab.js and data.js as the plugin ships them, run in Jint on a page with no DOM: what the tabs send, and the names
/// they show, without the game.
/// </summary>
public class TabScriptTests
{
    /// <summary>
    /// ItemWeight is a Mod Manager setting too. Written through the Mod Manager, its Mods screen shows the new value at
    /// once; written by Pack Rat behind its back, the Mods screen kept the old one until it was opened again.
    /// </summary>
    [Fact]
    public void The_Items_tab_changes_ItemWeight_through_the_Mod_Manager()
    {
        var page = new Page();

        page.Run("window.__slpackrat.setItemWeight(0.1 + 0.75)");
        page.Run("runTimers()");

        Assert.Equal("slmod|modmanager|set|com.dev.survivallog.packrat|Weight|ItemWeight|0.85", page.Text("sent.join('\\n')"));
    }

    [Fact]
    public void A_held_button_sends_one_change()
    {
        var page = new Page();

        page.Run("window.__slpackrat.setItemWeight(0.9); window.__slpackrat.setItemWeight(0.95); window.__slpackrat.setItemWeight(1.05)");
        page.Run("runTimers()");

        Assert.Equal("slmod|modmanager|set|com.dev.survivallog.packrat|Weight|ItemWeight|1.05", page.Text("sent.join('\\n')"));
    }

    /// <summary>The game names its own item categories; every category an item is in shows the game's name, never a number.</summary>
    [Fact]
    public void Every_item_category_has_the_game_s_name_in_both_languages()
    {
        var page = new Page();

        Assert.Equal("", page.Text(
            "window.__slpackratData.items.map(function (it) { return it[6]; })" +
            ".filter(function (cat) { var n = window.__slpackratData.cats[cat]; return !n || !n.en || !n.zh; }).join(',')"));
        Assert.Equal("Meds", page.Text("window.__slpackratData.cats[2].en"));
        Assert.Equal("药物", page.Text("window.__slpackratData.cats[2].zh"));
    }

    private sealed class Page
    {
        // Timers wait until the test runs them; a cleared one is dropped, as in a browser.
        private const string Fake = """
            var window = this;
            var sent = [];
            var timers = {};
            var nextTimer = 1;
            function setTimeout(fn) { var id = nextTimer++; timers[id] = fn; return id; }
            function clearTimeout(id) { delete timers[id]; }
            function runTimers() { var due = timers; timers = {}; for (var id in due) due[id](); }
            var location = { href: 'http://game/WebUI/Root.html' };
            var document = { getElementById: function () { return null; }, createElement: function () { return {}; }, head: { appendChild: function () {} } };
            window.addEventListener = function () {};
            window.vuplex = { postMessage: function (m) { sent.push(m); } };
            """;

        private readonly Engine _engine = new();

        public Page()
        {
            _engine.Execute(Fake);
            _engine.Execute(Resource("PackRat.data.js"));
            _engine.Execute(Resource("PackRat.tab.js"));
        }

        public void Run(string script) => _engine.Execute(script);

        public string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

        private static string Resource(string name)
        {
            using var stream = typeof(TabScriptTests).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
