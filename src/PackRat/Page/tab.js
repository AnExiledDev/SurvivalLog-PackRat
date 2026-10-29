// Pack Rat's Inventories and Items tabs in the Mod Manager. Inventories saves to PackRat.Inventories.json, Items to
// PackRat.Items.json; both apply the next time a save loads. Runs in Root.html and only once: the plugin sends it
// again whenever the web view lost it. Talks to the plugin through vuplex.postMessage("slmod|packrat|...") and is
// answered through window.__slpackrat. The names and icons of the game's inventories and items come once per page
// as window.__slpackratData; the game's live sizes and weights come with every load.
(function () {
  if (window.__slpackrat) return;

  const PREFIX = 'slmod|packrat|';
  const GUID = 'com.dev.survivallog.packrat';
  const STYLE_ID = 'slpackrat-css';
  const SAVE_MS = 500;
  const TOAST_MS = 2600;
  const RESET_MS = 3000;
  const SHOWN_STEP = 120;
  const FILES = { grids: 'PackRat.Inventories.json', items: 'PackRat.Items.json' };

  // The game gives its item categories numbers only; these names are ours.
  const CATEGORIES = {
    1: ['Food', '食物'], 2: ['Medicine', '药品'], 3: ['Books', '书籍'], 4: ['Game consoles', '游戏机'], 5: ['Recreation', '娱乐用品'],
    6: ['Tools', '工具'], 8: ['Supply packs', '物资包'], 9: ['Materials', '材料'], 10: ['Seeds', '种子'], 11: ['Fertilizer', '肥料'],
    12: ['Traps', '陷阱'], 13: ['Fuel and alcohol', '燃料与酒精'], 14: ['Furniture packages', '家具包裹'], 15: ['Flowers', '花卉'],
    16: ['Throwables and bait', '投掷物与诱饵'], 17: ['Robot modules', '机器人模块'], 18: ['Notes and clues', '笔记与线索'],
    19: ['Crafts and ornaments', '手工与摆件'], 20: ['Records', '唱片'], 21: ['Renovation', '装修'], 22: ['Keys and quest items', '钥匙与任务物品'],
  };

  const SIZE_CHOICES = [[1, 1], [1, 2], [2, 1], [2, 2], [2, 3], [3, 2], [3, 3], [4, 3]];
  const GROUPS = { bag: ['Bags', '背包'], vehicle: ['Vehicles', '载具'], storage: ['Storage', '储物家具'] };

  const LOADS = {
    haul: { en: 'Scavenging run', zh: '一次普通搜刮', picks: [[1, 10], [9, 8], [2, 3], [3, 2], [6, 2], [19, 2], [13, 1], [14, 2]] },
    food: { en: 'Food run', zh: '一次食物采购', picks: [[1, 30]] },
    build: { en: 'Furniture', zh: '家具包裹', picks: [[14, 8], [9, 10]] },
    none: { en: 'Empty', zh: '空', picks: [] },
  };

  const RES = (function () {
    const at = location.href.indexOf('/WebUI/');

    return at < 0 ? 'Res/' : location.href.slice(0, at + 7) + 'Res/';
  })();

  const isObj = v => !!v && typeof v === 'object' && !Array.isArray(v);
  const isNum = v => typeof v === 'number' && isFinite(v);

  // ===== State =====

  const st = {
    lang: 'en',
    loaded: false,
    asked: false,
    carry: 2,
    itemWeight: 1,
    grids: freshGrids(),
    items: freshItems(),
    tabs: { grids: { root: null, ctx: null }, items: { root: null, ctx: null } },
    shown: null,
    timers: {},
    status: { grids: null, items: null },
    resetArmed: null,
  };

  const ui = { entry: 'bag:Backpack', level: 0, gq: '', ggrp: 'all', load: 'haul', cat: 1, sub: null, iq: '', shown: SHOWN_STEP, item: null };

  let ENTRIES = [];
  let ITEMS = [];
  let SUBS = {};
  const bagById = new Map();
  const itemById = new Map();

  function freshGrids() {
    return { all: 1, bag: 1, vehicle: 1, storage: 1, rows: {} };
  }

  function freshItems() {
    return { allOne: false, cats: {}, subs: {}, ids: {} };
  }

  const T = (en, zh) => st.lang === 'zh' ? zh : en;
  const nameOf = n => n ? (st.lang === 'zh' ? n.zh : n.en) : '';
  const itemName = it => st.lang === 'zh' ? it.zh : it.en;
  const kg = grams => (grams / 1000).toFixed(grams < 10000 ? 1 : 0);
  const limitKg = limit => limit > 0 ? (limit / 1000).toFixed(1) + ' kg' : T('no limit', '无上限');
  const img = file => file ? '<img src="' + esc(RES + file) + '" loading="lazy" alt="">' : glyph();

  function esc(s) {
    return String(s == null ? '' : s).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]);
  }

  function glyph() {
    return '<svg class="pr-glyph" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M7 7V5a5 5 0 0 1 10 0v2"/><rect x="4" y="7" width="16" height="15" rx="3"/><path d="M8 13h8"/></svg>';
  }

  function injectStyle() {
    if (document.getElementById(STYLE_ID) || typeof window.__slpackratCss !== 'string') return;

    const style = document.createElement('style');
    style.id = STYLE_ID;
    style.textContent = window.__slpackratCss;
    document.head.appendChild(style);
  }

  // ===== Game data =====

  // The names and icons, with the game's live sizes and limits on top. Rows of the same player bag (backpack levels
  // 1-4) are one entry with levels.
  function build(live) {
    const D = window.__slpackratData;
    const bags = isObj(live.bags) ? live.bags : {};
    const items = isObj(live.items) ? live.items : {};

    bagById.clear();
    itemById.clear();
    SUBS = isObj(D.subs) ? D.subs : {};

    const byName = new Map();
    ENTRIES = [];

    for (const src of D.bags) {
      const now = bags[src.id];
      const b = Object.assign({}, src);

      if (Array.isArray(now) && now.length >= 3) {
        b.w = now[0];
        b.h = now[1];
        b.burden = now[2];
      }

      if (!(b.w > 0 && b.h > 0)) continue;

      bagById.set(b.id, b);

      const key = b.grp === 'bag' ? 'bag:' + b.name.en : 'row:' + b.id;

      if (!byName.has(key)) {
        const entry = { key, grp: b.grp, name: b.name, icon: b.icon, rows: [] };
        byName.set(key, entry);
        ENTRIES.push(entry);
      }

      byName.get(key).rows.push(b);
    }

    const order = { bag: 0, vehicle: 1, storage: 2 };
    ENTRIES.sort((a, b) => order[a.grp] - order[b.grp]);

    ITEMS = D.items.map(a => {
      const now = items[a[0]];
      const it = { id: a[0], en: a[1], zh: a[2], w: a[3], h: a[4], g: a[5], cat: a[6], sub: a[7], icon: a[8], stack: a[9] };

      if (Array.isArray(now) && now.length >= 3) {
        it.w = now[0];
        it.h = now[1];
        it.g = now[2];
      }

      return it;
    }).filter(it => it.w > 0 && it.h > 0);

    for (const it of ITEMS) itemById.set(it.id, it);
  }

  // ===== The rules, the same as the plugin's =====

  function gridScale(grp) {
    const g = st.grids;

    return g[grp] !== 1 ? g[grp] : g.all;
  }

  const scaledCells = (cells, scale) => Math.max(1, Math.round(cells * scale));

  function scaledLimit(limit) {
    return limit > 0 ? Math.max(1, Math.round(limit * st.carry)) : limit;
  }

  function gridOf(id) {
    const b = bagById.get(id);
    const own = st.grids.rows[id] || {};
    const scale = gridScale(b.grp);
    const w = own.w || scaledCells(b.w, scale);
    const h = own.h || scaledCells(b.h, scale);
    const limit = own.kg != null ? Math.round(own.kg * 1000) : scaledLimit(b.burden);

    return { w, h, limit, gameW: b.w, gameH: b.h, gameLimit: b.burden, hasOverride: !!(own.w || own.h || own.kg != null) };
  }

  function setRow(id, patch) {
    const b = bagById.get(id);
    const scale = gridScale(b.grp);
    const cur = Object.assign({}, st.grids.rows[id], patch);

    if (cur.w === scaledCells(b.w, scale)) delete cur.w;
    if (cur.h === scaledCells(b.h, scale)) delete cur.h;
    if (cur.kg == null) delete cur.kg;

    if (Object.keys(cur).length) st.grids.rows[id] = cur;
    else delete st.grids.rows[id];

    changed('grids');
  }

  function isEntryChanged(e) {
    return e.rows.some(r => { const g = gridOf(r.id); return g.w !== g.gameW || g.h !== g.gameH || g.hasOverride; });
  }

  // Item setting beats food group, which beats category, which beats "All items 1x1", which beats the game.
  function sizeOf(it) {
    const s = st.items;
    const own = s.ids[it.id];
    if (own && own.size) return { w: own.size[0], h: own.size[1], from: 'item' };

    const sub = it.cat === 1 ? s.subs[it.sub] : null;
    if (sub && sub.size) return { w: sub.size[0], h: sub.size[1], from: 'sub' };

    const cat = s.cats[it.cat];
    if (cat && cat.size) return { w: cat.size[0], h: cat.size[1], from: 'cat' };
    if (s.allOne) return { w: 1, h: 1, from: 'all' };

    return { w: it.w, h: it.h, from: 'game' };
  }

  function gramsOf(it) {
    const s = st.items;
    const own = s.ids[it.id];
    if (own && own.g != null) return { g: own.g, from: 'item' };
    if (it.g <= 0) return { g: it.g, from: 'game' };

    const sub = it.cat === 1 ? s.subs[it.sub] : null;
    const cat = s.cats[it.cat];
    const scale = sub && sub.x != null ? sub.x : cat && cat.x != null ? cat.x : st.itemWeight;
    const from = sub && sub.x != null ? 'sub' : cat && cat.x != null ? 'cat' : st.itemWeight !== 1 ? 'all' : 'game';

    return { g: Math.round(it.g * scale), from };
  }

  function setRule(kind, key, patch) {
    const bucket = st.items[kind];
    const cur = Object.assign({}, bucket[key], patch);

    for (const k of Object.keys(cur)) if (cur[k] == null) delete cur[k];

    if (Object.keys(cur).length) bucket[key] = cur;
    else delete bucket[key];

    changed('items');
  }

  function sizeCounts(list) {
    const counts = new Map();

    for (const it of list) {
      const s = sizeOf(it);
      const k = s.w + 'x' + s.h;
      counts.set(k, (counts.get(k) || 0) + 1);
    }

    return [...counts.entries()].sort((a, b) => b[1] - a[1]);
  }

  // A fixed pseudo-random pick, so the same load always holds the same items.
  function sampleLoad(name) {
    const out = [];
    let seed = 7;

    for (const [cat, n] of LOADS[name].picks) {
      const pool = ITEMS.filter(i => i.cat === cat && i.icon);

      for (let k = 0; k < n && pool.length; k++) {
        seed = (seed * 9301 + 49297) % 233280;
        out.push(pool[seed % pool.length]);
      }
    }

    return out;
  }

  // First fit, biggest items first. The game's own packing may lay things out differently.
  function pack(w, h, list) {
    const used = Array.from({ length: h }, () => new Array(w).fill(false));
    const placed = [];
    const left = [];
    const sorted = list.map(it => ({ it, s: sizeOf(it) })).sort((a, b) => b.s.w * b.s.h - a.s.w * a.s.h);

    for (const { it, s } of sorted) {
      const at = findSpot(used, w, h, s.w, s.h);
      if (!at) { left.push(it); continue; }

      for (let y = at.y; y < at.y + s.h; y++) for (let x = at.x; x < at.x + s.w; x++) used[y][x] = true;

      placed.push({ it, x: at.x, y: at.y, w: s.w, h: s.h });
    }

    return { placed, left };
  }

  function findSpot(used, w, h, iw, ih) {
    for (let y = 0; y + ih <= h; y++) {
      for (let x = 0; x + iw <= w; x++) {
        if (isFree(used, x, y, iw, ih)) return { x, y };
      }
    }

    return null;
  }

  function isFree(used, x, y, iw, ih) {
    for (let yy = y; yy < y + ih; yy++) for (let xx = x; xx < x + iw; xx++) if (used[yy][xx]) return false;

    return true;
  }

  // ===== Files =====

  function gridsFromFile(f) {
    const g = freshGrids();
    if (!isObj(f)) return g;

    const scale = isObj(f.scale) ? f.scale : {};
    for (const k of ['all', 'bag', 'vehicle', 'storage']) if (isNum(scale[k])) g[k] = scale[k];

    if (isObj(f.rows)) {
      for (const id of Object.keys(f.rows)) {
        const r = f.rows[id];
        if (!isObj(r)) continue;

        const row = {};
        if (isNum(r.w)) row.w = r.w;
        if (isNum(r.h)) row.h = r.h;
        if (isNum(r.kg)) row.kg = r.kg;
        if (Object.keys(row).length) g.rows[id] = row;
      }
    }

    return g;
  }

  function gridsToFile() {
    const g = st.grids;
    const scale = {};

    for (const k of ['all', 'bag', 'vehicle', 'storage']) if (g[k] !== 1) scale[k] = g[k];

    return { scale, rows: g.rows };
  }

  function itemsFromFile(f) {
    const s = freshItems();
    if (!isObj(f)) return s;

    s.allOne = f.allOne === true;

    for (const kind of ['cats', 'subs', 'ids']) {
      if (!isObj(f[kind])) continue;

      for (const key of Object.keys(f[kind])) {
        const r = f[kind][key];
        if (!isObj(r)) continue;

        const rule = {};
        if (Array.isArray(r.size) && r.size.length === 2 && isNum(r.size[0]) && isNum(r.size[1])) rule.size = [r.size[0], r.size[1]];
        if (kind !== 'ids' && isNum(r.x)) rule.x = r.x;
        if (kind === 'ids' && isNum(r.g)) rule.g = r.g;
        if (Object.keys(rule).length) s[kind][key] = rule;
      }
    }

    return s;
  }

  // ===== Talking to the plugin =====

  function send(msg) {
    try { window.vuplex.postMessage(PREFIX + msg); } catch (e) { /* the plugin is not listening; nothing to do */ }
  }

  function askForData() {
    if (st.asked) return;

    st.asked = true;
    const verb = window.__slpackratData ? 'load' : 'load|data';

    if (window.vuplex) send(verb);
    else window.addEventListener('vuplexready', () => send(verb), { once: true });
  }

  // Each change is saved a moment after the last one, so a held + button sends one save.
  function changed(kind) {
    st.status[kind] = { pending: true };
    clearTimeout(st.timers[kind]);
    st.timers[kind] = setTimeout(() => flush(kind), SAVE_MS);
  }

  function flush(kind) {
    clearTimeout(st.timers[kind]);
    st.timers[kind] = null;

    if (kind === 'grids') send('grids|save|' + JSON.stringify(gridsToFile()));
    else if (kind === 'items') send('items|save|' + JSON.stringify(st.items));
    else if (kind === 'iw') send('setting|ItemWeight|' + st.itemWeight);
  }

  function flushAll() {
    for (const kind of Object.keys(st.timers)) if (st.timers[kind]) flush(kind);
  }

  function setItemWeight(v) {
    st.itemWeight = v;
    clearTimeout(st.timers.iw);
    st.timers.iw = setTimeout(() => flush('iw'), SAVE_MS);
  }

  function data(d) {
    st.asked = false;

    if (!isObj(d) || !window.__slpackratData) return;

    build(d);
    st.carry = isNum(d.carryWeight) ? d.carryWeight : 2;
    st.itemWeight = isNum(d.itemWeight) ? d.itemWeight : 1;
    st.grids = gridsFromFile(d.grids);
    st.items = itemsFromFile(d.itemsFile);
    st.loaded = true;

    if (Array.isArray(d.problems) && d.problems.length) toast(esc(d.problems.join(' ')), true);

    rerender();
  }

  function saved(kind, ok, msg) {
    st.status[kind] = ok ? null : { error: String(msg || '') };

    if (!ok) toast(esc(msg || T('Not saved.', '未保存。')), true);

    rerender();
  }

  // ===== Shared drawing =====

  function stepper(act, value, unit, extra) {
    return '<span class="stp" data-act="' + act + '"' + (extra || '') + '><b data-d="-1">&minus;</b><input value="' + esc(value) + '"><b data-d="1">+</b>' + (unit ? '<i>' + unit + '</i>' : '') + '</span>';
  }

  function bindStepper(root, act, step, min, max, set) {
    root.querySelectorAll('.stp[data-act="' + act + '"]').forEach(el => {
      const input = el.querySelector('input');
      const clamp = v => Math.min(max, Math.max(min, Math.round(v / step) * step));
      const apply = v => { set(+clamp(v).toFixed(2), el); rerender(); };

      el.querySelectorAll('b').forEach(b => b.onclick = () => apply(parseFloat(input.value) + step * +b.dataset.d));
      input.onchange = () => apply(isNum(parseFloat(input.value)) ? parseFloat(input.value) : min);
    });
  }

  function toast(html, isWarn) {
    const tab = st.shown && st.tabs[st.shown];
    if (!tab || !tab.root) return;

    const el = document.createElement('div');
    el.className = 'pr-toast' + (isWarn ? ' warn' : '');
    el.innerHTML = html;
    tab.root.appendChild(el);
    setTimeout(() => el.remove(), TOAST_MS);
  }

  function footer(kind) {
    const ctx = st.tabs[kind].ctx;
    if (!ctx || typeof ctx.footer !== 'function') return;

    const s = st.status[kind];

    try {
      if (s && s.error) ctx.footer('<b>' + FILES[kind] + '</b> &middot; ' + T('not saved: ', '未保存：') + esc(s.error), true);
      else if (s && s.pending) ctx.footer('<b>' + FILES[kind] + '</b> &middot; ' + T('saving', '正在保存'), false);
      else ctx.footer(T('Saved to <b>' + FILES[kind] + '</b>. Changes apply when you load a save.', '保存在 <b>' + FILES[kind] + '</b>。读档时生效。'), false);
    } catch (e) { /* Mod Manager's footer is gone; the tab still works */ }
  }

  // A reset asks for a second click instead of a dialog box.
  function resetButton(act, label) {
    const armed = st.resetArmed === act;

    return '<div class="pr-acts"><div class="key-btn danger" data-act="' + act + '">' + (armed ? T('CLICK AGAIN TO RESET', '再点一次以重置') : label) + '</div></div>';
  }

  function bindReset(root, act, reset) {
    const el = root.querySelector('[data-act="' + act + '"]');
    if (!el) return;

    el.onclick = () => {
      if (st.resetArmed !== act) {
        st.resetArmed = act;
        setTimeout(() => { if (st.resetArmed === act) { st.resetArmed = null; rerender(); } }, RESET_MS);
        rerender();

        return;
      }

      st.resetArmed = null;
      reset();
      rerender();
    };
  }

  function rerender() {
    if (!st.shown) return;

    const tab = st.tabs[st.shown];
    if (!tab.root) return;

    const body = tab.root;

    if (!st.loaded) {
      body.innerHTML = '<div class="pr"><div class="pr-loading">' + T('Loading the game\'s inventories and items...', '正在读取游戏的库存和物品...') + '</div></div>';
      askForData();

      return;
    }

    if (st.shown === 'grids') renderGrids(body);
    else renderItems(body);

    footer(st.shown);
  }

  // ===== Inventories =====

  function renderGrids(body) {
    const entry = ENTRIES.find(e => e.key === ui.entry) || ENTRIES[0];

    if (!entry) {
      body.innerHTML = '<div class="pr"><div class="gv-empty">' + T('No inventories found.', '没有找到库存。') + '</div></div>';
      return;
    }

    ui.entry = entry.key;
    ui.level = Math.min(ui.level, entry.rows.length - 1);

    body.innerHTML = '<div class="pr">' + gridsTop() + '<div class="pr-main"><div class="pr-side">' + entryFinder() + '<div class="lr-list pr-list">' + entryList() + '</div></div>'
      + '<div class="pr-ed">' + entryEditor(entry) + '</div></div>' + resetButton('reset-grids', T('RESET ALL INVENTORIES', '全部恢复默认')) + '</div>';
    bindGrids(body, entry);
  }

  function gridsTop() {
    const g = st.grids;
    const scale = (key, label) => '<div class="pr-scale' + (g[key] !== 1 ? ' chg' : '') + '"><span>' + label + '</span>' + stepper('scale', g[key].toFixed(2), 'x', ' data-key="' + key + '"') + '</div>';

    return '<div class="pr-top"><div class="pr-top-l"><div class="pr-top-t">' + T('SIZE MULTIPLIER', '大小倍率') + '</div>'
      + scale('all', T('All', '全部')) + scale('bag', T('Bags', '背包')) + scale('vehicle', T('Vehicles', '载具')) + scale('storage', T('Storage', '储物'))
      + '</div><div class="pr-top-r"><span>' + T('Preview', '预览') + '</span><div class="pr-seg">'
      + Object.keys(LOADS).map(k => '<span data-load="' + k + '"' + (ui.load === k ? ' class="on"' : '') + '>' + T(LOADS[k].en, LOADS[k].zh) + '</span>').join('') + '</div></div></div>';
  }

  function entryFinder() {
    const count = grp => ENTRIES.filter(e => grp === 'all' || (grp === 'chg' ? isEntryChanged(e) : e.grp === grp)).length;
    const chip = (k, en, zh) => '<span data-grp="' + k + '"' + (ui.ggrp === k ? ' class="on"' : '') + '>' + T(en, zh) + ' <i>' + count(k) + '</i></span>';

    return '<input class="q pr-q" placeholder="' + T('Search', '搜索') + '" value="' + esc(ui.gq) + '">'
      + '<div class="lr-chips pr-chips">' + chip('all', 'All', '全部') + chip('bag', 'Bags', '背包') + chip('vehicle', 'Vehicles', '载具') + chip('storage', 'Storage', '储物') + chip('chg', 'Changed', '已改') + '</div>';
  }

  function entryMatches(e) {
    if (ui.ggrp === 'chg' && !isEntryChanged(e)) return false;
    if (ui.ggrp !== 'all' && ui.ggrp !== 'chg' && e.grp !== ui.ggrp) return false;
    if (!ui.gq) return true;

    const q = ui.gq.toLowerCase();
    return [e.name.en, e.name.zh, ...e.rows.flatMap(r => (r.pieces || []).flatMap(p => [p.en, p.zh]))].some(s => s && s.toLowerCase().includes(q));
  }

  function entryList() {
    let out = '';
    let grp = '';

    for (const e of ENTRIES.filter(entryMatches)) {
      if (e.grp !== grp) out += '<div class="pr-grp">' + T(GROUPS[e.grp][0], GROUPS[e.grp][1]) + '</div>';

      grp = e.grp;
      const r = e.rows[0];
      const g = gridOf(r.id);
      const isChanged = isEntryChanged(e);
      const size = g.w === g.gameW && g.h === g.gameH ? g.w + 'x' + g.h : '<s>' + g.gameW + 'x' + g.gameH + '</s> ' + g.w + 'x' + g.h;
      const sub = e.rows.length > 1 ? T(e.rows.length + ' levels', e.rows.length + ' 级') : r.npieces > 1 ? T(r.npieces + ' pieces', r.npieces + ' 种家具') : r.npieces === 1 ? nameOf(r.pieces[0]) : '';

      out += '<div class="ab pr-ab' + (e.key === ui.entry ? ' sel' : '') + '" data-entry="' + esc(e.key) + '">' + img(e.icon)
        + '<div><div class="nm">' + esc(nameOf(e.name)) + '</div><div class="mt"><span class="lvn">' + size + '</span><span>' + limitKg(g.limit) + '</span><span class="pr-sub">' + esc(sub) + '</span></div></div>'
        + (isChanged ? '<span class="pip chg">' + T('CHANGED', '已改') + '</span>' : '<span></span>') + '</div>';
    }

    return out || '<div class="gv-empty">' + T('Nothing matches.', '没有匹配项。') + '</div>';
  }

  function entryEditor(e) {
    const r = e.rows[ui.level];
    const g = gridOf(r.id);
    const own = st.grids.rows[r.id] || {};
    const scale = gridScale(r.grp);
    const cells = g.w * g.h;
    const gameCells = g.gameW * g.gameH;
    const diff = Math.round((cells / gameCells - 1) * 100);
    const fits = g.w <= 16 && g.h <= 12;
    const pieces = r.npieces ? r.pieces.slice(0, 4).map(nameOf).join(', ') + (r.npieces > 4 ? T(' and ' + (r.npieces - 4) + ' more', ' 等 ' + r.npieces + ' 种') : '') : '';
    const levels = e.rows.length > 1 ? '<div class="pr-levels">' + e.rows.map((x, i) => { const xg = gridOf(x.id); return '<span data-level="' + i + '"' + (i === ui.level ? ' class="on"' : '') + '>' + T('Level ', '等级 ') + (i + 1) + ' <i>' + xg.w + 'x' + xg.h + '</i></span>'; }).join('') + '</div>' : '';
    const ownGroup = st.grids[r.grp] !== 1;
    const from = own.w || own.h ? T('Custom', '自定义') : scale !== 1 ? T((ownGroup ? GROUPS[r.grp][0] : 'All') + ' multiplier (' + scale.toFixed(2) + 'x)', (ownGroup ? GROUPS[r.grp][1] : '全部') + '倍率（' + scale.toFixed(2) + 'x）') : T('Default', '默认');
    const accept = r.accept ? (CATEGORIES[r.accept] || ['Category ' + r.accept, '类别 ' + r.accept]) : null;

    return '<div class="pr-ed-h">' + img(e.icon) + '<div><div class="pr-ed-t">' + esc(nameOf(e.name)) + '</div><div class="pr-ed-s">' + (pieces ? T('Used by ', '用于：') + esc(pieces) : T(GROUPS[r.grp][0], GROUPS[r.grp][1]))
      + (accept ? ' &middot; ' + T(accept[0] + ' only', '仅限' + accept[1]) : '') + '</div></div>' + levels + '</div>'
      + '<div class="pr-ctl"><div><label>' + T('Columns', '列') + '</label>' + stepper('w', g.w, '', ' data-id="' + r.id + '"') + '</div>'
      + '<div><label>' + T('Rows', '行') + '</label>' + stepper('h', g.h, '', ' data-id="' + r.id + '"') + '</div>'
      + '<div><label>' + T('Weight limit', '负重上限') + '</label>' + (g.gameLimit > 0 || own.kg != null ? stepper('kg', (g.limit / 1000).toFixed(1), 'kg', ' data-id="' + r.id + '"') : '<span class="pr-nolimit">' + T('No limit', '无上限') + '</span>') + '</div>'
      + '<div class="pr-from"><label>' + T('Source', '来源') + '</label><b>' + from + '</b>' + (g.hasOverride ? '<a data-act="clear-row" data-id="' + r.id + '">' + T('Reset', '重置') + '</a>' : '') + '</div>'
      + '<div class="pr-stat"><b>' + cells + '</b> ' + T('cells', '格') + ' <span class="' + (diff > 0 ? 'up' : diff < 0 ? 'down' : '') + '">' + (diff === 0 ? T('(default)', '（默认）') : (diff > 0 ? '+' : '') + diff + '%') + '</span>'
      + '<div class="pr-fit ' + (fits ? 'ok' : 'warn') + '">' + (fits ? T('Fits on a 1080p screen', '1080p 屏幕可完整显示') : T('May need scrolling at 1080p', '1080p 下可能需要滚动')) + '</div></div></div>'
      + gridPreview(r, g);
  }

  function gridPreview(r, g) {
    const box = { w: 760, h: 420 };
    const cell = Math.max(10, Math.min(40, Math.floor(box.w / Math.max(g.w, g.gameW)), Math.floor(box.h / Math.max(g.h, g.gameH))));
    const load = sampleLoad(ui.load).filter(it => !r.accept || it.cat === r.accept);
    const { placed, left } = pack(g.w, g.h, load);
    const grams = placed.reduce((sum, p) => sum + gramsOf(p.it).g, 0);
    const shrunk = g.w < g.gameW || g.h < g.gameH;
    const free = g.w * g.h - placed.reduce((s, p) => s + p.w * p.h, 0);
    let tiles = '';

    for (const p of placed) {
      tiles += '<div class="pr-it" style="left:' + p.x * cell + 'px;top:' + p.y * cell + 'px;width:' + p.w * cell + 'px;height:' + p.h * cell + 'px" title="' + esc(itemName(p.it)) + ' ' + p.w + 'x' + p.h + '">' + img(p.it.icon) + '</div>';
    }

    const ghost = '<div class="pr-ghost" style="width:' + g.gameW * cell + 'px;height:' + g.gameH * cell + 'px"><em>' + T('default ', '默认 ') + g.gameW + 'x' + g.gameH + '</em></div>';
    const weightBar = g.limit > 0 ? '<div class="pr-wbar"><span>' + kg(grams) + ' / ' + (g.limit / 1000).toFixed(1) + ' kg</span><i style="width:' + Math.min(100, grams / g.limit * 100) + '%"' + (grams > g.limit ? ' class="over"' : '') + '></i></div>' : '';
    const verdict = !load.length ? T('Empty.', '空。')
      : left.length ? '<b class="warn">' + T(left.length + ' of ' + load.length + ' items don\'t fit.', load.length + ' 件物品中有 ' + left.length + ' 件放不下。') + '</b> ' + (shrunk ? T('In game, they go into shopping bags in the living room.', '游戏中会装进客厅的购物袋。') : '')
      : '<b class="ok">' + T('All ' + load.length + ' items fit.', load.length + ' 件物品全部放得下。') + '</b> ' + T(free + ' cells free.', '剩余 ' + free + ' 格。');

    return '<div class="pr-pv"><div class="pr-canvas" style="width:' + Math.max(g.w, g.gameW) * cell + 'px;height:' + Math.max(g.h, g.gameH) * cell + 'px">'
      + '<div class="pr-cells" style="width:' + g.w * cell + 'px;height:' + g.h * cell + 'px;background-size:' + cell + 'px ' + cell + 'px"></div>' + ghost + tiles + '</div>'
      + '<div class="pr-pv-f"><div>' + verdict + '</div>' + weightBar + '</div></div>';
  }

  function bindGrids(body, entry) {
    const r = entry.rows[ui.level];
    const q = body.querySelector('.pr-q');

    q.oninput = () => { ui.gq = q.value; body.querySelector('.pr-list').innerHTML = entryList(); bindEntries(body); };
    body.querySelectorAll('[data-grp]').forEach(el => el.onclick = () => { ui.ggrp = el.dataset.grp; rerender(); });
    body.querySelectorAll('[data-load]').forEach(el => el.onclick = () => { ui.load = el.dataset.load; rerender(); });
    body.querySelectorAll('[data-level]').forEach(el => el.onclick = () => { ui.level = +el.dataset.level; rerender(); });
    bindEntries(body);
    bindStepper(body, 'scale', 0.25, 0.5, 3, (v, el) => { st.grids[el.dataset.key] = v; changed('grids'); });
    bindStepper(body, 'w', 1, 1, 30, v => setRow(r.id, { w: v }));
    bindStepper(body, 'h', 1, 1, 30, v => setRow(r.id, { h: v }));
    bindStepper(body, 'kg', 0.5, 0.5, 9999, v => setRow(r.id, { kg: v }));

    const clear = body.querySelector('[data-act=clear-row]');
    if (clear) clear.onclick = () => { delete st.grids.rows[r.id]; changed('grids'); rerender(); };

    bindReset(body, 'reset-grids', () => { st.grids = freshGrids(); changed('grids'); });
  }

  function bindEntries(body) {
    body.querySelectorAll('[data-entry]').forEach(el => el.onclick = () => { ui.entry = el.dataset.entry; ui.level = 0; rerender(); });
  }

  // ===== Items =====

  function catItems() {
    return ITEMS.filter(it => it.cat === ui.cat && (ui.sub == null || it.sub === ui.sub));
  }

  function renderItems(body) {
    const item = ui.item != null ? itemById.get(ui.item) : null;
    if (!item) ui.item = null;

    body.innerHTML = '<div class="pr">' + itemsTop() + '<div class="pr-main items"><div class="pr-side"><div class="lr-list pr-cats">' + catList() + '</div></div><div class="pr-ed">' + catEditor() + '</div></div>'
      + resetButton('reset-items', T('RESET ALL ITEMS', '全部物品恢复默认')) + (item ? itemEditor(item) : '') + '</div>';
    bindItems(body, item);
  }

  function itemsTop() {
    const s = st.items;
    const counts = sizeCounts(ITEMS);
    const total = ITEMS.length;
    const bar = counts.slice(0, 6).map(([k, n], i) => '<i class="c' + i + '" style="width:' + (n / total * 100) + '%" title="' + k + ': ' + n + '"></i>').join('');
    const legend = counts.slice(0, 5).map(([k, n], i) => '<span><u class="c' + i + '"></u>' + k + ' <b>' + n.toLocaleString() + '</b></span>').join('');

    return '<div class="pr-top"><div class="pr-top-l"><div class="pr-big' + (s.allOne ? ' on' : '') + '"><div><b>' + T('All items 1x1', '所有物品1x1') + '</b><span>' + T('Category and item settings below override this.', '下方的类别和物品设置会覆盖此项。') + '</span></div>'
      + '<label class="setting-toggle"><input type="checkbox" data-act="all-one"' + (s.allOne ? ' checked' : '') + '><span class="track"></span><span class="knob"></span></label></div>'
      + '<div class="pr-scale' + (st.itemWeight !== 1 ? ' chg' : '') + '"><span>' + T('Item weight', '物品重量') + '</span>' + stepper('iw', st.itemWeight.toFixed(2), 'x') + '</div></div>'
      + '<div class="pr-dist"><div class="pr-dist-t">' + T('Item sizes (' + total.toLocaleString() + ' items)', '物品占格（共 ' + total.toLocaleString() + ' 件）') + '</div><div class="pr-dist-bar">' + bar + '</div><div class="pr-dist-l">' + legend + '</div></div></div>';
  }

  function ruleChip(rule) {
    const bits = [];
    if (rule && rule.size) bits.push(rule.size[0] + 'x' + rule.size[1]);
    if (rule && rule.x != null) bits.push(rule.x.toFixed(2) + 'x kg');

    return bits.length ? '<span class="pip chg">' + bits.join(' &middot; ') + '</span>' : '<span></span>';
  }

  function catNames(cat) {
    return CATEGORIES[cat] || ['Category ' + cat, '类别 ' + cat];
  }

  function catList() {
    const byCat = new Map();
    for (const it of ITEMS) byCat.set(it.cat, (byCat.get(it.cat) || 0) + 1);

    let out = '';

    for (const [cat, n] of [...byCat.entries()].sort((a, b) => b[1] - a[1])) {
      const names = catNames(cat);
      out += '<div class="pr-cat' + (ui.cat === cat && ui.sub == null ? ' sel' : '') + '" data-cat="' + cat + '"><span class="nm">' + T(names[0], names[1]) + '</span><span class="n">' + n.toLocaleString() + '</span>' + ruleChip(st.items.cats[cat]) + '</div>';

      if (cat === 1 && ui.cat === 1) out += foodSubs();
    }

    return out;
  }

  function foodSubs() {
    const bySub = new Map();
    for (const it of ITEMS) if (it.cat === 1) bySub.set(it.sub, (bySub.get(it.sub) || 0) + 1);

    return [...bySub.entries()].sort((a, b) => b[1] - a[1]).map(([sub, n]) => {
      const names = SUBS[sub] || { en: 'Other', zh: '其他' };
      return '<div class="pr-cat sub' + (ui.sub === sub ? ' sel' : '') + '" data-cat="1" data-sub="' + sub + '"><span class="nm">' + esc(nameOf(names)) + '</span><span class="n">' + n.toLocaleString() + '</span>' + ruleChip(st.items.subs[sub]) + '</div>';
    }).join('');
  }

  function catTitle() {
    const names = catNames(ui.cat);
    if (ui.sub == null) return T(names[0], names[1]);

    return T(names[0], names[1]) + ' &rsaquo; ' + esc(nameOf(SUBS[ui.sub] || { en: 'Other', zh: '其他' }));
  }

  function currentRule() {
    return ui.sub == null ? st.items.cats[ui.cat] : st.items.subs[ui.sub];
  }

  function sizePicker(rule, act) {
    const cur = rule && rule.size ? rule.size.join('x') : '';
    const chips = SIZE_CHOICES.map(([w, hh]) => '<span data-size="' + w + 'x' + hh + '" class="pr-sz' + (cur === w + 'x' + hh ? ' on' : '') + '" title="' + w + 'x' + hh + '"><em style="grid-template-columns:repeat(' + w + ',9px);grid-template-rows:repeat(' + hh + ',9px)">' + '<u></u>'.repeat(w * hh) + '</em><b>' + w + 'x' + hh + '</b></span>').join('');

    return '<div class="pr-szs" data-act="' + act + '"><span data-size="" class="pr-sz own' + (cur ? '' : ' on') + '"><b>' + T('Default', '默认') + '</b></span>' + chips + '</div>';
  }

  function catEditor() {
    const list = catItems();
    const rule = currentRule() || {};
    const q = ui.iq.toLowerCase();
    const shown = list.filter(it => !q || it.en.toLowerCase().includes(q) || it.zh.includes(ui.iq));
    const counts = sizeCounts(list).slice(0, 4).map(([k, n]) => k + ' <b>' + n + '</b>').join(' &middot; ');
    const overrides = list.filter(it => st.items.ids[it.id]).length;
    let tiles = '';

    for (const it of shown.slice(0, ui.shown)) tiles += itemTile(it);

    const more = shown.length > ui.shown ? '<div class="pr-more" data-act="more">' + T('Show more (' + (shown.length - ui.shown) + ' left)', '显示更多（还有 ' + (shown.length - ui.shown) + ' 件）') + '</div>' : '';

    return '<div class="pr-ce-h"><div><div class="pr-ed-t">' + catTitle() + '</div><div class="pr-ed-s">' + list.length.toLocaleString() + T(' items: ', ' 件：') + counts + (overrides ? ' &middot; <span class="hl">' + T(overrides + ' custom', overrides + ' 件自定义') + '</span>' : '') + '</div></div>'
      + '<div class="pr-scale' + (rule.x != null ? ' chg' : '') + '"><span>' + T('Weight', '重量') + '</span>' + stepper('cx', (rule.x != null ? rule.x : st.itemWeight).toFixed(2), 'x') + (rule.x != null ? '<a data-act="clear-cx">' + T('Reset', '重置') + '</a>' : '') + '</div></div>'
      + '<div class="pr-ce-sz"><label>' + T(ui.sub == null ? 'Category size' : 'Group size', ui.sub == null ? '类别占格' : '分组占格') + '</label>' + sizePicker(rule, 'cat-size') + '</div>'
      + '<div class="pr-ce-f"><input class="q pr-iq" placeholder="' + T('Search ', '搜索') + catTitle().replace(/&rsaquo;/, '>') + '" value="' + esc(ui.iq) + '"><span>' + T('Click an item to change its size or weight.', '点击物品可修改它的占格或重量。') + '</span></div>'
      + '<div class="pr-tiles">' + (tiles || '<div class="gv-empty">' + T('Nothing matches.', '没有匹配项。') + '</div>') + more + '</div>';
  }

  function itemTile(it) {
    const s = sizeOf(it);
    const g = gramsOf(it);
    const cell = 30;
    const isChanged = s.w !== it.w || s.h !== it.h;
    const mark = st.items.ids[it.id] ? '<span class="pr-own" title="' + T('Custom', '自定义') + '">&#9679;</span>' : '';

    return '<div class="pr-tile' + (ui.item === it.id ? ' sel' : '') + '" data-item="' + it.id + '"><div class="pr-fp" style="width:' + s.w * cell + 'px;height:' + s.h * cell + 'px;background-size:' + cell + 'px ' + cell + 'px">' + img(it.icon) + '</div>'
      + '<div class="pr-tile-t"><div class="nm">' + mark + esc(itemName(it)) + '</div><div class="mt"><span class="' + (isChanged ? 'hl' : '') + '">' + (isChanged ? '<s>' + it.w + 'x' + it.h + '</s> ' : '') + s.w + 'x' + s.h + '</span><span' + (g.g !== it.g ? ' class="hl"' : '') + '>' + kg(g.g) + ' kg</span></div></div></div>';
  }

  // How many of one item a level 1 backpack holds, by cells and by weight.
  function backpackHolds(it) {
    if (!bagById.has(1)) return null;

    const bp = gridOf(1);
    const s = sizeOf(it);
    const g = gramsOf(it).g;
    const byCells = Math.floor(bp.w / s.w) * Math.floor(bp.h / s.h) * Math.max(1, it.stack);
    const byWeight = g > 0 && bp.limit > 0 ? Math.floor(bp.limit / g) : Infinity;

    return { n: Math.min(byCells, byWeight), limitedBy: byWeight < byCells ? 'weight' : 'cells' };
  }

  function itemEditor(it) {
    const own = st.items.ids[it.id] || {};
    const s = sizeOf(it);
    const g = gramsOf(it);
    const holds = backpackHolds(it);
    const cat = st.items.cats[it.cat];
    const sub = it.cat === 1 ? st.items.subs[it.sub] : null;
    const catName = T(catNames(it.cat)[0], catNames(it.cat)[1]);
    const step = (label, val, on) => '<li class="' + (on ? 'on' : val ? '' : 'off') + '"><span>' + label + '</span><b>' + (val || '-') + '</b></li>';
    const chain = '<ol class="pr-chain">'
      + step(T('This item', '此物品'), own.size ? own.size.join('x') : '', s.from === 'item')
      + (it.cat === 1 ? step(esc(nameOf(SUBS[it.sub] || { en: 'Other', zh: '其他' })), sub && sub.size ? sub.size.join('x') : '', s.from === 'sub') : '')
      + step(catName, cat && cat.size ? cat.size.join('x') : '', s.from === 'cat')
      + step(T('All items 1x1', '所有物品1x1'), st.items.allOne ? '1x1' : '', s.from === 'all')
      + step(T('Default', '默认'), it.w + 'x' + it.h, s.from === 'game') + '</ol>';
    const multiplier = g.from === 'all' ? T(' (item weight multiplier applied)', '（已应用物品重量倍率）') : g.from === 'cat' || g.from === 'sub' ? T(' (category multiplier applied)', '（已应用类别倍率）') : '';
    const holdsHtml = holds ? '<label>' + T('Fits in a level 1 backpack', '1级背包可装') + '</label><div class="pr-holds"><b>' + holds.n.toLocaleString() + '</b> ' + T('of these', '件') + '<span>' + (holds.limitedBy === 'weight' ? T('(weight limit)', '（受重量限制）') : T('(space)', '（受空间限制）')) + '</span></div>'
      + '<div class="pr-m-pv">' + holdsPreview(it) + '</div>' : '';

    return '<div class="pr-modal"><div class="pr-mcard"><div class="pr-m-h"><div class="pr-m-icon">' + img(it.icon) + '</div><div><div class="pr-ed-t">' + esc(itemName(it)) + '</div><div class="pr-ed-s">' + catName + ' &middot; ' + T('stacks to ', '堆叠上限 ') + it.stack + ' &middot; #' + it.id + '</div></div><span class="close-btn" data-act="close-item">&#x2715;</span></div>'
      + '<div class="pr-m-b"><div><label>' + T('Size', '占格') + '</label>' + sizePicker(own, 'item-size') + '<label>' + T('Size source', '占格来源') + '</label>' + chain + '</div>'
      + '<div><label>' + T('Weight', '重量') + '</label><div class="pr-m-w">' + stepper('ig', kg(g.g), 'kg') + (own.g != null ? '<a data-act="clear-ig">' + T('Reset', '重置') + '</a>' : '') + '<span>' + T('Default ', '默认 ') + kg(it.g) + ' kg' + multiplier + '</span></div>'
      + holdsHtml + '</div></div></div></div>';
  }

  function holdsPreview(it) {
    const bp = gridOf(1);
    const s = sizeOf(it);
    const cell = Math.min(24, Math.floor(300 / bp.w), Math.floor(220 / bp.h));
    let tiles = '';

    for (let y = 0; y + s.h <= bp.h; y += s.h) for (let x = 0; x + s.w <= bp.w; x += s.w) tiles += '<div class="pr-it" style="left:' + x * cell + 'px;top:' + y * cell + 'px;width:' + s.w * cell + 'px;height:' + s.h * cell + 'px">' + img(it.icon) + '</div>';

    return '<div class="pr-canvas" style="width:' + bp.w * cell + 'px;height:' + bp.h * cell + 'px"><div class="pr-cells" style="width:100%;height:100%;background-size:' + cell + 'px ' + cell + 'px"></div>' + tiles + '</div>';
  }

  function bindSizes(root, act, set) {
    const box = root.querySelector('.pr-szs[data-act="' + act + '"]');
    if (!box) return;

    box.querySelectorAll('[data-size]').forEach(el => el.onclick = () => { set(el.dataset.size ? el.dataset.size.split('x').map(Number) : null); rerender(); });
  }

  function bindItems(body, item) {
    const iq = body.querySelector('.pr-iq');
    const ruleKind = ui.sub == null ? 'cats' : 'subs';
    const ruleKey = ui.sub == null ? ui.cat : ui.sub;

    body.querySelector('[data-act=all-one]').onchange = e => { st.items.allOne = e.target.checked; changed('items'); rerender(); };
    body.querySelectorAll('[data-cat]').forEach(el => el.onclick = () => { ui.cat = +el.dataset.cat; ui.sub = el.dataset.sub != null ? +el.dataset.sub : null; ui.shown = SHOWN_STEP; ui.iq = ''; rerender(); });
    body.querySelectorAll('[data-item]').forEach(el => el.onclick = () => { ui.item = +el.dataset.item; rerender(); });
    iq.oninput = () => {
      ui.iq = iq.value;
      ui.shown = SHOWN_STEP;
      rerender();

      const again = body.querySelector('.pr-iq');
      if (again) { again.focus(); again.setSelectionRange(again.value.length, again.value.length); }
    };

    const more = body.querySelector('[data-act=more]');
    if (more) more.onclick = () => { ui.shown += SHOWN_STEP; rerender(); };

    bindStepper(body, 'iw', 0.05, 0.1, 3, v => setItemWeight(v));
    bindStepper(body, 'cx', 0.05, 0, 5, v => setRule(ruleKind, ruleKey, { x: v }));
    bindSizes(body, 'cat-size', size => setRule(ruleKind, ruleKey, { size }));

    const clearCx = body.querySelector('[data-act=clear-cx]');
    if (clearCx) clearCx.onclick = () => { setRule(ruleKind, ruleKey, { x: null }); rerender(); };

    bindReset(body, 'reset-items', () => { st.items = freshItems(); changed('items'); setItemWeight(1); });

    if (item) bindItemEditor(body, item);
  }

  function bindItemEditor(body, it) {
    body.querySelector('[data-act=close-item]').onclick = () => { ui.item = null; rerender(); };
    body.querySelector('.pr-modal').onclick = e => { if (e.target.classList.contains('pr-modal')) { ui.item = null; rerender(); } };
    bindSizes(body, 'item-size', size => setRule('ids', it.id, { size }));
    bindStepper(body, 'ig', 0.1, 0, 500, v => setRule('ids', it.id, { g: Math.round(v * 1000) }));

    const clear = body.querySelector('[data-act=clear-ig]');
    if (clear) clear.onclick = () => { setRule('ids', it.id, { g: null }); rerender(); };
  }

  // ===== Mod Manager =====

  function show(kind, el, ctx) {
    injectStyle();

    const tab = st.tabs[kind];
    tab.ctx = ctx || null;
    st.lang = ctx && ctx.lang === 'zh' ? 'zh' : 'en';

    if (!tab.root) {
      tab.root = document.createElement('div');
      tab.root.className = 'pr-host';
    }

    if (tab.root.parentNode !== el) el.appendChild(tab.root);

    // Each time a tab opens it reads the files again, so an edit made by hand shows up.
    if (st.shown !== kind) st.loaded = st.loaded && !!window.__slpackratData;
    if (st.shown !== kind && st.loaded) askForData();

    st.shown = kind;
    rerender();
  }

  function hide(kind) {
    flushAll();
    if (kind === 'items') ui.item = null;
    if (st.shown === kind) st.shown = null;
  }

  function escape(kind) {
    if (kind === 'items' && ui.item != null) {
      ui.item = null;
      rerender();

      return true;
    }

    return false;
  }

  window.__slpackrat = { data, saved };

  if (!Array.isArray(window.__modmanagerTabs)) window.__modmanagerTabs = [];

  window.__modmanagerTabs.push(
    { guid: GUID, id: 'grids', title: { en: 'Inventories', zh: '库存' }, show: (el, ctx) => show('grids', el, ctx), hide: () => hide('grids'), escape: () => escape('grids') },
    { guid: GUID, id: 'items', title: { en: 'Items', zh: '物品' }, show: (el, ctx) => show('items', el, ctx), hide: () => hide('items'), escape: () => escape('items') });

  try {
    if (window.__modmanager && typeof window.__modmanager.tabsChanged === 'function') window.__modmanager.tabsChanged();
  } catch (err) { /* Mod Manager picks the tabs up when it opens */ }
})();
