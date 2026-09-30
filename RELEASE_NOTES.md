# WatchCircle 1.0.4.0 — one shared progress card

Movie, series and season detail pages now show **one WatchCircle card** with a row for each person who has started the title and shares at least one watch group with you.

- Each person appears once, even if you share several groups.
- Only people with existing Jellyfin viewing progress are included. Completed titles also count.
- People who have not started the title, people outside your groups and your own account are excluded. If nobody qualifies, the card stays hidden.
- Each row shows an avatar, username, progress bar and watched time or completion status.
- Series and seasons include the person's furthest started episode and its progress. **Episode finished** applies to that episode, not the whole show.
- This display reads existing Jellyfin data; it does not record additional viewing history.

Automatic group membership from 1.0.3.0 remains available. The inherited Watch together feature is still present and will be removed separately.

Validation: eleven automated group tests, JavaScript syntax check and browser checks with simulated Jellyfin data for films, series, empty results, many people, equal display names and progress refresh. Runtime verification on your Jellyfin 12.1 server is still pending.

Update WatchCircle through the existing plugin catalog, restart Jellyfin when convenient, then reload the web client with **Ctrl + F5**.

Catalog: `https://raw.githubusercontent.com/romanschenk37/WatchCircle/master/manifest.json`
