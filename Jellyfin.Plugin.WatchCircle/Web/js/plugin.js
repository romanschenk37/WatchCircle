(function () {
    'use strict';

    let BOOTSTRAP_FLAG = '__watchCircleBootstrapped';

    if (window[BOOTSTRAP_FLAG]) {
        return;
    }

    function runWhenApiClientReady(callback) {
        if (typeof ApiClient !== 'undefined') {
            callback();
            return;
        }

        let attempts = 0;
        let timer = setInterval(function () {
            if (typeof ApiClient !== 'undefined') {
                clearInterval(timer);
                callback();
                return;
            }

            if (++attempts >= 240) {
                clearInterval(timer);
            }
        }, 50);
    }

    function loadJellyfinHooks(callback) {
        if (window.__watchCircleJellyfinHooksInstalled) {
            callback();
            return;
        }

        if (document.getElementById('watchcircle-jellyfin-hooks-script')) {
            document.getElementById('watchcircle-jellyfin-hooks-script').addEventListener('load', callback, { once: true });
            return;
        }

        let script = document.createElement('script');
        script.id = 'watchcircle-jellyfin-hooks-script';
        script.src = ApiClient.getUrl('WatchCircle/js/utils/jellyfinHooks.js');
        script.addEventListener('load', callback, { once: true });
        document.head.appendChild(script);
    }

    function loadWatchTogetherQueueService() {
        if (document.getElementById('watchcircle-watch-together-queue-script')) {
            if (window.WatchCircleWatchTogetherQueueService) {
                WatchCircleWatchTogetherQueueService.start();
            }
            return;
        }

        let script = document.createElement('script');
        script.id = 'watchcircle-watch-together-queue-script';
        script.src = (window.WatchCircleAssets
            ? WatchCircleAssets.getUrl('services/watchTogetherQueueService.js')
            : ApiClient.getUrl('WatchCircle/js/services/watchTogetherQueueService.js'));
        script.addEventListener('load', function () {
            if (window.WatchCircleWatchTogetherQueueService) {
                WatchCircleWatchTogetherQueueService.start();
            }
        }, { once: true });
        document.head.appendChild(script);
    }

    function loadPlaybackStopService() {
        if (document.getElementById('watchcircle-playback-stop-script')) {
            if (window.WatchCirclePlaybackStopService) {
                WatchCirclePlaybackStopService.start();
            }
            return;
        }

        let script = document.createElement('script');
        script.id = 'watchcircle-playback-stop-script';
        script.src = (window.WatchCircleAssets
            ? WatchCircleAssets.getUrl('services/playbackStopService.js')
            : ApiClient.getUrl('WatchCircle/js/services/playbackStopService.js'));
        script.addEventListener('load', function () {
            if (window.WatchCirclePlaybackStopService) {
                WatchCirclePlaybackStopService.start();
            }
        }, { once: true });
        document.head.appendChild(script);
    }

    function loadPlaybackGateService() {
        if (document.getElementById('watchcircle-playback-gate-script')) {
            if (window.WatchCirclePlaybackGateService) {
                WatchCirclePlaybackGateService.start();
            }
            return;
        }

        let script = document.createElement('script');
        script.id = 'watchcircle-playback-gate-script';
        script.src = (window.WatchCircleAssets
            ? WatchCircleAssets.getUrl('services/playbackGateService.js')
            : ApiClient.getUrl('WatchCircle/js/services/playbackGateService.js'));
        script.addEventListener('load', function () {
            if (window.WatchCirclePlaybackGateService) {
                WatchCirclePlaybackGateService.start();
            }
        }, { once: true });
        document.head.appendChild(script);
    }

    function loadAssetUtils(callback) {
        if (window.WatchCircleAssets) {
            callback();
            return;
        }

        if (document.getElementById('watchcircle-asset-utils-script')) {
            document.getElementById('watchcircle-asset-utils-script').addEventListener('load', callback, { once: true });
            return;
        }

        let script = document.createElement('script');
        script.id = 'watchcircle-asset-utils-script';
        script.src = ApiClient.getUrl('WatchCircle/js/utils/assetUrl.js');
        script.addEventListener('load', callback, { once: true });
        document.head.appendChild(script);
    }

    function loadStylesheet() {
        if (!document.getElementById('watchcircle-overlay-styles')) {
            let link = document.createElement('link');
            link.id = 'watchcircle-overlay-styles';
            link.rel = 'stylesheet';
            link.href = (window.WatchCircleAssets ? WatchCircleAssets.getUrl('components/overlays/overlays.css') : ApiClient.getUrl('WatchCircle/js/components/overlays/overlays.css'));
            document.head.appendChild(link);
        }

        if (!document.getElementById('watchcircle-watch-progress-styles')) {
            let progressLink = document.createElement('link');
            progressLink.id = 'watchcircle-watch-progress-styles';
            progressLink.rel = 'stylesheet';
            progressLink.href = (window.WatchCircleAssets
                ? WatchCircleAssets.getUrl('components/watchProgress/watchProgress.css')
                : ApiClient.getUrl('WatchCircle/js/components/watchProgress/watchProgress.css'));
            document.head.appendChild(progressLink);
        }
    }

    function loadNavbarModule() {
        if (document.getElementById('watchcircle-navbar-script')) {
            return;
        }

        var script = document.createElement('script');
        script.id = 'watchcircle-navbar-script';
        script.src = (window.WatchCircleAssets ? WatchCircleAssets.getUrl('components/navbar/navbar.js') : ApiClient.getUrl('WatchCircle/js/components/navbar/navbar.js'));
        document.head.appendChild(script);
    }

    function loadWatchProgressModule(callback) {
        function finish() {
            if (window.WatchCircleWatchProgress) {
                WatchCircleWatchProgress.ensureStyles();
            }

            callback();
        }

        if (window.WatchCircleWatchProgress) {
            finish();
            return;
        }

        let existing = document.getElementById('watchcircle-watch-progress-script');
        if (existing) {
            existing.addEventListener('load', finish, { once: true });
            return;
        }

        let script = document.createElement('script');
        script.id = 'watchcircle-watch-progress-script';
        script.src = (window.WatchCircleAssets
            ? WatchCircleAssets.getUrl('components/watchProgress/watchProgress.js')
            : ApiClient.getUrl('WatchCircle/js/components/watchProgress/watchProgress.js'));
        script.addEventListener('load', finish, { once: true });
        document.head.appendChild(script);
    }

    function loadOverlayModule() {
        if (document.getElementById('watchcircle-overlay-script')) {
            loadWatchProgressModule(function () { });
            return;
        }

        loadWatchProgressModule(function () {
            loadStylesheet();

            let script = document.createElement('script');
            script.id = 'watchcircle-overlay-script';
            script.src = (window.WatchCircleAssets ? WatchCircleAssets.getUrl('components/overlays/overlays.js') : ApiClient.getUrl('WatchCircle/js/components/overlays/overlays.js'));
            document.head.appendChild(script);
        });
    }

    function userIsAuthenticated() {
        return ApiClient.getCurrentUserId && !!ApiClient.getCurrentUserId();
    }

    function tryStart() {
        if (!userIsAuthenticated()) {
            return false;
        }

        window[BOOTSTRAP_FLAG] = true;
        loadOverlayModule();
        loadPlaybackGateService();
        loadPlaybackStopService();
        loadWatchTogetherQueueService();
        return true;
    }

    function bindAuthWaiters() {
        function onViewShow() {
            if (tryStart()) {
                document.removeEventListener('viewshow', onViewShow);
            }
        }

        document.addEventListener('viewshow', onViewShow);

        if (typeof Events !== 'undefined') {
            Events.on(document, 'viewshow', function () {
                tryStart();
            });
        }
    }

    runWhenApiClientReady(function () {
        loadJellyfinHooks(function () {
            loadAssetUtils(function () {
                loadNavbarModule();

                if (!tryStart()) {
                    bindAuthWaiters();
                }
            });
        });
    });
})();
