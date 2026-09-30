using System;
using Jellyfin.Plugin.WatchCircle.Api;

namespace Jellyfin.Plugin.WatchCircle.Abstractions;

/// <summary>
/// Resolves pending watch-together media queues for the web client.
/// </summary>
public interface IWatchTogetherQueueService
{
    /// <summary>
    /// Gets pending watch-together media grouped by host for the current user.
    /// </summary>
    /// <param name="userId">The current Jellyfin user identifier.</param>
    /// <returns>The grouped queue payload.</returns>
    WatchTogetherQueueResponse GetQueueForUser(Guid userId);
}
