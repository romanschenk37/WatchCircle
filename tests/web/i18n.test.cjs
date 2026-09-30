const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const root = path.resolve(__dirname, '../../Jellyfin.Plugin.WatchCircle/Web/js');

function load() {
    let lang = 'de-DE';
    const context = vm.createContext({ window: {}, document: { documentElement: { getAttribute: () => lang } }, navigator: { language: 'en-US' }, MutationObserver: class { observe() {} } });
    vm.runInContext(fs.readFileSync(path.join(root, 'utils/i18n.js'), 'utf8'), context);
    context.WatchCircleI18n = context.window.WatchCircleI18n;
    vm.runInContext(fs.readFileSync(path.join(root, 'components/watchProgress/watchProgress.js'), 'utf8'), context);
    return { i18n: context.WatchCircleI18n, progress: context.window.WatchCircleWatchProgress, language: value => { lang = value; } };
}

test('Jellyfin display language wins over browser language and can change without reload', () => {
    const { i18n, language } = load();
    assert.equal(i18n.t('Started series'), 'Begonnene Serien');
    language('en-US');
    assert.equal(i18n.t('Started series'), 'Started series');
    language('de_CH');
    assert.equal(i18n.t('Started series'), 'Begonnene Serien');
});

test('unsupported or missing display languages consistently fall back to original English', () => {
    const { i18n, language } = load();
    for (const lang of ['fr', 'it-IT', 'es', '', null]) {
        language(lang);
        assert.equal(i18n.t('Finished'), 'Finished');
        assert.equal(i18n.t('WatchCircle: People & progress'), 'WatchCircle: People & progress');
    }
});

test('interpolation treats names literally, including braces and replacement characters', () => {
    const { i18n } = load();
    const name = '<img src=x> $& {count}';
    assert.equal(i18n.t('Group: {name}', { name }), 'Gruppe: ' + name);
});

test('profiles and detail cards use the same localized episode and duration formatter', () => {
    const { progress, language } = load();
    const episode = progress.parseProgress({ SeasonIndexNumber: 2, EpisodeIndexNumber: 4, PlaybackPositionTicks: 6000000000, EpisodeRunTimeTicks: 12000000000 });
    assert.equal(progress.formatEpisodeLine(episode), 'Staffel 2 · Folge 4');
    assert.equal(progress.formatProgressStatus(false, episode.playbackPositionTicks, episode.episodeRunTimeTicks), '10 Min. 0 Sek. gesehen (50 %)');
    language('en');
    assert.equal(progress.formatEpisodeLine(episode), 'Season 2 · Episode 4');
    assert.equal(progress.formatProgressStatus(true, 0, 0), 'Finished');
    assert.equal(progress.formatProgressStatus(false, 600000000, 0), '1m 0s watched');
});
