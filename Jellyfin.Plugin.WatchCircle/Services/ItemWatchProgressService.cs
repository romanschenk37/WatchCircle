using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.WatchCircle.Abstractions;
using Jellyfin.Plugin.WatchCircle.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Plugin.WatchCircle.Services;

/// <summary>
/// Resolves group members who have started watching media items.
/// </summary>
public class ItemWatchProgressService : IItemWatchProgressService
{
    private const int OverlayAvatarSize = 64;

    private readonly IGroupMembershipService _groupMembershipService;
    private readonly IUserProfileService _userProfileService;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemWatchProgressService"/> class.
    /// </summary>
    /// <param name="groupMembershipService">The group membership service.</param>
    /// <param name="userProfileService">The user profile service.</param>
    /// <param name="libraryManager">The Jellyfin library manager.</param>
    /// <param name="userManager">The Jellyfin user manager.</param>
    /// <param name="dbContextFactory">The Jellyfin database context factory.</param>
    public ItemWatchProgressService(
        IGroupMembershipService groupMembershipService,
        IUserProfileService userProfileService,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IDbContextFactory<JellyfinDbContext> dbContextFactory)
    {
        _groupMembershipService = groupMembershipService;
        _userProfileService = userProfileService;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, ItemOverlayDto> GetItemOverlays(
        Guid currentUserId,
        IReadOnlyList<Guid> itemIds)
    {
        var result = new Dictionary<Guid, ItemOverlayDto>();
        if (itemIds.Count == 0)
        {
            return result;
        }

        var distinctItemIds = itemIds.Distinct().ToList();
        var visibleMemberIds = _groupMembershipService.GetVisibleMemberIds(currentUserId);
        var itemContexts = new Dictionary<Guid, OverlayItemContext>();

        foreach (var itemId in distinctItemIds)
        {
            if (!TryResolveOverlayItem(itemId, currentUserId, out _, out var context))
            {
                result[itemId] = CreateEmptyOverlay();
                continue;
            }

            itemContexts[itemId] = context;
        }

        if (itemContexts.Count == 0)
        {
            return result;
        }

        var overlayProgress = LoadOverlayProgress(itemContexts, visibleMemberIds, currentUserId);

        foreach (var (itemId, context) in itemContexts)
        {
            var progress = overlayProgress.GetValueOrDefault(itemId);
            var currentUserProgress = progress?.CurrentUser ?? new WatchProgressDto();
            var watcherProgress = progress?.Watchers.Values.ToList() ?? new List<MemberWatchProgress>();

            var watchers = watcherProgress
                .Select(memberProgress =>
                {
                    var watcher = _userProfileService.MapWatcher(
                        memberProgress.UserId,
                        memberProgress.Played,
                        memberProgress.PlaybackPositionTicks,
                        OverlayAvatarSize,
                        memberProgress.EpisodeIndexNumber,
                        memberProgress.EpisodeRunTimeTicks,
                        memberProgress.SeasonIndexNumber);
                    if (watcher is not null)
                    {
                        watcher.Aggregate = memberProgress.Aggregate;
                    }

                    return watcher;
                })
                .Where(watcher => watcher is not null)
                .Select(watcher => watcher!)
                .OrderBy(watcher => watcher.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            result[itemId] = new ItemOverlayDto
            {
                IsSeason = context.IsSeason,
                IsSeries = context.IsSeries,
                RunTimeTicks = context.RunTimeTicks,
                CurrentUser = currentUserProgress,
                Watchers = watchers
            };
        }

        return result;
    }

    private static ItemOverlayDto CreateEmptyOverlay()
    {
        return new ItemOverlayDto
        {
            RunTimeTicks = 0,
            CurrentUser = new WatchProgressDto(),
            Watchers = Array.Empty<GroupWatcherDto>()
        };
    }

    private Dictionary<Guid, ItemProgressSnapshot> LoadOverlayProgress(
        IReadOnlyDictionary<Guid, OverlayItemContext> itemContexts,
        IReadOnlyList<Guid> memberIds,
        Guid currentUserId)
    {
        using var context = _dbContextFactory.CreateDbContext();

        var trackedUserIds = memberIds
            .Append(currentUserId)
            .Distinct()
            .ToList();

        var progressItemToOverlayIds = new Dictionary<Guid, List<Guid>>();
        foreach (var (overlayItemId, overlayContext) in itemContexts)
        {
            foreach (var progressItemId in overlayContext.ProgressItemIds)
            {
                if (!progressItemToOverlayIds.TryGetValue(progressItemId, out var overlayItemIds))
                {
                    overlayItemIds = new List<Guid>();
                    progressItemToOverlayIds[progressItemId] = overlayItemIds;
                }

                overlayItemIds.Add(overlayItemId);
            }
        }

        var progressItemIds = progressItemToOverlayIds.Keys.ToList();
        var rows = context.UserData
            .AsNoTracking()
            .Where(userData => progressItemIds.Contains(userData.ItemId) && trackedUserIds.Contains(userData.UserId))
            .ToList();

        var snapshots = itemContexts.Keys.ToDictionary(
            itemId => itemId,
            _ => new ItemProgressSnapshot());

        var userRows = trackedUserIds.ToDictionary(userId => userId, userId => rows.Where(row => row.UserId == userId).ToLookup(row => row.ItemId));
        foreach (var (itemId, overlayContext) in itemContexts.Where(pair => pair.Value.IsSeason || pair.Value.IsSeries))
        {
            var snapshot = snapshots[itemId];
            foreach (var userId in trackedUserIds)
            {
                var aggregate = TitleProgressCalculator.Calculate(overlayContext.Episodes, userRows[userId], episodic: true);
                var episode = aggregate.Episode ?? new WatchProgressDto();
                if (userId == currentUserId)
                {
                    // Keep the nested episode separate to avoid a cyclic DTO graph.
                    snapshot.CurrentUser = new WatchProgressDto
                    {
                        Played = episode.Played,
                        PlaybackPositionTicks = episode.PlaybackPositionTicks,
                        SeasonIndexNumber = episode.SeasonIndexNumber,
                        EpisodeIndexNumber = episode.EpisodeIndexNumber,
                        EpisodeRunTimeTicks = episode.EpisodeRunTimeTicks,
                        Aggregate = aggregate
                    };
                }
                else if (aggregate.Started)
                {
                    snapshot.Watchers[userId] = new MemberWatchProgress(
                        userId,
                        episode.Played,
                        episode.PlaybackPositionTicks,
                        episode.SeasonIndexNumber,
                        episode.EpisodeIndexNumber,
                        episode.EpisodeRunTimeTicks,
                        aggregate);
                }
            }
        }

        foreach (var row in rows)
        {
            if (!progressItemToOverlayIds.TryGetValue(row.ItemId, out var overlayItemIds))
            {
                continue;
            }

            foreach (var overlayItemId in overlayItemIds)
            {
                if (!snapshots.TryGetValue(overlayItemId, out var snapshot))
                {
                    continue;
                }

                if (!itemContexts.TryGetValue(overlayItemId, out var overlayContext))
                {
                    continue;
                }

                if (overlayContext.IsSeason || overlayContext.IsSeries)
                {
                    continue;
                }

                if (row.UserId == currentUserId)
                {
                    snapshot.CurrentUser = MergeWatchProgress(snapshot.CurrentUser, MapWatchProgress(row));
                    continue;
                }

                if (!HasStartedWatching(row))
                {
                    continue;
                }

                var incoming = new MemberWatchProgress(
                    row.UserId,
                    row.Played,
                    row.PlaybackPositionTicks,
                    null,
                    null,
                    0);

                snapshot.Watchers[row.UserId] = snapshot.Watchers.TryGetValue(row.UserId, out var existing)
                    ? MergeMemberProgress(existing, incoming)
                    : incoming;
            }
        }

        return snapshots;
    }

    private bool TryResolveOverlayItem(
        Guid itemId,
        Guid currentUserId,
        out BaseItem item,
        out OverlayItemContext context)
    {
        item = null!;
        context = null!;

        BaseItem? resolvedItem;
        try
        {
            resolvedItem = _libraryManager.GetItemById<BaseItem>(itemId, currentUserId);
        }
        catch (Exception)
        {
            return false;
        }

        if (resolvedItem is null)
        {
            return false;
        }

        if (resolvedItem is Movie movie)
        {
            item = movie;
            context = new OverlayItemContext
            {
                RunTimeTicks = movie.RunTimeTicks ?? 0,
                ProgressItemIds = new[] { itemId }
            };
            return true;
        }

        if (resolvedItem is Episode episode)
        {
            item = episode;
            context = new OverlayItemContext
            {
                RunTimeTicks = episode.RunTimeTicks ?? 0,
                ProgressItemIds = new[] { itemId }
            };
            return true;
        }

        if (resolvedItem is Season season)
        {
            item = season;
            var episodes = GetAvailableEpisodes(season.Id, currentUserId);
            context = new OverlayItemContext
            {
                IsSeason = true,
                RunTimeTicks = 0,
                ProgressItemIds = episodes.Select(episode => episode.Id).ToList(),
                Episodes = episodes
            };
            return true;
        }

        if (resolvedItem is Series series)
        {
            item = series;
            var episodes = GetAvailableEpisodes(series.Id, currentUserId);
            context = new OverlayItemContext
            {
                IsSeries = true,
                RunTimeTicks = 0,
                ProgressItemIds = episodes.Select(episode => episode.Id).ToList(),
                Episodes = episodes
            };
            return true;
        }

        return false;
    }

    private IReadOnlyList<BaseItem> GetAvailableEpisodes(Guid parentId, Guid userId)
    {
        var user = _userManager.GetUserById(userId);
        return user is null ? Array.Empty<BaseItem>() : TitleProgressCalculator.GetAvailableEpisodes(_libraryManager, user, parentId);
    }

    internal static WatchProgressDto? GetFurthestEpisodeProgress(IReadOnlyList<BaseItem> episodes, ILookup<Guid, UserData> rows)
    {
        WatchProgressDto? result = null;
        foreach (var (itemId, metadata) in BuildEpisodeMetadata(episodes, includeSeasonNumber: true))
        {
            foreach (var row in rows[itemId].Where(HasStartedWatching))
            {
                var incoming = MapEpisodeWatchProgress(row, metadata, includeSeasonNumber: true);
                result = result is null ? incoming : MergeEpisodeWatchProgress(result, incoming, compareSeason: true);
            }
        }

        return result;
    }

    private static Dictionary<Guid, EpisodeMetadata> BuildEpisodeMetadata(
        IReadOnlyList<BaseItem> episodes,
        bool includeSeasonNumber = false)
    {
        var orderedEpisodes = episodes
            .OrderBy(episode => episode.ParentIndexNumber ?? int.MaxValue)
            .ThenBy(episode => episode.IndexNumber ?? int.MaxValue)
            .ThenBy(episode => episode.SortName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(episode => episode.Id)
            .ToList();

        var metadata = new Dictionary<Guid, EpisodeMetadata>();
        for (var i = 0; i < orderedEpisodes.Count; i++)
        {
            var episode = orderedEpisodes[i];
            metadata[episode.Id] = new EpisodeMetadata(
                includeSeasonNumber ? ResolveSeasonNumber(episode) : null,
                ResolveEpisodeNumber(episode, i + 1),
                episode.RunTimeTicks ?? 0);
        }

        return metadata;
    }

    private static int? ResolveSeasonNumber(BaseItem episode)
    {
        if (episode is Episode tvEpisode && tvEpisode.ParentIndexNumber is >= 0)
        {
            return tvEpisode.ParentIndexNumber.Value;
        }

        return null;
    }

    private static int ResolveEpisodeNumber(BaseItem episode, int fallbackNumber)
    {
        if (episode.IndexNumber is > 0)
        {
            return episode.IndexNumber.Value;
        }

        return fallbackNumber;
    }

    private static WatchProgressDto MapWatchProgress(UserData userData)
    {
        return new WatchProgressDto
        {
            Played = userData.Played,
            PlaybackPositionTicks = userData.PlaybackPositionTicks
        };
    }

    private static WatchProgressDto MapEpisodeWatchProgress(
        UserData userData,
        EpisodeMetadata episodeMetadata,
        bool includeSeasonNumber)
    {
        return new WatchProgressDto
        {
            Played = userData.Played,
            PlaybackPositionTicks = userData.PlaybackPositionTicks,
            EpisodeIndexNumber = episodeMetadata.IndexNumber,
            SeasonIndexNumber = includeSeasonNumber ? episodeMetadata.SeasonIndexNumber : null,
            EpisodeRunTimeTicks = episodeMetadata.RunTimeTicks
        };
    }

    private static WatchProgressDto MergeEpisodeWatchProgress(
        WatchProgressDto existing,
        WatchProgressDto incoming,
        bool compareSeason)
    {
        var comparison = CompareEpisodePosition(
            existing.SeasonIndexNumber,
            existing.EpisodeIndexNumber,
            incoming.SeasonIndexNumber,
            incoming.EpisodeIndexNumber,
            compareSeason);

        if (comparison < 0)
        {
            return incoming;
        }

        if (comparison > 0)
        {
            return existing;
        }

        if (incoming.Played)
        {
            return incoming;
        }

        if (existing.Played)
        {
            return existing;
        }

        return incoming.PlaybackPositionTicks > existing.PlaybackPositionTicks
            ? incoming
            : existing;
    }

    private static int CompareEpisodePosition(
        int? existingSeason,
        int? existingEpisode,
        int? incomingSeason,
        int? incomingEpisode,
        bool compareSeason)
    {
        if (compareSeason)
        {
            var seasonComparison = (existingSeason ?? -1).CompareTo(incomingSeason ?? -1);
            if (seasonComparison != 0)
            {
                return seasonComparison;
            }
        }

        return (existingEpisode ?? -1).CompareTo(incomingEpisode ?? -1);
    }

    private static WatchProgressDto MergeWatchProgress(WatchProgressDto existing, WatchProgressDto incoming)
    {
        if (incoming.Played)
        {
            return incoming;
        }

        if (existing.Played)
        {
            return existing;
        }

        return incoming.PlaybackPositionTicks > existing.PlaybackPositionTicks
            ? incoming
            : existing;
    }

    private static MemberWatchProgress MergeMemberProgress(MemberWatchProgress existing, MemberWatchProgress incoming)
    {
        if (incoming.Played)
        {
            return incoming;
        }

        if (existing.Played)
        {
            return existing;
        }

        return incoming.PlaybackPositionTicks > existing.PlaybackPositionTicks
            ? incoming
            : existing;
    }

    private static bool HasStartedWatching(UserData userData)
    {
        return userData.Played
            || userData.PlayCount > 0
            || userData.PlaybackPositionTicks > 0
            || userData.LastPlayedDate.HasValue;
    }

    private sealed record EpisodeMetadata(int? SeasonIndexNumber, int IndexNumber, long RunTimeTicks);

    private sealed record MemberWatchProgress(
        Guid UserId,
        bool Played,
        long PlaybackPositionTicks,
        int? SeasonIndexNumber,
        int? EpisodeIndexNumber,
        long EpisodeRunTimeTicks,
        LibraryProgressDto? Aggregate = null);

    private sealed class OverlayItemContext
    {
        public bool IsSeason { get; init; }

        public bool IsSeries { get; init; }

        public long RunTimeTicks { get; init; }

        public IReadOnlyList<Guid> ProgressItemIds { get; init; } = Array.Empty<Guid>();

        public IReadOnlyList<BaseItem> Episodes { get; init; } = Array.Empty<BaseItem>();
    }

    private sealed class ItemProgressSnapshot
    {
        public WatchProgressDto CurrentUser { get; set; } = new();

        public Dictionary<Guid, MemberWatchProgress> Watchers { get; } = new();
    }
}
