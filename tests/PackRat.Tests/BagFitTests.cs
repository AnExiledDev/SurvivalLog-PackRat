using Jint;
using Xunit;

namespace PackRat.Tests;

/// <summary>
/// bags.js fitting one screen to the window, run in Jint on a fake screen with sizes. The screen's parts sit side by
/// side in its root, and the root is centred in the window with any margin under it. A screen the game shrinks gets
/// the game's shrink (createViewportFit in webui-bag.js): on every resize the body's zoom is the smaller of the room
/// for its width over its width and the room for its height over its height, from the root's own size, so a margin
/// under the root is never counted. With things lying nearby, the game's Nearby strip sits on the window's bottom edge.
/// </summary>
public class BagFitTests
{
    private const string Depot = "page: 'AidDepot', root: '.hub-root', shrink: 48";

    // ----- The Nearby strip -----

    /// <summary>
    /// The game puts its Nearby strip on the window's bottom edge when it finds no room under the screen, and there it
    /// covers the bottom of the bag. The screen moves up instead and keeps room under itself for the strip, and the bag
    /// keeps its full height because the screen had room to spare.
    /// </summary>
    [Fact]
    public void A_screen_the_nearby_strip_would_cover_moves_up_to_leave_it_room()
    {
        var page = new Fit($"{Depot}, w: 1280, h: 720, strip: true, parts: [{{ grid: '#bagGrid', w: 400, h: 600 }}, {{ grid: '#recvGrid', w: 400, h: 300 }}]");

        Assert.True(page.Number("rootBottom()") <= page.Number("stripTop()"));
        Assert.Equal(600, page.Number("boxOf('#bagGrid').offsetHeight"));
    }

    /// <summary>A screen that ends above the strip stays where the game centres it.</summary>
    [Fact]
    public void A_screen_the_nearby_strip_does_not_reach_stays_centred()
    {
        var page = new Fit($"{Depot}, w: 1280, h: 720, strip: true, parts: [{{ grid: '#bagGrid', w: 400, h: 400 }}]");

        Assert.Equal((720 - 400) / 2, page.Number("rootTop()"));
    }

    /// <summary>
    /// Taking the last thing off the ground hides the strip. The room stays until the screen is shown again, as on the
    /// game's trade screen, so the screen doesn't jump while the player is using it.
    /// </summary>
    [Fact]
    public void The_room_for_the_strip_stays_when_the_strip_hides_and_goes_when_the_screen_is_shown_again()
    {
        var page = new Fit($"{Depot}, w: 1280, h: 720, strip: true, parts: [{{ grid: '#bagGrid', w: 400, h: 600 }}]");
        var raised = page.Number("rootTop()");

        page.Run("hideStrip()");

        Assert.Equal(raised, page.Number("rootTop()"));

        page.Run("window.notifyPageReady('AidDepot')");

        Assert.Equal((720 - 600) / 2, page.Number("rootTop()"));
    }

    /// <summary>
    /// The game's shrink leaves the room under the screen out. On a small window, room for the strip under a screen as
    /// tall as the panel beside its bag would push the screen's top out of the window, so the strip sits over the
    /// screen as it does in the game.
    /// </summary>
    [Fact]
    public void Room_for_the_strip_that_would_push_the_screen_out_of_the_window_is_given_up()
    {
        var page = new Fit($"{Depot}, w: 1093, h: 614, strip: true, parts: [{{ w: 300, h: 560 }}, {{ grid: '#bagGrid', w: 400, h: 560 }}]");

        Assert.True(page.Number("rootTop()") >= 0);
        Assert.True(page.Number("stripTop()") < page.Number("rootBottom()"));
        Assert.Equal(560, page.Number("boxOf('#bagGrid').offsetHeight"));
    }

    // ----- Too tall or too wide -----

    /// <summary>
    /// On a small window a screen can be too tall even with its bag cut short, because the panel beside the bag can't be
    /// cut. The bag is cut to the panel's height and the game shrinks the screen only as far as the panel needs, instead
    /// of shrinking it with the whole bag to half size.
    /// </summary>
    [Fact]
    public void A_screen_too_tall_for_a_small_window_keeps_its_bag_as_tall_as_the_panel_beside_it()
    {
        var page = new Fit($"{Depot}, w: 1093, h: 614, parts: [{{ w: 300, h: 700 }}, {{ grid: '#bagGrid', w: 400, h: 1000 }}]");

        Assert.InRange(page.Number("boxOf('#bagGrid').offsetHeight"), 690, 701);
        Assert.InRange(page.Number("zoom()"), 0.8, 0.82);
    }

