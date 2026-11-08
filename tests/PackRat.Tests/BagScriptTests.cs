using Jint;
using PackRat.Core;
using Xunit;

namespace PackRat.Tests;

/// <summary>
/// bags.js as the plugin ships it, run in Jint on a bare Root page. It sets a screen up from inside Root's
/// notifyPageReady, the call that makes the screen visible, so a mistake there would leave every screen hidden.
/// </summary>
public class BagScriptTests
{
    [Fact]
    public void Root_still_shows_the_screen_when_its_frame_cannot_be_read()
    {
        var page = new Page("window.notifyPageReady = function (id) { shown.push(id); return 'ok ' + id; };");

        Assert.Equal("ok Backpack", page.Text("window.notifyPageReady('Backpack')"));
        Assert.Equal("Backpack", page.Text("shown.join(',')"));
    }

    /// <summary>Root can set notifyPageReady after the script is in, when the web view reloads.</summary>
    [Fact]
    public void A_notifyPageReady_set_after_the_script_still_runs()
    {
        var page = new Page("");

        page.Run("window.notifyPageReady = function (id) { shown.push(id); }");
        page.Run("window.notifyPageReady('Shop')");

        Assert.Equal("Shop", page.Text("shown.join(',')"));
    }

    /// <summary>The plugin sends the script again whenever it thinks the page lost it.</summary>
    [Fact]
    public void Sent_twice_the_screen_is_still_shown_once()
    {
        var page = new Page("window.notifyPageReady = function (id) { shown.push(id); };");

        page.Load();
        page.Run("window.notifyPageReady('Cooking')");

        Assert.Equal("Cooking", page.Text("shown.join(',')"));
    }

    /// <summary>
    /// ScrollBars off: the game alone fits the screen, as without the mod. A screen shown while it is off gets none of
    /// Pack Rat's styles, and gets them once it is turned back on.
    /// </summary>
    [Fact]
    public void A_screen_shown_with_scroll_bars_off_is_left_to_the_game_until_they_are_on()
    {
        var page = new Screen();

        page.Run(BagScroll.Script(false));
        page.Run("window.notifyPageReady('Cooking')");

        Assert.Equal("Cooking", page.Text("shown.join(',')"));
        Assert.Equal("0", page.Text("styles.length"));

        page.Run(BagScroll.Script(true));

        Assert.Equal("1", page.Text("styles.length"));
        Assert.Equal("false", page.Text("styles[0].disabled === true"));
    }

    /// <summary>
    /// Turned off while a screen is open, Pack Rat's styles go and the game is asked to fit the screen again; turned
    /// back on, the same styles come back rather than a second copy.
    /// </summary>
    [Fact]
    public void Turning_scroll_bars_off_hands_an_open_screen_back_to_the_game()
    {
        var page = new Screen();

        page.Run("window.notifyPageReady('Cooking')");
        Assert.Equal("1", page.Text("styles.length"));

        page.Run(BagScroll.Script(false));

        Assert.Equal("true", page.Text("styles[0].disabled === true"));
        Assert.Equal("resize", page.Text("events.join(',')"));

        page.Run(BagScroll.Script(true));

        Assert.Equal("1", page.Text("styles.length"));
        Assert.Equal("false", page.Text("styles[0].disabled === true"));
    }

    /// <summary>Root, plus one inventory screen (the Cooking Station) with no grids in it yet, in a frame it can read.</summary>
    private sealed class Screen : Page
    {
        public Screen() : base("""
            var styles = [];
            var events = [];
            var element = function () {
              return { style: {}, hasAttribute: function () { return false; }, setAttribute: function () {}, removeAttribute: function () {} };
            };
            var frameDoc = {
              location: { pathname: '/Cooking.html' },
              body: element(),
              documentElement: element(),
              head: { appendChild: function (el) { styles.push(el); } },
              createElement: function () { return element(); },
              querySelectorAll: function () { return []; },
              querySelector: function () { return null; },
              getElementById: function () { return null; },
              defaultView: {
                innerWidth: 1920,
                innerHeight: 1080,
                Element: { prototype: { getBoundingClientRect: function () { return {}; } } },
                ResizeObserver: function () { this.observe = function () {}; this.unobserve = function () {}; },
                MutationObserver: function () { this.observe = function () {}; },
                Event: function (type) { this.type = type; },
                addEventListener: function () {},
                dispatchEvent: function (e) { events.push(e.type); },
                requestAnimationFrame: function () { return 1; },
                getComputedStyle: function () { return {}; }
              }
            };
            var frame = { tagName: 'IFRAME', contentDocument: frameDoc };
            document.getElementById = function () { return frame; };
            window.notifyPageReady = function (id) { shown.push(id); };
            """)
        {
        }
    }

    private class Page
    {
        // Root with a frame it cannot read: getElementById hands back a frame whose page throws part way through
        // being looked at, as a page being torn down can.
        private const string Fake = """
            var window = this;
            var shown = [];
            var console = { warn: function () {} };
            var locked = { tagName: 'IFRAME', contentDocument: { body: {}, defaultView: {}, get location() { throw new Error('blocked'); } } };
            var document = {
              getElementsByTagName: function () { return []; },
              getElementById: function () { return locked; },
              addEventListener: function () {}
            };
            window.addEventListener = function () {};
            """;

        private readonly Engine _engine = new();

        public Page(string root)
        {
            _engine.Execute(Fake);
            _engine.Execute(root);
            Load();
        }

        public void Load() => _engine.Execute(Resource("PackRat.bags.js"));

        public void Run(string script) => _engine.Execute(script);

        public string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

        private static string Resource(string name)
        {
            using var stream = typeof(BagScriptTests).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
