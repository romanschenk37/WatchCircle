(function () {
    'use strict';
    function t(key, values) { return WatchCircleI18n.t(key, values); }

    if (window.__watchCircleNavbarBootstrapped) {
        return;
    }

    window.__watchCircleNavbarBootstrapped = true;

    let NAVBAR_BUTTON_CLASS = 'wc-navbar-button';
    let HEADER_RENDERED_EVENT = 'HEADER_RENDERED';
    let BUDDY_ICON_SVG = '<svg class="wc-navbar-icon" xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24" aria-hidden="true"><path d="M0 0h24v24H0z" fill="none"></path><path fill="currentColor" fill-rule="evenodd" d="M8.905 4.25h6.19c.838 0 1.372 0 1.832.091a4.75 4.75 0 0 1 3.732 3.732l-.736.147l.736-.147c.07.35.086.743.09 1.28A2.75 2.75 0 0 1 22.75 12v2.444c0 1.53-.798 2.874-2 3.637V19a.75.75 0 0 1-1.5 0v-.325q-.392.075-.806.075H5.556q-.414 0-.806-.075V19a.75.75 0 0 1-1.5 0v-.919a4.3 4.3 0 0 1-2-3.636V12c0-1.26.846-2.32 2.001-2.647c.004-.537.02-.93.09-1.28a4.75 4.75 0 0 1 3.732-3.732c.46-.091.994-.091 1.832-.091M4.752 9.354A2.75 2.75 0 0 1 6.75 12v1.2c0 .028.022.05.05.05h10.4a.05.05 0 0 0 .05-.05V12c0-1.258.845-2.319 1.998-2.646c-.004-.51-.017-.77-.06-.988a3.25 3.25 0 0 0-2.554-2.554c-.296-.058-.669-.062-1.634-.062H9c-.965 0-1.338.004-1.634.062a3.25 3.25 0 0 0-2.554 2.554c-.043.218-.056.479-.06.988M4 10.75c-.69 0-1.25.56-1.25 1.25v2.444a2.806 2.806 0 0 0 2.806 2.806h12.888a2.806 2.806 0 0 0 2.806-2.806V12a1.25 1.25 0 0 0-2.5 0v1.2a1.55 1.55 0 0 1-1.55 1.55H6.8a1.55 1.55 0 0 1-1.55-1.55V12c0-.69-.56-1.25-1.25-1.25" clip-rule="evenodd"></path></svg>';

    function getAssetUrl(path) {
        if (window.WatchCircleAssets) {
            return WatchCircleAssets.getUrl(path);
        }

        if (typeof ApiClient !== 'undefined' && ApiClient.getUrl) {
            return ApiClient.getUrl('WatchCircle/js/' + path);
        }

        return '/WatchCircle/js/' + path;
    }

    function loadStylesheet() {
        if (document.getElementById('watchcircle-navbar-styles')) {
            return;
        }

        let link = document.createElement('link');
        link.id = 'watchcircle-navbar-styles';
        link.rel = 'stylesheet';
        link.href = getAssetUrl('components/navbar/navbar.css');
        document.head.appendChild(link);
    }

    function loadWatchTogetherDialogModule(callback) {
        if (window.WatchCircleWatchTogetherDialog) {
            callback();
            return;
        }

        let existing = document.getElementById('watchcircle-watch-together-dialog-script');
        if (existing) {
            existing.addEventListener('load', callback, { once: true });
            return;
        }

        let script = document.createElement('script');
        script.id = 'watchcircle-watch-together-dialog-script';
        script.src = getAssetUrl('components/watchTogether/watchTogetherDialog.js');
        script.addEventListener('load', callback, { once: true });
        document.head.appendChild(script);
    }

    function createNavbarButton() {
        let button = document.createElement('button');
        button.type = 'button';
        button.setAttribute('is', 'paper-icon-button-light');
        button.className = 'headerButton headerButtonRight paper-icon-button-light ' + NAVBAR_BUTTON_CLASS;
        button.title = t('WatchCircle: Watch together');
        button.innerHTML = BUDDY_ICON_SVG;
        button.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();

            loadWatchTogetherDialogModule(function () {
                if (window.WatchCircleWatchTogetherDialog) {
                    window.WatchCircleWatchTogetherDialog.show();
                }
            });
        });
        return button;
    }

    function createProfilesButton() {
        const button = document.createElement('button');
        button.type = 'button';
        button.setAttribute('is', 'paper-icon-button-light');
        button.className = 'headerButton headerButtonRight paper-icon-button-light wc-profiles-navbar-button';
        button.innerHTML = '<svg class="wc-navbar-icon" viewBox="0 0 24 24" aria-hidden="true"><path fill="currentColor" d="M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-.32 0-.63.05-.91.14a4.97 4.97 0 010 5.72c.28.09.59.14.91.14zM8 11a3 3 0 100-6 3 3 0 000 6zm8 2c-.29 0-.62.02-.97.05C16.19 13.89 17 15.02 17 16.5V19h6v-2.5c0-2.33-4.67-3.5-7-3.5zM8 13c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5z"/></svg><span class="wc-profiles-navbar-label">WatchCircle</span>';
        button.addEventListener('click', function (event) {
            event.preventDefault(); event.stopPropagation(); WatchCircleAssets.showProfiles();
        });
        return button;
    }

    function ensureNavbarButton() {
        const authenticated = typeof ApiClient !== 'undefined' && ApiClient.getCurrentUserId && ApiClient.getCurrentUserId();
        document.querySelectorAll('.skinHeader .headerRight').forEach(function (header) {
            if (!authenticated) {
                header.querySelectorAll('.wc-navbar-button, .wc-profiles-navbar-button').forEach(function (node) { node.remove(); });
                return;
            }
            let together = header.querySelector('.wc-navbar-button');
            if (!together) { together = createNavbarButton(); header.insertBefore(together, header.firstElementChild); }
            let profiles = header.querySelector('.wc-profiles-navbar-button');
            if (!profiles) { profiles = createProfilesButton(); header.insertBefore(profiles, together); }
            together.title = t('WatchCircle: Watch together');
            together.setAttribute('aria-label', together.title);
            profiles.title = t('WatchCircle: People & progress');
            profiles.setAttribute('aria-label', profiles.title);
        });
    }

    let scheduled = false;
    function scheduleUpdate() {
        if (scheduled) return;
        scheduled = true;
        setTimeout(function () { scheduled = false; ensureNavbarButton(); }, 0);
    }
    // Jellyfin can replace the header after authentication or route changes.
    // Observe child changes only: setting our own titles cannot trigger a loop.
    new MutationObserver(scheduleUpdate).observe(document.documentElement, { childList: true, subtree: true });
    new MutationObserver(scheduleUpdate).observe(document.documentElement, { attributes: true, attributeFilter: ['lang'] });
    document.addEventListener(HEADER_RENDERED_EVENT, scheduleUpdate);
    document.addEventListener('viewshow', scheduleUpdate);
    window.addEventListener('hashchange', scheduleUpdate);
    // Covers auth changes that do not replace DOM nodes, including late login.
    setInterval(function () { if (!document.hidden) scheduleUpdate(); }, 2000);
    loadStylesheet();
    scheduleUpdate();
})();
