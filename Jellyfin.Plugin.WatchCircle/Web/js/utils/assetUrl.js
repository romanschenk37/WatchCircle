(function () {
    'use strict';

    if (window.WatchCircleAssets) {
        return;
    }

    function getAssetUrl(relativePath) {
        let path = (relativePath || '').replace(/^\/+/, '');

        if (typeof ApiClient !== 'undefined' && ApiClient.getUrl) {
            return ApiClient.getUrl('WatchCircle/js/' + path);
        }

        return '/WatchCircle/js/' + path;
    }

    window.WatchCircleAssets = {
        getUrl: getAssetUrl
    };
})();