    /// <summary>
    /// The game never shrinks the brewing screen. With a panel taller than the window beside the bag, no cut to the bag
    /// or the vat can make the screen fit. The bag is cut to the panel's height, and the vat, shorter than that, keeps
    /// all of its rows. Cutting both to their smallest would leave one row of each.
    /// </summary>
    [Fact]
    public void A_screen_the_game_never_shrinks_keeps_its_bag_as_tall_as_the_panel_beside_it()
    {
        var page = new Fit("page: 'BrewPanel', root: '.brew-system', w: 1093, h: 614, parts: [{ grid: '#bagGrid', w: 300, h: 1000 }, { w: 300, h: 640 }, { grid: '#vatGrid', w: 300, h: 300 }]");

        Assert.InRange(page.Number("boxOf('#bagGrid').offsetHeight"), 630, 641);
        Assert.Equal(300, page.Number("boxOf('#vatGrid').offsetHeight"));
        Assert.Equal(1, page.Number("zoom()"));
    }

    /// <summary>
    /// The trap screen centres the bag, the cage panel and the storage box in the window one by one, and the game never
    /// shrinks it. On a small window the cage panel is taller than the window and pokes out, as it does without the mod.
    /// The bag is cut so its own column fits the window, title and all, and the storage box keeps all of its rows.
    /// </summary>
    [Fact]
    public void The_trap_screen_fits_the_bag_in_the_window_when_the_cage_panel_is_taller_than_it()
    {
        var page = new Fit("page: 'TrapPanel', cols: true, pad: 20, w: 1093, h: 614, parts: [{ root: '#bagCol', grid: '#backpackGrid', w: 300, h: 1000 }, { root: '.popup-container', w: 300, h: 640 }, { root: '#storeCol', grid: '#storeGrid', w: 300, h: 300 }]");

        Assert.True(page.Number("colTop(0)") >= 0);
        Assert.True(page.Number("colBottom(0)") <= 614);
        Assert.InRange(page.Number("boxOf('#backpackGrid').offsetHeight"), 500, 526);
        Assert.Equal(300, page.Number("boxOf('#storeGrid').offsetHeight"));
    }

    /// <summary>A screen only a little too wide is shrunk a little by the game, and its bag keeps every column.</summary>
    [Fact]
    public void A_screen_a_little_too_wide_is_shrunk_a_little_instead_of_cutting_its_bag()
    {
        var page = new Fit($"{Depot}, w: 1248, h: 720, parts: [{{ grid: '#bagGrid', w: 980, h: 400 }}, {{ w: 300, h: 400 }}]");

        Assert.Equal(980, page.Number("boxOf('#bagGrid').offsetWidth"));
        Assert.Equal(0.9375, page.Number("zoom()"));
    }

    /// <summary>A screen far too wide is shrunk only a little, and its bag is cut to fit and scrolls sideways.</summary>
    [Fact]
    public void A_screen_far_too_wide_is_shrunk_a_little_and_its_bag_scrolls_sideways()
    {
        var page = new Fit($"{Depot}, w: 1248, h: 720, parts: [{{ grid: '#bagGrid', w: 1500, h: 400 }}, {{ w: 300, h: 400 }}]");

        Assert.True(page.Number("boxOf('#bagGrid').offsetWidth") < 1500);
        Assert.InRange(page.Number("zoom()"), 0.9, 0.91);
    }

    // ----- Screens with their own lists -----

    /// <summary>The material rack gets a row for every kind of material it holds. The rows scroll, and the game never has to shrink the screen.</summary>
    [Fact]
    public void A_full_material_rack_scrolls_its_rows_instead_of_the_screen_shrinking()
    {
        var page = new Fit("page: 'MaterialRack', root: '.rack-system', shrink: 24, w: 1280, h: 720, parts: [{ grid: '#bagGrid', w: 400, h: 500 }, { grid: '#rackBody', w: 500, h: 1400 }]");

        Assert.True(page.Number("boxOf('#rackBody').clientHeight") < 1400);
        Assert.Equal(1, page.Number("zoom()"));
    }

