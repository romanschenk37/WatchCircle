const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const root = path.resolve(__dirname, '../../Jellyfin.Plugin.WatchCircle/Web/js');
const itemId = '11111111222233334444555555555555';

// Minimal DOM for exercising the public refresh API with the actual rendering assets.
class Element {
    constructor(tag) {
        this.tagName = tag;
        this.children = []; this.style = {}; this.attributes = {}; this.dataset = {};
        this.className = ''; this.offsetParent = {};
        this.classList = { add: name => { this.className += ' ' + name; } };
    }
    appendChild(child) { child.parent = this; this.children.push(child); return child; }
    insertBefore(child, anchor) { child.parent = this; this.children.splice(this.children.indexOf(anchor), 0, child); }
    remove() { this.parent.children = this.parent.children.filter(child => child !== this); }
    replaceChildren(...children) { this.children = []; children.forEach(child => this.appendChild(child)); }
    replaceWith(child) { child.parent = this.parent; this.parent.children.splice(this.parent.children.indexOf(this), 1, child); }
    setAttribute(key, value) { this.attributes[key] = value; }
    getAttribute(key) { return this.attributes[key]; }
    addEventListener() {}
    get firstChild() { return this.children[0]; }
    get lastElementChild() { return this.children.at(-1); }
    matches(selector) {
        if (selector === '[data-wc-detail-buddies-item-id]') return !!this.dataset.wcDetailBuddiesItemId;
        if (selector === '.page:not(.hide)') return false;
        return selector[0] === '.' ? this.className.split(' ').includes(selector.slice(1)) : this.tagName === selector;
    }
    querySelectorAll(selector) {
        return this.children.flatMap(child => [...(child.matches(selector) ? [child] : []), ...child.querySelectorAll(selector)]);
    }
    querySelector(selector) { return this.querySelectorAll(selector)[0] || null; }
}

function load({ overlay, requesters = [], language = 'de' }) {
    const document = new Element('document');
    document.head = document.appendChild(new Element('head'));
    document.body = document.appendChild(new Element('body'));
    document.documentElement = { getAttribute: () => language };
    document.createElement = tag => new Element(tag);
    document.getElementById = () => null;
    const mount = document.body.appendChild(new Element('div'));
    mount.className = 'detailPagePrimaryContent';
    const anchor = mount.appendChild(new Element('div')); anchor.className = 'detailSection';
    const timers = new Map(); let timer = 0;
    const context = vm.createContext({
        window: { location: { hash: '#/details?id=' + itemId }, addEventListener() {} }, document,
        MutationObserver: class { observe() {} }, console,
        setTimeout: callback => { timers.set(++timer, callback); return timer; }, clearTimeout: id => timers.delete(id),
        ApiClient: { getCurrentUserId: () => 'viewer', getUrl: route => '/' + route,
            ajax: ({ url }) => url.includes('/Seerr/')
                ? (requesters instanceof Error ? Promise.reject(requesters) : Promise.resolve(requesters))
                : Promise.resolve(overlay ? { [itemId]: overlay } : {}) }
    });
    for (const asset of ['utils/i18n.js', 'components/watchProgress/watchProgress.js', 'components/overlays/overlays.js']) {
        vm.runInContext(fs.readFileSync(path.join(root, asset), 'utf8'), context);
        Object.assign(context, context.window);
    }
    async function flush() {
        // Flush the requester callback and its scheduled rerender, without running retries forever.
        await Promise.resolve();
        await Promise.resolve();
        const pending = [...timers.values()]; timers.clear(); pending.forEach(callback => callback());
        await Promise.resolve();
    }
    return { mount, context, flush, async refresh() {
        await context.window.WatchCircleOverlays.refreshDetailBuddies();
        await flush();
    } };
}

const untouched = { IsSupported: true, RunTimeTicks: 60000000000, CurrentUser: {}, Watchers: [] };
const textOf = (mount, selector) => mount.querySelector(selector)?.textContent;

