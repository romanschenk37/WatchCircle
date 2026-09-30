const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const root = path.resolve(__dirname, '../../Jellyfin.Plugin.WatchCircle/Web/js');
const id = n => `${String(n).padStart(8, '0')}-1111-2222-3333-444444444444`;

class Element {
    constructor(tag) {
        this.tagName = tag; this.children = []; this.style = {}; this.attributes = {}; this.dataset = {}; this.events = {};
        this.className = ''; this.classList = { add: name => { this.className += ' ' + name; } };
    }
    appendChild(child) { child.parent = this; this.children.push(child); return child; }
    remove() { this.parent.children = this.parent.children.filter(child => child !== this); this.parent = null; }
    replaceChildren(...children) { this.children.forEach(child => { child.parent = null; }); this.children = []; children.forEach(child => this.appendChild(child)); }
    setAttribute(key, value) { this.attributes[key] = value; }
    getAttribute(key) { return this.attributes[key]; }
    addEventListener(key, listener) { this.events[key] = listener; }
    removeEventListener() {}
    click() { return this.events.click?.({ target: this, currentTarget: this }); }
    focus() {}
    get isConnected() { return this.tagName === 'document' || !!this.parent?.isConnected; }
    getClientRects() { return this.isConnected ? [{}] : []; }
    matches(selector) {
        if (selector === '[data-wc-clean-request-item]') return !!this.dataset.wcCleanRequestItem;
        return selector[0] === '.' ? this.className.split(' ').includes(selector.slice(1)) : this.tagName === selector;
    }
    querySelectorAll(selector) {
        return this.children.flatMap(child => [...(child.matches(selector) ? [child] : []), ...child.querySelectorAll(selector)]);
    }
    querySelector(selector) { return this.querySelectorAll(selector)[0] || null; }
}

function entry(n, kind, size, name, extra = {}) {
    return { Entry: { Id: id(n), Media: { ItemId: id(n + 100), Kind: kind, Bytes: size, Name: name }, Present: true,
        NominationId: id(90), DeleteAt: '2000-01-01T00:00:00Z', ...extra }, Replies: [], Deletions: [], RestoreErrors: [] };
}
const mixed = () => [entry(1, 'Movie', 9, 'Small movie'), entry(2, 'Series', 2, 'Small series'),
    entry(3, 'Movie', 40, 'Big movie'), entry(4, 'Series', 100, 'Big series'), entry(5, 'Collection', 50, 'Collection')];
async function flush() { for (let n = 0; n < 35; n++) await Promise.resolve(); }

function load({ rows = mixed(), requesters = () => [], users = [], detailRequesters = [], language = 'de', lazy = false } = {}) {
    const document = new Element('document'); document.head = document.appendChild(new Element('head'));
    document.body = document.appendChild(new Element('body')); document.documentElement = { getAttribute: () => language };
    document.createElement = tag => new Element(tag); document.getElementById = () => null;
    let viewer = id(70); const calls = [], observers = [];
    const context = vm.createContext({
        document, window: { location: { hash: '' }, addEventListener() {}, removeEventListener() {} }, console,
        MutationObserver: class { observe() {} }, setTimeout() {}, clearTimeout() {}, setInterval() {},
        WatchCircleAssets: { getUrl: route => '/' + route },
        ApiClient: { getCurrentUserId: () => viewer, getUrl: route => '/' + route, ajax: async options => {
            calls.push(options.url);
            if (options.url.includes('/Seerr/Requests/')) return requesters(options.url.split('/').at(-1));
            if (options.url.endsWith('/Status')) return { Enabled: true, IsAdmin: true };
            if (options.url.endsWith('/Admin/Items')) return { Enabled: true, Entries: rows };
            if (options.url.includes('/Admin/Items/')) return { Entry: rows.find(row => row.Entry.Id === options.url.split('/').at(-1)).Entry,
                Users: users, Requesters: detailRequesters };
            throw new Error('Unexpected API request: ' + options.url);
        } }
    });
    if (lazy) context.IntersectionObserver = class {
        constructor(callback) { this.callback = callback; this.nodes = []; observers.push(this); }
        observe(node) { this.nodes.push(node); }
        unobserve(node) { this.nodes = this.nodes.filter(value => value !== node); }
        disconnect() { this.nodes = []; }
    };
    context.window.WatchCircleAssets = context.WatchCircleAssets;
    for (const file of ['utils/i18n.js', 'components/watchProgress/watchProgress.js', 'components/profiles/profileNavigation.js', 'components/cleanup/cleanup.js']) {
        vm.runInContext(fs.readFileSync(path.join(root, file), 'utf8'), context);
        Object.assign(context, context.window);
    }
    return { document, calls, observers, changeViewer: () => { viewer = id(71); },
        async show() { await context.window.WatchCircleCleanup.show('admin'); await flush(); },
        async click(text) { const control = document.querySelectorAll('button').find(node => node.textContent === text);
            assert.ok(control, 'Missing button: ' + text); await control.click(); await flush(); } };
}