    /// <summary>The trash can's pile starts at the bottom of its pit, so the cut pit opens scrolled to the bottom.</summary>
    [Fact]
    public void The_trash_pit_opens_on_the_pile_at_the_bottom()
    {
        var page = new Fit(Trash);

        Assert.True(page.Number("boxOf('#pit').scrollTop") > 0);
        Assert.Equal(0, page.Number("fromBottom('#pit')"));
        Assert.Equal(0, page.Number("boxOf('#bagGrid').scrollTop"));
    }

    /// <summary>A pit scrolled up keeps its place counted from the pile at the bottom when the screen is fitted again.</summary>
    [Fact]
    public void A_trash_pit_scrolled_up_keeps_its_place_from_the_bottom_when_fitted_again()
    {
        var page = new Fit(Trash);

        page.Run("boxOf('#pit').scrollTop -= 100");
        page.Run("resize(1280, 820)");

        Assert.Equal(100, page.Number("fromBottom('#pit')"));
    }

    private const string Trash = "page: 'TrashCan', root: '.trash-system', shrink: 24, w: 1280, h: 720, parts: [{ grid: '#bagGrid', w: 400, h: 500 }, { grid: '#pit', w: 400, h: 2000 }]";

    // ----- The key hints under the bag -----

    /// <summary>The line of key hints hangs under the bag screen. Under a bag too tall for the window it stays inside the window.</summary>
    [Fact]
    public void The_key_hints_under_a_tall_bag_stay_inside_the_window()
    {
        var page = new Fit("page: 'BackpackUI', root: '.backpack-container', w: 1280, h: 720, hang: 60, parts: [{ grid: '#bag', wrapper: true, w: 400, h: 900 }]");

        Assert.True(page.Number("hintBottom()") <= 720);
        Assert.True(page.Number("rootTop()") >= 0);
    }

    // ----- ScrollBars off -----

    private const string TallBrew = "page: 'BrewPanel', root: '.brew-system', w: 1280, h: 720, parts: [{ grid: '#bagGrid', w: 400, h: 1000 }, { grid: '#vatGrid', w: 300, h: 300 }]";

    /// <summary>
    /// The game never shrinks the brewing screen. With ScrollBars off a big bag ran it off the bottom of the window, so
    /// Pack Rat shrinks it the way the game shrinks its other screens: by the room for its height (the window less the
    /// game's 48px margin) over its height, with the bag at its full size.
    /// </summary>
    [Fact]
    public void With_scrollbars_off_a_screen_the_game_never_shrinks_is_shrunk_to_fit_the_window()
    {
        var page = new Fit(TallBrew);

        page.Run("window.__slprBagsScroll(false)");

        Assert.Equal((720 - 48) / 1000.0, page.Number("zoom()"), 3);
        Assert.Equal(1000, page.Number("boxOf('#bagGrid').offsetHeight"));
        Assert.True(page.Number("rootTop()") >= 0);
        Assert.True(page.Number("rootBottom()") <= 720);
    }

    /// <summary>A screen too wide for the window is shrunk for its width the same way, here the trap screen's three columns.</summary>
    [Fact]
    public void With_scrollbars_off_a_screen_too_wide_is_shrunk_for_its_width()
    {
        var page = new Fit("page: 'TrapPanel', cols: true, w: 1280, h: 720, parts: [{ root: '#bagCol', grid: '#backpackGrid', w: 1000, h: 300 }, { root: '.popup-container', w: 420, h: 500 }, { root: '#storeCol', grid: '#storeGrid', w: 400, h: 300 }]");

        page.Run("window.__slprBagsScroll(false)");

        Assert.Equal((1280 - 48) / 1820.0, page.Number("zoom()"), 3);
    }

    /// <summary>A screen first opened while ScrollBars is off is shrunk too, and fitted again when the window changes size.</summary>
    [Fact]
    public void A_screen_opened_with_scrollbars_off_is_shrunk_and_fitted_again_on_a_resize()
    {
        var page = new Fit(TallBrew, before: "window.__slprBagsScroll(false)");

        Assert.Equal((720 - 48) / 1000.0, page.Number("zoom()"), 3);

        page.Run("resize(1600, 900)");

        Assert.Equal((900 - 48) / 1000.0, page.Number("zoom()"), 3);
    }

