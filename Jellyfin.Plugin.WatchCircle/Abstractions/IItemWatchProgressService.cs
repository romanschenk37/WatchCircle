using System;
using System.Collections.Generic;
using Jellyfin.Plugin.WatchCircle.Api;

namespace Jellyfin.Plugin.WatchCircle.Abstractions;

/// <summary>
/// Determines which group members have started watching media items, episodes, or season episodes.
/// </summary>
public interface IItemWatchProgressService
{
    /// <summary>
    /// Gets group members who have started the specified items.
    /// </summary>
    /// <param name="currentUserId">The authenticated user identifier.</param>
    /// <param name="itemIds">The media item identifiers.</param>
    /// <returns>Overlay data keyed by item identifier.</returns>
    IReadOnlyDictionary<Guid, ItemOverlayDto> GetItemOverlays(
        Guid currentUserId,
        IReadOnlyList<Guid> itemIds);
}
