// Scrolling grids on the game's inventory screens. A grid too big for the window gets a scrollbar instead of the game
// shrinking the whole screen until it fits. Runs in Root.html, where every screen is a frame of the same origin; a
// frame is set up just before it is shown (Root's notifyPageReady), so it never shows unfitted.
(function () {
  if (typeof document.getElementsByTagName !== 'function' || window.__slprBags) return;
  window.__slprBags = true;

  // Some screens place their parts from the grid's rows rather than from what shows. While a screen is cut to fit
  // the window, the page carries data-slpr-own and its "css" turns those placements off, and its "follow" places
  // the parts from the cut grids instead. Outside that, the game's own layout stands untouched.
  function setVar(el, name, px) {
    const v = px == null ? '' : px + 'px';

    if (el.style.getPropertyValue(name) === v) return;

    if (v) el.style.setProperty(name, v); else el.style.removeProperty(name);
  }

  // The shop gives the bag's slot the size of the bag's rows and columns, and moves the shop list or the bag down so
  // the list's middle lines up with the grid's middle.
  function followShop(doc, own) {
    if (!own) return;

    const slot = doc.querySelector('.left-slot');
    const column = slot && slot.querySelector('.left-column');

    if (!column || !column.offsetHeight) return;

    const box = column.querySelector('.grid-wrapper');

    setVar(slot, '--slpr-h', column.offsetHeight);
    setVar(slot, '--slpr-w', box && box.style.maxWidth ? column.offsetWidth : null);

    const right = doc.querySelector('.right-panel');
    const list = right && right.querySelector('.shop-list');

    if (!list || !box) return;

    const c = column.getBoundingClientRect(), b = box.getBoundingClientRect();
    const r = right.getBoundingClientRect(), l = list.getBoundingClientRect();
    const delta = Math.round((l.top + l.height / 2 - r.top) - (b.top + b.height / 2 - c.top));

    setVar(slot, '--slpr-mt', Math.max(0, delta));
    setVar(right, '--slpr-mt', Math.max(0, -delta));
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
        setVar(slot, '--slpr-w', box && box.style.maxWidth ? panel.offsetWidth : null);
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

  // Each screen: the element(s) the game fits to the window, its margin, and the grids that may scroll. "boxes" names
  // boxes the screen already wraps its grid in; "follow" places parts the game places from the grid's rows (see
  // above); "watch" is an element whose height the game sets; "css" is added to the screen's styles.
  const PAGES = {
    AidDepot: { root: '.hub-root', margin: 48, grids: '#bagGrid,#recvGrid' },
    BrewPanel: { root: '.brew-system', margin: 48, grids: '#bagGrid,#vatGrid' },
    Compressor: { root: '#app', margin: 24, grids: '#cartGrid,#binGrid,#trayGrid' },
    Cooking: { root: '.cooking-system', margin: 48, grids: '#backpackGrid,#mainPotGrid', css: OWN_COOK },
    DailyFert: { root: '.df-root', margin: 48, grids: '#bagGrid' },
    DroneHub: { root: '.hub-root', margin: 48, grids: '#bagGrid,#storeGrid', racks: { '#bagGrid': '#bagRack' } },
    DyePanel: { root: '.dye-system', margin: 48, grids: '#bagGrid,#vatGrid' },
    FuelGenerator: { root: '.generator-system', margin: 48, grids: '#backpackGrid' },
    GreenhouseBuild: { root: '#ghRoot', margin: 40, grids: '#cartGrid,#pickGrid', racks: { '#cartGrid': '#cartRack' } },
    Heater: { root: '.heater-system', margin: 48, grids: '#backpackGrid' },
    MaterialRack: { root: '.rack-system', margin: 24, grids: '#bagGrid' },
    RatCage: { root: '.generator-system', margin: 48, grids: '#backpackGrid,#foodGrid' },
    Shredder: { root: '#app', margin: 24, grids: '#cartGrid,#hopperGrid,#trayGrid', racks: { '#cartGrid': '#cartRack' } },
    ToolTable: { root: '.ui-container', margin: 24, grids: '#backpack-grid,#workbench-grid', follow: followToolPanels, css: OWN_TOOLS, racks: { '#backpack-grid': '#rack-rows' } },
    TrapPanel: { root: '#bagCol,.popup-container,#storeCol', margin: 48, grids: '#backpackGrid,#storeGrid' },
    TradeUI: { root: '.trade', margin: 114, grids: '#tradeBagGrid,.col.mid>.grid-container,.col.mid>[data-slpr]>.grid-container', racks: { '#tradeBagGrid': '.rack-view' } },
    // The basket's panel is as wide as the game's five-column basket; a wider basket widens it.
    BackpackUI: { root: '.backpack-container', margin: 24, boxes: '.grid-wrapper', follow: followBags, css: OWN_BAGS + '.bag-panel.is-basket-panel .panel{min-width:min-content}' },
    ShopUI: { root: '.shop-container', margin: 24, boxes: '.grid-wrapper', follow: followShop, watch: '.left-slot', css: OWN_SHOP },
  };

  const MIN_SIDE = 120;
  const EDGE_IN = 12;
  const EDGE_OUT = 28;
  const EDGE_SPEED = 16;
  const DRAG_START = 6;
  const LEVEL_STEP = 4;
  const RING = 6;

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
    + '[data-slpr~=cap]::-webkit-scrollbar-corner{background:transparent}';

  function pageOf(doc) {
    const m = /\/([^/]+)\.html$/i.exec(doc.location.pathname || '');

    return m && Object.prototype.hasOwnProperty.call(PAGES, m[1]) ? PAGES[m[1]] : null;
  }

  function prepare(frame) {
    if (!frame || frame.tagName !== 'IFRAME') return;

    let doc;
    try { doc = frame.contentDocument; } catch (e) { return; }

    if (!doc || doc.__slprBags || !doc.body || !doc.defaultView) return;

    const cfg = pageOf(doc);

    if (!cfg) return;

    doc.__slprBags = true;

    try { setup(doc, cfg); } catch (e) { console.warn('[PackRat] bag scroll: ' + e); }
  }

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

  // ===== One screen =====

  function setup(doc, cfg) {
    const win = doc.defaultView;
    const nativeRect = win.Element.prototype.getBoundingClientRect;
    const states = [];
    // The boxes whose grids show; a grid the game hid for another tab (a material rack's rows) keeps its cut.
    let shown = states;
    const ptr = { x: 0, y: 0, held: false, dragging: false, sx: 0, sy: 0, onBox: false };
    let fitting = false;
    let own = false;
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

    style.textContent = CSS + wide + (cfg.css || '');
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

    function scan() {
      for (let i = states.length - 1; i >= 0; i--) {
        const s = states[i];

        if (s.grid.isConnected && s.grid.parentElement === s.box) continue;

        states.splice(i, 1);
        ro.unobserve(s.box);
        ro.unobserve(s.grid);
        setCap(s, 0, 0);
        setMin(s, 0);

        if (cfg.boxes) s.box.removeAttribute('data-slpr');
        else if (!s.box.firstElementChild) s.box.remove();
      }

      const grids = findGrids();

      for (let i = 0; i < grids.length; i++) {
        const grid = grids[i];

        if (stateOf(grid)) continue;

        const box = wrap(grid);

        if (!box) continue;

        if (cfg.boxes && !box.hasAttribute('data-slpr')) box.setAttribute('data-slpr', 'box');

        const s = { grid, box, h: 0, w: 0, mw: 0, rects: [] };
        states.push(s);
        box.addEventListener('scroll', () => onBoxScroll(s), { passive: true });
        ro.observe(box);
        ro.observe(grid);
        mo.observe(grid, { attributes: true, attributeFilter: ['style'] });
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

    function rootSize() {
      const els = doc.querySelectorAll(cfg.root);

      if (els.length === 1) return { w: els[0].offsetWidth, h: els[0].offsetHeight };

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
      if (cfg.follow) cfg.follow(doc, own);
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

      return axis === 'h' ? r.h : r.w;
    }

    function tryCap(s, axis, value) {
      const oldH = s.h, oldW = s.w;

      if (axis === 'h') setCap(s, value, s.w); else setCap(s, s.h, value);

      return () => setCap(s, oldH, oldW);
    }

    // The screen too tall or wide for the window: every box bigger than some size is cut to that size, the biggest
    // size that lets the screen fit, so two big grids share the room rather than one being cut to a sliver. A box
    // that did not need its cut (one beside a taller column) then gets back what the screen can spare.
    function fitWindow(axis, limit) {
      const start = screen(axis);

      if (start <= limit) return;

      const natural = shown.map(s => size(s, axis));
      const level = L => {
        for (let i = 0; i < shown.length; i++) tryCap(shown[i], axis, natural[i] > L ? L : 0);

        return screen(axis) <= limit;
      };
      let lo = MIN_SIDE, hi = Math.max.apply(null, natural);

      if (hi <= lo) return;

      if (!level(lo)) {
        // Even the smallest boxes leave it too big; keep them if that helped, and the game shrinks the rest.
        if (screen(axis) >= start - 1) level(Infinity);

        return;
      }

      while (hi - lo > LEVEL_STEP) {
        const mid = Math.floor((lo + hi) / 2);

        if (level(mid)) lo = mid; else hi = mid;
      }

      level(lo);

      for (let i = 0; i < shown.length; i++) {
        const s = shown[i];
        const cap = axis === 'h' ? s.h : s.w;

        if (!cap) continue;

        const undo = tryCap(s, axis, 0);
        const free = screen(axis);

        if (free <= limit) continue;

        const target = Math.max(MIN_SIDE, Math.floor(size(s, axis) - (free - limit)));

        if (target <= cap) { undo(); continue; }

        tryCap(s, axis, target);

        if (screen(axis) > limit) undo();
      }
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

      return k;
    }

    function fit() {
      if (fitting) return;

      const iw = win.innerWidth, ih = win.innerHeight;

      if (!iw || !ih || !states.length) return;

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

        const scrolls = states.map(s => [s.box.scrollTop, s.box.scrollLeft]);

        for (let i = 0; i < states.length; i++) {
          const s = states[i];

          s.hidden = !s.grid.offsetWidth && !s.grid.offsetHeight;

          if (s.hidden) {
            if (s.box.style.minHeight) s.box.style.minHeight = '';

            continue;
          }

          setCap(s, 0, 0);
          setMin(s, 0);
        }

        shown = states.filter(s => !s.hidden);
        capRacks();
        setOwn(false);

        const full = rootSize();

        setOwn(full.h > ih - cfg.margin || full.w > iw - cfg.margin);
        fitWindow('h', ih - cfg.margin);
        fitWindow('w', iw - cfg.margin);

        // A box cut sideways gains a scrollbar's height, and parts placed from the grids' middles move with it.
        fitPanels();
        fitWindow('h', ih - cfg.margin);

        settleScrollbars();
        capRacks();
        follow();

        for (let i = 0; i < states.length; i++) {
          states[i].box.scrollTop = scrolls[i][0];
          states[i].box.scrollLeft = scrolls[i][1];
          refresh(states[i]);
        }

        // The game worked out its own shrink before the boxes were capped; ask it again.
        if (zoom.length) win.dispatchEvent(new win.Event('resize'));

        lastKey = key();
        lastSig = sig();
      } finally {
        fitting = false;
      }
    }

    function fitSoon() {
      if (fitRaf || fitting) return;

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

      return out;
    }

    let sigIds = 0;

    function onMutate() {
      if (fitting) return;

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

    mo.observe(doc.body, { childList: true, subtree: true });

    if (cfg.watch) {
      const watched = doc.querySelector(cfg.watch);

      if (watched) mo.observe(watched, { attributes: true, attributeFilter: ['style'] });
    }

    const roots = doc.querySelectorAll(cfg.root);

    for (let i = 0; i < roots.length; i++) ro.observe(roots[i]);

    win.addEventListener('resize', () => { if (!fitting) { lastSig = ''; onMutate(); } });

    scan();
    fit();
  }
})();
