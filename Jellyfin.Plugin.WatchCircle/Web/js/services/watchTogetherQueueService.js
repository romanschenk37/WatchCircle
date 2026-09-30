(function () {
    'use strict';

    if (window.WatchCircleWatchTogetherQueueService) {
        return;
    }

    let isStarted = false;
    let queueProcessed = false;
    let modulePromise = null;

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

    function ensureModules() {
        if (modulePromise) {
            return modulePromise;
        }

        modulePromise = loadScriptModule(
            'watchcircle-watch-together-pending-dialog-script',
            'components/watchTogetherPending/watchTogetherPendingDialog.js',
            function () { return window.WatchCircleWatchTogetherPendingDialog; }
        ).then(function () {
            WatchCircleWatchTogetherPendingDialog.ensureStyles();
        });

        return modulePromise;
    }

    function normalizeHostQueue(host) {
        return {
            hostId: host.HostId || host.hostId,
            hostName: host.HostName || host.hostName || 'your buddy',
            hostImageUrl: host.HostImageUrl || host.hostImageUrl || null,
            hostHasPrimaryImage: !!(host.HostHasPrimaryImage || host.hostHasPrimaryImage),
            media: host.Media || host.media || []
        };
    }

    function loadQueue() {
        return ApiClient.ajax({
            type: 'GET',
            url: ApiClient.getUrl('WatchCircle/WatchTogether/Queue'),
            dataType: 'json'
        }).then(function (response) {
            let hosts = response && (response.Hosts || response.hosts);
            if (!Array.isArray(hosts)) {
                return [];
            }

            return hosts.map(normalizeHostQueue).filter(function (hostQueue) {
                return hostQueue.media && hostQueue.media.length;
            });
        }).catch(function (error) {
            console.warn('[WatchCircle] Could not load watch together queue.', error);
            return [];
        });
    }

    function loadProgressUiRefreshModule() {
        return new Promise(function (resolve, reject) {
            if (window.WatchCircleProgressUiRefresh) {
                resolve();
                return;
            }

            let scriptId = 'watchcircle-progress-ui-refresh-script';
            let existing = document.getElementById(scriptId);
            if (existing) {
                existing.addEventListener('load', function () { resolve(); }, { once: true });
                existing.addEventListener('error', reject, { once: true });
                return;
            }

            let script = document.createElement('script');
            script.id = scriptId;
            script.src = getAssetUrl('utils/progressUiRefresh.js');
            script.addEventListener('load', function () { resolve(); }, { once: true });
            script.addEventListener('error', reject, { once: true });
            document.head.appendChild(script);
        });
    }

    function refreshProgressUi(selectedMediaIds) {
        let itemIds = (selectedMediaIds || []).filter(function (id) {
            return !!id;
        });

        function refreshOverlaysWhenReady(attempt) {
            if (!itemIds.length) {
                return Promise.resolve();
            }

            if (window.WatchCircleOverlays && typeof WatchCircleOverlays.refresh === 'function') {
                let result = WatchCircleOverlays.refresh(itemIds);
                if (result && typeof result.then === 'function') {
                    return result;
                }

                return Promise.resolve();
            }

            if ((attempt || 0) < 30) {
                return new Promise(function (resolve) {
                    setTimeout(function () {
                        refreshOverlaysWhenReady((attempt || 0) + 1).then(resolve);
                    }, 100);
                });
            }

            return Promise.resolve();
        }

        let overlaysRefresh = refreshOverlaysWhenReady(0);

        if (!itemIds.length) {
            return Promise.resolve();
        }

        let nativeRefresh = loadProgressUiRefreshModule().then(function () {
            if (window.WatchCircleProgressUiRefresh && WatchCircleProgressUiRefresh.refreshNativeCards) {
                return WatchCircleProgressUiRefresh.refreshNativeCards(itemIds);
            }
        });

        return Promise.all([overlaysRefresh, nativeRefresh]).catch(function (error) {
            console.warn('[WatchCircle] Progress UI refresh failed.', error);
        });
    }

    function acknowledgeHostQueue(hostQueue, dialogResult) {
        if (!dialogResult || dialogResult.action !== 'continue') {
            return Promise.resolve();
        }

        let selectedMediaIds = dialogResult.selectedMediaIds || [];

        return ApiClient.ajax({
            type: 'POST',
            url: ApiClient.getUrl('WatchCircle/WatchTogether/Queue/Acknowledge'),
            data: JSON.stringify({
                hostId: hostQueue.hostId,
                selectedMediaIds: selectedMediaIds
            }),
            contentType: 'application/json'
        }).then(function () {
            return refreshProgressUi(selectedMediaIds);
        }).catch(function (error) {
            console.warn('[WatchCircle] Failed to acknowledge watch together queue.', error);
        });
    }

    function showHostDialogs(hostQueues) {
        let chain = Promise.resolve();

        hostQueues.forEach(function (hostQueue) {
            if (!hostQueue.media || !hostQueue.media.length) {
                return;
            }

            chain = chain.then(function () {
                return WatchCircleWatchTogetherPendingDialog.show({
                    hostId: hostQueue.hostId,
                    hostName: hostQueue.hostName,
                    hostImageUrl: hostQueue.hostImageUrl,
                    hostHasPrimaryImage: hostQueue.hostHasPrimaryImage,
                    media: hostQueue.media
                }).then(function (dialogResult) {
                    return acknowledgeHostQueue(hostQueue, dialogResult);
                });
            });
        });

        return chain;
    }

    function processQueue() {
        if (queueProcessed) {
            return Promise.resolve();
        }

        if (typeof ApiClient === 'undefined' || !ApiClient.ajax) {
            return Promise.resolve();
        }

        return ensureModules()
            .then(function () {
                return loadQueue();
            })
            .then(function (hostQueues) {
                if (!hostQueues.length) {
                    return;
                }

                queueProcessed = true;
                return showHostDialogs(hostQueues);
            })
            .catch(function (error) {
                console.warn('[WatchCircle] Watch together queue workflow failed.', error);
            });
    }

    function start() {
        if (isStarted) {
            return;
        }

        isStarted = true;
        processQueue();
    }

    window.WatchCircleWatchTogetherQueueService = {
        start: start,
        processQueue: processQueue
    };
})();
