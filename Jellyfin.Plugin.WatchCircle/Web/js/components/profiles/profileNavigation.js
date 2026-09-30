(function () {
    'use strict';

    // Raw remote keys vary between webOS, Tizen and desktop browsers.
    function keyAction(key, code) {
        const names = { ArrowLeft: 'left', Left: 'left', ArrowRight: 'right', Right: 'right', ArrowUp: 'up', Up: 'up', ArrowDown: 'down', Down: 'down', Enter: 'select', Accept: 'select', ' ': 'select', Spacebar: 'select', Escape: 'back', Backspace: 'back', BrowserBack: 'back', GoBack: 'back', Back: 'back', Tab: 'tab' };
        const codes = { 37: 'left', 39: 'right', 38: 'up', 40: 'down', 13: 'select', 32: 'select', 27: 'back', 8: 'back', 461: 'back', 10009: 'back', 9: 'tab' };
        return names[key] || codes[code] || null;
    }

    function editsText(action, key, code) {
        return action === 'left' || action === 'right' || (action === 'back' && (key === 'Backspace' || code === 8))
            || (action === 'select' && (key === ' ' || key === 'Spacebar' || code === 32));
    }

    // Prefer the same screen column when changing shelves or grid rows.
    function nearestColumn(rects, center) {
        let index = -1;
        let distance = Infinity;
        rects.forEach(function (rect, candidate) {
            const nextDistance = Math.abs(rect.left + rect.width / 2 - center);
            if (nextDistance < distance) { index = candidate; distance = nextDistance; }
        });
        return index;
    }

    // A flat grid becomes rows without relying on a fixed number of TV columns.
    function gridRows(rects) {
        const rows = [];
        rects.forEach(function (rect, index) {
            let row = rows.find(function (value) { return Math.abs(value.top - rect.top) < 8; });
            if (!row) { row = { top: rect.top, indices: [] }; rows.push(row); }
            row.indices.push(index);
        });
        return rows.sort(function (a, b) { return a.top - b.top; }).map(function (row) { return row.indices; });
    }

    const api = { keyAction: keyAction, editsText: editsText, nearestColumn: nearestColumn, gridRows: gridRows };
    if (typeof module !== 'undefined' && module.exports) module.exports = api;
    else window.WatchCircleProfileNavigation = api;
})();
