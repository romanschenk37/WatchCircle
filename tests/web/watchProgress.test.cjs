const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const root = path.resolve(__dirname, '../../Jellyfin.Plugin.WatchCircle/Web/js');

class Element {
    constructor() { this.children = []; this.style = {}; this.attributes = {}; this.className = ''; this.classList = { add: value => { this.className += ' ' + value; } }; }
    appendChild(child) { this.children.push(child); }
    setAttribute(name, value) { this.attributes[name] = value; }
    querySelector(selector) {
        for (const child of this.children) {
            if (child.className.split(' ').includes(selector.slice(1))) return child;
            const nested = child.querySelector(selector);
            if (nested) return nested;
        }
        return null;
    }
}
function load(lang = 'de') {
    const context = vm.createContext({ window: {}, document: { createElement: () => new Element(), documentElement: { getAttribute: () => lang } }, MutationObserver: class { observe() {} } });
    vm.runInContext(fs.readFileSync(path.join(root, 'utils/i18n.js'), 'utf8'), context);
    context.WatchCircleI18n = context.window.WatchCircleI18n;
    vm.runInContext(fs.readFileSync(path.join(root, 'components/watchProgress/watchProgress.js'), 'utf8'), context);
    return context.window.WatchCircleWatchProgress;
}
const halfEpisode = { Played: false, SeasonIndexNumber: 2, EpisodeIndexNumber: 1, PlaybackPositionTicks: 6000000000, EpisodeRunTimeTicks: 12000000000 };

test('detail and profile rows show the same whole-series progress, distinct from the current episode', () => {
    const helper = load();
    const aggregate = { CompletedEpisodes: 1, TotalEpisodes: 4, Percent: 37.5, Episode: halfEpisode };
    const detail = helper.createRow({ labelText: 'Anna', progress: helper.parseProgress({ ...halfEpisode, Aggregate: aggregate }), showEpisodeLine: true });
    const profile = helper.createRow({ labelText: 'Anna', progress: halfEpisode, aggregate, showEpisodeLine: true });
    for (const row of [detail, profile]) {
        assert.equal(row.querySelector('.wc-detail-progress-status').textContent, '1 von 4 Folgen abgeschlossen (38 %)');
        assert.equal(row.querySelector('.wc-detail-progress-episode').textContent, 'Staffel 2 · Folge 1 · Begonnen');
        assert.equal(row.querySelector('.wc-detail-progress-fill').style.width, '38%');
        assert.equal(row.querySelector('.wc-detail-progress-track').attributes['aria-valuenow'], '38');
        assert.equal(row.querySelector('.wc-detail-progress-track').attributes['aria-valuetext'], '1 von 4 Folgen abgeschlossen (38 %), Staffel 2 · Folge 1 · Begonnen');
    }
});

test('a watched last episode does not fill the series bar; camelCase responses work too', () => {
    const helper = load('en');
    const row = helper.createRow({ labelText: 'Anna', progress: { played: true, seasonIndexNumber: 1, episodeIndexNumber: 3,
        aggregate: { completedEpisodes: 1, totalEpisodes: 3, percent: 100 / 3 } }, showEpisodeLine: true });
    assert.equal(row.querySelector('.wc-detail-progress-status').textContent, '1 of 3 episodes completed (33%)');
    assert.equal(row.querySelector('.wc-detail-progress-episode').textContent, 'Season 1 · Episode 3 · Watched');
    assert.equal(row.querySelector('.wc-detail-progress-fill').style.width, '33%');
});

test('season scope uses its own total while single episodes and movies retain duration progress', () => {
    const helper = load();
    const season = helper.createRow({ progress: halfEpisode, aggregate: { TotalEpisodes: 2, CompletedEpisodes: 0, Percent: 25 }, showEpisodeLine: true });
    assert.equal(season.querySelector('.wc-detail-progress-status').textContent, '0 von 2 Folgen abgeschlossen (25 %)');
    assert.equal(season.querySelector('.wc-detail-progress-fill').style.width, '25%');
    const episode = helper.createRow({ labelText: 'Anna', progress: halfEpisode });
    const movie = helper.createRow({ labelText: 'Anna', progress: { PlaybackPositionTicks: halfEpisode.PlaybackPositionTicks }, runTimeTicks: halfEpisode.EpisodeRunTimeTicks });
    for (const row of [episode, movie]) {
        assert.equal(row.querySelector('.wc-detail-progress-status').textContent, '10 Min. 0 Sek. gesehen (50 %)');
        assert.equal(row.querySelector('.wc-detail-progress-fill').style.width, '50%');
        assert.equal(row.querySelector('.wc-detail-progress-episode'), null);
    }
});

test('only completed episodes yield 100%; empty and untouched titles never invent an episode', () => {
    const helper = load();
    for (const [total, completed, percent, expected] of [[1, 0, 99.999, 99], [4, 4, 100, 100], [0, 0, 0, 0], [5, 0, 0, 0]]) {
        const row = helper.createRow({ progress: {}, aggregate: { TotalEpisodes: total, CompletedEpisodes: completed, Percent: percent }, showEpisodeLine: true });
        assert.equal(row.querySelector('.wc-detail-progress-fill').style.width, expected + '%');
        assert.equal(row.querySelector('.wc-detail-progress-episode'), null);
        if (!total) assert.equal(row.querySelector('.wc-detail-progress-status').textContent, 'Keine verfügbaren Folgen');
    }
});

test('specials and started episodes without a resume position remain explicitly labelled', () => {
    const helper = load();
    assert.equal(helper.formatEpisodeStatus(helper.parseProgress({ SeasonIndexNumber: 0, EpisodeIndexNumber: 3 })), 'Staffel 0 · Folge 3 · Begonnen');
    assert.equal(helper.formatEpisodeStatus(helper.parseProgress({ SeasonIndexNumber: 0, EpisodeIndexNumber: 3, Played: true })), 'Staffel 0 · Folge 3 · Gesehen');
});
