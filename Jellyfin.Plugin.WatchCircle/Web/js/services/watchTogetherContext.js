(function () {
    'use strict';

    if (window.WatchCircleWatchTogetherContext) {
        return;
    }

    let sessionDepsPromise = null;
    let gateDepsPromise = null;

    function getAssetUrl(path) {
        if (window.WatchCircleAssets) {
            return WatchCircleAssets.getUrl(path);
        }

        if (typeof ApiClient !== 'undefined' && ApiClient.getUrl) {
            return ApiClient.getUrl('WatchCircle/js/' + path);
        }

        return '/WatchCircle/js/' + path;
    }

    function loadScriptModule(scriptId, scriptPath, isReady) {
        return new Promise(function (resolve, reject) {
            if (isReady()) {
                resolve();
                return;
            }

            let existing = document.getElementById(scriptId);
            if (existing) {
                existing.addEventListener('load', function () { resolve(); }, { once: true });
                existing.addEventListener('error', reject, { once: true });
                return;
            }

            let script = document.createElement('script');
            script.id = scriptId;
            script.src = getAssetUrl(scriptPath);
            script.addEventListener('load', function () { resolve(); }, { once: true });
            script.addEventListener('error', reject, { once: true });
            document.head.appendChild(script);
        });
    }

    function ensureSessionModules() {
        if (sessionDepsPromise) {
            return sessionDepsPromise;
        }

        sessionDepsPromise = Promise.all([
            loadScriptModule(
                'watchcircle-watch-together-session-script',
                'services/watchTogetherSession.js',
                function () { return window.WatchCircleWatchTogetherSession; }
            ),
            loadScriptModule(
                'watchcircle-user-select-script',
                'components/userSelect/userSelect.js',
                function () { return window.WatchCircleUserSelect; }
            )
        ]).then(function () {
            WatchCircleUserSelect.ensureStyles();
        });

        return sessionDepsPromise;
    }

    function ensureGateModules() {
        if (gateDepsPromise) {
            return gateDepsPromise;
        }

        gateDepsPromise = ensureSessionModules().then(function () {
            return loadScriptModule(
                'watchcircle-watch-together-dialog-script',
                'components/watchTogether/watchTogetherDialog.js',
                function () { return window.WatchCircleWatchTogetherDialog; }
            );
        });

        return gateDepsPromise;
    }

    function resolveActiveBuddyIds() {
        return ensureSessionModules()
            .then(function () {
                return WatchCircleUserSelect.loadBuddies();
            })
            .then(function (buddies) {
                return WatchCircleWatchTogetherSession.pruneToKnownBuddies(buddies);
            });
    }

    function isSupportedMediaType(mediaType) {
        return mediaType === 'Video' || mediaType === 'Audio';
    }

    window.WatchCircleWatchTogetherContext = {
        ensureSessionModules: ensureSessionModules,
        ensureGateModules: ensureGateModules,
        resolveActiveBuddyIds: resolveActiveBuddyIds,
        isSupportedMediaType: isSupportedMediaType
    };
})();
