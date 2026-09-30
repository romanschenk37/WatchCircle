(function () {
    'use strict';
    function t(key, values) { return WatchCircleI18n.t(key, values); }

    if (window.WatchCircleWatchProgress) {
        return;
    }

    let STYLES_ID = 'watchcircle-watch-progress-styles';
    let TICKS_PER_SECOND = 10000000;

    function getAssetUrl(path) {
        if (window.WatchCircleAssets) {
            return WatchCircleAssets.getUrl(path);
        }

        if (typeof ApiClient !== 'undefined' && ApiClient.getUrl) {
            return ApiClient.getUrl('WatchCircle/js/' + path);
        }

        return '/WatchCircle/js/' + path;
    }

    function parseProgress(raw) {
        if (!raw) {
            return {
                played: false,
                playbackPositionTicks: 0,
                seasonIndexNumber: null,
                episodeIndexNumber: null,
                episodeRunTimeTicks: 0,
                aggregate: null
            };
        }

        let seasonIndexNumber = raw.seasonIndexNumber ?? raw.SeasonIndexNumber;
        let episodeIndexNumber = raw.episodeIndexNumber ?? raw.EpisodeIndexNumber;

        return {
            played: !!(raw.played || raw.Played),
            playbackPositionTicks: Number(raw.playbackPositionTicks || raw.PlaybackPositionTicks || 0),
            seasonIndexNumber: seasonIndexNumber === undefined || seasonIndexNumber === null
                ? null
                : Number(seasonIndexNumber),
            episodeIndexNumber: episodeIndexNumber === undefined || episodeIndexNumber === null
                ? null
                : Number(episodeIndexNumber),
            episodeRunTimeTicks: Number(raw.episodeRunTimeTicks || raw.EpisodeRunTimeTicks || 0),
            aggregate: raw.aggregate || raw.Aggregate || null
        };
    }

    function getProgressRuntime(progress, fallbackRunTimeTicks) {
        if (progress && progress.episodeRunTimeTicks > 0) {
            return progress.episodeRunTimeTicks;
        }

        return fallbackRunTimeTicks || 0;
    }

    function getProgressPercent(played, playbackPositionTicks, runTimeTicks) {
        if (played) {
            return 100;
        }

        if (!runTimeTicks || runTimeTicks <= 0) {
            return 0;
        }

        return Math.min(100, Math.round((playbackPositionTicks / runTimeTicks) * 100));
    }

    function formatWatchedDuration(playbackPositionTicks) {
        let totalSeconds = Math.max(0, Math.floor(playbackPositionTicks / TICKS_PER_SECOND));
        let hours = Math.floor(totalSeconds / 3600);
        let minutes = Math.floor((totalSeconds % 3600) / 60);
        let seconds = totalSeconds % 60;

        if (hours >= 1) {
            return t("{hours}h {minutes}m {seconds}s", { hours: hours, minutes: minutes, seconds: seconds });
        }

        return t("{minutes}m {seconds}s", { minutes: minutes, seconds: seconds });
    }

    function formatCompactStatus(played, playbackPositionTicks, runTimeTicks) {
        if (played) {
            return t("Finished");
        }

        let watched = formatWatchedDuration(playbackPositionTicks);
        let percent = getProgressPercent(false, playbackPositionTicks, runTimeTicks);
        return runTimeTicks > 0 ? watched + ' (' + percent + '%)' : watched;
    }

    function formatProgressStatus(played, playbackPositionTicks, runTimeTicks) {
        if (played) {
            return t("Finished");
        }

        let watched = formatWatchedDuration(playbackPositionTicks);
        let percent = getProgressPercent(false, playbackPositionTicks, runTimeTicks);
        if (!(runTimeTicks > 0)) return t("{duration} watched", { duration: watched });
        return t("{duration} watched ({percent}%)", { duration: watched, percent: percent });
    }

    function ensureStyles() {
        if (document.getElementById(STYLES_ID)) {
            return;
        }

        let link = document.createElement('link');
        link.id = STYLES_ID;
        link.rel = 'stylesheet';
        link.href = getAssetUrl('components/watchProgress/watchProgress.css');
        document.head.appendChild(link);
    }

    function formatEpisodeLine(progress) {
        let season = progress.seasonIndexNumber;
        let episode = progress.episodeIndexNumber;

        if (season !== null && season !== undefined && episode !== null && episode !== undefined) {
            return t("Season {season} · Episode {episode}", { season: season, episode: episode });
        }

        if (episode !== null && episode !== undefined) {
            return t("Episode {episode}", { episode: episode });
        }

        return '';
    }

    function formatEpisodeStatus(progress) {
        const episode = formatEpisodeLine(progress);
        return episode ? episode + ' · ' + t(progress.played ? 'Watched' : 'Started') : '';
    }

    function getAggregateDisplay(raw) {
        function field(name) { return raw[name[0].toLowerCase() + name.slice(1)] ?? raw[name]; }
        const total = Math.max(0, Number(field('TotalEpisodes')) || 0);
        const completed = Math.max(0, Math.min(total, Number(field('CompletedEpisodes')) || 0));
        // Only Jellyfin's played flags can complete the title. Rounding an almost
        // finished episode must not produce a misleading 100% series/season bar.
        const percent = total > 0 && completed === total ? 100
            : total > 0 ? Math.max(0, Math.min(99, Math.round(Number(field('Percent')) || 0))) : 0;
        return {
            percent: percent,
            status: total > 0 ? t('{completed} of {total} episodes completed ({percent}%)', { completed: completed, total: total, percent: percent })
                : t('No available episodes')
        };
    }

    function createRow(options) {
        options = options || {};

        let progress = parseProgress(options.progress);
        let aggregate = options.aggregate || progress.aggregate;
        let aggregateDisplay = aggregate ? getAggregateDisplay(aggregate) : null;
        let runTimeTicks = getProgressRuntime(progress, options.runTimeTicks || 0);
        let variant = options.variant || 'you';
        let showStatus = options.showStatus !== false;
        let labelText = options.labelText || '';
        let statusText = aggregateDisplay ? aggregateDisplay.status : options.statusText || formatProgressStatus(
            progress.played, progress.playbackPositionTicks, runTimeTicks);
        let percent = aggregateDisplay ? aggregateDisplay.percent : getProgressPercent(
            progress.played, progress.playbackPositionTicks, runTimeTicks);
        let episodeLineText = options.showEpisodeLine ? formatEpisodeStatus(progress) : '';

        if (!labelText) {
            labelText = formatCompactStatus(
                progress.played,
                progress.playbackPositionTicks,
                runTimeTicks
            );
        }

        let row = document.createElement('div');
        row.className = 'wc-detail-progress-row';
        if (aggregateDisplay) row.classList.add('wc-detail-progress-aggregate');

        let meta = document.createElement('div');
        meta.className = 'wc-detail-progress-meta';

        let label = document.createElement('span');
        label.className = 'wc-detail-progress-label';
        label.textContent = labelText;

        meta.appendChild(label);

        if (showStatus) {
            let status = document.createElement('span');
            status.className = 'wc-detail-progress-status';
            status.textContent = statusText;
            meta.appendChild(status);
        }

        row.appendChild(meta);

        if (options.showEpisodeLine) {
            if (episodeLineText) {
                let episodeLine = document.createElement('div');
                episodeLine.className = 'wc-detail-progress-episode';
                episodeLine.textContent = episodeLineText;
                row.appendChild(episodeLine);
            }
        }

        let track = document.createElement('div');
        track.className = 'wc-detail-progress-track';
        track.setAttribute('role', 'progressbar');
        track.setAttribute('aria-label', labelText);
        track.setAttribute('aria-valuemin', '0');
        track.setAttribute('aria-valuemax', '100');
        track.setAttribute('aria-valuenow', String(percent));
        track.setAttribute('aria-valuetext', [statusText, episodeLineText].filter(Boolean).join(', '));

        let fill = document.createElement('div');
        fill.className = 'wc-detail-progress-fill wc-detail-progress-fill-' + variant;
        fill.style.width = percent + '%';

        track.appendChild(fill);
        row.appendChild(track);

        return row;
    }

    function createSectionHeading(text) {
        let heading = document.createElement('div');
        heading.className = 'wc-detail-progress-heading';
        heading.textContent = text || t("Progress");
        return heading;
    }

    window.WatchCircleWatchProgress = {
        TICKS_PER_SECOND: TICKS_PER_SECOND,
        parseProgress: parseProgress,
        getProgressRuntime: getProgressRuntime,
        getProgressPercent: getProgressPercent,
        formatWatchedDuration: formatWatchedDuration,
        formatCompactStatus: formatCompactStatus,
        formatProgressStatus: formatProgressStatus,
        formatEpisodeLine: formatEpisodeLine,
        formatEpisodeStatus: formatEpisodeStatus,
        getAggregateDisplay: getAggregateDisplay,
        ensureStyles: ensureStyles,
        createRow: createRow,
        createSectionHeading: createSectionHeading
    };
})();
