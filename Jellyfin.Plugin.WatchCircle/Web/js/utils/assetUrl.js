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
            profilesLoading = new Promise(function (resolve, reject) {
                if (window.WatchCircleProfiles) { resolve(); return; }
                let script = document.createElement('script');
                script.src = getAssetUrl('components/profiles/profiles.js');
                script.addEventListener('load', resolve, { once: true });
                script.addEventListener('error', function () { script.remove(); profilesLoading = null; reject(new Error('Profile module unavailable')); }, { once: true });
                document.head.appendChild(script);
            });
        }
        return profilesLoading.then(function () { WatchCircleProfiles.show(memberId); }).catch(function () {
            window.alert('WatchCircle konnte nicht geladen werden. Bitte versuche es erneut.');
        });
    }
    window.WatchCircleAssets = { getUrl: getAssetUrl, showProfiles: showProfiles };
})();
