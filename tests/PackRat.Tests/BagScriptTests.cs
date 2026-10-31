using Jint;
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

    private sealed class Page
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
