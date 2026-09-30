(function () {
    'use strict';
    function t(key, values) { return WatchCircleI18n.t(key, values); }

    if (typeof ApiClient === 'undefined') {
        return;
    }

    if (window.WatchCircleWatchProgress) {
        WatchCircleWatchProgress.ensureStyles();
    }

    function getWatchProgress() {
        return window.WatchCircleWatchProgress || null;
    }

    let CARD_SELECTOR = '.cardImageContainer.cardContent, .cardContent > .cardImageContainer, .listItemImage';
    let DETAIL_SECTION_SELECTOR = '.detailSection';
    let DETAIL_MOUNT_SELECTOR = '.detailPagePrimaryContent';
    let OVERLAY_CLASS = 'wc-watcher-stack';
    let DETAIL_BUDDIES_CLASS = 'wc-detail-buddies';
    let DETAIL_MOUNT_DATA_ATTR = 'wcDetailBuddiesItemId';
    let DETAIL_RETRY_MS = 150;
    let DETAIL_RETRY_MAX = 40;
    let pendingItemIds = new Set();
    let overlayCache = new Map();
    let requesterCache = new Map();
    let requesterPending = new Map();
    let fetchTimer = null;
    let detailRetryTimer = null;
    let detailScanTimer = null;
    let observer = null;
    let overlayFetchGeneration = 0;

    function normalizeGuid(value) {
        return (value || '').toString().replace(/-/g, '').toLowerCase();
    }

    function findCardRoot(element) {
        return element.closest('.card[data-id], [data-id].card, .listItem[data-id], [data-id].listItem');
    }

    function extractItemId(element) {
        let card = findCardRoot(element);
        if (card && card.dataset && card.dataset.id) {
            return card.dataset.id;
        }

        let link = element.closest('a[href*="id="]');
        if (link && link.href) {
            let match = link.href.match(/[?&]id=([a-f0-9-]{32,36})/i);
            if (match) {
                return match[1];
            }
        }

        let image = element.querySelector('img[data-src], img[src]') || element;
        let source = image.getAttribute('data-src') || image.getAttribute('src') || '';
        let imageMatch = source.match(/\/Items\/([a-f0-9-]{32,36})\//i);
        if (imageMatch) {
            return imageMatch[1];
        }

        return null;
    }

    function getMountPoint(imageContainer) {
        return imageContainer;
    }

    function resolveImageUrl(imageUrl) {
        if (!imageUrl) {
            return null;
        }

        if (imageUrl.indexOf('http') === 0) {
            return imageUrl;
        }

        return ApiClient.getUrl(imageUrl.replace(/^\//, ''));
    }

    function getInitial(name) {
        let trimmed = (name || '').trim();
        return trimmed ? trimmed.charAt(0).toUpperCase() : '?';
    }

    function removeExistingOverlay(mount) {
        let existing = mount.querySelector('.' + OVERLAY_CLASS);
        if (existing) {
            existing.remove();
        }
    }

    function renderOverlay(mount, watchers) {
        removeExistingOverlay(mount);

        if (!watchers || !watchers.length) {
            return;
        }

        let stack = document.createElement('div');
        stack.className = OVERLAY_CLASS;

        let visibleWatchers = watchers.slice(0, 3);
        visibleWatchers.forEach(function (watcher, index) {
            let name = watcher.Name || watcher.name || '';
            let imageUrl = resolveImageUrl(watcher.ImageUrl || watcher.imageUrl);

            if (imageUrl) {
                let img = document.createElement('img');
                img.className = 'wc-watcher-avatar';
                img.alt = name;
                img.title = name;
                img.src = imageUrl;
                img.style.zIndex = String(index + 1);
                img.addEventListener('error', function () {
                    img.replaceWith(createInitialAvatar(name, index));
                });
                stack.appendChild(img);
            } else {
                stack.appendChild(createInitialAvatar(name, index));
            }
        });

        if (watchers.length > 3) {
            let more = document.createElement('span');
            more.count = watchers.length - 3;
            more.className = 'wc-watcher-more';
            more.textContent = more.count > 99 ? '99+' : '+' + more.count;
            more.title = watchers.slice(3).map(function (watcher) {
                return watcher.Name || watcher.name;
            }).join(', ');
            more.style.zIndex = '4';
            stack.appendChild(more);
        }

        mount.appendChild(stack);
    }

    function createInitialAvatar(name, index) {
        let avatar = document.createElement('span');
        avatar.className = 'wc-watcher-avatar wc-watcher-initial';
        avatar.textContent = getInitial(name);
        avatar.title = name;
        avatar.style.zIndex = String(index + 1);
        return avatar;
    }

    function getDetailsItemIdFromHash() {
        let hash = window.location.hash || '';
        if (hash.indexOf('/details') === -1) {
            return null;
        }

        let match = hash.match(/[?&]id=([a-f0-9-]{32,36})/i);
        return match ? match[1] : null;
    }

    function parseWatchProgress(raw) {
        let watchProgress = getWatchProgress();
        if (!watchProgress) {
            return {
                played: false,
                playbackPositionTicks: 0,
                seasonIndexNumber: null,
                episodeIndexNumber: null,
                episodeRunTimeTicks: 0
            };
        }

        return watchProgress.parseProgress(raw);
    }

    function parseWatcher(raw) {
        let progress = parseWatchProgress(raw);

        return {
            Id: raw.Id || raw.id || '',
            Name: raw.Name || raw.name || '',
            name: raw.Name || raw.name || '',
            ImageUrl: raw.ImageUrl || raw.imageUrl || '',
            imageUrl: raw.ImageUrl || raw.imageUrl || '',
            played: progress.played,
            playbackPositionTicks: progress.playbackPositionTicks,
            seasonIndexNumber: progress.seasonIndexNumber,
            episodeIndexNumber: progress.episodeIndexNumber,
            episodeRunTimeTicks: progress.episodeRunTimeTicks,
            aggregate: progress.aggregate
        };
    }

    function parseItemOverlay(raw) {
        if (!raw) {
            return {
                watchers: [],
                runTimeTicks: 0,
                isSeason: false,
                isSeries: false,
                currentUser: parseWatchProgress(null)
            };
        }

        if (Array.isArray(raw)) {
            return {
                watchers: raw.map(parseWatcher),
                runTimeTicks: 0,
                isSeason: false,
                isSeries: false,
                currentUser: parseWatchProgress(null)
            };
        }

        return {
            watchers: (raw.watchers || raw.Watchers || []).map(parseWatcher),
            runTimeTicks: Number(raw.runTimeTicks || raw.RunTimeTicks || 0),
            isSeason: !!(raw.isSeason || raw.IsSeason),
            isSeries: !!(raw.isSeries || raw.IsSeries),
            currentUser: parseWatchProgress(raw.currentUser || raw.CurrentUser)
        };
    }

    function getItemOverlayFromResponse(response, itemId) {
        if (!response) {
            return parseItemOverlay(null);
        }

        return parseItemOverlay(response[normalizeGuid(itemId)] || response[itemId]);
    }

    function removeDetailBuddiesFromMount(mountPoint) {
        if (!mountPoint) {
            return;
        }

        mountPoint.querySelectorAll('.' + DETAIL_BUDDIES_CLASS).forEach(function (element) {
            element.remove();
        });
    }

    function clearAllDetailBuddiesState() {
        document.querySelectorAll('.' + DETAIL_BUDDIES_CLASS).forEach(function (element) {
            element.remove();
        });

        document.querySelectorAll('[data-wc-detail-buddies-item-id]').forEach(function (element) {
            delete element.dataset[DETAIL_MOUNT_DATA_ATTR];
        });
    }

    function isElementVisible(element) {
        if (!element) {
            return false;
        }

        if (element.offsetParent !== null) {
            return true;
        }

        return element.getClientRects().length > 0;
    }

    function findDetailMountPoint() {
        let visiblePage = document.querySelector('.page:not(.hide)');
        let candidates = [];

        if (visiblePage) {
            candidates = candidates.concat(Array.from(visiblePage.querySelectorAll(DETAIL_MOUNT_SELECTOR)));
            candidates = candidates.concat(Array.from(visiblePage.querySelectorAll(DETAIL_SECTION_SELECTOR)));
        }

        candidates = candidates.concat(Array.from(document.querySelectorAll(DETAIL_MOUNT_SELECTOR)));
        candidates = candidates.concat(Array.from(document.querySelectorAll(DETAIL_SECTION_SELECTOR)));

        for (let i = 0; i < candidates.length; i++) {
            if (isElementVisible(candidates[i])) {
                return candidates[i];
            }
        }

        return candidates.length ? candidates[candidates.length - 1] : null;
    }

    function getDetailInsertAnchor(mountPoint) {
        if (!mountPoint) {
            return null;
        }

        if (mountPoint.matches(DETAIL_MOUNT_SELECTOR)) {
            return mountPoint.querySelector(DETAIL_SECTION_SELECTOR) || mountPoint.firstChild;
        }

        return mountPoint.firstChild;
    }

    function isDetailBuddiesMounted(mountPoint, itemId) {
        if (!mountPoint) {
            return false;
        }

        return mountPoint.dataset[DETAIL_MOUNT_DATA_ATTR] === itemId
            && !!mountPoint.querySelector('.' + DETAIL_BUDDIES_CLASS);
    }

    function clearDetailBuddiesMountState(mountPoint) {
        if (!mountPoint) {
            return;
        }

        delete mountPoint.dataset[DETAIL_MOUNT_DATA_ATTR];
        removeDetailBuddiesFromMount(mountPoint);
    }

    function createDetailInitialAvatar(name) {
        let avatar = document.createElement('span');
        avatar.className = 'wc-detail-buddy-avatar wc-detail-buddy-initial';
        avatar.setAttribute('aria-hidden', 'true');
        avatar.textContent = getInitial(name);
        avatar.title = name;
        return avatar;
    }

    function createDetailBuddyAvatar(watcher) {
        let name = watcher.Name || watcher.name || '';
        let imageUrl = resolveImageUrl(watcher.ImageUrl || watcher.imageUrl);

        if (imageUrl) {
            let img = document.createElement('img');
            img.className = 'wc-detail-buddy-avatar';
            img.alt = name;
            img.title = name;
            img.src = imageUrl;
            img.addEventListener('error', function () {
                img.replaceWith(createDetailInitialAvatar(name));
            });
            return img;
        }

        return createDetailInitialAvatar(name);
    }

    function renderSharedProgressCard(overlay, requesters) {
        let card = document.createElement('div');
        card.className = 'wc-shared-progress-card';

        if (requesters.length) {
            let attribution = document.createElement('div');
            attribution.className = 'wc-request-attribution';
            let heading = document.createElement('div');
            heading.className = 'wc-request-heading';
            heading.textContent = t("Requested by");
            attribution.appendChild(heading);
            let names = document.createElement('ul');
            names.className = 'wc-request-names';
            requesters.forEach(function (requester) {
                let entry = document.createElement('li');
                entry.textContent = requester.Name || requester.name || '';
                let profileId = requester.ProfileUserId || requester.profileUserId;
                if (profileId && window.WatchCircleAssets && WatchCircleAssets.showProfiles) {
                    let link = document.createElement('button');
                    link.type = 'button';
                    link.className = 'wc-profile-name-link';
                    link.textContent = entry.textContent;
                    link.title = t("{name}'s WatchCircle profile", { name: entry.textContent });
                    link.addEventListener('click', function () { WatchCircleAssets.showProfiles(profileId); });
                    entry.replaceChildren(link);
                }
                let seasons = requester.Seasons || requester.seasons || [];
                if (overlay.isSeries && seasons.length) {
                    let detail = document.createElement('span');
                    detail.className = 'wc-request-seasons';
                    detail.textContent = ' · ' + t(seasons.length === 1 ? 'Season {seasons}' : 'Seasons {seasons}', { seasons: seasons.join(', ') });
                    entry.appendChild(detail);
                }
                names.appendChild(entry);
            });
            attribution.appendChild(names);
            card.appendChild(attribution);
        }

        if (!overlay.watchers.length) {
            return card;
        }

        let count = document.createElement('div');
        count.className = 'wc-shared-progress-count';
        count.textContent = t(overlay.watchers.length === 1 ? '1 person from your groups' : '{count} people from your groups', { count: overlay.watchers.length });
        card.appendChild(count);

        let list = document.createElement('ul');
        list.className = 'wc-shared-progress-members';
        list.setAttribute('aria-label', t("Group members' watch progress"));
        let watchProgress = getWatchProgress();
        let isEpisodeScoped = overlay.isSeason || overlay.isSeries;

        overlay.watchers.forEach(function (watcher) {
            let row = document.createElement('li');
            row.className = 'wc-shared-progress-member';
            let avatar = createDetailBuddyAvatar(watcher);
            avatar.setAttribute('aria-hidden', 'true');
            row.appendChild(avatar);

            let name = watcher.Name || watcher.name || '';
            if (watchProgress) {
                let runtime = isEpisodeScoped
                    ? watchProgress.getProgressRuntime(watcher, 0)
                    : overlay.runTimeTicks;
                let progress = watchProgress.createRow({
                    labelText: name,
                    progress: watcher,
                    runTimeTicks: runtime,
                    variant: 'them',
                    showEpisodeLine: isEpisodeScoped
                });
                row.appendChild(progress);
            } else {
                let label = document.createElement('span');
                label.textContent = name;
                row.appendChild(label);
            }

            let profileLabel = row.querySelector('.wc-detail-progress-label') || row.lastElementChild;
            if (watcher.Id && window.WatchCircleAssets && WatchCircleAssets.showProfiles) {
                let profileButton = document.createElement('button');
                profileButton.type = 'button';
                profileButton.className = 'wc-profile-name-link ' + profileLabel.className;
                profileButton.textContent = name;
                profileButton.title = t("{name}'s WatchCircle profile", { name: name });
                profileButton.addEventListener('click', function () { WatchCircleAssets.showProfiles(watcher.Id); });
                profileLabel.replaceWith(profileButton);
                let avatarButton = document.createElement('button');
                avatarButton.type = 'button';
                avatarButton.className = 'wc-profile-avatar-link';
                avatarButton.setAttribute('aria-label', profileButton.title);
                avatar.replaceWith(avatarButton);
                avatarButton.appendChild(avatar);
                avatarButton.addEventListener('click', function () { WatchCircleAssets.showProfiles(watcher.Id); });
            }

            list.appendChild(row);
        });

        card.appendChild(list);
        return card;
    }

    function renderDetailBuddiesSection(mountPoint, itemId, overlay) {
        removeDetailBuddiesFromMount(mountPoint);
        delete mountPoint.dataset[DETAIL_MOUNT_DATA_ATTR];

        overlay = parseItemOverlay(overlay);
        let requesters = requesterCache.get(normalizeGuid(itemId)) || [];

        if (!overlay.watchers.length && !requesters.length) {
            return false;
        }

        let section = document.createElement('div');
        section.className = DETAIL_BUDDIES_CLASS + ' verticalSection detailVerticalSection';

        if (overlay.isSeason) {
            section.classList.add('wc-detail-buddies-season');
        }

        if (overlay.isSeries) {
            section.classList.add('wc-detail-buddies-series');
        }

        let title = document.createElement('h2');
        title.className = 'sectionTitle';
        title.textContent = 'WatchCircle';
        section.appendChild(title);

        section.appendChild(renderSharedProgressCard(overlay, requesters));

        let insertAnchor = getDetailInsertAnchor(mountPoint);
        if (insertAnchor) {
            mountPoint.insertBefore(section, insertAnchor);
        } else {
            mountPoint.appendChild(section);
        }

        mountPoint.dataset[DETAIL_MOUNT_DATA_ATTR] = itemId;
        return true;
    }

    function tryRenderDetailBuddies() {
        let itemId = getDetailsItemIdFromHash();
        if (!itemId) {
            return true;
        }

        queueDetailRequestersFetch(itemId);

        let mountPoint = findDetailMountPoint();
        if (!mountPoint) {
            return false;
        }

        if (isDetailBuddiesMounted(mountPoint, itemId)) {
            return true;
        }

        if (mountPoint.dataset[DETAIL_MOUNT_DATA_ATTR]
            && mountPoint.dataset[DETAIL_MOUNT_DATA_ATTR] !== itemId) {
            clearDetailBuddiesMountState(mountPoint);
        } else if (mountPoint.dataset[DETAIL_MOUNT_DATA_ATTR] === itemId) {
            delete mountPoint.dataset[DETAIL_MOUNT_DATA_ATTR];
        }

        let cached = overlayCache.get(normalizeGuid(itemId));
        if (cached) {
            renderDetailBuddiesSection(mountPoint, itemId, cached);
            return true;
        }

        if (!pendingItemIds.has(itemId)) {
            queueDetailBuddiesFetch(itemId);
        }

        return false;
    }

    function queueDetailRequestersFetch(itemId) {
        let key = normalizeGuid(itemId);
        if (requesterCache.has(key) || requesterPending.has(key) || !ApiClient.getCurrentUserId || !ApiClient.getCurrentUserId()) {
            return;
        }
        let token = {};
        requesterPending.set(key, token);
        ApiClient.ajax({ type: 'GET', url: ApiClient.getUrl('WatchCircle/Seerr/Requests/' + encodeURIComponent(itemId)), dataType: 'json' })
            .catch(function () { return []; })
            .then(function (requesters) {
                if (requesterPending.get(key) !== token) { return; }
                requesterPending.delete(key);
                requesterCache.set(key, Array.isArray(requesters) ? requesters : []);
                if (normalizeGuid(getDetailsItemIdFromHash()) !== key) { return; }
                let mountPoint = findDetailMountPoint();
                if (mountPoint) { clearDetailBuddiesMountState(mountPoint); }
                scheduleDetailBuddiesRetry(true);
            });
    }

    function scheduleDetailBuddiesRetry(resetAttempts) {
        if (resetAttempts) {
            if (detailRetryTimer) {
                clearTimeout(detailRetryTimer);
                detailRetryTimer = null;
            }
        } else if (detailRetryTimer) {
            return;
        }

        let attempts = 0;

        function tick() {
            detailRetryTimer = null;
            attempts++;

            if (!getDetailsItemIdFromHash()) {
                return;
            }

            if (tryRenderDetailBuddies()) {
                return;
            }

            if (attempts < DETAIL_RETRY_MAX) {
                detailRetryTimer = setTimeout(tick, DETAIL_RETRY_MS);
            }
        }

        detailRetryTimer = setTimeout(tick, 0);
    }

    function scheduleDetailBuddiesScan() {
        if (detailScanTimer) {
            clearTimeout(detailScanTimer);
        }

        detailScanTimer = setTimeout(function () {
            detailScanTimer = null;
            if (getDetailsItemIdFromHash()) {
                scheduleDetailBuddiesRetry(true);
            }
        }, 80);
    }

    function scanDetailPage() {
        if (!getDetailsItemIdFromHash()) {
            return;
        }

        scheduleDetailBuddiesRetry(true);
    }

    function queueDetailBuddiesFetch(itemId) {
        if (!itemId) {
            return;
        }

        queueFetch(itemId);
    }

    function queueFetch(itemId) {
        if (!itemId) {
            return;
        }

        pendingItemIds.add(itemId);

        if (fetchTimer) {
            clearTimeout(fetchTimer);
        }

        fetchTimer = setTimeout(function () {
            fetchTimer = null;
            fetchPendingOverlays();
        }, 120);
    }

    function applyOverlayResponse(response) {
        Object.keys(response || {}).forEach(function (key) {
            overlayCache.set(normalizeGuid(key), parseItemOverlay(response[key]));
        });
    }

    function fetchOverlaysForItemIds(itemIds) {
        if (!itemIds.length || !ApiClient.getCurrentUserId || !ApiClient.getCurrentUserId()) {
            return Promise.resolve({});
        }

        let query = itemIds.map(function (itemId) {
            return 'itemIds=' + encodeURIComponent(itemId);
        }).join('&');

        return ApiClient.ajax({
            type: 'GET',
            url: ApiClient.getUrl('WatchCircle/Overlays?' + query),
            dataType: 'json'
        });
    }

    function fetchPendingOverlays() {
        let generation = overlayFetchGeneration;
        let itemIds = Array.from(pendingItemIds);
        pendingItemIds.clear();

        if (!itemIds.length || !ApiClient.getCurrentUserId || !ApiClient.getCurrentUserId()) {
            return;
        }

        fetchOverlaysForItemIds(itemIds).then(function (response) {
            if (generation !== overlayFetchGeneration) {
                return;
            }

            applyOverlayResponse(response);

            document.querySelectorAll(CARD_SELECTOR).forEach(function (container) {
                if (!isPosterMount(container)) {
                    return;
                }

                let itemId = extractItemId(container);
                if (!itemId) {
                    return;
                }

                let overlay = overlayCache.get(normalizeGuid(itemId));
                if (overlay) {
                    renderOverlay(getMountPoint(container), overlay.watchers);
                }
            });

            scheduleDetailBuddiesRetry(true);
        });
    }

    function refreshDetailBuddies(itemIds) {
        let detailItemId = getDetailsItemIdFromHash();
        if (!detailItemId) {
            return Promise.resolve();
        }

        if (detailRetryTimer) {
            clearTimeout(detailRetryTimer);
            detailRetryTimer = null;
        }

        clearAllDetailBuddiesState();

        requesterCache.delete(normalizeGuid(detailItemId));
        requesterPending.delete(normalizeGuid(detailItemId));
        queueDetailRequestersFetch(detailItemId);

        let fetchIds = [detailItemId];
        (itemIds || []).forEach(function (id) {
            if (id && normalizeGuid(id) !== normalizeGuid(detailItemId)) {
                fetchIds.push(id);
            }
        });

        fetchIds.forEach(function (id) {
            overlayCache.delete(normalizeGuid(id));
        });

        let generation = overlayFetchGeneration;

        return fetchOverlaysForItemIds(fetchIds).then(function (response) {
            if (generation !== overlayFetchGeneration) {
                return;
            }

            applyOverlayResponse(response);

            let mountPoint = findDetailMountPoint();
            if (!mountPoint) {
                scheduleDetailBuddiesRetry(true);
                return;
            }

            let currentDetailId = getDetailsItemIdFromHash();
            if (!currentDetailId || normalizeGuid(currentDetailId) !== normalizeGuid(detailItemId)) {
                return;
            }

            let overlay = overlayCache.get(normalizeGuid(detailItemId));
            if (!overlay) {
                return;
            }

            renderDetailBuddiesSection(mountPoint, detailItemId, overlay);
        }).catch(function (err) {
            console.warn('[WatchCircle] Failed to refresh detail buddies.', err);
            scheduleDetailBuddiesRetry(true);
        });
    }

    function isPosterMount(container) {
        return !!container && !container.closest('.cardOverlayContainer');
    }

    function processContainer(container) {
        if (!isPosterMount(container)) {
            return;
        }

        let itemId = extractItemId(container);
        if (!itemId) {
            return;
        }

        let mount = getMountPoint(container);
        if (mount.querySelector('.' + OVERLAY_CLASS)) {
            return;
        }

        let cached = overlayCache.get(normalizeGuid(itemId));
        if (cached) {
            if (cached.watchers && cached.watchers.length) {
                renderOverlay(mount, cached.watchers);
            }

            return;
        }

        if (container.dataset.wcWatcherProcessed === 'true') {
            return;
        }

        container.dataset.wcWatcherProcessed = 'true';
        queueFetch(itemId);
    }

    function scanCards(root) {
        (root || document).querySelectorAll(CARD_SELECTOR).forEach(processContainer);
    }

    function setupObserver() {
        if (observer) {
            return;
        }

        observer = new MutationObserver(function () {
            scanCards(document);
            scheduleDetailBuddiesScan();
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    scanCards(document);
    scanDetailPage();
    setupObserver();
    document.addEventListener('viewshow', function () {
        scanCards(document);
        scanDetailPage();
    });
    window.addEventListener('hashchange', function () {
        requesterCache.clear();
        requesterPending.clear();
        clearAllDetailBuddiesState();
        scheduleDetailBuddiesRetry(true);
    });

    function invalidateOverlayCache(itemIds) {
        if (!itemIds || !itemIds.length) {
            overlayCache.clear();
            return;
        }

        itemIds.forEach(function (itemId) {
            overlayCache.delete(normalizeGuid(itemId));
        });
    }

    function clearProcessedCardMarkers() {
        document.querySelectorAll(CARD_SELECTOR).forEach(function (container) {
            delete container.dataset.wcWatcherProcessed;
        });
    }

    function refreshOverlays(itemIds) {
        overlayFetchGeneration++;

        invalidateOverlayCache(itemIds);

        let detailItemId = getDetailsItemIdFromHash();
        if (detailItemId) {
            overlayCache.delete(normalizeGuid(detailItemId));
        }

        clearProcessedCardMarkers();
        clearAllDetailBuddiesState();
        scanCards(document);

        (itemIds || []).forEach(function (id) {
            queueFetch(id);
        });

        if (detailItemId) {
            queueFetch(detailItemId);
        }

        let detailRefresh = refreshDetailBuddies(itemIds);

        scheduleDetailBuddiesRetry(true);

        return detailRefresh;
    }

    window.WatchCircleOverlays = {
        refresh: refreshOverlays,
        refreshDetailBuddies: refreshDetailBuddies
    };
    document.addEventListener('watchcirclelanguagechange', function () {
        clearAllDetailBuddiesState();
        scanDetailPage();
    });
})();