test('an untouched movie always has own progress, with or without requesters; no heading or empty group list', async () => {
    for (const requesters of [[], [{ Name: 'Alex' }]]) {
        const page = load({ overlay: untouched, requesters });
        await page.refresh();
        assert.equal(page.mount.querySelectorAll('.wc-shared-progress-card').length, 1);
        assert.equal(textOf(page.mount, '.wc-detail-progress-label'), 'Du');
        assert.equal(textOf(page.mount, '.wc-detail-progress-status'), '0 Min. 0 Sek. gesehen (0 %)');
        assert.equal(page.mount.querySelector('.wc-detail-progress-track').attributes['aria-valuenow'], '0');
        assert.equal(page.mount.querySelector('h2'), null);
        assert.equal(page.mount.querySelector('.wc-shared-progress-count'), null);
        assert.equal(page.mount.querySelector('.wc-shared-progress-members'), null);
        assert.equal(textOf(page.mount, '.wc-request-heading'), requesters.length ? 'Angefragt von' : undefined);
        if (requesters.length) assert.equal(textOf(page.mount, 'li'), 'Alex');
    }
});

test('request failure leaves own completed progress visible in English', async () => {
    const page = load({ overlay: { ...untouched, CurrentUser: { Played: true } }, language: 'en', requesters: new Error('offline') });
    await page.refresh();
    assert.equal(textOf(page.mount, '.wc-detail-progress-label'), 'You');
    assert.equal(page.mount.querySelector('.wc-detail-progress-track').attributes['aria-valuenow'], '100');
    assert.equal(page.mount.querySelector('.wc-request-attribution'), null);
});

test('series and season use the current user aggregate, independently of buddy progress', async () => {
    for (const kind of ['IsSeries', 'IsSeason']) {
        const page = load({ overlay: { ...untouched, [kind]: true, CurrentUser: {
            SeasonIndexNumber: 2, EpisodeIndexNumber: 1, PlaybackPositionTicks: 6000000000,
            Aggregate: { CompletedEpisodes: 1, TotalEpisodes: 4, Percent: 37.5 }
        }, Watchers: [{ Id: 'buddy', Name: 'Anna', Played: true, SeasonIndexNumber: 1, EpisodeIndexNumber: 1,
            Aggregate: { CompletedEpisodes: 1, TotalEpisodes: 4, Percent: 25 } }] } });
        await page.refresh();
        assert.equal(textOf(page.mount, '.wc-detail-progress-status'), '1 von 4 Folgen abgeschlossen (38 %)');
        assert.equal(textOf(page.mount, '.wc-detail-progress-episode'), 'Staffel 2 · Folge 1 · Begonnen');
        assert.equal(textOf(page.mount, '.wc-shared-progress-count'), '1 Person aus deinen Gruppen');
        const buddy = page.mount.querySelector('.wc-shared-progress-members');
        assert.equal(textOf(buddy, '.wc-detail-progress-label'), 'Anna');
        assert.equal(buddy.querySelector('.wc-detail-progress-track').attributes['aria-valuenow'], '25');
    }
});

test('unstarted series keeps its episode count and single episodes retain duration progress', async () => {
    const series = load({ overlay: { ...untouched, isSupported: true, isSeries: true,
        currentUser: { aggregate: { completedEpisodes: 0, totalEpisodes: 12, percent: 0 } } } });
    await series.refresh();
    assert.equal(textOf(series.mount, '.wc-detail-progress-status'), '0 von 12 Folgen abgeschlossen (0 %)');
    assert.equal(series.mount.querySelector('.wc-detail-progress-episode'), null);
    const episode = load({ overlay: { ...untouched, CurrentUser: { PlaybackPositionTicks: 30000000000 } } });
    await episode.refresh();
    assert.equal(textOf(episode.mount, '.wc-detail-progress-status'), '50 Min. 0 Sek. gesehen (50 %)');
});

test('late requester data rerenders the same card without losing own progress', async () => {
    let resolve;
    const requesters = new Promise(done => { resolve = done; });
    const page = load({ overlay: untouched, requesters });
    await page.refresh();
    assert.equal(page.mount.querySelectorAll('.wc-shared-progress-card').length, 1);
    resolve([{ Name: '<img src=x onerror=alert(1)>' }]);
    await page.flush();
    assert.equal(page.mount.querySelectorAll('.wc-shared-progress-card').length, 1);
    assert.equal(textOf(page.mount, 'li'), '<img src=x onerror=alert(1)>');
    assert.equal(textOf(page.mount, '.wc-detail-progress-label'), 'Du');
    assert.equal(page.mount.querySelector('img'), null);
});

test('unsupported, inaccessible or missing items do not acquire a fabricated own-progress card', async () => {
    for (const overlay of [null, { ...untouched, IsSupported: false }]) {
        const page = load({ overlay });
        await page.refresh();
        assert.equal(page.mount.querySelector('.wc-shared-progress-card'), null);
    }
});
