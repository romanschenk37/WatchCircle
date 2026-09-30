(function () {
    'use strict';

    if (window.__watchCircleJellyfinHooksInstalled) {
        return;
    }

    function notifyDiscovery(kind, instance) {
        if (!instance) {
            return;
        }

        if (kind === 'pluginManager') {
            window.__watchCirclePluginManager = instance;
        }

        if (kind === 'playbackManager') {
            window.__watchCirclePlaybackManager = instance;
        }

        if (window.WatchCirclePlaybackGateService) {
            window.WatchCirclePlaybackGateService.onJellyfinInstanceDiscovered(kind, instance);
        }

        if (window.WatchCirclePlaybackStopService) {
            window.WatchCirclePlaybackStopService.onJellyfinInstanceDiscovered(kind, instance);
        }
    }

    function looksLikePluginManager(obj) {
        return !!(obj && Array.isArray(obj.pluginsList) && typeof obj.ofType === 'function');
    }

    function looksLikePlaybackManager(obj) {
        return !!(obj && typeof obj.pause === 'function' && typeof obj.getCurrentPlayer === 'function' && obj._playQueueManager);
    }

    function installHooks() {
        if (window.__watchCircleJellyfinHooksInstalled || !window.Events) {
            return !!window.__watchCircleJellyfinHooksInstalled;
        }

        let originalOn = Events.on.bind(Events);
        let originalTrigger = Events.trigger.bind(Events);

        Events.on = function (obj, type, fn) {
            if (looksLikePluginManager(obj)) {
                notifyDiscovery('pluginManager', obj);
            }

            if (looksLikePlaybackManager(obj)) {
                notifyDiscovery('playbackManager', obj);
            }

            return originalOn(obj, type, fn);
        };

        Events.trigger = function (obj, type, args) {
            if (looksLikePluginManager(obj)) {
                notifyDiscovery('pluginManager', obj);
            }

            if (looksLikePlaybackManager(obj)) {
                notifyDiscovery('playbackManager', obj);
            }

            return originalTrigger(obj, type, args);
        };

        window.__watchCircleJellyfinHooksInstalled = true;
        return true;
    }

    function waitForEvents() {
        if (installHooks()) {
            return;
        }

        let attempts = 0;
        let timer = setInterval(function () {
            if (installHooks() || ++attempts >= 240) {
                clearInterval(timer);
            }
        }, 50);
    }

    waitForEvents();
})();
