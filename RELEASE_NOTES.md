# WatchCircle 1.0.2.0 — first test release

WatchCircle now has its own name, plugin ID, settings page, API routes and client assets, separate from Binge Buddy.

This release is intended to test installation and the renamed plugin on Jellyfin 12.1. The catalog minimum remains Jellyfin 12.0, as inherited from the upstream plugin. Runtime verification on a Jellyfin server is still pending.

The inherited individual progress cards and Watch together feature are still present. The planned single shared progress card, automatic group membership and removal of Watch together are not implemented yet. Existing Binge Buddy groups are not imported automatically.

After publication, add this repository in Jellyfin under **Dashboard → Plugins → Repositories**:

`https://raw.githubusercontent.com/romanschenk37/WatchCircle/master/manifest.json`

Install **WatchCircle** from the catalog, restart Jellyfin and reload the web client. Disable Binge Buddy while testing to avoid showing both plugins' interfaces.
