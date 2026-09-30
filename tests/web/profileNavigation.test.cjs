const test = require('node:test');
const assert = require('node:assert/strict');
const navigation = require('../../Jellyfin.Plugin.WatchCircle/Web/js/components/profiles/profileNavigation.js');

test('desktop and legacy TV arrow keys map to the same movement', () => {
    assert.equal(navigation.keyAction('ArrowLeft', 0), 'left');
    assert.equal(navigation.keyAction('Right', 0), 'right');
    assert.equal(navigation.keyAction('Unidentified', 38), 'up');
    assert.equal(navigation.keyAction('', 40), 'down');
});

test('webOS and Samsung Back keys map to profile back, along with desktop Escape', () => {
    for (const code of [461, 10009, 27, 8]) assert.equal(navigation.keyAction('Unidentified', code), 'back');
    for (const key of ['Escape', 'BrowserBack', 'GoBack', 'Backspace']) assert.equal(navigation.keyAction(key, 0), 'back');
});

test('OK and Tab are supported without intercepting unrelated media controls', () => {
    assert.equal(navigation.keyAction('Enter', 13), 'select');
    assert.equal(navigation.keyAction('Accept', 0), 'select');
    assert.equal(navigation.keyAction('Tab', 9), 'tab');
    assert.equal(navigation.keyAction('MediaPlayPause', 179), null);
    assert.equal(navigation.keyAction('VolumeUp', 175), null);
});

test('moving between shelves uses the nearest visible column after horizontal scrolling', () => {
    const rects = [{left:-320,width:180},{left:-100,width:180},{left:120,width:180},{left:340,width:180}];
    assert.equal(navigation.nearestColumn(rects, 225), 2);
    assert.equal(navigation.nearestColumn(rects, 500), 3);
});

test('responsive grids form rows even with fractional pixel offsets', () => {
    const rects = [{top:100.25}, {top:100.3}, {top:450}, {top:450.1}, {top:799.9}];
    assert.deepEqual(navigation.gridRows(rects), [[0,1],[2,3],[4]]);
});

test('one-column grids and empty collections are handled', () => {
    assert.deepEqual(navigation.gridRows([{top:20},{top:350},{top:680}]), [[0],[1],[2]]);
    assert.deepEqual(navigation.gridRows([]), []);
    assert.equal(navigation.nearestColumn([], 0), -1);
});

test('people search keeps spaces, caret movement and deletion, but TV Back still leaves the view', () => {
    assert.equal(navigation.editsText('select', ' ', 32), true);
    assert.equal(navigation.editsText('left', 'ArrowLeft', 37), true);
    assert.equal(navigation.editsText('back', 'Backspace', 8), true);
    assert.equal(navigation.editsText('back', 'Unidentified', 461), false);
    assert.equal(navigation.editsText('back', 'Escape', 27), false);
    assert.equal(navigation.editsText('down', 'ArrowDown', 40), false);
});
