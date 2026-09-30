# WatchCircle 1.0.3.0 — automatic group membership

Each group now has an **Automatically add new users** option under **Dashboard → Plugins → WatchCircle → Manage group**. Enable it and click **Save group** to automatically include future Jellyfin accounts in that group.

- Disabled by default, including for existing groups.
- Existing accounts are not added retroactively.
- Turning the option off keeps current members.
- Manually removed members stay removed when other users join or Jellyfin restarts.
- Users added automatically while the group editor is open are preserved when you save.
- Uses Jellyfin's user-created event and existing group configuration; this feature collects no watch data and maintains no separate user database.

The inherited individual progress cards and Watch together feature are still present. The planned single shared progress card and removal of Watch together will follow separately. Existing Binge Buddy groups are not imported automatically.

The release has passed automated tests for persistence, defaults, duplicate events, manual removal and concurrent edits, plus browser checks using simulated Jellyfin data. Runtime verification on your Jellyfin 12.1 server is still pending. The catalog minimum remains Jellyfin 12.0, as inherited from upstream.

Add this repository in Jellyfin under **Dashboard → Plugins → Repositories**:

`https://raw.githubusercontent.com/romanschenk37/WatchCircle/master/manifest.json`

Install **WatchCircle** from the catalog, restart Jellyfin and reload the web client. Disable Binge Buddy while testing to avoid showing both plugins' interfaces.
