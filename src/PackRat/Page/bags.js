// Scrolling grids on the game's inventory screens. A grid too big for the window gets a scrollbar instead of the game
// shrinking the whole screen until it fits. Runs in Root.html, where every screen is a frame of the same origin; a
// frame is set up just before it is shown (Root's notifyPageReady), so it never shows unfitted.
// With the ScrollBars setting off (window.__slprBagsScroll(false)) a screen already set up gives its grids back, and
// a screen too big for the window is shrunk until it fits instead: most by the game, the rest here (see "ScrollBars
// off" below).
(function () {
  if (typeof document.getElementsByTagName !== 'function' || window.__slprBags) return;
  window.__slprBags = true;

  let scrollOn = true;
  // The screens set up, and those shown while scrolling was off (set up once it is back on).
  let screens = [];
  let waiting = [];
  // How each screen is shrunk while scrolling is off.
  let shrinks = [];

  // Some screens place their parts from the grid's rows rather than from what shows. While a screen is cut to fit
  // the window, the page carries data-slpr-own and its "css" turns those placements off, and its "follow" places
  // the parts from the cut grids instead. Outside that, the game's layout stands untouched.
  function setVar(el, name, px) {
    const v = px == null ? '' : px + 'px';

    if (el.style.getPropertyValue(name) === v) return;

    if (v) el.style.setProperty(name, v); else el.style.removeProperty(name);
  }

  // The shop gives the bag's slot the size of the bag's rows and columns, and moves the shop list or the bag down so
  // the list's middle lines up with the grid's middle. Each side only moves down into room the screen has spare
  // ("room" is the height the screen may take), so the grid is never cut shorter just to line up.
  function followShop(doc, own, room) {
    if (!own) return;

    const slot = doc.querySelector('.left-slot');
    const column = slot && slot.querySelector('.left-column');

    if (!column || !column.offsetHeight) return;

    const box = column.querySelector('.grid-wrapper');

    setVar(slot, '--slpr-h', column.offsetHeight);
    setVar(slot, '--slpr-w', column.offsetWidth);

    const right = doc.querySelector('.right-panel');
    const list = right && right.querySelector('.shop-list');

    if (!list || !box) return;

    const c = column.getBoundingClientRect(), b = box.getBoundingClientRect();
    const r = right.getBoundingClientRect(), l = list.getBoundingClientRect();
    const delta = Math.round((l.top + l.height / 2 - r.top) - (b.top + b.height / 2 - c.top));

    // A rounded height can be a fraction of a pixel short, so the room left comes from the exact heights.
    setVar(slot, '--slpr-mt', Math.min(Math.max(0, delta), Math.max(0, Math.floor(room - c.height))));
    setVar(right, '--slpr-mt', Math.min(Math.max(0, -delta), Math.max(0, Math.floor(room - r.height))));
  }

  // A bag with tabs (several inventories in one) keeps room for its tallest and widest tab, and the two sides move
  // down so their grids' middles line up.
  function followBags(doc, own) {
    if (!own) return;

    const slots = doc.querySelectorAll('.bag-slot');
    const mids = [];
    let anchored = false;

    for (let i = 0; i < slots.length; i++) {
      const slot = slots[i];
      const panel = slot.querySelector('.bag-panel');
      const box = panel && panel.querySelector('.grid-wrapper');

      if (slot.classList.contains('is-anchored') && panel) {
        anchored = true;
        setVar(slot, '--slpr-h', panel.offsetHeight);
        setVar(slot, '--slpr-w', panel.offsetWidth);
      } else {
        setVar(slot, '--slpr-h', null);
        setVar(slot, '--slpr-w', null);
      }

      const b = box && box.getBoundingClientRect();

      if (b && b.height) mids.push({ slot, mid: b.top + b.height / 2 - slot.getBoundingClientRect().top });
      else setVar(slot, '--slpr-mt', null);
    }

    const delta = anchored && mids.length === 2 ? Math.round(mids[1].mid - mids[0].mid) : 0;

    for (let i = 0; i < mids.length; i++) setVar(mids[i].slot, '--slpr-mt', i === 0 ? Math.max(0, delta) : Math.max(0, -delta));
  }

  // The tool table pins its right panel to the taller of the left panel and the workbench's own content, and the left
  // panel stretches to the right one, so neither gets shorter once a grid is cut. This works the height out the way
  // the game does, from the left panel's own height.
  function followToolPanels(doc, own) {
    if (!own) return;

    const left = doc.querySelector('.backpack-panel');
    const right = doc.querySelector('.combined-panel');

    if (!left || !right || !right.style.height) return;

    const bench = right.querySelector('.workbench-section');
    const controls = right.querySelector('.craft-controls');
    const recent = doc.getElementById('recentBlock');
    const old = right.style.height;

    right.style.height = '0px';

    let h = left.offsetHeight;

    if (bench && controls) {
      const last = recent && recent.offsetHeight > 0 ? recent : controls;
      const pad = parseFloat(doc.defaultView.getComputedStyle(bench).paddingBottom) || 0;
      const borders = right.offsetHeight - right.clientHeight;

      h = Math.max(h, Math.ceil(bench.offsetTop + last.offsetTop + last.offsetHeight + pad + borders));
    }

    right.style.height = h > 0 ? h + 'px' : old;
  }

  const OWN_BAGS = '[data-slpr-own] .bag-slot{height:var(--slpr-h,auto)!important;max-width:var(--slpr-w,none)!important;margin-top:var(--slpr-mt,0px)!important}'
    + '[data-slpr-own] .bag-slot.is-anchored>.bag-panel{top:0!important}';
  const OWN_SHOP = '[data-slpr-own] .left-slot{height:var(--slpr-h,auto)!important;max-width:var(--slpr-w,none)!important;margin-top:var(--slpr-mt,0px)!important}'
    + '[data-slpr-own] .left-column{top:0!important}'
    + '[data-slpr-own] .right-panel{margin-top:var(--slpr-mt,0px)!important}';
  // The tool table and the cooking station pin their tab rows to the grid's width; the row takes the panel's width instead.
  const OWN_TOOLS = '[data-slpr-own] .left-tabs{width:0!important;min-width:100%!important}';
  const OWN_COOK = '[data-slpr-own] .bag-tabs{width:0!important;min-width:100%!important}';
  // The car trunk's note wraps inside its panel instead of widening it. The line of hints under the bag is centred
  // under it at its own width, and --slpr-hx moves it back inside the window when the bag is narrower than it.
  const BAG_HINTS = '.bag-panel .trunk-hint{width:0;min-width:calc(100% - 28px)}'
    + '.bottom-hint{width:max-content;left:50%;right:auto;transform:translateX(calc(-50% + var(--slpr-hx,0px)))}';
  // The game shrinks the bag screen by the size of its panels, and the line of hints hangs 30px under them (an 11px
  // gap and the line), so a screen shrunk to fit ran the line off the bottom of the window. Room under the panels as
  // tall as the line makes the game count it.
  const HINT_ROOM = '.backpack-container:has(.bottom-hint){padding-bottom:30px}';

  // Each screen: the element(s) the game fits to the window, its margin, and the grids that may scroll. "boxes" names
  // boxes the screen already wraps its grid in; "follow" places parts the game places from the grid's rows (see
  // above); "watch" is an element whose height the game sets; "css" is added to the screen's styles. "sized" names
  // parts that grow with what they hold, so the screen is fitted again when they change size; "below" names parts that
  // hang under the screen and count toward its height; "hint" names lines that must stay inside the window; "lists"
  // names parts that scroll what they hold, cut shorter when the screen is too tall for the window; "noZoom" marks a
  // screen the game never shrinks; "side" is the game's margin for width when it differs from its margin for height;
  // "bottom" names grids that fill from the bottom up, so their boxes open scrolled to the bottom; "offCss" is added
  // to the screen's styles while scrolling is off.
  const PAGES = {
    AidDepot: { root: '.hub-root', margin: 48, grids: '#bagGrid,#recvGrid' },
    BrewPanel: { root: '.brew-system', margin: 48, grids: '#bagGrid,#vatGrid', noZoom: true },
    Compressor: { root: '#app', margin: 24, grids: '#cartGrid,#binGrid,#trayGrid' },
    Cooking: { root: '.cooking-system', margin: 48, grids: '#backpackGrid,#mainPotGrid', css: OWN_COOK },
    DailyFert: { root: '.df-root', margin: 48, grids: '#bagGrid' },
    DroneHub: { root: '.hub-root', margin: 48, grids: '#bagGrid,#storeGrid', racks: { '#bagGrid': '#bagRack' } },
    DyePanel: { root: '.dye-system', margin: 48, grids: '#bagGrid,#vatGrid', noZoom: true },
    FuelGenerator: { root: '.generator-system', margin: 48, grids: '#backpackGrid' },
    GreenhouseBuild: { root: '#ghRoot', margin: 40, grids: '#cartGrid,#pickGrid', racks: { '#cartGrid': '#cartRack' } },
    Heater: { root: '.heater-system', margin: 48, grids: '#backpackGrid' },
    // The rack's rows grow one per kind of material it holds; they scroll like a grid rather than shrink the screen.
    MaterialRack: { root: '.rack-system', margin: 24, grids: '#bagGrid,#rackBody' },
    RatCage: { root: '.generator-system', margin: 48, grids: '#backpackGrid,#foodGrid' },
    Shredder: { root: '#app', margin: 24, grids: '#cartGrid,#hopperGrid,#trayGrid', racks: { '#cartGrid': '#cartRack' } },
    ToolTable: { root: '.ui-container', margin: 24, grids: '#backpack-grid,#workbench-grid', follow: followToolPanels, css: OWN_TOOLS, racks: { '#backpack-grid': '#rack-rows' } },
    TrapPanel: { root: '#bagCol,.popup-container,#storeCol', margin: 48, grids: '#backpackGrid,#storeGrid', noZoom: true },
    // The furniture trash can's pit grows with its room, far taller than the window, and its pile starts at the bottom.
    // The line telling the player to drag things in sits at the bottom too, where the cut pit opens. The help text
    // under the pit wraps to the pit's width; in English it is one long line that made the screen twice as wide.
    TrashCan: {
      root: '.trash-system', margin: 24, grids: '#bagGrid,#pit', bottom: '#pit',
      css: '[data-slpr~=cy]>#pit>#pitHint{top:auto;height:120px}#panelB>.foot{width:0;min-width:100%}',
    },
    TradeUI: { root: '.trade', margin: 114, side: 48, grids: '#tradeBagGrid,.col.mid>.grid-container,.col.mid>[data-slpr]>.grid-container', racks: { '#tradeBagGrid': '.rack-view' } },
    // The basket's panel is as wide as the game's five-column basket; a wider basket widens it.
    BackpackUI: {
      root: '.backpack-container', margin: 24, boxes: '.grid-wrapper', follow: followBags,
      sized: '.bag-panel', below: '.bottom-hint', hint: '.bottom-hint',
      css: OWN_BAGS + BAG_HINTS + '.bag-panel.is-basket-panel .panel{min-width:min-content}', offCss: HINT_ROOM,
    },
    // The shop's left column hangs out of its slot whenever it is taller than the bag's rows. The dark layer behind the
    // shop is as big as the window, so it shrinks with the screen unless it is sized from the window again.
    ShopUI: {
      root: '.shop-container', margin: 24, boxes: '.grid-wrapper', follow: followShop, watch: '.left-slot',
      sized: '.left-column', below: '.left-column', lists: '.right-panel', noZoom: true, css: OWN_SHOP,
      offCss: '.bg-mask{width:var(--slpr-vw,100vw);height:var(--slpr-vh,100vh)}',
    },
  };

  const MIN_SIDE = 120;
  // Room kept between a line of hints and the window's edge.
  const HINT_EDGE = 12;
  const EDGE_IN = 12;
  const EDGE_OUT = 28;
  const EDGE_SPEED = 16;
  const DRAG_START = 6;
  const LEVEL_STEP = 4;
  const RING = 6;
  // The game shrinks most screens to fit the window. One only a little too wide looks better shrunk a little than
  // with a column cut short, so the width is cut only as far as needed to keep the game's shrink at this or above.
  const MIN_ZOOM = 0.9;
  // Room kept at each side of a screen the game never shrinks.
  const SIDE_EDGE = 12;
  // The game's strip of things lying nearby (webui-grounddock.js) goes in the first free band under the screen, and
  // with none it sits on the window's bottom edge, over the screen. The game's trade screen keeps a band this tall
  // under itself for it (GROUND_RESERVE in TradeUI.html: the strip's 72, a 10 gap and 6 from the edge, plus 2).
  const STRIP_ROOM = 90;

  // A grid in a box cut narrower than it keeps its own width and scrolls, rather than squeezing to the box. A screen
  // with tabs keeps room around a grid for its biggest tab (margins on the grid); in a cut box that room would only
  // scroll into empty space, so it goes, and the box keeps its height and centres a narrower grid instead.
  const CSS = '[data-slpr~=wrap]{display:flow-root;flex:none}'
    + '[data-slpr~=wrap][data-slpr~=cap]>*{min-width:max-content}'
    + '[data-slpr~=wrap][data-slpr~=cy]>*{margin-bottom:0!important}'
    + '[data-slpr~=wrap][data-slpr~=cx]>*{width:max-content;margin-left:auto!important;margin-right:auto!important}'
    + '[data-slpr~=cy]{overflow-y:scroll}'
    + '[data-slpr~=cap]:not([data-slpr~=cy]){overflow-y:hidden}'
    + '[data-slpr~=cx]{overflow-x:auto}'
    + '[data-slpr~=cap]:not([data-slpr~=cx]){overflow-x:hidden}'
    + '.grid-wrapper[data-slpr~=cap]{justify-content:flex-start}'
    + '.grid-wrapper[data-slpr~=cap]>.grid-container{flex:none;margin-left:auto;margin-right:auto}'
    + '[data-slpr~=cap]::-webkit-scrollbar{width:8px;height:8px}'
    + '[data-slpr~=cap]::-webkit-scrollbar-track{background:rgba(0,0,0,0.35);border-radius:4px}'
    + '[data-slpr~=cap]::-webkit-scrollbar-thumb{background:rgba(255,200,58,0.45);border-radius:4px}'
    + '[data-slpr~=cap]::-webkit-scrollbar-thumb:hover{background:rgba(255,200,58,0.7)}'
    + '[data-slpr~=cap]::-webkit-scrollbar-corner{background:transparent}'
    // A box cut only in height shrinks to its grid and scrollbar; its margins take the rest (see hugGrids).
    + '[data-slpr~=hug]{flex:none!important;width:max-content!important;margin-left:var(--slpr-l)!important;margin-right:var(--slpr-r)!important}'
    + '[data-slpr~=hug]>*{margin-left:0!important;margin-right:0!important}'
    // A part that scrolls what it holds, cut shorter to fit (see fitLists).
    + '[data-slpr-cut]{max-height:var(--slpr-lh)!important}';

  function pageOf(doc) {
    const m = /\/([^/]+)\.html$/i.exec(doc.location.pathname || '');

    return m && Object.prototype.hasOwnProperty.call(PAGES, m[1]) ? PAGES[m[1]] : null;
  }

  function prepare(frame) {
    if (!frame || frame.tagName !== 'IFRAME') return;

    let doc;
    try { doc = frame.contentDocument; } catch (e) { return; }

    if (!doc || !doc.body || !doc.defaultView) return;

    if (doc.__slprBags) {
      shownAgain(doc);
      return;
    }

    const cfg = pageOf(doc);

    if (!cfg) return;

    const shrink = shrinkOf(doc, cfg);

    doc.__slprBags = true;
    shrinks.push(shrink);

    if (scrollOn) {
      start(doc, cfg);
      return;
    }

    waiting.push({ doc, cfg });
    shrink.start();
  }

  function start(doc, cfg) {
    try { screens.push(setup(doc, cfg)); } catch (e) { console.warn('[PackRat] bag scroll: ' + e); }
  }

  // A screen whose frame is gone has no window any more.
  function alive(doc) { return !!(doc.defaultView && doc.body); }

  // Root shows a screen it keeps loaded again rather than building a new one.
  function shownAgain(doc) {
    for (let i = 0; i < screens.length; i++) {
      if (screens[i].doc !== doc) continue;

      try { screens[i].reopen(); } catch (e) { console.warn('[PackRat] bag scroll: ' + e); }
    }

    for (let i = 0; i < shrinks.length; i++) {
      if (shrinks[i].doc === doc) shrinks[i].refit();
    }
  }

  window.__slprBagsScroll = function (on) {
    on = !!on;

    if (on === scrollOn) return;

    scrollOn = on;
    screens = screens.filter(c => alive(c.doc));
    shrinks = shrinks.filter(c => alive(c.doc));

    // Scrolling grids and a screen shrunk to fit never mix, so one goes before the other starts.
    if (on) shrinks.forEach(c => c.stop());

    for (let i = 0; i < screens.length; i++) {
      try { if (on) screens[i].resume(); else screens[i].stop(); } catch (e) { console.warn('[PackRat] bag scroll: ' + e); }
    }

    if (!on) {
      shrinks.forEach(c => c.start());
      return;
    }

    const shown = waiting;
    waiting = [];

    for (let i = 0; i < shown.length; i++) if (alive(shown[i].doc)) start(shown[i].doc, shown[i].cfg);
  };

  function frameOf(win) {
    const frames = document.getElementsByTagName('iframe');

    for (let i = 0; i < frames.length; i++) if (frames[i].contentWindow === win) return frames[i];

    return null;
  }

  // Root reveals a frame in notifyPageReady; the frame's page calls it through window, so a wrapper runs first.
  let ready = window.notifyPageReady;
  const readyHook = function (pageId) {
    try { prepare(document.getElementById(pageId)); } catch (e) { /* the page still shows */ }

    return typeof ready === 'function' ? ready.apply(this, arguments) : undefined;
  };

  try {
    Object.defineProperty(window, 'notifyPageReady', {
      configurable: true,
      get: () => typeof ready === 'function' ? readyHook : ready,
      set: v => { ready = v; },
    });
  } catch (e) { /* the load listener below still sets frames up */ }

  window.addEventListener('message', e => {
    const t = e.data && e.data.type;

    if (t === '__PAGE_READY__' || t === 'PAGE_READY') prepare(frameOf(e.source));
  }, true);

  document.addEventListener('load', e => prepare(e.target), true);

  const existing = document.getElementsByTagName('iframe');

  for (let i = 0; i < existing.length; i++) prepare(existing[i]);

  // ===== ScrollBars off =====

  // With scrolling off, a screen too big for the window is shrunk until it fits, as the game does without Pack Rat.
  // A screen the game never shrinks (brewing, dyeing, the trap, the shop) ran off the window with a big bag, so it is
  // shrunk here the way the game shrinks the rest (createViewportFit in webui-bag.js): the body is zoomed by the
  // smaller of the room for its width over its width and the room for its height over its height, and made as tall
  // as the window so the screen stays centred. Those screens' own drags already divide by the body's zoom.
  function shrinkOf(doc, cfg) {
    const win = doc.defaultView;
    const body = doc.body;
    // Taken before the screen is set up for scrolling, which changes it for grids in cut boxes.
    const rectOf = win.Element.prototype.getBoundingClientRect;
    const parts = cfg.noZoom ? cfg.root + (cfg.below ? ',' + cfg.below : '') : '';
    let style = null;
    let on = false;
    let lastSizes = '';

    function start() {
      if (on) return;

      on = true;
      useOffCss(true);
      refit();
    }

    function stop() {
      if (!on) return;

      on = false;
      useOffCss(false);
      lastSizes = '';

      if (parts) unzoom();
    }

    // Fits the screen again: when it is shown, the window changes size, or a part of it does.
    function refit() {
      if (!on || !parts) return;

      lastSizes = sizes();
      unzoom();

      const size = extent(doc.querySelectorAll(parts), rectOf);
      const k = size.w && size.h ? Math.min(1, (win.innerWidth - cfg.margin) / size.w, (win.innerHeight - cfg.margin) / size.h) : 1;

      if (k >= 1) return;

      body.style.zoom = String(k);
      body.style.minHeight = win.innerHeight / k + 'px';
      // The window's size in the body's shrunk pixels, for parts that must still cover the whole window.
      body.style.setProperty('--slpr-vw', win.innerWidth / k + 'px');
      body.style.setProperty('--slpr-vh', win.innerHeight / k + 'px');
    }

    function unzoom() {
      body.style.zoom = '';
      body.style.minHeight = '';
      body.style.removeProperty('--slpr-vw');
      body.style.removeProperty('--slpr-vh');
    }

    // The parts' own sizes, which the zoom doesn't change, so a screen is fitted again only when one of them grows
    // or shrinks.
    function sizes() {
      const els = doc.querySelectorAll(parts);
      let out = '';

      for (let i = 0; i < els.length; i++) out += els[i].offsetWidth + 'x' + els[i].offsetHeight + ',';

      return out;
    }

    // Adds or takes off the styles a screen gets while scrolling is off, then sends a resize so the game fits it again.
    function useOffCss(use) {
      if (!cfg.offCss) return;

      if (!style) {
        style = doc.createElement('style');
        style.id = 'slpackrat-bag-shrink';
        style.textContent = cfg.offCss;
        (doc.head || doc.documentElement).appendChild(style);
      }

      style.disabled = !use;
      win.dispatchEvent(new win.Event('resize'));
    }

    if (parts) {
      const ro = new win.ResizeObserver(() => { if (on && sizes() !== lastSizes) refit(); });
      const els = doc.querySelectorAll(parts);

      for (let i = 0; i < els.length; i++) ro.observe(els[i]);

      win.addEventListener('resize', refit);
    }

    return { doc, start, stop, refit };
  }

  // The box round every element given that shows, in the window's pixels.
  function extent(els, rectOf) {
    let left = Infinity, top = Infinity, right = -Infinity, bottom = -Infinity;

    for (let i = 0; i < els.length; i++) {
      const r = rectOf.call(els[i]);

      if (!r.width && !r.height) continue;

      left = Math.min(left, r.left);
      top = Math.min(top, r.top);
      right = Math.max(right, r.right);
      bottom = Math.max(bottom, r.bottom);
    }

    return right > left ? { w: right - left, h: bottom - top } : { w: 0, h: 0 };
  }

  // ===== One screen =====

  function setup(doc, cfg) {
    const win = doc.defaultView;
    const nativeRect = win.Element.prototype.getBoundingClientRect;
    const states = [];
    // The boxes whose grids show; a grid the game hid for another tab (a material rack's rows) keeps its cut.
    let shown = states;
    const ptr = { x: 0, y: 0, held: false, dragging: false, sx: 0, sy: 0, onBox: false };
    let fitting = false;
    let off = false;
    let own = false;
    // The height the screen may take while it is being fitted.
    let room = Infinity;
    // The band the screen keeps under itself for the strip of things lying nearby, in the screen's pixels (0 for
    // none). Once kept it stays until the screen is shown again, as on the game's trade screen, so taking the last
    // thing doesn't make the screen jump. A band that makes the game draw the whole screen smaller than it did
    // without one is given up ("noBand"), and the strip sits over the screen as it does in the game. Both hold for
    // the window size they were worked out for ("bandWin").
    let band = 0;
    let noBand = false;
    let bandWin = '';
    let zoomWithout = 1;
    let strip = null;
    let lastSig = '';
    let lastKey = '';
    let fitRaf = 0;
    let autoRaf = 0;
    let pokeRaf = 0;

    const style = doc.createElement('style');
    style.id = 'slpackrat-bag-scroll';
    // A screen wider than the window squeezes its side panels (or is cut off) instead of growing, so the size it
    // shows is the window's, not what it needs. Held at least as wide as its contents, it shows what it needs and
    // the boxes are cut by that. A screen that fits is already that wide, so nothing changes for it.
    const wide = cfg.root.split(',').map(r => 'html ' + r.trim()).join(',') + '{min-width:max-content}';
    // The game centres the screen on its own and lets the parts named by "below" hang under it, so on a tall screen
    // they run off the bottom of the window. A margin as tall as what hangs below centres the two together. The band
    // kept for the strip of things nearby is a margin the same way.
    const lifted = '[data-slpr-lift]{margin-bottom:var(--slpr-below)!important}';
    const sizedSeen = [];
    const hintSeen = [];

    style.textContent = CSS + wide + lifted + (cfg.css || '');
    (doc.head || doc.documentElement).appendChild(style);

    // ----- Grids and their boxes -----

    function stateOf(grid) {
      for (let i = 0; i < states.length; i++) if (states[i].grid === grid) return states[i];

      return null;
    }

    function findGrids() {
      const out = [];

      if (cfg.grids) {
        const found = doc.querySelectorAll(cfg.grids);

        for (let i = 0; i < found.length; i++) if (out.indexOf(found[i]) < 0) out.push(found[i]);
      } else {
        const boxes = doc.querySelectorAll(cfg.boxes);

        for (let i = 0; i < boxes.length; i++) {
          const grid = boxes[i].querySelector('.grid-container');

          if (grid && grid.parentElement === boxes[i]) out.push(grid);
        }
      }

      return out;
    }

    function wrap(grid) {
      if (cfg.boxes) return grid.parentElement;

      const parent = grid.parentElement;

      if (parent && parent.hasAttribute('data-slpr')) return parent;

      const cs = win.getComputedStyle(grid);

      if (cs.position === 'absolute' || cs.position === 'fixed' || !parent) return null;

      const box = doc.createElement('div');
      box.setAttribute('data-slpr', 'box wrap');

      if (cs.alignSelf !== 'auto' && cs.alignSelf !== 'normal') box.style.alignSelf = cs.alignSelf;
      if (cs.justifySelf !== 'auto' && cs.justifySelf !== 'normal') box.style.justifySelf = cs.justifySelf;
      if (cs.order !== '0') box.style.order = cs.order;

      parent.insertBefore(box, grid);
      box.appendChild(grid);

      return box;
    }

    // The box gives its grid back as the game made it: no cut, and a box of our own is taken out.
    function release(s) {
      ro.unobserve(s.box);
      ro.unobserve(s.grid);
      uncap(s);

      if (cfg.boxes) {
        s.box.removeAttribute('data-slpr');
      } else if (s.grid.parentElement === s.box && s.box.parentElement) {
        s.box.parentElement.insertBefore(s.grid, s.box);
        s.box.remove();
      } else if (!s.box.firstElementChild) {
        s.box.remove();
      }
    }

    function scan() {
      if (off) return;

      for (let i = states.length - 1; i >= 0; i--) {
        const s = states[i];

        if (s.grid.isConnected && s.grid.parentElement === s.box) continue;

        states.splice(i, 1);
        release(s);
      }

      const grids = findGrids();

      for (let i = 0; i < grids.length; i++) {
        const grid = grids[i];

        if (stateOf(grid)) continue;

        const box = wrap(grid);

        if (!box) continue;

        if (cfg.boxes && !box.hasAttribute('data-slpr')) box.setAttribute('data-slpr', 'box');

        const s = { grid, box, h: 0, w: 0, mw: 0, rects: [], bottom: !!cfg.bottom && grid.matches(cfg.bottom) };
        states.push(s);
        box.addEventListener('scroll', () => onBoxScroll(s), { passive: true });
        ro.observe(box);
        ro.observe(grid);
        mo.observe(grid, { attributes: true, attributeFilter: ['style'] });
      }

      watchParts(cfg.sized, ro, sizedSeen);
      watchParts(cfg.hint, hintRo, hintSeen);
    }

    // Watches the parts a selector names for size changes, and lets go of those the game has taken out.
    function watchParts(sel, observer, seen) {
      if (!sel) return;

      for (let i = seen.length - 1; i >= 0; i--) {
        if (seen[i].isConnected) continue;

        observer.unobserve(seen[i]);
        seen.splice(i, 1);
      }

      const parts = doc.querySelectorAll(sel);

      for (let i = 0; i < parts.length; i++) {
        if (seen.indexOf(parts[i]) >= 0) continue;

        seen.push(parts[i]);
        observer.observe(parts[i]);
      }
    }

    // ----- Caps -----

    function setCap(s, h, w) {
      if (s.h === h && s.w === w) return;

      s.h = h;
      s.w = w;
      s.box.style.maxHeight = h ? h + 'px' : '';
      s.box.style.minHeight = h && !s.hidden ? h + 'px' : '';
      s.box.style.maxWidth = w ? w + 'px' : '';
      flags(s, !!h, !!w);
    }

    function flags(s, y, x) {
      const tokens = s.box.getAttribute('data-slpr').split(' ').filter(t => t !== 'cap' && t !== 'cy' && t !== 'cx');

      if (y || x) tokens.push('cap');
      if (y) tokens.push('cy');
      if (x) tokens.push('cx');

      const next = tokens.join(' ');

      if (next !== s.box.getAttribute('data-slpr')) s.box.setAttribute('data-slpr', next);
    }

    function setMin(s, w) {
      if (s.mw === w) return;

      s.mw = w;
      s.box.style.minWidth = w ? w + 'px' : '';
    }

    function capped(s) { return !!(s.h || s.w); }

    function uncap(s) {
      unhug(s);
      setCap(s, 0, 0);
      setMin(s, 0);
    }

    function tokensOf(s) { return (s.box.getAttribute('data-slpr') || '').split(' '); }

    // A box cut only in height keeps the width the screen gave it, so its scrollbar can sit far from a narrower grid.
    // It shrinks to the grid and its scrollbar instead, and its margins grow by what it gave up, so the grid and
    // everything around it stay where they were.
    function hugGrids() {
      for (let i = 0; i < shown.length; i++) {
        const s = shown[i];
        const tokens = tokensOf(s);

        if (tokens.indexOf('cy') < 0 || tokens.indexOf('cx') >= 0 || s.mw) continue;

        const cs = win.getComputedStyle(s.box);
        const g = nativeRect.call(s.grid), b = nativeRect.call(s.box);
        const borderL = s.box.clientLeft, borderR = parseFloat(cs.borderRightWidth) || 0;
        const bar = s.box.offsetWidth - s.box.clientWidth - borderL - borderR;
        const gapL = g.left - (b.left + borderL + (parseFloat(cs.paddingLeft) || 0));
        const gapR = b.right - borderR - bar - (parseFloat(cs.paddingRight) || 0) - g.right;

        if (gapL < -0.5 || gapR < -0.5 || gapL + gapR <= 1) continue;

        s.box.style.setProperty('--slpr-l', (parseFloat(cs.marginLeft) || 0) + gapL + 'px');
        s.box.style.setProperty('--slpr-r', (parseFloat(cs.marginRight) || 0) + gapR + 'px');
        s.box.setAttribute('data-slpr', tokens.concat('hug').join(' '));
      }
    }

    function unhug(s) {
      const tokens = tokensOf(s);

      if (tokens.indexOf('hug') < 0) return;

      s.box.setAttribute('data-slpr', tokens.filter(t => t !== 'hug').join(' '));
      s.box.style.removeProperty('--slpr-l');
      s.box.style.removeProperty('--slpr-r');
    }

    // Rectangles are in window pixels and sizes in the screen's pixels; the two differ while the game shrinks the screen.
    function scaleOf(el) {
      const q = nativeRect.call(el);

      return el.offsetWidth && q.width ? q.width / el.offsetWidth : 1;
    }

    function rootSize() {
      const els = doc.querySelectorAll(cfg.root);
      const size = els.length === 1 ? { w: els[0].offsetWidth, h: els[0].offsetHeight } : unionSize(els);

      if (size.h) size.h += overhang(els) + band;

      return size;
    }

    function stripRow() {
      if (!strip || !strip.isConnected) strip = doc.querySelector('.gd-row');

      return strip;
    }

    function stripShown() {
      const row = stripRow();

      return !!row && !!row.style.display && row.style.display !== 'none';
    }

    // The game's shrink leaves margins out, so a band it did not count can push the screen past the window's top.
    function bandFits() {
      const parts = doc.querySelectorAll(cfg.root + (cfg.below ? ',' + cfg.below : ''));
      let top = Infinity, bottom = -Infinity;

      for (let i = 0; i < parts.length; i++) {
        const r = nativeRect.call(parts[i]);

        if (!r.height) continue;

        top = Math.min(top, r.top);
        bottom = Math.max(bottom, r.bottom);
      }

      return top >= -0.5 && bottom + band * scaleOf(parts[0]) <= win.innerHeight + 0.5;
    }

    // How much more room the strip needs under the screen, in the screen's pixels. It only asks once it sits over the
    // screen (the game found no free band for it) or a band is kept already.
    function stripShort() {
      if (noBand || !stripShown()) return 0;

      const q = nativeRect.call(stripRow());
      const parts = doc.querySelectorAll(cfg.root + (cfg.below ? ',' + cfg.below : ''));
      let bottom = -Infinity, covers = false;

      for (let i = 0; i < parts.length; i++) {
        const r = nativeRect.call(parts[i]);

        if (!r.height) continue;

        bottom = Math.max(bottom, r.bottom);
        covers = covers || q.top < r.bottom && q.bottom > r.top && q.left < r.right && q.right > r.left;
      }

      const short = STRIP_ROOM - (win.innerHeight - bottom);

      return (covers || band) && short > 1 ? Math.ceil(short / scaleOf(parts[0])) : 0;
    }

    // How far the parts named by "below" hang under the screen, in the screen's pixels.
    function overhang(els) {
      if (!cfg.below || !els.length) return 0;

      const parts = doc.querySelectorAll(cfg.below);
      let bottom = -Infinity, over = 0;

      for (let i = 0; i < els.length; i++) {
        const q = nativeRect.call(els[i]);

        if (q.height) bottom = Math.max(bottom, q.bottom);
      }

      for (let i = 0; i < parts.length; i++) {
        const q = nativeRect.call(parts[i]);

        if (q.height) over = Math.max(over, q.bottom - bottom);
      }

      return over > 0 ? Math.ceil(over / scaleOf(els[0])) : 0;
    }

    function liftAbove() {
      const els = doc.querySelectorAll(cfg.root);

      for (let i = 0; i < els.length; i++) lift(els[i], overhang([els[i]]) + band);
    }

    function lift(el, px) {
      setVar(el, '--slpr-below', px || null);

      if (px) el.setAttribute('data-slpr-lift', '');
      else el.removeAttribute('data-slpr-lift');
    }

    function unionSize(els) {
      let l = Infinity, t = Infinity, r = -Infinity, b = -Infinity;

      for (let i = 0; i < els.length; i++) {
        const q = nativeRect.call(els[i]);

        if (!q.width || !q.height) continue;

        l = Math.min(l, q.left);
        t = Math.min(t, q.top);
        r = Math.max(r, q.right);
        b = Math.max(b, q.bottom);
      }

      return r > l ? { w: r - l, h: b - t } : { w: 0, h: 0 };
    }

    function follow() {
      if (cfg.follow) cfg.follow(doc, own, room);
    }

    function setOwn(on) {
      own = on && !!(cfg.follow || cfg.css);

      if (doc.documentElement.hasAttribute('data-slpr-own') === own) return;

      if (own) doc.documentElement.setAttribute('data-slpr-own', '');
      else doc.documentElement.removeAttribute('data-slpr-own');
    }

    function measure() {
      follow();

      return rootSize();
    }

    function size(s, axis) { return axis === 'h' ? s.box.offsetHeight : s.box.offsetWidth; }

    function screen(axis) {
      const r = measure();

      return axis === 'h' ? gridPartsHeight(r) : r.w;
    }

    // The game centres each part of a screen made of several on its own (the trap's bag, cage panel and storage box).
    // When it never shrinks the screen, a part with no grid that is too tall stays too tall whatever is cut, so the
    // screen counts as fitting once every part with a grid fits.
    function gridPartsHeight(r) {
      const els = doc.querySelectorAll(cfg.root);
      let most = 0;

      if (!cfg.noZoom || els.length < 2) return r.h;

      for (let i = 0; i < els.length; i++) {
        if (shown.some(s => els[i].contains(s.box))) most = Math.max(most, els[i].offsetHeight + overhang([els[i]]) + band);
      }

      return most;
    }

    function tryCap(s, axis, value) {
      const oldH = s.h, oldW = s.w;

      if (axis === 'h') setCap(s, value, s.w); else setCap(s, s.h, value);

      return () => setCap(s, oldH, oldW);
    }

    // The screen too tall or wide for the window: every box bigger than some size is cut to that size, the biggest
    // size that lets the screen fit, so two big grids share the room rather than one being cut to a sliver. A box
    // that did not need its cut (one beside a taller column) then gets back what the screen can spare. Says whether
    // the screen fits.
    function fitWindow(axis, limit) {
      const start = screen(axis);

      if (start <= limit) return true;

      const natural = shown.map(s => size(s, axis));
      const level = (L, most = limit) => {
        for (let i = 0; i < shown.length; i++) tryCap(shown[i], axis, natural[i] > L ? L : 0);

        return screen(axis) <= most;
      };
      // The biggest size from lo up that keeps the screen within "most".
      const biggest = (lo, most) => {
        let hi = Math.max.apply(null, natural);

        while (hi - lo > LEVEL_STEP) {
          const mid = Math.floor((lo + hi) / 2);

          if (level(mid, most)) lo = mid; else hi = mid;
        }

        return lo;
      };

      if (Math.max.apply(null, natural) <= MIN_SIDE) return false;

      if (!level(MIN_SIDE)) {
        // Even the smallest boxes leave it too big. The boxes keep the room the parts that can't be cut leave them (a
        // bag as tall as the panel beside it), so the game shrinks the screen as little as it can. Where that would
        // leave only slivers of the grids, the game shrinks them whole instead. A screen the game never shrinks stays
        // too big, and its boxes stay cut only if that made it smaller than with no cuts at all.
        const floor = screen(axis) + 1;

        level(biggest(MIN_SIDE, floor));
        giveBack(axis, floor);

        if (cfg.noZoom) {
          const cut = screen(axis);
          const recut = shown.map(s => tryCap(s, axis, 0));

          if (screen(axis) > cut + 1) recut.forEach(undo => undo());

          return false;
        }

        if (!shown.some(s => (axis === 'h' ? s.h : s.w) > 2 * MIN_SIDE)) level(Infinity);

        return false;
      }

      level(biggest(MIN_SIDE, limit));
      giveBack(axis, limit);

      return true;
    }

    // A cut box that did not need all of its cut (one beside a taller column) gets back what the screen can spare
    // within "most".
    function giveBack(axis, most) {
      for (let i = 0; i < shown.length; i++) {
        const s = shown[i];
        const cap = axis === 'h' ? s.h : s.w;

        if (!cap) continue;

        const undo = tryCap(s, axis, 0);
        const free = screen(axis);

        if (free <= most) continue;

        const target = Math.max(MIN_SIDE, Math.floor(size(s, axis) - (free - most)));

        if (target <= cap) { undo(); continue; }

        tryCap(s, axis, target);

        if (screen(axis) > most) undo();
      }
    }

    // Cuts the boxes until the screen, once the game has shrunk it, fits the window.
    function cutToFit(roomH, roomW) {
      fitWindow('h', roomH / widthZoom(measure().w, roomW));

      // Measured after the height cut, since a box cut shorter gains a scrollbar and grows a little wider.
      const k = gameZoom(measure(), roomH, roomW);

      fitWindow('w', roomW / k);
      // A box cut sideways gains a scrollbar's height, and parts placed from the grids' middles move with it.
      fitPanels();
      fitWindow('h', roomH / k);
    }

    // How far the game may shrink a screen this wide before its boxes are cut instead.
    function widthZoom(w, roomW) {
      return cfg.noZoom || !w ? 1 : Math.max(MIN_ZOOM, Math.min(1, roomW / w));
    }

    // How far the game will shrink the screen. One still too tall once its boxes are cut is shrunk for its height
    // anyway, and may be wider by as much.
    function gameZoom(size, roomH, roomW) {
      const k = widthZoom(size.w, roomW);

      return cfg.noZoom || !size.h ? k : Math.min(k, roomH / size.h);
    }

    // A box in a panel of fixed width (a cooking pot, the rat cage's food) spills out of the panel when its grid is
    // wider; it is cut to the panel's width and scrolls sideways instead.
    function fitPanels() {
      let cut = false;

      for (let i = 0; i < shown.length; i++) {
        const s = shown[i];

        if (s.w) continue;

        // The grid's own width; a box that is a plain block is only as wide as its parent and lets the grid spill.
        const need = Math.max(s.box.clientWidth, s.box.scrollWidth);
        let panel = s.box.parentElement;

        while (panel && panel !== doc.body && panel.clientWidth >= need) panel = panel.parentElement;

        if (!panel || panel === doc.body) continue;

        // A grid the game centres past a wrapper but still inside the panel around it (into the panel's padding) stays.
        const outer = panel.parentElement;

        if (outer && outer !== doc.body) {
          const o = nativeRect.call(outer), b = nativeRect.call(s.box);
          const right = b.left + need + b.width - s.box.clientWidth;

          if (b.left >= o.left - 0.5 && right <= o.right + 0.5) continue;
        }

        const cs = win.getComputedStyle(panel);
        const padL = (parseFloat(cs.paddingLeft) || 0) + panel.clientLeft;
        const padR = parseFloat(cs.paddingRight) || 0;
        let width = panel.clientWidth - (parseFloat(cs.paddingLeft) || 0) - padR;

        // Wrappers between the panel and the box may add their own padding; take off what still sticks out.
        for (let k = 0; k < 3 && width >= MIN_SIDE; k++) {
          tryCap(s, 'w', Math.floor(width));
          cut = true;

          const p = nativeRect.call(panel), b = nativeRect.call(s.box);
          const left = p.left + padL, right = left + panel.clientWidth - (parseFloat(cs.paddingLeft) || 0) - padR;
          const over = Math.max(0, b.right - right) + Math.max(0, left - b.left);

          if (over <= 0.5) break;

          width -= Math.ceil(over);
        }
      }

      return cut;
    }

    // A box cut only in height in a parent narrower than its grid and scrollbar (the basket's fixed-width panel) grows
    // past the parent, as the grid did before it scrolled, rather than scrolling sideways. A box cut in width keeps
    // its scrollbars.
    function settleScrollbars() {
      for (let i = 0; i < shown.length; i++) {
        const s = shown[i];

        if (!capped(s)) continue;

        if (!s.w && s.box.scrollWidth > s.box.clientWidth + 1) setMin(s, s.box.scrollWidth + s.box.offsetWidth - s.box.clientWidth);

        const y = !!s.h || s.box.scrollHeight > s.box.clientHeight + 1;
        const x = !!s.w || s.box.scrollWidth > s.box.clientWidth + 1;

        flags(s, y, x);
      }
    }

    // A tab that shows a material rack's rows in place of the grid sizes them to the grid's biggest tab; they take
    // the grid's cut instead, and scroll inside it.
    function capRacks() {
      if (!cfg.racks) return;

      for (const sel in cfg.racks) {
        const grid = doc.querySelector(sel), rack = doc.querySelector(cfg.racks[sel]);

        if (!rack) continue;

        const s = grid && stateOf(grid);
        const h = s && s.h ? s.h + 'px' : '', w = s && s.w ? s.w + 'px' : '';

        if (rack.style.maxHeight !== h) rack.style.maxHeight = h;
        if (rack.style.maxWidth !== w) rack.style.maxWidth = w;
      }
    }

    // How much smaller than its own size the game draws the screen.
    function shownZoom() {
      const zoom = zoomed();
      let k = 1;

      for (let i = 0; i < zoom.length; i++) k *= parseFloat(zoom[i].style.zoom) || 1;

      return k;
    }

    function zoomed() {
      const out = [];
      const app = doc.getElementById('app');

      if (doc.body.style.zoom) out.push(doc.body);
      if (app && app.style.zoom) out.push(app);

      return out;
    }

    function key() {
      const r = rootSize();
      let k = win.innerWidth + 'x' + win.innerHeight + ':' + r.w + 'x' + r.h;

      for (let i = 0; i < states.length; i++) k += '|' + states[i].grid.offsetWidth + 'x' + states[i].grid.offsetHeight;

      if (cfg.sized) {
        const parts = doc.querySelectorAll(cfg.sized);

        for (let i = 0; i < parts.length; i++) k += '/' + parts[i].offsetWidth + 'x' + parts[i].offsetHeight;
      }

      return k;
    }

    // A part named by "lists" (the shop's list) that reaches lower than the room is cut to the room, unless what it
    // holds would then spill out of it. Its margin is left out: follow() works that out again from the cut grids.
    function fitLists(limitH) {
      if (!cfg.lists) return;

      const parts = doc.querySelectorAll(cfg.lists);
      const root = doc.querySelector(cfg.root);

      for (let i = 0; i < parts.length; i++) uncut(parts[i]);

      if (!root) return;

      const top = nativeRect.call(root).top;

      for (let i = 0; i < parts.length; i++) {
        const part = parts[i];
        const q = nativeRect.call(part);

        if (!q.height) continue;

        const margin = parseFloat(win.getComputedStyle(part).marginTop) || 0;
        const over = Math.ceil((q.bottom - top) / scaleOf(part) - margin - limitH);

        if (over <= 0) continue;

        // Set the height first. The attribute without a height would drop the cap the game sets.
        setVar(part, '--slpr-lh', part.offsetHeight - over);
        part.setAttribute('data-slpr-cut', '');

        if (part.scrollHeight > part.clientHeight + 1) uncut(part);
      }
    }

    function uncut(part) {
      if (part.hasAttribute('data-slpr-cut')) part.removeAttribute('data-slpr-cut');

      setVar(part, '--slpr-lh', null);
    }

    // A line of hints is centred under its panel. Under a narrow panel near the window's edge it can run past that
    // edge, so it moves sideways just far enough to come back inside.
    function placeHints() {
      if (!cfg.hint || off) return;

      const lines = doc.querySelectorAll(cfg.hint);

      for (let i = 0; i < lines.length; i++) {
        const line = lines[i];
        const q = nativeRect.call(line);

        if (!q.width) continue;

        const k = scaleOf(line);
        const moved = (parseFloat(line.style.getPropertyValue('--slpr-hx')) || 0) * k;
        const lo = HINT_EDGE - (q.left - moved), hi = win.innerWidth - HINT_EDGE - (q.right - moved);
        const dx = lo > hi ? (lo + hi) / 2 : Math.min(Math.max(0, lo), hi);

        setVar(line, '--slpr-hx', Math.abs(dx) < 0.5 ? null : Math.round(dx / k));
      }
    }

    // "pass" counts the fits run again to make room for the strip of things nearby. A centred screen with room to spare
    // only rises half as far as its band grows, so it can take a few.
    function fit(pass = 0) {
      if (fitting || off) return;

      const iw = win.innerWidth, ih = win.innerHeight;
      let bandTooBig = false;

      if (!iw || !ih || !states.length) return;

      if ((band || noBand) && bandWin !== iw + 'x' + ih) {
        band = 0;
        noBand = false;
      }

      fitting = true;

      try {
        const zoom = zoomed();

        for (let i = 0; i < zoom.length; i++) zoom[i].style.zoom = '';

        if (zoom.indexOf(doc.body) >= 0) {
          doc.body.style.minHeight = '';
          doc.body.style.height = '';
        }

        if (!rootSize().h) {
          if (zoom.length) win.dispatchEvent(new win.Event('resize'));

          return;
        }

        // A box that fills from the bottom keeps its place counted from the bottom, so it opens there.
        const scrolls = states.map(s => [s.bottom ? fromBottom(s.box) : s.box.scrollTop, s.box.scrollLeft]);

        for (let i = 0; i < states.length; i++) {
          const s = states[i];

          s.hidden = !s.grid.offsetWidth && !s.grid.offsetHeight;

          if (s.hidden) {
            if (s.box.style.minHeight) s.box.style.minHeight = '';

            unhug(s);
            continue;
          }

          uncap(s);
        }

        shown = states.filter(s => !s.hidden);
        capRacks();
        setOwn(false);

        const full = rootSize();
        const roomW = iw - (cfg.noZoom ? 2 * SIDE_EDGE : cfg.side || cfg.margin);

        room = ih - cfg.margin;
        setOwn(full.h > room || full.w > roomW);
        fitLists(room);
        cutToFit(room, roomW);
        settleScrollbars();
        hugGrids();
        capRacks();
        follow();
        liftAbove();

        // The game worked out its own shrink before the boxes were capped; ask it again. The strip of things nearby
        // finds its place again on the same event.
        if (zoom.length || stripShown()) win.dispatchEvent(new win.Event('resize'));

        if (band) bandTooBig = shownZoom() < zoomWithout - 0.005 || !bandFits();
        else zoomWithout = shownZoom();

        placeHints();

        for (let i = 0; i < states.length; i++) {
          const box = states[i].box;

          box.scrollTop = states[i].bottom ? box.scrollHeight - box.clientHeight - scrolls[i][0] : scrolls[i][0];
          box.scrollLeft = scrolls[i][1];
          refresh(states[i]);
        }

        lastKey = key();
        lastSig = sig();
      } finally {
        fitting = false;
      }

      if (bandTooBig) {
        band = 0;
        noBand = true;
        fit();
        return;
      }

      const short = pass < 3 ? stripShort() : 0;

      if (short) {
        bandWin = iw + 'x' + ih;
        band += short;
        fit(pass + 1);
      }
    }

    function fromBottom(box) { return box.scrollHeight - box.clientHeight - box.scrollTop; }

    function fitSoon() {
      if (fitRaf || fitting || off) return;

      fitRaf = win.requestAnimationFrame(() => {
        fitRaf = 0;

        if (key() !== lastKey) fit();
      });
    }

    // What decides the caps and is cheap to read without layout: which grids there are, their inline size styles
    // and the window size. Checked on every change to the screen's elements.
    function sig() {
      let out = win.innerWidth + 'x' + win.innerHeight;
      const grids = findGrids();

      for (let i = 0; i < grids.length; i++) {
        const g = grids[i];

        if (!g.__slprId) g.__slprId = ++sigIds;

        out += '|' + g.__slprId + ':' + g.style.cssText;
      }

      if (cfg.watch) {
        const watched = doc.querySelector(cfg.watch);

        out += '|' + (watched ? watched.style.height : '');
      }

      return out + (stripShown() ? '|strip' : '');
    }

    let sigIds = 0;

    function onMutate() {
      if (fitting || off) return;

      const now = sig();

      if (now === lastSig) return;

      lastSig = now;
      scan();
      fit();
    }

    // ----- Grid rectangles while capped -----

    // The game finds the cell under the cursor from the grid's rectangle, and decides the cursor is over the grid when
    // it is inside it. A capped grid answers with its whole rectangle for the cell sums, cut to the part that shows
    // for the hit test: right and bottom stop at the box's edges, and while a button is held, top and left move to
    // the box's edges whenever the cursor is above or left of them (the part scrolled out of sight).
    function Rect(s) { this.s = s; }

    Rect.prototype.update = function () {
      const s = this.s;
      const g = nativeRect.call(s.grid), b = nativeRect.call(s.box);
      const k = s.box.offsetWidth ? b.width / s.box.offsetWidth : 1;

      this.gl = g.left;
      this.gt = g.top;
      this.gw = g.width;
      this.gh = g.height;
      this.vl = b.left + s.box.clientLeft * k;
      this.vt = b.top + s.box.clientTop * k;
      this.vr = this.vl + s.box.clientWidth * k;
      this.vb = this.vt + s.box.clientHeight * k;

      return this;
    };

    Object.defineProperties(Rect.prototype, {
      left: { get() { return ptr.held && ptr.x < this.vl ? this.vl : this.gl; } },
      top: { get() { return ptr.held && ptr.y < this.vt ? this.vt : this.gt; } },
      right: { get() { return Math.min(this.gl + this.gw, this.vr); } },
      bottom: { get() { return Math.min(this.gt + this.gh, this.vb); } },
      width: { get() { return this.gw; } },
      height: { get() { return this.gh; } },
      x: { get() { return this.left; } },
      y: { get() { return this.top; } },
    });

    Rect.prototype.toJSON = function () {
      return { left: this.left, top: this.top, right: this.right, bottom: this.bottom, width: this.width, height: this.height, x: this.x, y: this.y };
    };

    function liveRect(s) {
      const r = new Rect(s).update();

      s.rects.push(r);

      if (s.rects.length > RING) s.rects.shift();

      return r;
    }

    function refresh(s) {
      for (let i = 0; i < s.rects.length; i++) s.rects[i].update();
    }

    win.Element.prototype.getBoundingClientRect = function () {
      const box = this.parentElement;

      if (box && box.hasAttribute && box.hasAttribute('data-slpr')) {
        const s = stateOf(this);

        if (s && s.box === box && capped(s)) return liveRect(s);
      }

      return nativeRect.call(this);
    };

    function onBoxScroll(s) {
      refresh(s);

      if (ptr.dragging) poke();
    }

    // ----- Dragging near a box's edge scrolls it -----

    function visible(s) {
      const b = nativeRect.call(s.box);
      const k = s.box.offsetWidth ? b.width / s.box.offsetWidth : 1;
      const l = b.left + s.box.clientLeft * k, t = b.top + s.box.clientTop * k;

      return { l, t, r: l + s.box.clientWidth * k, b: t + s.box.clientHeight * k };
    }

    function stateAt(x, y) {
      for (let i = 0; i < states.length; i++) {
        if (!capped(states[i])) continue;

        const v = visible(states[i]);

        if (x >= v.l && x <= v.r && y >= v.t && y <= v.b) return states[i];
      }

      return null;
    }

    function step(depth) {
      return Math.max(1, Math.round(EDGE_SPEED * Math.min(1, depth / (EDGE_IN + EDGE_OUT))));
    }

    function edgeScroll(s) {
      const v = visible(s);
      const box = s.box;
      let moved = false;

      if (ptr.x >= v.l && ptr.x <= v.r) {
        let d = 0;

        if (ptr.y < v.t + EDGE_IN && ptr.y > v.t - EDGE_OUT) d = -step(v.t + EDGE_IN - ptr.y);
        else if (ptr.y > v.b - EDGE_IN && ptr.y < v.b + EDGE_OUT) d = step(ptr.y - (v.b - EDGE_IN));

        if (d) {
          const before = box.scrollTop;
          box.scrollTop = before + d;
          moved = moved || box.scrollTop !== before;
        }
      }

      if (ptr.y >= v.t && ptr.y <= v.b) {
        let d = 0;

        if (ptr.x < v.l + EDGE_IN && ptr.x > v.l - EDGE_OUT) d = -step(v.l + EDGE_IN - ptr.x);
        else if (ptr.x > v.r - EDGE_IN && ptr.x < v.r + EDGE_OUT) d = step(ptr.x - (v.r - EDGE_IN));

        if (d) {
          const before = box.scrollLeft;
          box.scrollLeft = before + d;
          moved = moved || box.scrollLeft !== before;
        }
      }

      return moved;
    }

    function autoTick() {
      autoRaf = 0;

      if (!ptr.dragging) return;

      for (let i = 0; i < states.length; i++) if (capped(states[i])) edgeScroll(states[i]);

      autoRaf = win.requestAnimationFrame(autoTick);
    }

    // The game updates its drop preview on mouse moves; after a scroll the cursor sits over another cell.
    function poke() {
      if (pokeRaf) return;

      pokeRaf = win.requestAnimationFrame(() => {
        pokeRaf = 0;

        if (!ptr.dragging) return;

        const target = doc.elementFromPoint(ptr.x, ptr.y) || doc.body;
        target.dispatchEvent(new win.MouseEvent('mousemove', { bubbles: true, cancelable: true, view: win, clientX: ptr.x, clientY: ptr.y, buttons: 1 }));
      });
    }

    win.addEventListener('mousedown', e => {
      ptr.x = e.clientX;
      ptr.y = e.clientY;

      if (e.button !== 0) return;

      ptr.held = true;
      ptr.dragging = false;
      ptr.sx = e.clientX;
      ptr.sy = e.clientY;
      // A press on a box itself is on its scrollbar.
      ptr.onBox = states.some(s => s.box === e.target);
    }, true);

    win.addEventListener('mousemove', e => {
      if (!e.isTrusted) return;

      ptr.x = e.clientX;
      ptr.y = e.clientY;

      if (!ptr.held || ptr.dragging || ptr.onBox || !(e.buttons & 1)) return;

      if (Math.abs(e.clientX - ptr.sx) + Math.abs(e.clientY - ptr.sy) < DRAG_START) return;

      ptr.dragging = true;

      if (!autoRaf && states.some(capped)) autoRaf = win.requestAnimationFrame(autoTick);
    }, true);

    win.addEventListener('mouseup', e => {
      ptr.x = e.clientX;
      ptr.y = e.clientY;

      if (e.button !== 0) return;

      ptr.dragging = false;
      // The game reads the grid's rectangle while it handles this same release.
      win.setTimeout(() => { if (!ptr.dragging) ptr.held = false; }, 0);
    }, true);

    // The wheel scrolls the box under the cursor while an item is dragged over it.
    win.addEventListener('wheel', e => {
      if (!ptr.held) return;

      const s = stateAt(e.clientX, e.clientY);

      if (!s || s.box.contains(e.target)) return;

      const k = e.deltaMode === 1 ? 40 : e.deltaMode === 2 ? s.box.clientHeight : 1;
      s.box.scrollTop += e.deltaY * k;
      s.box.scrollLeft += e.deltaX * k;
      e.preventDefault();
    }, { capture: true, passive: false });

    // ----- Watching the screen -----

    const ro = new win.ResizeObserver(() => fitSoon());
    const mo = new win.MutationObserver(onMutate);
    // A line of hints that changes size only moves.
    const hintRo = new win.ResizeObserver(() => { if (!fitting) placeHints(); });

    mo.observe(doc.body, { childList: true, subtree: true });

    if (cfg.watch) {
      const watched = doc.querySelector(cfg.watch);

      if (watched) mo.observe(watched, { attributes: true, attributeFilter: ['style'] });
    }

    const roots = doc.querySelectorAll(cfg.root);

    for (let i = 0; i < roots.length; i++) ro.observe(roots[i]);

    win.addEventListener('resize', () => { if (!fitting) { lastSig = ''; onMutate(); } });

    // ----- The ScrollBars setting -----

    function stop() {
      if (off) return;

      off = true;
      ptr.dragging = false;

      for (let i = states.length - 1; i >= 0; i--) release(states[i]);

      states.length = 0;
      shown = states;
      capRacks();
      setOwn(false);

      // The hints and the screen go back to where the game puts them.
      const lines = cfg.hint ? doc.querySelectorAll(cfg.hint) : [];

      for (let i = 0; i < lines.length; i++) setVar(lines[i], '--slpr-hx', null);

      const roots = doc.querySelectorAll(cfg.root);

      band = 0;
      noBand = false;

      for (let i = 0; i < roots.length; i++) lift(roots[i], 0);

      const lists = cfg.lists ? doc.querySelectorAll(cfg.lists) : [];

      for (let i = 0; i < lists.length; i++) uncut(lists[i]);

      style.disabled = true;
      lastKey = '';
      lastSig = '';
      // The game works out its own shrink again, now for the grids at their full size.
      win.dispatchEvent(new win.Event('resize'));
    }

    function resume() {
      if (!off) return;

      off = false;
      style.disabled = false;
      lastKey = '';
      lastSig = '';
      scan();
      fit();
    }

    function reopen() {
      if (!band && !noBand || off) return;

      band = 0;
      noBand = false;
      lastKey = '';
      fit();
    }

    scan();
    fit();

    return { doc, stop, resume, reopen };
  }
})();
