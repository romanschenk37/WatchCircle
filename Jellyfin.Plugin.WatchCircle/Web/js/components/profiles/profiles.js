(function () {
    'use strict';
    if (window.WatchCircleProfiles) return;

    let activeDialog = null;
    const categories = { started: 'Begonnen', completed: 'Abgeschlossen', favorites: 'Favoriten' };
    function field(value, name) { return value[name] !== undefined ? value[name] : value[name[0].toLowerCase() + name.slice(1)]; }
    function element(tag, className, text) {
        const node = document.createElement(tag);
        node.className = className || '';
        if (text !== undefined) node.textContent = text;
        return node;
    }
    function button(text, action, className) {
        const node = element('button', className || 'wc-profile-action', text);
        node.type = 'button';
        node.addEventListener('click', action);
        return node;
    }
    function avatar(user) {
        const initial = element('span', 'wc-profile-avatar', (field(user, 'Name') || '?').slice(0, 1).toUpperCase());
        initial.setAttribute('aria-hidden', 'true');
        if (!field(user, 'HasPrimaryImage')) return initial;
        const img = element('img', 'wc-profile-avatar');
        img.alt = '';
        img.src = ApiClient.getUrl('Users/' + encodeURIComponent(field(user, 'Id')) + '/Images/Primary', { maxWidth: 96, tag: field(user, 'PrimaryImageTag') });
        img.addEventListener('error', () => img.replaceWith(initial), { once: true });
        return img;
    }
    function percentText(progress) {
        const value = field(progress, 'Percent');
        return value === null || value === undefined ? 'Fortschritt unbekannt' : Math.floor(Math.max(0, Math.min(100, value))) + ' %';
    }
    function progressText(progress, series) {
        if (series) {
            const total = field(progress, 'TotalEpisodes') || 0;
            if (!total) return 'Keine verfügbaren Folgen';
            return (field(progress, 'CompletedEpisodes') || 0) + ' / ' + total + ' Folgen · ' + percentText(progress);
        }
        if (field(progress, 'Completed')) return 'Abgeschlossen · 100 %';
        if (!field(progress, 'Started')) return 'Noch nicht begonnen';
        const minutes = Math.floor((field(progress, 'PositionTicks') || 0) / 600000000);
        return (minutes ? minutes + ' Min. · ' : 'Begonnen · ') + percentText(progress);
    }
    function progressRow(label, progress, series, own) {
        const row = element('div', 'wc-profile-progress' + (own ? ' wc-profile-progress-you' : ''));
        const meta = element('div', 'wc-profile-progress-meta');
        meta.append(element('span', '', label), element('span', '', progressText(progress, series)));
        const track = element('div', 'wc-profile-track');
        track.setAttribute('role', 'progressbar');
        track.setAttribute('aria-label', label);
        track.setAttribute('aria-valuemin', '0');
        track.setAttribute('aria-valuemax', '100');
        const percent = field(progress, 'Percent');
        if (percent !== null && percent !== undefined) track.setAttribute('aria-valuenow', String(Math.floor(percent)));
        track.setAttribute('aria-valuetext', progressText(progress, series));
        const fill = element('div', 'wc-profile-fill');
        fill.style.width = Math.max(0, Math.min(100, percent || 0)) + '%';
        track.append(fill);
        row.append(meta, track);
        return row;
    }

    function show(memberId) {
        if (!ApiClient.getCurrentUserId()) return;
        if (activeDialog) activeDialog.close();
        if (!document.getElementById('wc-profiles-css')) {
            const link = document.createElement('link');
            link.id = 'wc-profiles-css';
            link.rel = 'stylesheet';
            link.href = WatchCircleAssets.getUrl('components/profiles/profiles.css');
            document.head.append(link);
        }
        const viewerId = ApiClient.getCurrentUserId();
        const previousFocus = document.activeElement;
        const dialog = element('dialog', 'wc-profiles-dialog');
        activeDialog = dialog;
        dialog.setAttribute('aria-labelledby', 'wc-profiles-title');
        const header = element('header', 'wc-profiles-header');
        const title = element('h2', '', 'WatchCircle');
        title.id = 'wc-profiles-title';
        const back = button('← Personen', () => showDirectory(), 'wc-profile-action wc-profile-back');
        back.hidden = true;
        const close = button('×', () => dialog.close(), 'wc-profile-close');
        close.setAttribute('aria-label', 'WatchCircle schließen');
        header.append(back, title, close);
        const body = element('div', 'wc-profiles-body');
        dialog.append(header, body);
        document.body.append(dialog);
        let members = [];
        let requestVersion = 0;
        function valid(version) {
            return dialog.open && version === requestVersion && ApiClient.getCurrentUserId() === viewerId;
        }
        function message(text) {
            body.replaceChildren(element('p', 'wc-profile-message', text));
            body.firstChild.setAttribute('role', 'status');
        }
        function failure(text, retry) {
            message(text);
            body.append(button('Erneut versuchen', retry));
        }
        function guardSession() {
            if (ApiClient.getCurrentUserId() !== viewerId) dialog.close();
        }
        const closeOnNavigation = () => dialog.close();
        window.addEventListener('hashchange', closeOnNavigation);
        document.addEventListener('viewshow', guardSession);
        dialog.addEventListener('close', () => {
            requestVersion++;
            window.removeEventListener('hashchange', closeOnNavigation);
            document.removeEventListener('viewshow', guardSession);
            dialog.remove();
            if (activeDialog === dialog) activeDialog = null;
            if (previousFocus && previousFocus.isConnected) previousFocus.focus();
        }, { once: true });
        dialog.addEventListener('click', event => { if (event.target === dialog) {
            const bounds = dialog.getBoundingClientRect();
            if (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom) dialog.close();
        } });

        function showDirectory() {
            const version = ++requestVersion;
            title.textContent = 'WatchCircle';
            back.hidden = true;
            message('Personen werden geladen …');
            ApiClient.ajax({ type: 'GET', url: ApiClient.getUrl('WatchCircle/Buddies'), dataType: 'json' }).then(data => {
                if (!valid(version)) return;
                members = data || [];
                if (memberId) {
                    const requested = memberId;
                    memberId = null;
                    const user = members.find(value => String(field(value, 'Id')).replace(/-/g, '').toLowerCase() === String(requested).replace(/-/g, '').toLowerCase());
                    if (user) { showProfile(user); return; }
                    message('Dieses Profil ist nicht verfügbar. Ihr müsst mindestens eine gemeinsame Gruppe haben.');
                    body.append(button('Zu den Personen', showDirectory));
                    return;
                }
                body.replaceChildren(element('p', 'wc-profile-subtitle', 'Personen, mit denen du mindestens eine Gruppe teilst.'));
                if (!members.length) {
                    body.append(element('p', 'wc-profile-message', 'Noch keine gemeinsamen Gruppen. Sobald eine Gruppe weitere Personen enthält, erscheinen sie hier.'));
                    return;
                }
                const search = element('input', 'wc-profile-search');
                search.type = 'search';
                search.placeholder = 'Person suchen';
                search.setAttribute('aria-label', 'Person suchen');
                const grid = element('div', 'wc-profile-people');
                function renderPeople() {
                    grid.replaceChildren();
                    members.filter(user => field(user, 'Name').toLocaleLowerCase().includes(search.value.toLocaleLowerCase())).forEach(user => {
                        const card = button('', () => showProfile(user), 'wc-profile-person');
                        const copy = element('span', 'wc-profile-person-copy');
                        copy.append(element('strong', '', field(user, 'Name')), element('span', '', 'Fortschritt & Favoriten'));
                        card.append(avatar(user), copy, element('span', '', '→'));
                        grid.append(card);
                    });
                    if (!grid.childElementCount) grid.append(element('p', '', 'Keine Person gefunden.'));
                }
                search.addEventListener('input', renderPeople);
                body.append(search, grid);
                renderPeople();
                search.focus();
            }).catch(() => { if (valid(version)) failure('Die Personen konnten nicht geladen werden.', showDirectory); });
        }

        function showProfile(user) {
            const version = ++requestVersion;
            const name = field(user, 'Name');
            title.textContent = name;
            back.hidden = false;
            message('Fortschritt und Favoriten werden geladen …');
            ApiClient.ajax({ type: 'GET', url: ApiClient.getUrl('WatchCircle/Profiles/' + encodeURIComponent(field(user, 'Id'))), dataType: 'json' }).then(data => {
                if (!valid(version)) return;
                const items = field(data, 'Items') || [];
                body.replaceChildren();
                const intro = element('div', 'wc-profile-intro');
                const introCopy = element('div');
                introCopy.append(element('strong', '', 'Euer Fortschritt im Vergleich'), element('p', 'wc-profile-subtitle', 'Serien: alle für dich verfügbaren Folgen inklusive Specials. Abgeschlossen heißt auf dem aktuellen Stand.'));
                intro.append(avatar(user), introCopy);
                const tabs = element('div', 'wc-profile-tabs');
                tabs.setAttribute('role', 'group');
                tabs.setAttribute('aria-label', 'Kategorie');
                const filters = element('div', 'wc-profile-filters');
                const search = element('input', 'wc-profile-search');
                search.type = 'search';
                search.placeholder = 'Film oder Serie suchen';
                search.setAttribute('aria-label', 'Film oder Serie suchen');
                const type = element('select', 'wc-profile-type');
                type.setAttribute('aria-label', 'Medientyp');
                [['', 'Filme & Serien'], ['Movie', 'Filme'], ['Series', 'Serien']].forEach(pair => {
                    const option = element('option', '', pair[1]); option.value = pair[0]; type.append(option);
                });
                filters.append(search, type);
                const resultCount = element('p', 'wc-profile-subtitle');
                resultCount.setAttribute('role', 'status');
                const grid = element('div', 'wc-profile-titles');
                const more = button('Weitere Titel anzeigen', () => { limit += 24; renderTitles(); });
                let category = 'started';
                let limit = 24;
                function renderTitles() {
                    const matching = items.filter(item => field(item, 'Category') === category
                        && (!type.value || field(item, 'Type') === type.value)
                        && field(item, 'Name').toLocaleLowerCase().includes(search.value.toLocaleLowerCase()));
                    tabs.querySelectorAll('button').forEach(tab => tab.setAttribute('aria-pressed', String(tab.dataset.category === category)));
                    grid.replaceChildren();
                    resultCount.textContent = matching.length + ' Titel' + (category === 'favorites' ? ' · Nur Favoriten, die ' + name + ' noch nicht begonnen hat.' : '');
                    matching.slice(0, limit).forEach(item => {
                        const card = element('article', 'wc-profile-title-card');
                        const link = element('a', 'wc-profile-title-link');
                        const serverId = typeof ApiClient.serverId === 'function' ? ApiClient.serverId() : '';
                        link.href = '#/details?id=' + encodeURIComponent(field(item, 'Id')) + (serverId ? '&serverId=' + encodeURIComponent(serverId) : '');
                        link.addEventListener('click', () => dialog.close());
                        const poster = element('div', 'wc-profile-poster', field(item, 'Type') === 'Series' ? 'TV' : '▶');
                        if (field(item, 'HasImage')) {
                            const img = element('img'); img.alt = ''; img.loading = 'lazy';
                            img.src = ApiClient.getUrl('Items/' + encodeURIComponent(field(item, 'Id')) + '/Images/Primary', { maxWidth: 160, quality: 85 });
                            img.addEventListener('error', () => img.remove(), { once: true }); poster.append(img);
                        }
                        const copy = element('div');
                        copy.append(element('h3', '', field(item, 'Name')), element('span', 'wc-profile-subtitle', (field(item, 'Type') === 'Series' ? 'Serie' : 'Film') + (field(item, 'Year') ? ' · ' + field(item, 'Year') : '')));
                        link.append(poster, copy);
                        card.append(link, progressRow(name, field(item, 'Member'), field(item, 'Type') === 'Series', false), progressRow('Du', field(item, 'You'), field(item, 'Type') === 'Series', true));
                        grid.append(card);
                    });
                    if (!matching.length) grid.append(element('p', 'wc-profile-message', search.value || type.value ? 'Keine passenden Titel gefunden.' : 'Hier gibt es noch keine Titel.'));
                    more.hidden = matching.length <= limit;
                }
                Object.keys(categories).forEach(key => {
                    const count = items.filter(item => field(item, 'Category') === key).length;
                    const tab = button(categories[key] + ' · ' + count, () => { category = key; limit = 24; renderTitles(); }, 'wc-profile-tab');
                    tab.dataset.category = key;
                    tabs.append(tab);
                });
                search.addEventListener('input', () => { limit = 24; renderTitles(); });
                type.addEventListener('change', () => { limit = 24; renderTitles(); });
                body.append(intro, tabs, filters, resultCount, grid, more);
                renderTitles();
                back.focus();
            }).catch(() => { if (valid(version)) failure('Das Profil konnte nicht geladen werden. Möglicherweise besteht keine gemeinsame Gruppe mehr.', () => showProfile(user)); });
        }
        dialog.showModal();
        showDirectory();
    }
    window.WatchCircleProfiles = { show: show };
})();
