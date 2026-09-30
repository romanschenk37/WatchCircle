using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.WatchCircle.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.WatchCircle.Services;

/// <summary>Shared, read-only progress rules for detail pages and profiles.</summary>
internal static class TitleProgressCalculator
{
    internal static IReadOnlyList<BaseItem> GetAvailableEpisodes(ILibraryManager library, User viewer, Guid parentId)
    {
        return library.GetItemList(new InternalItemsQuery(viewer)
        {
            ParentId = parentId,
            Recursive = true,
            IncludeItemTypes = new[] { BaseItemKind.Episode },
            IsVirtualItem = false,
            IsMissing = false,
            EnableTotalRecordCount = false
        }).DistinctBy(item => item.Id).ToArray();
    }

    internal static LibraryProgressDto Calculate(IReadOnlyList<BaseItem> items, ILookup<Guid, UserData> rows, bool episodic)
    {
        var result = new LibraryProgressDto { TotalEpisodes = episodic ? items.Count : 0, Percent = 0 };
        double fractions = 0;
        foreach (var item in items)
        {
            var data = rows[item.Id].ToList();
            var completed = data.Any(row => row.Played);
            var position = Math.Max(0, data.Select(row => row.PlaybackPositionTicks).DefaultIfEmpty().Max());
            var runtime = Math.Max(0, item.RunTimeTicks ?? 0);
            var started = completed || data.Any(row => row.PlayCount > 0 || row.PlaybackPositionTicks > 0 || row.LastPlayedDate.HasValue);
            result.Started |= started;
            var fraction = completed ? 1 : runtime > 0 ? Math.Clamp((double)position / runtime, 0, 0.9999) : 0;
            fractions += fraction;
            if (episodic)
            {
                result.CompletedEpisodes += completed ? 1 : 0;
            }
            else
            {
                result.Completed = completed;
                result.PositionTicks = completed && runtime > 0 ? runtime : position;
                result.RuntimeTicks = runtime;
                result.Percent = started && !completed && runtime == 0 ? null : fraction * 100;
            }
        }

        if (episodic && items.Count > 0)
        {
            result.Episode = ItemWatchProgressService.GetFurthestEpisodeProgress(items, rows);
            result.Completed = result.CompletedEpisodes == items.Count;
            result.Percent = fractions / items.Count * 100;
        }

        return result;
    }
}