test('admin groups series, movies and collections, sorted numerically largest first in every filter', async () => {
    const page = load(); await page.show();
    for (const filter of ['Zur Löschung vorgemerkt', 'Alle Titel']) {
        await page.click(filter);
        const sections = page.document.querySelectorAll('.wc-clean-media-group');
        assert.deepEqual(sections.map(section => section.attributes['aria-label']), ['Serien', 'Filme', 'Sammlungen']);
        assert.deepEqual(sections.map(section => section.querySelectorAll('.wc-clean-title-link').map(node => node.textContent)),
            [['Big series', 'Small series'], ['Big movie', 'Small movie'], ['Collection']]);
        assert.ok(page.document.querySelectorAll('p').some(node => node.textContent === 'Löschdatum erreicht'));
    }
});

test('request attribution loads without blocking cards, treats names as text and tolerates missing or failed Seerr', async () => {
    const page = load({ requesters: itemId => {
        if (itemId === id(104)) return [{ Name: '<img src=x onerror=alert(1)>' }, { Name: 'Anna' }];
        if (itemId === id(103)) throw new Error('offline');
        return [];
    } });
    await page.show();
    const cards = page.document.querySelectorAll('.wc-clean-card');
    assert.equal(cards[0].querySelector('.wc-clean-requesters').textContent, 'Angefragt von: <img src=x onerror=alert(1)>, Anna');
    assert.equal(cards[0].querySelectorAll('img').length, 1); // Only the poster, no parsed requester markup.
    assert.equal(cards[1].querySelector('.wc-clean-requesters').textContent, 'Angefragt von: —');
    assert.equal(cards[2].querySelector('.wc-clean-requesters').textContent, 'Angefragt von: Nicht verfügbar');
    assert.equal(cards[4].querySelector('.wc-clean-requesters'), null);
});

test('linked requesters come first, retain their own progress, and unlinked names never match an unrelated account', async () => {
    const page = load({ users: [
        { Id: id(10), Name: 'Alex', Replies: [], Progress: { Started: false } },
        { Id: id(11), Name: 'Zoe', Replies: [], Progress: { Started: true, TotalEpisodes: 5, CompletedEpisodes: 2, Percent: 40 } },
        { Id: id(12), Name: 'Anna', Replies: [] }
    ], detailRequesters: [{ Id: 1, Name: 'Zoe in Seerr', ProfileUserId: id(11).replaceAll('-', '') },
        { Id: 2, Name: 'Alex' }, { Id: 3, Name: 'Zoe alternate', ProfileUserId: id(11) }] });
    await page.show(); await page.click('Big series');
    const users = page.document.querySelectorAll('.wc-clean-user');
    assert.deepEqual(users.map(node => node.attributes['aria-label']), ['Alex', 'Zoe', 'Alex', 'Anna']);
    assert.ok(users[0].querySelectorAll('p').some(node => node.textContent.includes('Kein verknüpftes Jellyfin-Konto')));
    assert.equal(users[1].querySelector('.wc-detail-progress-track').attributes['aria-valuenow'], '40');
    assert.equal(users[2].querySelector('.wc-clean-protected'), null);
    assert.equal(page.document.querySelectorAll('p').filter(node => node.textContent?.startsWith('In Seerr angefragt:')).length, 0);
});

test('English labels and empty groups remain usable without Seerr requesters', async () => {
    const page = load({ rows: [entry(1, 'Movie', 0, 'Movie')], language: 'en' }); await page.show();
    assert.deepEqual(page.document.querySelectorAll('.wc-clean-media-group').map(node => node.attributes['aria-label']), ['Series', 'Movies']);
    assert.equal(page.document.querySelector('.wc-clean-requesters').textContent, 'Requested by: —');
});

test('only nearby cards load Seerr with at most three concurrent calls; navigation cancels the remaining queue', async () => {
    const pending = [];
    const page = load({ lazy: true, rows: Array.from({ length: 8 }, (_, n) => entry(n + 1, 'Movie', n, 'Movie ' + n)),
        requesters: () => new Promise(resolve => pending.push(resolve)) });
    await page.show();
    assert.equal(pending.length, 0);
    const observer = page.observers[0];
    observer.callback(observer.nodes.map(target => ({ target, isIntersecting: true })));
    await flush(); assert.equal(pending.length, 3);
    await page.click('Schliessen');
    pending.forEach(resolve => resolve([{ Name: 'Late result' }])); await flush();
    assert.equal(page.calls.filter(url => url.includes('/Seerr/')).length, 3);
    assert.equal(page.document.querySelector('.wc-clean-dialog'), null);
    observer.callback([{ target: new Element('p'), isIntersecting: true }]); // A queued observer callback after closing is harmless.
});

test('requester responses from a previous account are not rendered after a session change', async () => {
    let resolve;
    const page = load({ rows: [entry(1, 'Movie', 1, 'Movie')], requesters: () => new Promise(done => { resolve = done; }) });
    await page.show(); page.changeViewer(); resolve([{ Name: 'Previous account result' }]); await flush();
    assert.equal(page.document.querySelector('.wc-clean-requesters').textContent, 'Angefragt von: Wird geladen …');
});