    /// <summary>Turning ScrollBars back on takes the shrink off, and the bag scrolls again at full size.</summary>
    [Fact]
    public void Turning_scrollbars_back_on_undoes_the_shrink()
    {
        var page = new Fit(TallBrew);

        page.Run("window.__slprBagsScroll(false); window.__slprBagsScroll(true)");

        Assert.Equal(1, page.Number("zoom()"));
        Assert.True(page.Number("boxOf('#bagGrid').offsetHeight") < 1000);
        Assert.True(page.Number("rootBottom()") <= 720);
    }

    /// <summary>
    /// The game shrinks the bag screen by its panels alone, so the line of hints hanging under them ran off a shrunk
    /// screen. While ScrollBars is off the screen holds room for the line under its panels, and with it back on it doesn't.
    /// </summary>
    [Fact]
    public void With_scrollbars_off_the_bag_screen_holds_room_for_its_hints()
    {
        var page = new Fit("page: 'BackpackUI', root: '.backpack-container', shrink: 24, w: 1280, h: 720, hang: 30, parts: [{ grid: '#bag', wrapper: true, w: 400, h: 900 }]");
        const string room = "frame.contentDocument.head.children.filter(function (c) { return c.id === 'slpackrat-bag-shrink'; })[0]";

        page.Run("window.__slprBagsScroll(false)");

        Assert.Contains("padding-bottom:30px", page.Text(room + ".textContent"));
        Assert.Equal("false", page.Text(room + ".disabled"));

        page.Run("window.__slprBagsScroll(true)");

        Assert.Equal("true", page.Text(room + ".disabled"));
    }

    /// <summary>
    /// The shop's dark layer is as big as the window, so a shrunk shop drew it smaller and left part of the window
    /// uncovered. While the shop is shrunk the layer is sized from the window's size in the shrunk pixels, which comes
    /// back to the whole window once shrunk.
    /// </summary>
    [Fact]
    public void A_shrunk_shop_still_darkens_the_whole_window()
    {
        var page = new Fit("page: 'ShopUI', root: '.shop-container', w: 1280, h: 720, parts: [{ w: 400, h: 1000 }]", before: "window.__slprBagsScroll(false)");
        const string layer = "frame.contentDocument.head.children.filter(function (c) { return c.id === 'slpackrat-bag-shrink'; })[0]";

        Assert.Equal((720 - 24) / 1000.0, page.Number("zoom()"), 3);
        Assert.Contains(".bg-mask{width:var(--slpr-vw,100vw);height:var(--slpr-vh,100vh)}", page.Text(layer + ".textContent"));
        Assert.Equal("false", page.Text(layer + ".disabled"));
        Assert.Equal(1280, page.Number("parseFloat(view.body.style.getPropertyValue('--slpr-vw')) * zoom()"), 3);
        Assert.Equal(720, page.Number("parseFloat(view.body.style.getPropertyValue('--slpr-vh')) * zoom()"), 3);
    }

