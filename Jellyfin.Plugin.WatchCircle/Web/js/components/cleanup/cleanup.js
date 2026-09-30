(function () {
    'use strict';
    if (window.WatchCircleCleanup) return;
    const translations = {
        'Library cleanup': 'Bibliothek aufräumen', 'Close': 'Schliessen', 'Back': 'Zurück', 'Settings': 'Einstellungen',
        'Administration': 'Verwaltung', 'Your feedback': 'Deine Rückmeldungen', 'Loading…': 'Wird geladen …',
        'Scheduled for deletion': 'Zur Löschung vorgemerkt', 'Earliest deletion: {date}': 'Löschung frühestens: {date}',
        'Please keep': 'Bitte noch nicht löschen', 'I do not mind': 'Mir doch egal', 'No open feedback': 'Keine offenen Rückmeldungen',
        'Your answer: {answer} · {date}': 'Deine Antwort: {answer} · {date}', 'Deletion requested; verification pending': 'Löschung angefordert; Bestätigung steht aus',
        'Keeps the whole series.': 'Behält die ganze Serie.', 'Also keeps the movies in its collections.': 'Behält auch die Filme seiner Sammlungen.',
        'Feedback does not shorten the warning period. You can change your answer on the title page.': 'Eine Rückmeldung verkürzt die Vorwarnfrist nicht. Auf der Titelseite kannst du deine Antwort ändern.',
        'Could not load or save. Please refresh and try again.': 'Laden oder Speichern fehlgeschlagen. Bitte aktualisieren und erneut versuchen.',
        'Refresh': 'Aktualisieren', 'Technical details': 'Technische Details', 'No entries': 'Keine Einträge',
        'Deletion date reached': 'Löschdatum erreicht', 'Upcoming deletions': 'Bevorstehende Löschungen',
        'Kept on request': 'Auf Wunsch behalten', 'Permanently protected': 'Dauerhaft behalten', 'Deleted': 'Gelöscht',
        'Failed or incomplete': 'Fehlgeschlagen oder unvollständig', 'All titles': 'Alle Titel', 'Evaluate now': 'Jetzt auswerten',
        'Inactive': 'Deaktiviert', 'Manual deletion': 'Manuelle Löschung', 'Automatic deletion': 'Automatische Löschung',
        'Last interaction': 'Letzte Interaktion', 'First observed': 'Beginn der Startfrist', 'Nominated': 'Vorgemerkt seit',
        'Estimated size': 'Geschätzter Speicherbedarf', 'Collections': 'Sammlungen', 'Keep permanently': 'Dauerhaft behalten',
        'Series': 'Serien', 'Movies': 'Filme', 'Largest titles first': 'Grösste Inhalte zuerst', 'Requested by': 'Angefragt von',
        'Unavailable': 'Nicht verfügbar', 'No linked Jellyfin account': 'Kein verknüpftes Jellyfin-Konto',
        'Remove protection': 'Dauerhaften Schutz aufheben', 'Protected by a collection': 'Durch eine Sammlung geschützt',
        'Review deletion': 'Löschung prüfen', 'Open title': 'Titel öffnen', 'Mapping': 'Zuordnung', 'Replies': 'Rückmeldungen',
        'No reply yet': 'Noch keine Rückmeldung', 'Requested in Seerr': 'In Seerr angefragt', 'Not started': 'Noch nicht begonnen',
        'Unknown': 'Unbekannt', 'Playback': 'Wiedergabe', 'WatchedStatus': 'Gesehen-Status geändert', 'FavoriteAdded': 'Favorit hinzugefügt',
        'No current progress data': 'Keine aktuellen Fortschrittsdaten', 'Movies in this collection': 'Filme in dieser Sammlung',
        'Keep': 'Bitte noch nicht löschen', 'Indifferent': 'Mir doch egal', 'ObservationStart': 'Startfrist',
        'Confirm deletion': 'Löschung bestätigen', 'Delete listed titles and files': 'Aufgeführte Titel und Dateien löschen',
        'These titles and their media files will be removed through Radarr/Sonarr.': 'Diese Titel und ihre Mediendateien werden über Radarr/Sonarr entfernt.',
        'Whole series, including all episodes. Sonarr will also stop monitoring future episodes.': 'Ganze Serie einschliesslich aller Folgen. Sonarr überwacht danach auch zukünftige Folgen nicht mehr.',
        'Movie and all managed media files.': 'Film und alle verwalteten Mediendateien.',
        'Deadlines, protection and activity are checked again before deletion.': 'Fristen, Schutz und Aktivität werden vor der Löschung erneut geprüft.',
        'Enable library cleanup': 'Bibliothek aufräumen aktivieren', 'Months without interaction': 'Monate ohne Interaktion',
        'Warning period in days': 'Vorwarnfrist in Tagen',
        'Run evaluations and automatic deletions or edit their schedules under Dashboard → Scheduled Tasks → WatchCircle.': 'Auswertungen und automatische Löschungen starten oder ihre Zeitpläne ändern: Dashboard → Geplante Aufgaben → WatchCircle.',
        'Libraries': 'Mediatheken', 'Delete eligible titles automatically': 'Berechtigte Titel automatisch löschen',
        'When enabled, due titles and files are deleted without individual confirmation. Entire series are removed from Sonarr, including future monitoring.': 'Wenn aktiviert, werden fällige Titel und Dateien ohne einzelne Bestätigung gelöscht. Ganze Serien werden aus Sonarr entfernt, einschliesslich der Überwachung zukünftiger Folgen.',
        'Unknown history starts with a full inactivity period. Existing favorites alone do not restart it.': 'Bei unbekannter Vorgeschichte beginnt eine vollständige Inaktivitätsfrist. Bereits vorhandene Favoriten allein starten sie nicht neu.',
        'URL': 'URL', 'API key': 'API-Schlüssel', 'A key is saved. Leave blank to keep it.': 'Ein Schlüssel ist gespeichert. Leer lassen, um ihn zu behalten.',
        'No key saved.': 'Kein Schlüssel gespeichert.', 'Remove saved API key': 'Gespeicherten API-Schlüssel entfernen',
        'Exclude deleted titles from import lists': 'Gelöschte Titel aus Importlisten ausschliessen',
        'Path mappings': 'Pfadzuordnungen', 'Jellyfin root': 'Stammordner in Jellyfin', 'Service root': 'Stammordner im Dienst',
        'Add path mapping': 'Pfadzuordnung hinzufügen', 'Remove': 'Entfernen', 'Test saved connection': 'Gespeicherte Verbindung testen',
        'Save settings': 'Einstellungen speichern', 'Saved': 'Gespeichert', 'Connected: {version}': 'Verbunden: {version}',
        'Deletion log': 'Löschprotokoll', 'Dispatched': 'Auftrag gesendet', 'Failed': 'Fehlgeschlagen',
        'Only administrators can access this view.': 'Diese Ansicht ist nur für Administratoren verfügbar.',
        'Refresh to verify unfinished deletions. Retrying a deletion requires a new confirmation.': 'Aktualisiere die Auswertung, um unvollständige Löschungen zu prüfen. Ein erneuter Löschversuch benötigt eine neue Bestätigung.',
        'Feedback is unavailable because this nomination changed. Refresh the page.': 'Die Vormerkung hat sich geändert. Bitte aktualisiere die Seite.',
        'No access or unavailable': 'Nicht verfügbar oder kein Zugriff'
    };
    function t(key, values) {
        let text = WatchCircleI18n.locale().split('-')[0] === 'de' ? (translations[key] || key) : key;
        return text.replace(/\{(\w+)\}/g, (match, name) => values && values[name] !== undefined ? String(values[name]) : match);
    }
    function normalize(value) {
        if (Array.isArray(value)) return value.map(normalize);
        if (!value || typeof value !== 'object') return value;
        const result = {};
        Object.keys(value).forEach(key => { result[key[0].toUpperCase() + key.slice(1)] = normalize(value[key]); });
        return result;
    }
    function guid(value) {
        const id = String(value || '').replace(/-/g, '').toLowerCase();
        return /^[a-f0-9]{32}$/.test(id) ? id.replace(/(.{8})(.{4})(.{4})(.{4})(.{12})/, '$1-$2-$3-$4-$5') : null;
    }
    const el = (tag, cls, text) => {
        const node = document.createElement(tag); node.className = cls || '';
        if (text !== undefined) node.textContent = text;
        return node;
    };
    function button(text, action, cls) {
        const node = el('button', cls || 'wc-clean-action', text); node.type = 'button';
        node.addEventListener('click', action); return node;
    }
    function date(value) { return value ? new Date(value).toLocaleDateString(WatchCircleI18n.locale(), { day: 'numeric', month: 'short', year: 'numeric' }) : '—'; }
    function message(parent, text, cls) { const node = el('p', cls || 'wc-clean-muted', text); parent.appendChild(node); return node; }
    function api(path, body) {
        return ApiClient.ajax({ type: body === undefined ? 'GET' : 'POST', url: ApiClient.getUrl('WatchCircle/Cleanup/' + path),
            dataType: 'json', contentType: 'application/json', data: body === undefined ? undefined : JSON.stringify(body) }).then(normalize);
    }
    function diagnostic(parent, error) {
        const data = error && (error.responseJSON || error);
        const text = data && (data.Error || data.error || data.message);
        if (!text) return;
        const details = el('details'); details.appendChild(el('summary', '', t('Technical details')));
        message(details, String(text)); parent.appendChild(details);
    }
    function problem(parent, error) { message(parent, t('Could not load or save. Please refresh and try again.'), 'wc-clean-error'); diagnostic(parent, error); }
    function poster(item) {
        const image = el('img', 'wc-clean-poster'); image.alt = ''; image.loading = 'lazy';
        image.src = ApiClient.getUrl('Items/' + encodeURIComponent(item.ItemId) + '/Images/Primary', { maxWidth: 240 });
        image.addEventListener('error', () => { image.removeAttribute('src'); image.classList.add('wc-clean-no-poster'); }, { once: true });
        return image;
    }
    function openTitle(id) { close(); window.location.hash = '/details?id=' + encodeURIComponent(id); }
    let availability = { Enabled: false, IsAdmin: false }, session = '', dialog, body, previousFocus, previousOverflow;
    let currentRender, renderVersion = 0, busy = false, started = false, popupWanted = true, refreshTimer, scanning = false;
    let cache = new Map(), protectionCache = new Map();
    let requesterObserver, requesterQueue = [], requesterActive = 0;
    const heldKeys = new Set();
    const cardSelector = '.cardImageContainer.cardContent, .cardContent > .cardImageContainer, .listItemImage';
    const focusSelector = 'button:not([disabled]), a[href], input:not([disabled]), select, summary, .wc-clean-user[tabindex]';
    function focusables() { return dialog ? Array.from(dialog.querySelectorAll(focusSelector)).filter(node => node.getClientRects().length) : []; }
    function onKey(event) {
        if (!dialog) return;
        if (event.altKey || event.ctrlKey || event.metaKey || event.isComposing) return;
        const action = WatchCircleProfileNavigation.keyAction(event.key, event.keyCode);
        const editing = document.activeElement && /INPUT|SELECT|TEXTAREA/.test(document.activeElement.tagName);
        if (editing && WatchCircleProfileNavigation.editsText(action, event.key, event.keyCode)) return;
        if (action) heldKeys.add(event.keyCode || event.key);
        const key = ({ left: 'ArrowLeft', right: 'ArrowRight', up: 'ArrowUp', down: 'ArrowDown', back: 'Escape', select: 'Enter', tab: 'Tab' })[action] || event.key;
        if (key === 'Enter' || key === ' ' || event.keyCode === 23) {
            if (document.activeElement && !/INPUT|SELECT|TEXTAREA/.test(document.activeElement.tagName)) {
                event.preventDefault(); event.stopImmediatePropagation(); if (!event.repeat) document.activeElement.click();
            }
            return;
        }
        if (['Escape', 'BrowserBack', 'GoBack'].includes(key) || [4, 10009, 461].includes(event.keyCode)) {
            event.preventDefault(); event.stopImmediatePropagation(); close(); return;
        }
        const nodes = focusables(), active = document.activeElement;
        if (key === 'Tab') {
            event.preventDefault(); event.stopImmediatePropagation();
            const index = nodes.indexOf(active); const next = nodes[(index + (event.shiftKey ? -1 : 1) + nodes.length) % nodes.length];
            if (next) next.focus(); return;
        }
        if (!/^Arrow(Left|Right|Up|Down)$/.test(key) || (active && /INPUT|SELECT|TEXTAREA/.test(active.tagName))) return;
        event.preventDefault(); event.stopImmediatePropagation();
        const rect = active.getBoundingClientRect(), horizontal = key === 'ArrowLeft' || key === 'ArrowRight';
        const sign = key === 'ArrowLeft' || key === 'ArrowUp' ? -1 : 1;
        const x = rect.left + rect.width / 2, y = rect.top + rect.height / 2;
        const candidates = nodes.filter(node => node !== active).map(node => {
            const r = node.getBoundingClientRect(), dx = r.left + r.width / 2 - x, dy = r.top + r.height / 2 - y;
            const forward = (horizontal ? dx : dy) * sign, cross = Math.abs(horizontal ? dy : dx);
            return { node, forward, score: forward + cross * 3 };
        }).filter(value => value.forward > 5).sort((a, b) => a.score - b.score);
        if (candidates[0]) candidates[0].node.focus();
        else if (!horizontal) body.scrollTop += sign * body.clientHeight * 0.65;
    }
    function trapFocus(event) { if (dialog && !dialog.contains(event.target)) { const first = focusables()[0]; if (first) first.focus(); } }
    function close(force) {
        if (!dialog || (busy && force !== true)) return;
        clearRequesterCards();
        dialog.remove(); dialog = null; renderVersion++; document.body.style.overflow = previousOverflow;
        window.removeEventListener('keydown', onKey, true); document.removeEventListener('focusin', trapFocus, true);
        if (previousFocus && previousFocus.isConnected) previousFocus.focus();
    }
    async function show(mode) {
        const viewer = ApiClient.getCurrentUserId();
        if (!viewer) return;
        await loadProgress();
        availability = await api('Status');
        if (viewer !== ApiClient.getCurrentUserId()) return;
        if (!dialog) {
            previousFocus = document.activeElement; previousOverflow = document.body.style.overflow;
            dialog = el('div', 'wc-clean-dialog'); dialog.setAttribute('role', 'dialog'); dialog.setAttribute('aria-modal', 'true'); dialog.setAttribute('aria-labelledby', 'wc-clean-title');
            const header = el('header', 'wc-clean-header'), title = el('h2', '', t('Library cleanup')); title.id = 'wc-clean-title';
            header.appendChild(title); header.appendChild(button(t('Close'), close)); dialog.appendChild(header);
            body = el('main', 'wc-clean-body'); dialog.appendChild(body); document.body.appendChild(dialog); document.body.style.overflow = 'hidden';
            window.addEventListener('keydown', onKey, true); document.addEventListener('focusin', trapFocus, true);
        }
        popupWanted = false;
        return render(mode === 'settings' ? settings : mode === 'admin' ? admin : feedback);
    }
    async function render(view) {
        clearRequesterCards();
        currentRender = view; const version = ++renderVersion, viewer = ApiClient.getCurrentUserId(); body.replaceChildren(); body.scrollTop = 0;
        message(body, t('Loading…'));
        try {
            const content = await view();
            if (!dialog || version !== renderVersion || viewer !== ApiClient.getCurrentUserId()) return;
            body.replaceChildren(content);
            observeRequesterCards(version, viewer);
        } catch (error) {
            if (!dialog || version !== renderVersion || viewer !== ApiClient.getCurrentUserId()) return;
            body.replaceChildren(); problem(body, error); body.appendChild(button(t('Refresh'), () => render(view)));
        }
        const first = focusables()[0]; if (first) first.focus();
    }
    function navigation(parent) {
        const nav = el('nav', 'wc-clean-actions');
        nav.appendChild(button(t('Your feedback'), () => render(feedback)));
        if (availability.IsAdmin) {
            nav.appendChild(button(t('Administration'), () => render(admin))); nav.appendChild(button(t('Settings'), () => render(settings)));
        }
        parent.appendChild(nav);
    }
    async function answer(item, value, host, after) {
        const controls = Array.from(host.querySelectorAll('button')); controls.forEach(node => { node.disabled = true; });
        try {
            await api('Replies/' + item.Id, { NominationId: item.NominationId, Answer: value });
            invalidate(); if (after) await after();
        } catch (error) { message(host, t('Feedback is unavailable because this nomination changed. Refresh the page.'), 'wc-clean-error'); invalidate(); }
        finally { controls.forEach(node => { node.disabled = false; }); }
    }
    function replyControls(parent, item, after) {
        if (item.Answer) message(parent, t('Your answer: {answer} · {date}', { answer: t(item.Answer), date: date(item.AnswerAt) }));
        if (item.Deleting) { message(parent, t('Deletion requested; verification pending')); return; }
        const actions = el('div', 'wc-clean-actions');
        actions.appendChild(button(t('Please keep'), () => answer(item, 'Keep', parent, after), 'wc-clean-action wc-clean-keep'));
        const indifferent = button(t('I do not mind'), () => answer(item, 'Indifferent', parent, after));
        indifferent.disabled = item.Answer === 'Indifferent'; actions.appendChild(indifferent); parent.appendChild(actions);
    }
    async function feedback() {
        const items = await api('Pending'), page = el('div'); navigation(page);
        message(page, t('Feedback does not shorten the warning period. You can change your answer on the title page.'));
        if (!items.length) message(page, t('No open feedback'));
        const list = el('div', 'wc-clean-grid');
        items.forEach(item => {
            const card = el('article', 'wc-clean-card'); card.appendChild(poster(item));
            const content = el('div'); content.appendChild(button(item.Name, () => openTitle(item.ItemId), 'wc-clean-title-link'));
            message(content, t('Earliest deletion: {date}', { date: date(item.DeleteAt) }), 'wc-clean-due');
            message(content, t(item.Kind === 'Series' ? 'Keeps the whole series.' : 'Also keeps the movies in its collections.'));
            replyControls(content, item, () => render(feedback)); card.appendChild(content); list.appendChild(card);
        }); page.appendChild(list); return page;
    }
    function summary(parent, row) {
        const entry = row.Entry, media = entry.Media;
        if (entry.NominationId && entry.DeleteAt && new Date(entry.DeleteAt) <= new Date()) message(parent, t('Deletion date reached'), 'wc-clean-due');
        if (entry.DeleteAt) message(parent, t('Earliest deletion: {date}', { date: date(entry.DeleteAt) }), 'wc-clean-due');
        message(parent, t('Last interaction') + ': ' + (entry.LastInteraction ? date(entry.LastInteraction) + ' · ' + (row.LastUserName || t('Unknown')) + ' · ' + t(entry.LastKind) : t('First observed') + ' ' + date(entry.Baseline)));
        if (entry.NominatedAt) message(parent, t('Nominated') + ': ' + date(entry.NominatedAt));
        message(parent, t('Estimated size') + ': ' + (media.Bytes / 1073741824).toLocaleString(WatchCircleI18n.locale(), { maximumFractionDigits: 1 }) + ' GB');
        if (row.Collections && row.Collections.length) message(parent, t('Collections') + ': ' + row.Collections.map(value => value.Name).join(', '));
        if (row.EffectiveProtection) message(parent, t('Permanently protected'), 'wc-clean-protected');
        [entry.Error, media.Problem].concat(row.RestoreErrors || []).filter(Boolean).forEach(error => diagnostic(parent, { Error: error }));
    }
    function clearRequesterCards() {
        if (requesterObserver) requesterObserver.disconnect();
        requesterObserver = null; requesterQueue = [];
    }
    function observeRequesterCards(version, viewer) {
        const nodes = Array.from(body.querySelectorAll('[data-wc-clean-request-item]'));
        const enqueue = node => { requesterQueue.push({ node, itemId: node.dataset.wcCleanRequestItem, version, viewer }); loadRequesterCards(); };
        if (typeof IntersectionObserver === 'undefined') { nodes.forEach(enqueue); return; }
        const observer = new IntersectionObserver(entries => {
            if (!dialog || version !== renderVersion || viewer !== ApiClient.getCurrentUserId()) return;
            entries.filter(entry => entry.isIntersecting).forEach(entry => {
                observer.unobserve(entry.target); enqueue(entry.target);
            });
        }, { root: body, rootMargin: '300px' });
        requesterObserver = observer;
        nodes.forEach(node => observer.observe(node));
    }
    function loadRequesterCards() {
        // Keep large libraries responsive and avoid flooding Seerr with requests.
        while (requesterActive < 3 && requesterQueue.length) {
            const task = requesterQueue.shift();
            const current = () => dialog && task.version === renderVersion && task.viewer === ApiClient.getCurrentUserId() && task.node.isConnected;
            if (!current()) continue;
            requesterActive++;
            Promise.resolve().then(() => ApiClient.ajax({ type: 'GET',
                url: ApiClient.getUrl('WatchCircle/Seerr/Requests/' + encodeURIComponent(task.itemId)), dataType: 'json' }))
                .then(normalize).then(requesters => {
                    if (!current()) return;
                    const names = (Array.isArray(requesters) ? requesters : []).map(value => value.Name || value.DisplayName).filter(Boolean);
                    task.node.textContent = t('Requested by') + ': ' + (names.length ? names.join(', ') : '—');
                }).catch(() => {
                    if (current()) task.node.textContent = t('Requested by') + ': ' + t('Unavailable');
                }).finally(() => { requesterActive--; loadRequesterCards(); });
        }
    }
    let adminFilter = 'scheduled';
    async function admin() {
        if (!availability.IsAdmin) throw new Error(t('Only administrators can access this view.'));
        const data = await api('Admin/Items'), page = el('div'); navigation(page);
        message(page, t(!data.Enabled ? 'Inactive' : data.AutomaticDeletion ? 'Automatic deletion' : 'Manual deletion'));
        if (data.Error) problem(page, { Error: data.Error });
        const actions = el('div', 'wc-clean-actions');
        actions.appendChild(button(t('Evaluate now'), async event => {
            event.currentTarget.disabled = true;
            try { await api('Admin/Evaluate', {}); invalidate(); await render(admin); } catch (error) { problem(page, error); event.target.disabled = false; }
        })); page.appendChild(actions);
        message(page, t('Refresh to verify unfinished deletions. Retrying a deletion requires a new confirmation.'));
        const filters = el('nav', 'wc-clean-actions');
        [['scheduled', 'Scheduled for deletion'], ['kept', 'Kept on request'], ['protected', 'Permanently protected'], ['deleted', 'Deleted'], ['failed', 'Failed or incomplete'], ['all', 'All titles']].forEach(pair => {
            const control = button(t(pair[1]), () => { adminFilter = pair[0]; render(admin); });
            control.setAttribute('aria-pressed', String(adminFilter === pair[0])); filters.appendChild(control);
        }); page.appendChild(filters);
        const entries = data.Entries.filter(row => adminFilter === 'all' || (adminFilter === 'scheduled' && row.Entry.NominationId)
            || (adminFilter === 'kept' && !row.Entry.NominationId && row.Replies.some(reply => reply.Answer === 'Keep'))
            || (adminFilter === 'protected' && row.EffectiveProtection)
            || (adminFilter === 'deleted' && row.Deletions.some(job => job.Phase === 'Deleted'))
            || (adminFilter === 'failed' && (row.Entry.Error || row.Entry.Media.Problem || row.RestoreErrors.length || row.Deletions.some(job => job.Phase !== 'Deleted'))));
        function addGroup(title, rows) {
            const section = el('section', 'wc-clean-media-group'); section.setAttribute('aria-label', t(title));
            section.appendChild(el('h3', '', t(title))); if (!rows.length) message(section, t('No entries'));
            const grid = el('div', 'wc-clean-grid');
            rows.sort((a, b) => (Number(b.Entry.Media.Bytes) || 0) - (Number(a.Entry.Media.Bytes) || 0)
                || a.Entry.Media.Name.localeCompare(b.Entry.Media.Name, WatchCircleI18n.locale())).forEach(row => {
                const card = el('article', 'wc-clean-card'); if (row.Entry.Present) card.appendChild(poster(row.Entry.Media));
                const content = el('div'); content.appendChild(button(row.Entry.Media.Name, () => render(() => details(row)), 'wc-clean-title-link'));
                if (row.Entry.Present && row.Entry.Media.Kind !== 'Collection') {
                    const requesters = message(content, t('Requested by') + ': ' + t('Loading…'), 'wc-clean-requesters');
                    requesters.dataset.wcCleanRequestItem = row.Entry.Media.ItemId;
                }
                summary(content, row); card.appendChild(content); grid.appendChild(card);
            }); section.appendChild(grid); page.appendChild(section);
        }
        message(page, t('Largest titles first'));
        addGroup('Series', entries.filter(row => row.Entry.Media.Kind === 'Series'));
        addGroup('Movies', entries.filter(row => row.Entry.Media.Kind === 'Movie'));
        const collections = entries.filter(row => row.Entry.Media.Kind === 'Collection');
        if (collections.length) addGroup('Collections', collections);
        return page;
    }
    async function details(row) {
        const data = await api('Admin/Items/' + row.Entry.Id), entry = data.Entry, page = el('div');
        page.appendChild(button(t('Back'), () => render(admin))); page.appendChild(el('h3', '', entry.Media.Name)); summary(page, row);
        const actions = el('div', 'wc-clean-actions');
        if (entry.Present) actions.appendChild(button(t('Open title'), () => openTitle(entry.Media.ItemId)));
        actions.appendChild(button(t(entry.Protected ? 'Remove protection' : 'Keep permanently'), async () => {
            try { await api('Admin/Protection/' + entry.Id, { Protected: !entry.Protected }); invalidate(); await render(admin); } catch (error) { problem(page, error); }
        }));
        if (!row.EffectiveProtection && entry.Present && (entry.Media.Kind === 'Collection' || (entry.DeleteAt && new Date(entry.DeleteAt) <= new Date()))) {
            actions.appendChild(button(t('Review deletion'), () => render(() => confirmation(entry.Id)), 'wc-clean-action wc-clean-danger'));
        }
        page.appendChild(actions);
        if (data.Mapping) message(page, t('Mapping') + ': ' + data.Mapping.Id + ' · ' + data.Mapping.Path);
        if (data.MappingError) { message(page, t('Mapping') + ': ' + t('No access or unavailable')); diagnostic(page, { Error: data.MappingError }); }
        if (data.Members && data.Members.length) {
            page.appendChild(el('h3', '', t('Movies in this collection')));
            data.Members.forEach(member => page.appendChild(button(member.Name, () => render(async () => {
                const overview = await api('Admin/Items'); return details(overview.Entries.find(value => value.Entry.Id === member.Id));
            }))));
        }
        const users = el('section', 'wc-clean-users'); users.appendChild(el('h3', '', 'WatchCircle'));
        const members = entry.Media.Kind === 'Collection' ? [] : (data.Users || []).slice();
        const linkedIds = new Set(members.map(user => guid(user.Id)).filter(Boolean));
        (data.Requesters || []).forEach(requester => {
            const profileId = guid(requester.ProfileUserId);
            if (profileId && linkedIds.has(profileId)) {
                members.filter(user => guid(user.Id) === profileId).forEach(user => { user.IsRequester = true; });
            } else {
                members.push({ Name: requester.Name || requester.DisplayName, IsRequester: true, SeerrOnly: true, Replies: [] });
            }
        });
        members.sort((a, b) => Number(!!b.IsRequester) - Number(!!a.IsRequester)
            || a.Name.localeCompare(b.Name, WatchCircleI18n.locale())).forEach(user => {
            const block = el('article', 'wc-clean-user'), progress = user.Progress;
            block.tabIndex = 0; block.setAttribute('aria-label', user.Name);
            if (progress) {
                const series = entry.Media.Kind === 'Series';
                const raw = series ? progress.Episode || {} : { Played: progress.Completed, PlaybackPositionTicks: progress.PositionTicks };
                block.appendChild(WatchCircleWatchProgress.createRow({ labelText: user.Name, progress: raw, runTimeTicks: progress.RuntimeTicks,
                    variant: guid(user.Id) === guid(ApiClient.getCurrentUserId()) ? 'you' : 'them', showEpisodeLine: series,
                    statusText: progress.Started ? undefined : t('Not started'), aggregate: series ? progress : null }));
            } else message(block, user.Name + ' · ' + t(user.SeerrOnly ? 'No linked Jellyfin account' : 'No current progress data'));
            if (user.IsRequester) message(block, t('Requested in Seerr'), 'wc-clean-protected');
            if (progress && !progress.Started && entry.Media.Kind === 'Series') message(block, t('Not started'));
            if (user.Open) message(block, t('No reply yet'));
            user.Replies.slice().reverse().forEach(reply => message(block, t(reply.Answer) + ' · ' + date(reply.At)));
            users.appendChild(block);
        }); if (entry.Media.Kind !== 'Collection') page.appendChild(users);
        if (row.Deletions.length) {
            page.appendChild(el('h3', '', t('Deletion log')));
            row.Deletions.slice().reverse().forEach(job => { message(page, date(job.At) + ' · ' + t(job.Phase)); if (job.Error) diagnostic(page, { Error: job.Error }); });
        }
        return page;
    }
    async function confirmation(id) {
        const plan = await api('Admin/Plan/' + id, {}), page = el('div'); page.appendChild(button(t('Back'), () => render(admin)));
        page.appendChild(el('h3', '', t('Confirm deletion'))); message(page, t('These titles and their media files will be removed through Radarr/Sonarr.'));
        plan.Targets.forEach(target => { page.appendChild(el('h4', '', target.Name)); message(page, t(target.Kind === 'Series'
            ? 'Whole series, including all episodes. Sonarr will also stop monitoring future episodes.' : 'Movie and all managed media files.')); });
        message(page, t('Deadlines, protection and activity are checked again before deletion.'));
        page.appendChild(button(t('Delete listed titles and files'), async event => {
            event.currentTarget.disabled = true; busy = true;
            try { await api('Admin/Delete', { PlanId: plan.Id }); invalidate(); await render(admin); }
            catch (error) { problem(page, error); }
            finally { busy = false; }
        }, 'wc-clean-action wc-clean-danger')); return page;
    }
    async function settings() {
        if (!availability.IsAdmin) throw new Error(t('Only administrators can access this view.'));
        const data = await api('Admin/Settings'), value = data.Settings, page = el('div'); navigation(page);
        if (data.Error) problem(page, { Error: data.Error });
        const form = el('form', 'wc-clean-settings'), fields = {};
        function input(parent, label, initial, type, min, max) {
            const wrap = el('label', 'wc-clean-field'), node = el('input'); node.type = type || 'text';
            if (type === 'checkbox') node.checked = !!initial; else node.value = initial || '';
            if (min) { node.min = min; node.max = max; node.required = true; }
            wrap.appendChild(el('span', '', t(label))); wrap.appendChild(node); parent.appendChild(wrap); return node;
        }
        fields.Enabled = input(form, 'Enable library cleanup', value.Enabled, 'checkbox');
        message(form, t('Unknown history starts with a full inactivity period. Existing favorites alone do not restart it.'));
        fields.InactivityMonths = input(form, 'Months without interaction', value.InactivityMonths, 'number', 1, 120);
        fields.WarningDays = input(form, 'Warning period in days', value.WarningDays, 'number', 1, 3650);
        message(form, t('Run evaluations and automatic deletions or edit their schedules under Dashboard → Scheduled Tasks → WatchCircle.'));
        form.appendChild(el('h3', '', t('Libraries')));
        const libraries = data.Libraries.map(library => ({ Id: library.ItemId,
            Node: input(form, library.Name, value.LibraryIds.some(id => guid(id) === guid(library.ItemId)), 'checkbox') }));
        fields.AutomaticDeletion = input(form, 'Delete eligible titles automatically', value.AutomaticDeletion, 'checkbox');
        message(form, t('When enabled, due titles and files are deleted without individual confirmation. Entire series are removed from Sonarr, including future monitoring.'));
        const services = {};
        ['Radarr', 'Sonarr'].forEach(name => {
            const original = value[name], section = el('section', 'wc-clean-service'); section.appendChild(el('h3', '', name));
            const controls = { Url: input(section, 'URL', original.Url, 'url'), ApiKey: input(section, 'API key', '', 'password'),
                ClearApiKey: input(section, 'Remove saved API key', false, 'checkbox'),
                AddImportExclusion: input(section, 'Exclude deleted titles from import lists', original.AddImportExclusion, 'checkbox') };
            controls.ApiKey.autocomplete = 'new-password';
            message(section, t(original.HasApiKey ? 'A key is saved. Leave blank to keep it.' : 'No key saved.'));
            section.appendChild(el('h4', '', t('Path mappings'))); const mappingHost = el('div'); section.appendChild(mappingHost); const mappings = [];
            function addMapping(mapping) {
                const row = el('div', 'wc-clean-paths'), pair = { Jellyfin: input(row, 'Jellyfin root', mapping.Jellyfin), Arr: input(row, 'Service root', mapping.Arr), Row: row };
                row.appendChild(button(t('Remove'), () => { row.remove(); mappings.splice(mappings.indexOf(pair), 1); })); mappings.push(pair); mappingHost.appendChild(row);
            }
            original.Paths.forEach(addMapping); section.appendChild(button(t('Add path mapping'), () => addMapping({})));
            section.appendChild(button(t('Test saved connection'), async event => {
                event.currentTarget.disabled = true;
                try { const result = await api('Admin/Test/' + name, {}); message(section, t('Connected: {version}', { version: result.Version })); }
                catch (error) { problem(section, error); } finally { event.target.disabled = false; }
            })); controls.Mappings = mappings; services[name] = controls; form.appendChild(section);
        });
        const save = button(t('Save settings'), () => {}); save.type = 'submit'; form.appendChild(save);
        form.addEventListener('submit', async event => {
            event.preventDefault(); save.disabled = true;
            const next = { LibraryIds: libraries.filter(item => item.Node.checked).map(item => guid(item.Id)) };
            Object.keys(fields).forEach(key => { next[key] = fields[key].type === 'checkbox' ? fields[key].checked : Number(fields[key].value); });
            Object.keys(services).forEach(key => { const controls = services[key]; next[key] = {
                Url: controls.Url.value, ApiKey: controls.ApiKey.value, ClearApiKey: controls.ClearApiKey.checked, AddImportExclusion: controls.AddImportExclusion.checked,
                Paths: controls.Mappings.map(pair => ({ Jellyfin: pair.Jellyfin.value, Arr: pair.Arr.value })) }; });
            try { await api('Admin/Settings', next); invalidate(); await render(settings); message(body, t('Saved')); }
            catch (error) { problem(form, error); save.disabled = false; }
        }); page.appendChild(form); return page;
    }
    let progressLoading;
    function loadProgress() {
        function load(file, name) {
            if (window[name]) return Promise.resolve();
            return new Promise((resolve, reject) => {
                const script = el('script'); script.src = WatchCircleAssets.getUrl('components/' + file + '.js');
                script.onload = resolve; script.onerror = () => { script.remove(); reject(new Error('Module unavailable')); }; document.head.appendChild(script);
            });
        }
        if (!progressLoading) progressLoading = Promise.all([load('watchProgress/watchProgress', 'WatchCircleWatchProgress'), load('profiles/profileNavigation', 'WatchCircleProfileNavigation')])
            .then(() => { WatchCircleWatchProgress.ensureStyles(); }).catch(error => { progressLoading = null; throw error; });
        return progressLoading;
    }
    function itemIdFromHash() { const hash = window.location.hash; return hash.includes('/details') ? guid(new URLSearchParams(hash.split('?')[1] || '').get('id')) : null; }
    function menu() {
        const host = document.querySelector('.wc-navbar-actions'); if (!host) return;
        const existing = host.querySelector('.wc-clean-menu');
        if (!availability.Enabled && !availability.IsAdmin) { if (existing) existing.remove(); return; }
        if (existing) { existing.title = t('Library cleanup'); existing.setAttribute('aria-label', t('Library cleanup')); return; }
        const action = button('', () => show(availability.Enabled ? 'feedback' : 'settings').catch(() => {}), 'wc-clean-menu headerButton paper-icon-button-light');
        action.title = t('Library cleanup'); action.setAttribute('aria-label', t('Library cleanup'));
        const icon = document.createElementNS('http://www.w3.org/2000/svg', 'svg'); icon.setAttribute('viewBox', '0 0 24 24');
        icon.setAttribute('width', '24'); icon.setAttribute('height', '24'); icon.setAttribute('aria-hidden', 'true');
        const path = document.createElementNS('http://www.w3.org/2000/svg', 'path'); path.setAttribute('fill', 'none'); path.setAttribute('stroke', 'currentColor'); path.setAttribute('stroke-width', '1.8');
        path.setAttribute('d', 'M5 7h14M9 4h6M7 7l1 13h8l1-13M10 10v7M14 10v7'); icon.appendChild(path); action.appendChild(icon); host.appendChild(action);
    }
    function invalidate() { cache.clear(); protectionCache.clear(); schedule(); }
    function schedule() { clearTimeout(refreshTimer); refreshTimer = setTimeout(scan, 300); }
    function banner(mount, status, protection, id) {
        const existing = mount.querySelector('.wc-clean-banner');
        const signature = JSON.stringify([status, protection, WatchCircleI18n.locale(), id]);
        if (existing && existing.dataset.signature === signature) return;
        const hadFocus = existing && existing.contains(document.activeElement);
        if (existing) existing.remove();
        if (!status && !(availability.IsAdmin && protection && protection.Id)) return;
        const section = el('section', 'wc-clean-banner'); section.dataset.signature = signature;
        if (!status) section.classList.add('wc-clean-protection-only');
        section.setAttribute('aria-label', t('Library cleanup'));
        if (status) {
            section.appendChild(el('h3', '', t('Scheduled for deletion')));
            message(section, status.Name + ' · ' + t('Earliest deletion: {date}', { date: date(status.DeleteAt) }), 'wc-clean-due');
            if (status.Kind === 'Series') message(section, t('Keeps the whole series.'));
            replyControls(section, status, () => { invalidate(); });
        }
        if (availability.IsAdmin && protection && protection.Id) {
            if (protection.EffectiveProtection && !protection.Protected) message(section, t('Protected by a collection'));
            section.appendChild(button(t(protection.Protected ? 'Remove protection' : 'Keep permanently'), async () => {
                try { await api('Admin/Protection/' + protection.Id, { Protected: !protection.Protected }); invalidate(); }
                catch (error) { problem(section, error); }
            }));
        }
        mount.prepend(section); if (hadFocus) { const control = section.querySelector('button'); if (control) control.focus(); }
    }
    async function scan() {
        const userId = ApiClient.getCurrentUserId();
        if (session !== userId) { session = userId; cache.clear(); protectionCache.clear(); popupWanted = true; close(true); }
        if (!userId) {
            availability = { Enabled: false, IsAdmin: false }; menu();
            document.querySelectorAll('.wc-clean-marker, .wc-clean-banner').forEach(node => node.remove()); return;
        }
        if (scanning || document.hidden) return;
        scanning = true;
        try {
            availability = await api('Status'); if (session !== ApiClient.getCurrentUserId()) return; menu();
            if (!availability.Enabled) {
                document.querySelectorAll('.wc-clean-marker, .wc-clean-banner').forEach(node => node.remove()); return;
            }
            const cards = Array.from(document.querySelectorAll(cardSelector)).map(node => ({ Node: node,
                Id: guid((node.closest('[data-id].card, [data-id].listItem') || node).getAttribute('data-id')) })).filter(card => card.Id);
            const detailId = itemIdFromHash(), ids = Array.from(new Set(cards.map(card => card.Id).concat(detailId || [])));
            const missing = ids.filter(id => !cache.has(id) || cache.get(id).Until < Date.now());
            for (let i = 0; i < missing.length; i += 200) {
                const batch = missing.slice(i, i + 200), statuses = await api('Items/Status', batch);
                if (session !== ApiClient.getCurrentUserId()) return;
                batch.forEach(id => cache.set(id, { Status: null, Until: Date.now() + 30000 }));
                statuses.forEach(status => cache.set(guid(status.ItemId), { Status: status, Until: Date.now() + 30000 }));
            }
            cards.forEach(card => {
                const status = cache.get(card.Id)?.Status, old = card.Node.querySelector('.wc-clean-marker');
                if (status && !old) { const mark = el('span', 'wc-clean-marker', t('Scheduled for deletion')); mark.title = t('Earliest deletion: {date}', { date: date(status.DeleteAt) }); card.Node.appendChild(mark); }
                if (!status && old) old.remove();
            });
            if (detailId && availability.IsAdmin && (!protectionCache.has(detailId) || protectionCache.get(detailId).Until < Date.now())) {
                protectionCache.set(detailId, { Value: await api('Admin/Protection/Item/' + detailId), Until: Date.now() + 30000 });
            }
            if (session !== ApiClient.getCurrentUserId()) return;
            const mount = Array.from(document.querySelectorAll('.detailPagePrimaryContent')).find(node => node.getClientRects().length);
            if (mount && itemIdFromHash() === detailId && detailId) banner(mount, cache.get(detailId)?.Status, protectionCache.get(detailId)?.Value, detailId);
            if (popupWanted && !dialog && !document.querySelector('[role="dialog"], .dialogContainer, .videoPlayerContainer:not(.hide)')) {
                const items = await api('Pending');
                if (session !== ApiClient.getCurrentUserId()) return;
                popupWanted = false; if (items.length) await show('feedback');
            }
        } catch (_) { /* Retry on the next refresh; never infer that a failed request cancels a nomination. */ }
        finally { scanning = false; }
    }
    function resume() { if (!document.hidden) { popupWanted = true; invalidate(); } }
    function start() {
        if (started) return; started = true;
        const style = el('link'); style.rel = 'stylesheet'; style.href = WatchCircleAssets.getUrl('components/cleanup/cleanup.css'); document.head.appendChild(style);
        new MutationObserver(records => { if (records.some(record => Array.from(record.addedNodes).some(node => node.nodeType === 1 && !node.closest?.('.wc-clean-dialog, .wc-clean-banner') && !node.classList?.contains('wc-clean-marker')))) schedule(); }).observe(document.body, { childList: true, subtree: true });
        window.addEventListener('hashchange', () => { close(); schedule(); }); document.addEventListener('viewshow', schedule);
        document.addEventListener('visibilitychange', resume); window.addEventListener('pageshow', resume); window.addEventListener('focus', resume);
        window.addEventListener('keyup', event => {
            if (!heldKeys.delete(event.keyCode || event.key)) return;
            event.preventDefault(); event.stopImmediatePropagation();
        }, true);
        window.addEventListener('command', event => {
            if (!dialog || !event.detail) return;
            const key = ({ left: 'ArrowLeft', right: 'ArrowRight', up: 'ArrowUp', down: 'ArrowDown', back: 'Escape', select: 'Enter' })[event.detail.command];
            if (key) onKey({ key, preventDefault: () => event.preventDefault(), stopImmediatePropagation: () => event.stopImmediatePropagation() });
        }, true);
        document.addEventListener('watchcirclelanguagechange', () => {
            invalidate();
            document.querySelectorAll('.wc-clean-marker').forEach(node => node.remove());
            if (dialog) {
                dialog.querySelector('h2').textContent = t('Library cleanup');
                dialog.querySelector('header button').textContent = t('Close'); render(currentRender);
            }
        });
        setInterval(scan, 30000); schedule();
    }
    window.WatchCircleCleanup = { start, show, invalidate, normalize, guid };
    start();
})();
