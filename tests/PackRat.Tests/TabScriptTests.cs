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

    /// <summary>The tabs talk to the plugin through Exiled's Toolkit, and the game may run the Toolkit's script before or after tab.js.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void The_Items_tab_reaches_the_Mod_Manager_whichever_script_the_game_ran_first(bool isToolkitFirst)
    {
        var page = new Page(isToolkitFirst);

        if (!isToolkitFirst) page.LoadToolkit();

        page.Run("window.__slpackrat.setItemWeight(0.5)");
        page.Run("runTimers()");

        Assert.Equal("slmod|modmanager|set|com.dev.survivallog.packrat|Weight|ItemWeight|0.5", page.Text("sent.join('\\n')"));
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

    /// <summary>The page shows each inventory under the category the plugin named, and works out its grid the plugin's way.</summary>
    [Fact]
    public void The_page_uses_the_plugin_s_categories_and_its_order_of_sizes()
    {
        var page = new Page();

        page.Run("""
            window.__slpackrat.data({
              bags: { 1001: [8, 6, 14400], 1004: [8, 7, 0], 1: [8, 7, 9600] },
              cats: { 1001: 'medium', 1004: 'medium', 1: 'bags' },
              grids: { scale: { all: 2 }, cats: { medium: { w: 10, h: 9 } }, rows: { 1004: { h: 12 } } }
            })
            """);

        Assert.Equal("medium", page.Text("window.__slpackrat.categoryOf(1001)"));
        Assert.Equal("10x9", page.Text("(function (g) { return g.w + 'x' + g.h; })(window.__slpackrat.gridOf(1001))"));
        Assert.Equal("10x12", page.Text("(function (g) { return g.w + 'x' + g.h; })(window.__slpackrat.gridOf(1004))"));
        Assert.Equal("16x14", page.Text("(function (g) { return g.w + 'x' + g.h; })(window.__slpackrat.gridOf(1))"));
    }

    /// <summary>The plugin reads the file the page saves; a category the page shows must reach it under the same key.</summary>
    [Fact]
    public void The_page_saves_categories_in_the_plugin_s_file_shape()
    {
        var page = new Page();

        page.Run("""
            window.__slpackrat.data({
              bags: { 3: [12, 8, 18000] },
              cats: { 3: 'vehicles' },
              grids: { scale: { all: 1 }, cats: { vehicles: { x: 1.5 }, huge: { w: 4 } }, rows: { 3: { kg: 40 } } }
            })
            """);

        Assert.Equal("""{"scale":{},"cats":{"vehicles":{"x":1.5}},"rows":{"3":{"kg":40}}}""", page.Text("window.__slpackrat.gridsFile()"));
    }

    /// <summary>With All items half size on, the Items tab shows every item at the size the plugin gives it in game.</summary>
    [Fact]
    public void The_page_halves_every_item_the_way_the_plugin_does()
    {
        var page = new Page();

        page.Run("window.__slpackrat.data({ itemsFile: { allHalf: true } })");

        var rows = page.Text(
            "window.__slpackratData.items.filter(function (a) { return a[3] > 0 && a[4] > 0; })" +
            ".map(function (a) { return a[3] + 'x' + a[4] + '=' + window.__slpackrat.sizeOf(a[0]); }).join(' ')").Split(' ');

        Assert.True(rows.Length > 1000, rows.Length + " items");
        Assert.Contains(rows, row => row.StartsWith("3x2="));

        foreach (var row in rows)
        {
            var game = row.Split('=')[0].Split('x').Select(int.Parse).ToArray();
            var half = PackRat.Core.Items.Half(new PackRat.Core.Size(game[0], game[1]));

            Assert.Equal(row.Split('=')[0] + "=" + half.W + "x" + half.H, row);
        }
    }

    [Fact]
    public void An_inventory_the_plugin_did_not_name_falls_into_a_category_by_its_size()
    {
        var page = new Page();

        page.Run("window.__slpackrat.data({ bags: { 1001: [8, 6, 14400], 1003: [8, 10, 24000] } })");

        Assert.Equal("medium", page.Text("window.__slpackrat.categoryOf(1001)"));
        Assert.Equal("large", page.Text("window.__slpackrat.categoryOf(1003)"));
    }

    /// <summary>
    /// Both search boxes matched the whole typed text, so a space before or after a word, or two words in another order
    /// than the name has them, found nothing.
    /// </summary>
    [Theory]
    [InlineData("camping backpack", true)]
    [InlineData("  backpack   camping ", true)]
    [InlineData("BACKPACK", true)]
    [InlineData("背包", true)]
    [InlineData("", true)]
    [InlineData("backpack rope", false)]
    public void A_search_finds_every_typed_word_in_any_order(string query, bool isFound)
    {
        var page = new Page();

        Assert.Equal(isFound ? "true" : "false", page.Text($"window.__slpackrat.matchesSearch(['Camping Backpack', '露营背包', null], '{query}')"));
    }

    /// <summary>
    /// A number box left blank read as its lowest value: clearing the item weight box set every item to 0.1. A blank box,
    /// or one with no number in it, now changes nothing and shows the setting again; - and + step from the setting.
    /// </summary>
    [Fact]
    public void A_blank_number_box_changes_nothing_and_shows_the_setting_again()
    {
        var page = new Page();

        page.Run("""
            var setTo = [];
            var input = { value: '', defaultValue: '1.00' };
            var plus = { dataset: { d: '1' } };
            var el = { querySelector: function () { return input; }, querySelectorAll: function () { return [plus]; } };
            window.__slpackrat.bindStepper({ querySelectorAll: function () { return [el]; } }, 'iw', 0.05, 0.1, 3, function (v) { setTo.push(v); });
            function type(text) { input.value = text; input.onchange(); return input.value; }
            """);

        Assert.Equal("1.00", page.Text("type(' ')"));
        Assert.Equal("1.00", page.Text("type('abc')"));
        Assert.Equal("", page.Text("setTo.join(',')"));

        page.Run("type('99')");
        page.Run("input.value = ''; plus.onclick()");

        Assert.Equal("3,1.05", page.Text("setTo.join(',')"));
    }

    /// <summary>A category side that follows the multiplier is blanked to follow it again; that must still work.</summary>
    [Fact]
    public void Blanking_a_box_that_follows_the_multiplier_makes_it_follow_again()
    {
        var page = new Page();

        page.Run("""
            var setTo = [];
            var input = { value: ' ', defaultValue: '12' };
            var el = { querySelector: function () { return input; }, querySelectorAll: function () { return []; } };
            window.__slpackrat.bindStepper({ querySelectorAll: function () { return [el]; } }, 'cw', 1, 1, 30, function (v) { setTo.push(v); }, function () { return 8; });
            input.onchange();
            """);

        Assert.Equal("1", page.Text("setTo.length"));
        Assert.Equal("null", page.Text("setTo[0]"));
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

        /// <summary>The game runs the Toolkit's page API and tab.js in either order; <paramref name="isToolkitFirst"/> false runs only tab.js, see <see cref="LoadToolkit"/>.</summary>
        public Page(bool isToolkitFirst = true)
        {
            _engine.Execute(Fake);
            if (isToolkitFirst) LoadToolkit();
            _engine.Execute(Resource("PackRat.data.js"));
            _engine.Execute(Resource("PackRat.tab.js"));
        }

        public void LoadToolkit() => _engine.Execute(Resource("ExiledToolkit.api.js"));

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