    /// <summary>One screen in a frame Root can read, with bags.js loaded and the screen shown.</summary>
    private sealed class Fit
    {
        private const string Fake = """
            var window = this;
            var shown = [];
            var warnings = [];
            var console = { warn: function (m) { warnings.push(String(m)); } };
            var frame = null;
            var document = {
              getElementsByTagName: function () { return []; },
              getElementById: function () { return frame; },
              addEventListener: function () {}
            };
            window.addEventListener = function () {};
            window.notifyPageReady = function (id) { shown.push(id); };

            // The screen: window size, the game's shrink margin (0 for none), padding round the parts, how far the
            // hints hang under the root, and the parts (a grid, or a panel of fixed size that can't be cut).
            var view = null;
            var mutations = [];
            var listeners = {};

            function px(v) { var n = parseFloat(v); return isNaN(n) ? null : n; }
            function rect(l, t, w, h) { return { left: l, top: t, width: w, height: h, right: l + w, bottom: t + h, x: l, y: t }; }
            function none() { return rect(0, 0, 0, 0); }

            function makeStyle() {
              var props = {};
              return {
                cssText: '',
                getPropertyValue: function (n) { return props[n] || ''; },
                setProperty: function (n, v) { props[n] = String(v); },
                removeProperty: function (n) { delete props[n]; }
              };
            }

            function detach(n) {
              var p = n.parentElement;
              if (!p) return;
              p.children.splice(p.children.indexOf(n), 1);
              n.parentElement = null;
            }

            function cut(n, max, min) {
              var v = px(max) == null ? n : Math.min(n, px(max));
              return px(min) == null ? v : Math.max(v, px(min));
            }

            // The size a part takes: a grid's box cut by bags.js, or the grid's own size.
            function slotSize(i) {
              var p = view.parts[i];
              if (!p.grid) return { w: p.w, h: p.h };
              var g = p.grid, b = g.parentElement;
              if (!b || !b.hasAttribute('data-slpr')) return { w: g.gw, h: g.gh };
              return { w: cut(g.gw, b.style.maxWidth, b.style.minWidth), h: cut(g.gh, b.style.maxHeight, b.style.minHeight) };
            }

            function slotOf(e) {
              for (var i = 0; i < view.parts.length; i++) if (view.parts[i].grid && view.parts[i].grid.parentElement === e) return i;
              return -1;
            }

            function zoom() { return parseFloat(view.body.style.zoom) || 1; }
            function rootW() { var w = 2 * view.pad; for (var i = 0; i < view.parts.length; i++) w += slotSize(i).w; return w; }
            function rootH() { var h = 0; for (var i = 0; i < view.parts.length; i++) h = Math.max(h, slotSize(i).h); return h + 2 * view.pad; }
            function lifted() { return view.root.hasAttribute('data-slpr-lift') ? px(view.root.style.getPropertyValue('--slpr-below')) || 0 : 0; }
            function rootRect() {
              if (view.cols) return colsRect();
              var k = zoom();
              return rect((view.w - rootW() * k) / 2, (view.h - (rootH() + lifted()) * k) / 2, rootW() * k, rootH() * k);
            }
            function slotRect(i) {
              var s = slotSize(i);
              if (view.cols) { var c = colRect(i); return rect(c.left + view.pad, c.top + view.pad, s.w, s.h); }
              var r = rootRect(), k = zoom(), x = r.left + view.pad * k;
              for (var j = 0; j < i; j++) x += slotSize(j).w * k;
              return rect(x, r.top + view.pad * k, s.w * k, s.h * k);
            }

            // A screen of columns: each part is its own root, side by side, each centred in the window's height on its
            // own, the way the trap screen lays out its bag, cage panel and storage box. The game never shrinks it.
            function colW(i) { return slotSize(i).w + 2 * view.pad; }
            function colH(i) { return slotSize(i).h + 2 * view.pad; }
            function colRect(i) {
              var all = 0, x;
              for (var j = 0; j < view.parts.length; j++) all += colW(j);
              x = (view.w - all) / 2;
              for (var j = 0; j < i; j++) x += colW(j);
              return rect(x, (view.h - colH(i)) / 2, colW(i), colH(i));
            }
            function colsRect() {
              var l = Infinity, t = Infinity, r = -Infinity, b = -Infinity;
              for (var i = 0; i < view.parts.length; i++) {
                var c = colRect(i);
                l = Math.min(l, c.left); t = Math.min(t, c.top); r = Math.max(r, c.right); b = Math.max(b, c.bottom);
              }
              return rect(l, t, r - l, b - t);
            }
            function gridRect(i) { var r = slotRect(i), g = view.parts[i].grid; return rect(r.left, r.top, g.gw * zoom(), g.gh * zoom()); }
            function hintRect() { var r = rootRect(); return rect(r.left, r.bottom, r.width, view.hang * zoom()); }
            function stripRect() {
              var d = view.strip.style.display;
              if (!d || d === 'none') return none();
              return rect(view.w / 2 - 300, view.h - 6 - 72 * zoom(), 600, 72 * zoom());
            }

            function sized(e, w, h, r) {
              Object.defineProperty(e, 'offsetWidth', { get: w, configurable: true });
              Object.defineProperty(e, 'offsetHeight', { get: h, configurable: true });
              Object.defineProperty(e, 'clientWidth', { get: w, configurable: true });
              Object.defineProperty(e, 'clientHeight', { get: h, configurable: true });
              e.rect = r;
            }

            // An element. One holding a grid is that grid's box: as big as the grid, or its cut, and it scrolls.
            function el(names) {
              var attrs = {}, top = 0, left = 0;
              var e = {
                names: names, style: makeStyle(), parentElement: null, children: [], isConnected: true, clientLeft: 0, clientTop: 0,
                classList: { contains: function (c) { return names.indexOf('.' + c) >= 0; } },
                hasAttribute: function (n) { return Object.prototype.hasOwnProperty.call(attrs, n); },
                getAttribute: function (n) { return e.hasAttribute(n) ? attrs[n] : null; },
                setAttribute: function (n, v) { attrs[n] = String(v); },
                removeAttribute: function (n) { delete attrs[n]; },
                addEventListener: function () {},
                matches: function (sel) { return sel.split(',').some(function (s) { return names.indexOf(s.trim()) >= 0; }); },
                contains: function (o) { for (; o; o = o.parentElement) if (o === e) return true; return false; },
                querySelector: function (sel) { return e.children.filter(function (c) { return c.matches(sel); })[0] || null; },
                insertBefore: function (n, ref) { detach(n); var i = e.children.indexOf(ref); e.children.splice(i < 0 ? e.children.length : i, 0, n); n.parentElement = e; },
                appendChild: function (n) { detach(n); e.children.push(n); n.parentElement = e; },
                remove: function () { detach(e); }
              };
              var w = function () { var i = slotOf(e); return i < 0 ? 0 : slotSize(i).w; };
              var h = function () { var i = slotOf(e); return i < 0 ? 0 : slotSize(i).h; };
              sized(e, w, h, function () { var i = slotOf(e); return i < 0 ? none() : slotRect(i); });
              Object.defineProperty(e, 'scrollWidth', { get: function () { var i = slotOf(e); return i < 0 ? 0 : Math.max(w(), view.parts[i].grid.gw); } });
              Object.defineProperty(e, 'scrollHeight', { get: function () { var i = slotOf(e); return i < 0 ? 0 : Math.max(h(), view.parts[i].grid.gh); } });
              Object.defineProperty(e, 'scrollTop', { get: function () { return top; }, set: function (v) { top = Math.max(0, Math.min(v, e.scrollHeight - e.clientHeight)); } });
              Object.defineProperty(e, 'scrollLeft', { get: function () { return left; }, set: function (v) { left = Math.max(0, Math.min(v, e.scrollWidth - e.clientWidth)); } });
              Object.defineProperty(e, 'firstElementChild', { get: function () { return e.children[0] || null; } });
              return e;
            }

            function gameShrink() {
              if (!view.shrink) return;
              view.body.style.zoom = '';
              var k = Math.min(1, (view.w - view.shrink) / rootW(), (view.h - view.shrink) / rootH());
              if (k < 1) view.body.style.zoom = String(k);
            }

            var win = {
              get innerWidth() { return view.w; },
              get innerHeight() { return view.h; },
              Element: { prototype: { getBoundingClientRect: function () { return this.rect ? this.rect() : none(); } } },
              ResizeObserver: function () { this.observe = function () {}; this.unobserve = function () {}; },
              MutationObserver: function (f) { mutations.push(f); this.observe = function () {}; },
              Event: function (type) { this.type = type; },
              addEventListener: function (type, f) { (listeners[type] = listeners[type] || []).push(f); },
              dispatchEvent: function (e) {
                if (e.type === 'resize') gameShrink();
                (listeners[e.type] || []).slice().forEach(function (f) { f(e); });
              },
              requestAnimationFrame: function () { return 1; },
              getComputedStyle: function () { return { position: 'static', alignSelf: 'auto', justifySelf: 'auto', order: '0' }; },
              setTimeout: function () {}
            };

            function build(spec) {
              var named = {};
              var add = function (e) { e.names.forEach(function (n) { (named[n] = named[n] || []).push(e); }); return e; };
              view = { w: spec.w, h: spec.h, shrink: spec.shrink || 0, pad: spec.pad || 0, hang: spec.hang || 0, parts: [] };
              var body = view.body = el(['body']);
              var root = view.root = spec.cols ? null : add(el([spec.root]));
              if (root) {
                body.appendChild(root);
                sized(root, rootW, rootH, rootRect);
              } else {
                view.cols = spec.parts.map(function (p, i) {
                  var col = add(el([p.root]));
                  sized(col, function () { return colW(i); }, function () { return colH(i); }, function () { return colRect(i); });
                  body.appendChild(col);
                  return col;
                });
                view.root = view.cols[0];
              }
              spec.parts.forEach(function (p, i) {
                var holder = root || view.cols[i];
                if (!p.grid) { view.parts.push({ w: p.w, h: p.h }); return; }
                var g = add(el([p.grid, '.grid-container']));
                g.gw = p.w;
                g.gh = p.h;
                sized(g, function () { return g.gw; }, function () { return g.gh; }, function () { return gridRect(i); });
                view.parts.push({ grid: g });
                if (!p.wrapper) { holder.appendChild(g); return; }
                var wrapper = add(el(['.grid-wrapper']));
                wrapper.appendChild(g);
                holder.appendChild(wrapper);
              });
              if (view.hang) {
                view.hint = add(el(['.bottom-hint']));
                sized(view.hint, rootW, function () { return view.hang; }, hintRect);
                body.appendChild(view.hint);
              }
              view.strip = add(el(['.gd-row']));
              sized(view.strip, function () { return 600; }, function () { return 72; }, stripRect);
              view.strip.style.display = spec.strip ? 'flex' : 'none';
              body.appendChild(view.strip);
              var doc = {
                location: { pathname: '/' + spec.page + '.html' },
                body: body,
                documentElement: el(['html']),
                head: el(['head']),
                createElement: function () { return el([]); },
                querySelectorAll: function (sel) {
                  var out = [];
                  sel.split(',').forEach(function (s) { (named[s.trim()] || []).forEach(function (e) { if (out.indexOf(e) < 0) out.push(e); }); });
                  return out;
                },
                querySelector: function (sel) { return doc.querySelectorAll(sel)[0] || null; },
                getElementById: function (id) { return (named['#' + id] || [])[0] || null; },
                defaultView: win
              };
              frame = { tagName: 'IFRAME', contentDocument: doc };
              gameShrink();
            }

            // What the tests read, in window pixels.
            function rootTop() { return rootRect().top; }
            function rootBottom() { return rootRect().bottom; }
            function stripTop() { return stripRect().top; }
            function hintBottom() { return hintRect().bottom; }
            function colTop(i) { return colRect(i).top; }
            function colBottom(i) { return colRect(i).bottom; }
            function boxOf(sel) {
              var kids = (view.cols || [view.root]).reduce(function (all, holder) { return all.concat(holder.children); }, []);
              return kids.reduce(function (found, c) { return found || (c.matches(sel) ? c.parentElement : c.querySelector(sel) && c); }, null);
            }
            function fromBottom(sel) { var b = boxOf(sel); return b.scrollHeight - b.clientHeight - b.scrollTop; }
            function hideStrip() { view.strip.style.display = 'none'; mutations.forEach(function (f) { f([]); }); }
            function resize(w, h) { view.w = w; view.h = h; win.dispatchEvent(new win.Event('resize')); }
            """;

        private readonly Engine _engine = new();

        /// <param name="before">Script run once bags.js is loaded, before the screen is shown.</param>
        public Fit(string spec, string? before = null)
        {
            _engine.Execute(Fake);
            _engine.Execute(Resource("PackRat.bags.js"));

            if (before != null) _engine.Execute(before);

            _engine.Execute("build({ " + spec + " }); window.notifyPageReady('" + Page(spec) + "')");

            var warnings = Text("warnings.join(' | ')");

            if (warnings != "") throw new InvalidOperationException("bags.js warned: " + warnings);
        }

        public void Run(string script) => _engine.Execute(script);

        public string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

        public double Number(string expression) => _engine.Evaluate(expression).AsNumber();

        private static string Page(string spec)
        {
            var start = spec.IndexOf("page: '", StringComparison.Ordinal) + 7;

            return spec[start..spec.IndexOf('\'', start)];
        }

        private static string Resource(string name)
        {
            using var stream = typeof(BagFitTests).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
