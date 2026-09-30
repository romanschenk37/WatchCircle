(function () {
    'use strict';
    if (window.WatchCircleProfiles) return;

    let activeView = null;
    const collections = [
        { key: 'started-movies', category: 'started', type: 'Movie', title: 'Begonnene Filme' },
        { key: 'started-series', category: 'started', type: 'Series', title: 'Begonnene Serien' },
        { key: 'completed-movies', category: 'completed', type: 'Movie', title: 'Abgeschlossene Filme' },
        { key: 'completed-series', category: 'completed', type: 'Series', title: 'Abgeschlossene Serien' },
        { key: 'favorite-movies', category: 'favorites', type: 'Movie', title: 'Favorisierte Filme' },
        { key: 'favorite-series', category: 'favorites', type: 'Series', title: 'Favorisierte Serien' }
    ];
    const focusSelector = 'button:not([disabled]):not([tabindex="-1"]), a[href], input:not([disabled])';
    function field(value, name) { return value[name] !== undefined ? value[name] : value[name[0].toLowerCase() + name.slice(1)]; }
    function element(tag, className, text) {
        const node = document.createElement(tag);
        node.className = className || '';
        if (text !== undefined) node.textContent = text;
        return node;
    }
    function clear(node) { while (node.firstChild) node.removeChild(node.firstChild); }
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
        img.addEventListener('error', function () { if (img.parentNode) img.parentNode.replaceChild(initial, img); }, { once: true });
        return img;
    }
    function percentText(progress) {
        const value = field(progress, 'Percent');
        return value === null || value === undefined ? '?' : Math.floor(Math.max(0, Math.min(100, value))) + ' %';
    }
    function progressText(progress, series) {
        if (series) {
            const total = field(progress, 'TotalEpisodes') || 0;
            return total ? (field(progress, 'CompletedEpisodes') || 0) + '/' + total + ' Folgen · ' + percentText(progress) : 'Keine verfügbaren Folgen';
        }
        if (field(progress, 'Completed')) return '100 % · Gesehen';
        if (!field(progress, 'Started')) return 'Nicht begonnen';
        return field(progress, 'Percent') === null ? 'Fortschritt unbekannt' : percentText(progress);
    }
    function progressRow(label, progress, series, own) {
        const row = element('div', 'wc-profile-progress' + (own ? ' wc-profile-progress-you' : ''));
        const meta = element('div', 'wc-profile-progress-meta');
        meta.appendChild(element('span', 'wc-profile-progress-name', label));
        meta.appendChild(element('span', 'wc-profile-progress-status', progressText(progress, series)));
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
        track.appendChild(fill);
        row.appendChild(meta);
        row.appendChild(track);
        return row;
    }

    function show(memberId) {
        if (!ApiClient.getCurrentUserId()) return;
        if (activeView) activeView.close();
        if (!document.getElementById('wc-profiles-css')) {
            const link = document.createElement('link');
            link.id = 'wc-profiles-css'; link.rel = 'stylesheet';
            link.href = WatchCircleAssets.getUrl('components/profiles/profiles.css');
            document.head.appendChild(link);
        }
        const navigation = WatchCircleProfileNavigation;
        const viewerId = ApiClient.getCurrentUserId();
        const previousFocus = document.activeElement;
        // A regular element also works on TV engines without HTMLDialogElement.showModal.
        const dialog = element('div', 'wc-profiles-dialog');
        dialog.setAttribute('role', 'dialog');
        dialog.setAttribute('aria-modal', 'true');
        dialog.setAttribute('aria-labelledby', 'wc-profiles-title');
        const header = element('header', 'wc-profiles-header');
        const title = element('h2', '', 'WatchCircle'); title.id = 'wc-profiles-title';
        const back = button('← Zurück', goBack, 'wc-profile-action wc-profile-back'); back.hidden = true;
        const close = button('Schließen', closeView, 'wc-profile-action wc-profile-close');
        close.setAttribute('aria-label', 'WatchCircle schließen');
        header.appendChild(back); header.appendChild(title); header.appendChild(close);
        const body = element('div', 'wc-profiles-body');
        dialog.appendChild(header); dialog.appendChild(body);
        const previousOverflow = document.body.style.overflow;
        document.body.style.overflow = 'hidden';
        document.body.appendChild(dialog);
        activeView = { close: closeView };
        const thisView = activeView;
        let isOpen = true;
        let requestVersion = 0;
        let view = 'directory';
        let selectedUser = null;
        let profileItems = [];
        let directorySearch = '';
        let profilePosition = null;
        let heldKeys = new Set();
        let releaseTimer;

        function valid(version) { return isOpen && version === requestVersion && ApiClient.getCurrentUserId() === viewerId; }
        function focusable(root) {
            return Array.from(root.querySelectorAll(focusSelector)).filter(node => !node.hidden && node.getClientRects().length && node.offsetWidth > 0);
        }
        function reveal(node) {
            const rail = node.closest('.wc-profile-rail');
            if (rail) {
                const bounds = rail.getBoundingClientRect();
                const item = node.getBoundingClientRect();
                if (item.left < bounds.left + 10) rail.scrollLeft -= bounds.left + 10 - item.left;
                else if (item.right > bounds.right - 10) rail.scrollLeft += item.right - bounds.right + 10;
            }
            if (body.contains(node)) {
                const item = node.getBoundingClientRect();
                const bounds = body.getBoundingClientRect();
                if (item.top < bounds.top + 16) body.scrollTop -= bounds.top + 16 - item.top;
                else if (item.bottom > bounds.bottom - 16) body.scrollTop += item.bottom - bounds.bottom + 16;
            }
        }
        function focus(node) {
            if (!node) return;
            try { node.focus({ preventScroll: true }); } catch (_) { node.focus(); }
            reveal(node);
        }
        function message(text) {
            clear(body);
            const status = element('p', 'wc-profile-message', text); status.setAttribute('role', 'status');
            body.appendChild(status);
            body.scrollTop = 0;
            focus(back.hidden ? close : back);
        }
        function failure(text, retry) {
            message(text);
            const action = button('Erneut versuchen', retry);
            body.appendChild(action); focus(action);
        }
        function closeView() {
            if (!isOpen) return;
            isOpen = false; requestVersion++;
            window.removeEventListener('hashchange', closeView);
            document.removeEventListener('viewshow', guardSession);
            window.removeEventListener('keydown', onKeyDown, true);
            window.removeEventListener('command', onCommand, true);
            document.removeEventListener('focusin', onFocus, true);
            // Swallow the matching key-up too, so Back/OK cannot also trigger Jellyfin behind this view.
            if (heldKeys.size) releaseTimer = setTimeout(removeKeyUp, 1500);
            else removeKeyUp();
            dialog.remove();
            document.body.style.overflow = previousOverflow;
            if (activeView === thisView) activeView = null;
            if (previousFocus && previousFocus.isConnected) previousFocus.focus();
        }
        function guardSession() { if (ApiClient.getCurrentUserId() !== viewerId) closeView(); }
        function removeKeyUp() { window.removeEventListener('keyup', onKeyUp, true); clearTimeout(releaseTimer); }
        function goBack() {
            if (view === 'collection') renderProfile(profilePosition);
            else if (view === 'profile') showDirectory(selectedUser && field(selectedUser, 'Id'));
            else closeView();
        }
        function onFocus(event) {
            if (!isOpen) return;
            if (!dialog.contains(event.target)) { focus(focusable(body)[0] || close); return; }
            reveal(event.target);
        }
        function navigationRows() {
            const rows = [focusable(header)];
            const grid = body.querySelector('.wc-profile-people, .wc-profile-collection-grid');
            const search = body.querySelector('input');
            if (search) rows.push([search]);
            if (grid) {
                const nodes = focusable(grid);
                navigation.gridRows(nodes.map(node => node.getBoundingClientRect())).forEach(indices => rows.push(indices.map(index => nodes[index])));
                const more = body.querySelector('.wc-profile-load-more');
                if (more && !more.hidden) rows.push([more]);
            } else {
                body.querySelectorAll('.wc-profile-shelf').forEach(shelf => {
                    const nodes = focusable(shelf.querySelector('.wc-profile-rail'));
                    if (nodes.length) rows.push(nodes);
                });
                if (rows.length === 1) { const nodes = focusable(body); if (nodes.length) rows.push(nodes); }
            }
            return rows.filter(row => row.length);
        }
        function move(direction) {
            const rows = navigationRows();
            const current = document.activeElement;
            let rowIndex = rows.findIndex(row => row.indexOf(current) >= 0);
            if (rowIndex < 0) {
                // Shelf headings are reachable with Tab; a down press enters their own rail.
                const shelf = current && current.closest('.wc-profile-shelf');
                if (shelf) { focus(focusable(shelf.querySelector('.wc-profile-rail'))[0]); return; }
                focus((rows[1] || rows[0])[0]); return;
            }
            const row = rows[rowIndex];
            const index = row.indexOf(current);
            if (direction === 'left' || direction === 'right') {
                focus(row[Math.max(0, Math.min(row.length - 1, index + (direction === 'left' ? -1 : 1)))]);
            } else {
                const next = rows[rowIndex + (direction === 'up' ? -1 : 1)];
                if (!next) return;
                const rect = current.getBoundingClientRect();
                focus(next[navigation.nearestColumn(next.map(node => node.getBoundingClientRect()), rect.left + rect.width / 2)]);
            }
        }
        function handle(action, event) {
            const current = document.activeElement;
            const editing = current && current.tagName === 'INPUT';
            if (editing && navigation.editsText(action, event.key, event.keyCode)) return false;
            if (['left', 'right', 'up', 'down'].indexOf(action) >= 0) move(action);
            else if (action === 'back') { if (!event.repeat) goBack(); }
            else if (action === 'select') {
                if (!event.repeat && !editing && dialog.contains(current) && current.matches('a[href], button')) current.click();
            } else if (action === 'tab') {
                const nodes = focusable(dialog);
                const index = nodes.indexOf(current);
                focus(nodes[(index + (event.shiftKey ? -1 : 1) + nodes.length) % nodes.length]);
            } else return false;
            return true;
        }
        function onKeyDown(event) {
            const action = navigation.keyAction(event.key, event.keyCode);
            if (!action || event.altKey || event.ctrlKey || event.metaKey || event.isComposing) return;
            const editing = document.activeElement && document.activeElement.tagName === 'INPUT';
            if (editing && navigation.editsText(action, event.key, event.keyCode)) return;
            heldKeys.add(event.keyCode || event.key);
            event.preventDefault(); event.stopImmediatePropagation();
            handle(action, event);
        }
        function onKeyUp(event) {
            const key = event.keyCode || event.key;
            if (!heldKeys.has(key)) return;
            heldKeys.delete(key);
            event.preventDefault(); event.stopImmediatePropagation();
            if (!isOpen && !heldKeys.size) removeKeyUp();
        }
        function onCommand(event) {
            if (event.detail && handle(event.detail.command, event)) { event.preventDefault(); event.stopImmediatePropagation(); }
        }
        window.addEventListener('hashchange', closeView);
        document.addEventListener('viewshow', guardSession);
        window.addEventListener('keydown', onKeyDown, true);
        window.addEventListener('keyup', onKeyUp, true);
        window.addEventListener('command', onCommand, true);
        document.addEventListener('focusin', onFocus, true);

        function showDirectory(restoreId) {
            view = 'directory';
            const version = ++requestVersion;
            title.textContent = 'WatchCircle'; back.hidden = true;
            message('Personen werden geladen …');
            ApiClient.ajax({ type: 'GET', url: ApiClient.getUrl('WatchCircle/Buddies'), dataType: 'json' }).then(data => {
                if (!valid(version)) return;
                const members = data || [];
                if (memberId) {
                    const requested = memberId; memberId = null;
                    const user = members.find(value => String(field(value, 'Id')).replace(/-/g, '').toLowerCase() === String(requested).replace(/-/g, '').toLowerCase());
                    if (user) { showProfile(user); return; }
                    failure('Dieses Profil ist nicht verfügbar. Ihr müsst mindestens eine gemeinsame Gruppe haben.', showDirectory); return;
                }
                clear(body);
                body.appendChild(element('p', 'wc-profile-subtitle', 'Personen, mit denen du mindestens eine Gruppe teilst.'));
                if (!members.length) {
                    body.appendChild(element('p', 'wc-profile-message', 'Noch keine Personen in gemeinsamen Gruppen.')); return;
                }
                const search = element('input', 'wc-profile-search'); search.type = 'search';
                search.placeholder = 'Person suchen'; search.value = directorySearch;
                search.setAttribute('aria-label', 'Person suchen');
                const grid = element('div', 'wc-profile-people');
                function renderPeople() {
                    directorySearch = search.value;
                    clear(grid);
                    members.filter(user => field(user, 'Name').toLocaleLowerCase().includes(directorySearch.toLocaleLowerCase())).forEach(user => {
                        const card = button('', () => showProfile(user), 'wc-profile-person');
                        card.dataset.userId = field(user, 'Id');
                        card.appendChild(avatar(user)); card.appendChild(element('strong', '', field(user, 'Name')));
                        grid.appendChild(card);
                    });
                    if (!grid.childElementCount) grid.appendChild(element('p', '', 'Keine Person gefunden.'));
                }
                search.addEventListener('input', renderPeople);
                body.appendChild(search); body.appendChild(grid); renderPeople();
                focus(focusable(grid).find(node => node.dataset.userId === restoreId) || focusable(grid)[0] || search);
            }).catch(() => { if (valid(version)) failure('Die Personen konnten nicht geladen werden.', () => showDirectory(restoreId)); });
        }
        function showProfile(user) {
            view = 'profile'; selectedUser = user; profilePosition = null;
            const version = ++requestVersion;
            title.textContent = 'WatchCircle · ' + field(user, 'Name'); back.hidden = false;
            message('Fortschritt und Favoriten werden geladen …');
            ApiClient.ajax({ type: 'GET', url: ApiClient.getUrl('WatchCircle/Profiles/' + encodeURIComponent(field(user, 'Id'))), dataType: 'json' }).then(data => {
                if (!valid(version)) return;
                profileItems = field(data, 'Items') || [];
                renderProfile();
            }).catch(() => { if (valid(version)) failure('Das Profil konnte nicht geladen werden. Möglicherweise besteht keine gemeinsame Gruppe mehr.', () => showProfile(user)); });
        }
        function titleCard(item) {
            const wrapper = element('li', 'wc-profile-title-card');
            const link = element('a', 'wc-profile-title-link'); link.dataset.focusKey = 'item-' + field(item, 'Id');
            const serverId = typeof ApiClient.serverId === 'function' ? ApiClient.serverId() : '';
            link.href = '#/details?id=' + encodeURIComponent(field(item, 'Id')) + (serverId ? '&serverId=' + encodeURIComponent(serverId) : '');
            link.addEventListener('click', closeView);
            const series = field(item, 'Type') === 'Series';
            const poster = element('div', 'wc-profile-poster');
            poster.appendChild(element('span', 'wc-profile-poster-fallback', series ? 'TV' : '▶'));
            poster.setAttribute('aria-hidden', 'true');
            if (field(item, 'HasImage')) {
                const img = element('img'); img.alt = ''; img.loading = 'lazy';
                img.src = ApiClient.getUrl('Items/' + encodeURIComponent(field(item, 'Id')) + '/Images/Primary', { maxWidth: 400, quality: 90 });
                img.addEventListener('error', () => img.remove(), { once: true }); poster.appendChild(img);
            }
            const copy = element('div', 'wc-profile-title-copy');
            const name = element('h3', '', field(item, 'Name')); name.title = field(item, 'Name');
            copy.appendChild(name);
            copy.appendChild(element('span', 'wc-profile-year', (series ? 'Serie' : 'Film') + (field(item, 'Year') ? ' · ' + field(item, 'Year') : '')));
            copy.appendChild(progressRow(field(selectedUser, 'Name'), field(item, 'Member'), series, false));
            copy.appendChild(progressRow('Du', field(item, 'You'), series, true));
            link.appendChild(poster); link.appendChild(copy); wrapper.appendChild(link);
            return wrapper;
        }
        function saveProfilePosition() {
            const rails = {};
            body.querySelectorAll('.wc-profile-rail').forEach(rail => { rails[rail.dataset.collection] = rail.scrollLeft; });
            profilePosition = { top: body.scrollTop, rails: rails, key: document.activeElement.dataset.focusKey };
        }
        function renderProfile(position) {
            view = 'profile'; requestVersion++;
            title.textContent = 'WatchCircle · ' + field(selectedUser, 'Name'); back.hidden = false;
            clear(body);
            const intro = element('div', 'wc-profile-intro');
            const copy = element('div');
            copy.appendChild(element('p', 'wc-profile-legend', field(selectedUser, 'Name') + ' im Vergleich mit dir'));
            copy.appendChild(element('p', 'wc-profile-subtitle', 'Serien: alle verfügbaren Folgen inklusive Specials. Abgeschlossen = aktuell alles gesehen.'));
            intro.appendChild(avatar(selectedUser)); intro.appendChild(copy); body.appendChild(intro);
            collections.forEach(collection => {
                const items = profileItems.filter(item => field(item, 'Category') === collection.category && field(item, 'Type') === collection.type);
                const shelf = element('section', 'wc-profile-shelf');
                const heading = element('div', 'wc-profile-shelf-heading');
                const label = element('h3', '', collection.title); label.id = 'wc-shelf-' + collection.key;
                heading.appendChild(label); heading.appendChild(element('span', 'wc-profile-count', String(items.length)));
                if (items.length) {
                    const all = button('Alle anzeigen ›', () => { saveProfilePosition(); renderCollection(collection, items); }, 'wc-profile-all');
                    all.dataset.focusKey = 'all-' + collection.key;
                    all.setAttribute('aria-label', collection.title + ': alle ' + items.length + ' Titel anzeigen');
                    heading.appendChild(all);
                }
                shelf.appendChild(heading);
                const rail = element('ul', 'wc-profile-rail'); rail.dataset.collection = collection.key;
                rail.setAttribute('aria-labelledby', label.id);
                items.slice(0, 24).forEach(item => rail.appendChild(titleCard(item)));
                if (items.length > 24) {
                    const more = element('li', 'wc-profile-title-card');
                    const open = button('Alle ' + items.length + ' Titel anzeigen →', () => { saveProfilePosition(); renderCollection(collection, items); }, 'wc-profile-more-card');
                    open.dataset.focusKey = 'more-' + collection.key; more.appendChild(open); rail.appendChild(more);
                }
                if (!items.length) shelf.appendChild(element('p', 'wc-profile-empty', 'Noch keine Titel.'));
                shelf.appendChild(rail); body.appendChild(shelf);
            });
            if (position) {
                body.querySelectorAll('.wc-profile-rail').forEach(rail => { rail.scrollLeft = position.rails[rail.dataset.collection] || 0; });
                body.scrollTop = position.top;
                focus(focusable(body).find(node => node.dataset.focusKey === position.key) || back);
            } else {
                body.scrollTop = 0;
                focus(body.querySelector('.wc-profile-title-link') || back);
            }
        }
        function renderCollection(collection, items) {
            view = 'collection'; requestVersion++;
            title.textContent = field(selectedUser, 'Name') + ' · ' + collection.title;
            clear(body); body.scrollTop = 0;
            body.appendChild(element('p', 'wc-profile-subtitle', items.length + ' Titel'));
            const grid = element('ul', 'wc-profile-collection-grid'); grid.setAttribute('aria-label', collection.title);
            let count = 0;
            const more = button('Weitere Titel anzeigen', () => appendItems(true), 'wc-profile-action wc-profile-load-more');
            function appendItems(moveFocus) {
                const start = count;
                count = Math.min(count + 48, items.length);
                items.slice(start, count).forEach(item => grid.appendChild(titleCard(item)));
                more.hidden = count === items.length;
                if (moveFocus) focus(grid.children[start].querySelector('a'));
            }
            body.appendChild(grid); body.appendChild(more); appendItems(false);
            focus(grid.querySelector('a') || back);
        }
        showDirectory();
    }
    window.WatchCircleProfiles = { show: show };
})();
