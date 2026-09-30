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

    let profilesLoading;
    function showProfiles(memberId) {
        if (!profilesLoading) {
            profilesLoading = Promise.all([
                loadProfileModule('profileNavigation', 'WatchCircleProfileNavigation'),
                loadProfileModule('profiles', 'WatchCircleProfiles')
            ]).catch(function (error) { profilesLoading = null; throw error; });
        }
        return profilesLoading.then(function () { WatchCircleProfiles.show(memberId); }).catch(function () {
            window.alert('WatchCircle konnte nicht geladen werden. Bitte versuche es erneut.');
        });
    }
    function loadProfileModule(file, globalName) {
        return new Promise(function (resolve, reject) {
            if (window[globalName]) { resolve(); return; }
            let script = document.createElement('script');
            script.src = getAssetUrl('components/profiles/' + file + '.js');
            script.addEventListener('load', function () {
                if (window[globalName]) resolve();
                else { script.remove(); reject(new Error('Profile module unavailable')); }
            }, { once: true });
            script.addEventListener('error', function () { script.remove(); reject(new Error('Profile module unavailable')); }, { once: true });
            document.head.appendChild(script);
        });
    }
    window.WatchCircleAssets = { getUrl: getAssetUrl, showProfiles: showProfiles };
})();
