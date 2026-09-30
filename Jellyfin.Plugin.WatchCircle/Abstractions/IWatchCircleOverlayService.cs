using System;
using System.Collections.Generic;
using Jellyfin.Plugin.WatchCircle.Api;

namespace Jellyfin.Plugin.WatchCircle.Abstractions;

/// <summary>
/// Builds overlay data for the web client.
/// </summary>
public interface IWatchCircleOverlayService
{
    /// <summary>
    /// Gets watcher avatars for the requested items and user.
    /// </summary>
    /// <param name="userId">The authenticated user identifier.</param>
    /// <param name="itemIds">The media item identifiers.</param>
    /// <returns>Watchers keyed by item identifier.</returns>
    IReadOnlyDictionary<Guid, ItemOverlayDto> GetOverlaysForUser(Guid userId, IReadOnlyList<Guid> itemIds);
}
