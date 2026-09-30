using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.WatchCircle.Abstractions;
using Jellyfin.Plugin.WatchCircle.Api;

namespace Jellyfin.Plugin.WatchCircle.Services;

/// <summary>
/// Builds overlay data for the web client.
/// </summary>
public class WatchCircleOverlayService : IWatchCircleOverlayService
{
    private const int MaxItemsPerRequest = 50;

    private readonly IItemWatchProgressService _itemWatchProgressService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchCircleOverlayService"/> class.
    /// </summary>
    /// <param name="itemWatchProgressService">The item watch progress service.</param>
    public WatchCircleOverlayService(IItemWatchProgressService itemWatchProgressService)
    {
        _itemWatchProgressService = itemWatchProgressService;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, ItemOverlayDto> GetOverlaysForUser(Guid userId, IReadOnlyList<Guid> itemIds)
    {
        if (itemIds.Count == 0 || userId == Guid.Empty)
        {
            return new Dictionary<Guid, ItemOverlayDto>();
        }

        var limitedItemIds = itemIds.Count <= MaxItemsPerRequest
            ? itemIds
            : itemIds.Take(MaxItemsPerRequest).ToList();

        return _itemWatchProgressService.GetItemOverlays(userId, limitedItemIds);
    }
}
