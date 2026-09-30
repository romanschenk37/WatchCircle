(function () {
    'use strict';

    if (window.WatchCircleDialog) {
        return;
    }

    let FALLBACK_STYLES_ID = 'watchcircle-dialog-styles';

    function escapeHtml(value) {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function escapeAttr(value) {
        return escapeHtml(value).replace(/`/g, '&#96;');
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

    function getJellyfinDialogHelper() {
        return typeof Dashboard !== 'undefined' && Dashboard.dialogHelper
            ? Dashboard.dialogHelper
            : null;
    }

    function ensureStyles() {
        if (document.getElementById(FALLBACK_STYLES_ID)) {
            return;
        }

        let link = document.createElement('link');
        link.id = FALLBACK_STYLES_ID;
        link.rel = 'stylesheet';
        link.href = getAssetUrl('components/dialog/dialog.css');
        document.head.appendChild(link);
    }

    function normalizeButtons(buttons) {
        return buttons && buttons.length ? buttons : [];
    }

    function buildButtonsHtml(buttons) {
        let html = '';

        for (let i = 0; i < buttons.length; i++) {
            let button = buttons[i];
            let autoFocus = i === 0 ? ' autofocus' : '';
            let buttonClass = 'btnOption raised formDialogFooterItem formDialogFooterItem-autosize';

            if (button.type) {
                buttonClass += ' button-' + button.type;
            }

            html += '<button is="emby-button" type="button" class="' + buttonClass + '" data-id="' + escapeAttr(button.id) + '"' + autoFocus + '><span>' + escapeHtml(button.name) + '</span></button>';
        }

        return html;
    }

    function appendCustomContent(container, options) {
        if (!container) {
            return;
        }

        if (typeof options.renderContent === 'function') {
            options.renderContent(container);
            return;
        }

        if (options.content instanceof HTMLElement) {
            container.appendChild(options.content);
        }
    }

    function resolveDialogWidth(buttonCount, options) {
        if (options.maxWidth) {
            return Math.min(options.maxWidth, window.innerWidth - 50);
        }

        if (options.preferredWidth) {
            return Math.min(options.preferredWidth, window.innerWidth - 50);
        }

        let calculated = Math.min((buttonCount * 150) + 200, window.innerWidth - 50);
        return Math.max(calculated, 320);
    }

    function isRequireContinue(options) {
        return !!(options && options.requireContinue);
    }

    function applyRequireContinueGuards(dlg, helper, options) {
        if (!isRequireContinue(options)) {
            return;
        }

        dlg.__watchCircleAllowClose = false;
        dlg.setAttribute('data-wc-require-continue', 'true');

        let originalClose = helper.close.bind(helper);
        helper.close = function (targetDlg) {
            if (targetDlg === dlg && !dlg.__watchCircleAllowClose) {
                return Promise.resolve();
            }

            if (targetDlg === dlg) {
                helper.close = originalClose;
            }

            return originalClose(targetDlg);
        };

        dlg.addEventListener('open', function () {
            if (dlg.backdrop && dlg.backdrop.parentNode) {
                let cleanBackdrop = dlg.backdrop.cloneNode(false);
                cleanBackdrop.className = dlg.backdrop.className;
                void cleanBackdrop.offsetWidth;
                cleanBackdrop.classList.add('dialogBackdropOpened');
                dlg.backdrop.parentNode.replaceChild(cleanBackdrop, dlg.backdrop);
                dlg.backdrop = cleanBackdrop;
            }

            if (dlg.dialogContainer) {
                dlg.dialogContainer.addEventListener('click', function (event) {
                    if (!dlg.__watchCircleAllowClose && event.target === dlg.dialogContainer) {
                        event.preventDefault();
                        event.stopPropagation();
                        event.stopImmediatePropagation();
                    }
                }, true);
            }
        }, { once: true });

        dlg.addEventListener('keydown', function (event) {
            if (!dlg.__watchCircleAllowClose && event.key === 'Escape') {
                event.stopPropagation();
                event.stopImmediatePropagation();
                event.preventDefault();
            }
        }, true);
    }

    function showJellyfinDialog(options) {
        ensureStyles();

        let helper = getJellyfinDialogHelper();
        let buttons = normalizeButtons(options.buttons);
        let dialogOptions = {
            removeOnClose: true,
            scrollY: false
        };

        if (isRequireContinue(options)) {
            dialogOptions.enableHistory = false;
        }

        if (options.size) {
            dialogOptions.size = options.size;
        }

        let dlg = helper.createDialog(dialogOptions);
        let title = options.title || '';
        let bodyHtml = options.html || options.text || options.message || '';
        let hasCustomContent = typeof options.renderContent === 'function' || options.content instanceof HTMLElement;

        dlg.classList.add('formDialog');
        dlg.classList.add('align-items-center');
        dlg.classList.add('justify-content-center');
        dlg.classList.add('dialog-fullscreen-lowres');
        dlg.classList.add('wc-dialog');

        if (hasCustomContent) {
            dlg.classList.add('wc-dialog-has-custom');
        }

        dlg.innerHTML =
            '<div class="formDialogContent wc-form-dialog-content' + (hasCustomContent ? '' : ' no-grow') + '">' +
                '<div class="formDialogHeader">' +
                    (title ? '<h3 class="formDialogHeaderTitle">' + escapeHtml(title) + '</h3>' : '<h3 class="formDialogHeaderTitle hide"></h3>') +
                '</div>' +
                '<div class="dialogContentInner scrollContainer">' +
                    '<div class="text' + (bodyHtml ? '' : ' hide') + '">' + bodyHtml + '</div>' +
                    (hasCustomContent ? '<div class="wc-dialog-custom"></div>' : '') +
                '</div>' +
                (buttons.length ? '<div class="formDialogFooter' + (hasCustomContent ? ' formDialogFooter-flex' : '') + '">' + buildButtonsHtml(buttons) + '</div>' : '') +
            '</div>';

        let formDialogContent = dlg.querySelector('.formDialogContent');
        if (formDialogContent) {
            let maxWidth = resolveDialogWidth(buttons.length, options);
            formDialogContent.style.maxWidth = maxWidth + 'px';
            formDialogContent.style.width = 'min(100vw - 2rem, ' + maxWidth + 'px)';
        }

        appendCustomContent(dlg.querySelector('.wc-dialog-custom'), options);

        applyRequireContinueGuards(dlg, helper, options);

        let dialogResult;

        function onButtonClick() {
            dlg.__watchCircleAllowClose = true;
            dialogResult = this.getAttribute('data-id');
            helper.close(dlg);
        }

        let buttonElements = dlg.querySelectorAll('.btnOption');
        for (let i = 0; i < buttonElements.length; i++) {
            buttonElements[i].addEventListener('click', onButtonClick);
        }

        return helper.open(dlg).then(function () {
            return dialogResult;
        });
    }

    function showFallbackDialog(options) {
        ensureStyles();

        let buttons = normalizeButtons(options.buttons);
        let title = options.title || '';
        let bodyHtml = options.html || options.text || options.message || '';
        let hasCustomContent = typeof options.renderContent === 'function' || options.content instanceof HTMLElement;

        return new Promise(function (resolve) {
            let backdrop = document.createElement('div');
            backdrop.className = 'wc-dialog-fallback-backdrop';
            let requireContinue = isRequireContinue(options);

            let panel = document.createElement('div');
            panel.className = 'wc-dialog-fallback' + (hasCustomContent ? ' wc-dialog-has-custom' : '');
            panel.setAttribute('role', 'dialog');
            panel.setAttribute('aria-modal', 'true');
            if (title) {
                panel.setAttribute('aria-label', title);
            }

            let header = document.createElement('div');
            header.className = 'wc-dialog-fallback-header';
            header.textContent = title;

            let body = document.createElement('div');
            body.className = 'wc-dialog-fallback-body';

            if (bodyHtml) {
                let description = document.createElement('div');
                description.className = 'wc-dialog-fallback-text';
                description.innerHTML = bodyHtml;
                body.appendChild(description);
            }

            if (hasCustomContent) {
                let custom = document.createElement('div');
                custom.className = 'wc-dialog-custom';
                body.appendChild(custom);
                appendCustomContent(custom, options);
            }

            let footer = document.createElement('div');
            footer.className = 'wc-dialog-fallback-footer';

            function closeDialog(result) {
                document.removeEventListener('keydown', onKeyDown);
                backdrop.remove();
                resolve(result);
            }

            function onKeyDown(event) {
                if (requireContinue) {
                    return;
                }

                if (event.key === 'Escape') {
                    event.preventDefault();
                    closeDialog();
                }
            }

            for (let i = 0; i < buttons.length; i++) {
                let button = buttons[i];
                let buttonEl = document.createElement('button');
                buttonEl.type = 'button';
                buttonEl.className = 'wc-dialog-fallback-button';
                if (button.type === 'submit') {
                    buttonEl.classList.add('wc-dialog-fallback-button-primary');
                }
                buttonEl.textContent = button.name;
                buttonEl.addEventListener('click', function () {
                    closeDialog(button.id);
                });
                footer.appendChild(buttonEl);
            }

            backdrop.addEventListener('click', function (event) {
                if (requireContinue) {
                    return;
                }

                if (event.target === backdrop) {
                    closeDialog();
                }
            });

            if (title) {
                panel.appendChild(header);
            }

            panel.appendChild(body);

            if (buttons.length) {
                panel.appendChild(footer);
            }

            backdrop.appendChild(panel);
            document.body.appendChild(backdrop);
            document.addEventListener('keydown', onKeyDown);

            let focusTarget = footer.querySelector('button');
            if (focusTarget) {
                focusTarget.focus();
            }
        });
    }

    function show(options) {
        if (typeof options === 'string') {
            options = { text: options };
        }

        options = options || {};

        if (getJellyfinDialogHelper()) {
            return showJellyfinDialog(options);
        }

        return showFallbackDialog(options);
    }

    window.WatchCircleDialog = {
        show: show,
        ensureStyles: ensureStyles
    };
})();
