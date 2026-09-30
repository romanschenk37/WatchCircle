(function () {
    'use strict';
    function t(key, values) { return WatchCircleI18n.t(key, values); }

    if (window.WatchCircleWatchTogetherDialog) {
        return;
    }

    let STYLES_ID = 'watchcircle-watch-together-dialog-styles';
    let DEFAULT_DIALOG_WIDTH = 672;
    let CONTINUE_BUTTON_ID = 'continue';

    function defaults() { return {
        title: t("WatchCircle: Watch together"),
        text: t("Currently watching media with your buddies on this device ?<br> Select who is watching with you to sync their progress."),
        stillWatchingText: t("Still watching media with your buddies on this device ?<br> Select who is watching with you to sync their progress."),
        buttons: [{ id: CONTINUE_BUTTON_ID, name: t("Continue"), type: 'submit' }],
        maxWidth: DEFAULT_DIALOG_WIDTH,
        emptyBuddyTitle: t("No buddies found"),
        emptyBuddyMessage: ''
    }; }

    function resolveDescription(options) {
        if (options.html) {
            return { html: options.html };
        }

        if (options.stillWatching) {
            return { text: options.stillWatchingText || defaults().stillWatchingText };
        }

        if (options.text) {
            return { text: options.text };
        }

        return { text: defaults().text };
    }

    function getAssetUrl(path) {
        if (window.WatchCircleAssets) {
            return WatchCircleAssets.getUrl(path);
        }

        if (typeof ApiClient !== 'undefined' && ApiClient.getUrl) {
            return ApiClient.getUrl('WatchCircle/js/' + path);
        }

        return '/WatchCircle/js/' + path;
    }

    function ensureStyles() {
        if (document.getElementById(STYLES_ID)) {
            return;
        }

        let link = document.createElement('link');
        link.id = STYLES_ID;
        link.rel = 'stylesheet';
        link.href = getAssetUrl('components/watchTogether/watchTogetherDialog.css');
        document.head.appendChild(link);
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

    function ensureDialogModule() {
        return loadScriptModule(
            'watchcircle-dialog-script',
            'components/dialog/dialog.js',
            function () { return window.WatchCircleDialog; }
        ).then(function () {
            WatchCircleDialog.ensureStyles();
        });
    }

    function ensureUserSelectModule() {
        return loadScriptModule(
            'watchcircle-user-select-script',
            'components/userSelect/userSelect.js',
            function () { return window.WatchCircleUserSelect; }
        ).then(function () {
            WatchCircleUserSelect.ensureStyles();
        });
    }

    function ensureWatchTogetherSessionModule() {
        return loadScriptModule(
            'watchcircle-watch-together-session-script',
            'services/watchTogetherSession.js',
            function () { return window.WatchCircleWatchTogetherSession; }
        );
    }

    function renderBuddyList(container, buddies, options) {
        let scroll = document.createElement('div');
        scroll.className = 'wc-watch-together-user-scroll';

        let list = document.createElement('div');
        list.className = 'wc-users-list checkboxListContainer';

        scroll.appendChild(list);
        container.appendChild(scroll);

        WatchCircleUserSelect.render(list, buddies, {
            selectedUserIds: options && options.selectedUserIds,
            isSelected: options && options.isSelected,
            onChange: options && options.onChange,
            emptyTitle: (options && options.emptyTitle) || defaults().emptyBuddyTitle,
            emptyMessage: (options && options.emptyMessage !== undefined)
                ? options.emptyMessage
                : defaults().emptyBuddyMessage
        });

        return list;
    }

    function resolveRequireContinue(options) {
        return !!(options.requireContinue || options.stillWatching);
    }

    function buildDialogOptions(buddies, merged, buddyListElementRef) {
        let storedSelection = WatchCircleWatchTogetherSession.getSelectedUserIds();
        let initialSelection = merged.selectedUserIds || WatchCircleWatchTogetherSession.filterToKnownBuddies(storedSelection, buddies);
        let description = resolveDescription(merged);

        return {
            title: merged.title,
            text: description.text,
            html: description.html,
            buttons: merged.buttons,
            maxWidth: merged.maxWidth,
            size: merged.size,
            requireContinue: resolveRequireContinue(merged),
            renderContent: function (container) {
                buddyListElementRef.current = renderBuddyList(container, buddies, {
                    selectedUserIds: initialSelection,
                    emptyTitle: merged.emptyBuddyTitle,
                    emptyMessage: merged.emptyBuddyMessage
                });
            }
        };
    }

    function show(options) {
        ensureStyles();

        let merged = Object.assign({}, defaults(), options || {});
        let buddyListElementRef = { current: null };

        return Promise.all([
            ensureDialogModule(),
            ensureUserSelectModule(),
            ensureWatchTogetherSessionModule()
        ])
            .then(function () {
                return WatchCircleUserSelect.loadBuddies();
            })
            .then(function (buddies) {
                let dialogOptions = buildDialogOptions(buddies, merged, buddyListElementRef);

                return WatchCircleDialog.show(dialogOptions).then(function (result) {
                    let buddyListElement = buddyListElementRef.current;

                    if (result === CONTINUE_BUTTON_ID && buddyListElement) {
                        let selectedUserIds = WatchCircleUserSelect.getSelectedUserIds(buddyListElement);
                        WatchCircleWatchTogetherSession.setSelectedUserIds(selectedUserIds);
                    }

                    return {
                        action: result,
                        selectedUserIds: result === CONTINUE_BUTTON_ID && buddyListElement
                            ? WatchCircleUserSelect.getSelectedUserIds(buddyListElement)
                            : WatchCircleWatchTogetherSession.getSelectedUserIds()
                    };
                });
            });
    }

    window.WatchCircleWatchTogetherDialog = {
        show: show,
        ensureStyles: ensureStyles,
        defaults: defaults()
    };
})();
