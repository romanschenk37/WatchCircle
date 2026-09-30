# WatchCircle 1.0.5.0 — Seerr request attribution

WatchCircle can now show **Requested by** for movies and series that were requested through Seerr.

- Shows all requesters, including people outside your WatchCircle groups and people who have not started watching.
- Each requester appears once. Series also show which seasons each person requested.
- Progress rows keep their existing rules: only people sharing a group who have started the title appear there.
- Request information can appear on its own before anyone has started watching.
- Matches titles by their TMDB IDs and reads existing Seerr requests without storing an additional request history.
- Seerr is optional and disabled by default. If it is unavailable, the existing watch progress display continues working.

Configure it under **Dashboard → Plugins → WatchCircle → Seerr requests**: enable the option, enter your Seerr URL and API key, then click **Save and test connection**. The URL must be reachable from the Jellyfin server. The saved key is not returned to regular users or prefilled in the settings page.

The existing automatic group membership and shared progress card remain available. The inherited Watch together feature is still present.

Validation: 29 automated tests, successful release build, JavaScript syntax checks and browser checks using simulated Seerr/Jellyfin data. Your actual Seerr instance has not been connected or tested yet.

Update WatchCircle through the existing plugin catalog, restart Jellyfin when convenient, then reload the web client with **Ctrl + F5**.

Catalog: `https://raw.githubusercontent.com/romanschenk37/WatchCircle/master/manifest.json`
