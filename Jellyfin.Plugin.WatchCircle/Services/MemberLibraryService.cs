using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.WatchCircle.Abstractions;
using Jellyfin.Plugin.WatchCircle.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Plugin.WatchCircle.Services;

/// <summary>Reads existing Jellyfin progress and favorites for shared-group profiles.</summary>
public class MemberLibraryService
{
    private readonly IGroupMembershipService _groups;
    private readonly IUserProfileService _profiles;
    private readonly ILibraryManager _library;
    private readonly IUserManager _users;
    private readonly IDbContextFactory<JellyfinDbContext> _database;

    /// <summary>Initializes a new instance of the <see cref="MemberLibraryService"/> class.</summary>
    /// <param name="groups">Group visibility.</param>
    /// <param name="profiles">User display metadata.</param>
    /// <param name="library">Library access.</param>
    /// <param name="users">Jellyfin users.</param>
    /// <param name="database">Existing user data.</param>
    public MemberLibraryService(IGroupMembershipService groups, IUserProfileService profiles, ILibraryManager library, IUserManager users, IDbContextFactory<JellyfinDbContext> database)
    {
        _groups = groups;
        _profiles = profiles;
        _library = library;
        _users = users;
        _database = database;
    }

    /// <summary>Gets a profile only while both users share a group.</summary>
    /// <param name="viewerId">Authenticated viewer.</param>
    /// <param name="memberId">Requested member.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>A profile, or null when inaccessible.</returns>
    public MemberProfileDto? GetProfile(Guid viewerId, Guid memberId, CancellationToken cancellationToken)
    {
        if (viewerId == Guid.Empty || viewerId == memberId || !_groups.GetVisibleMemberIds(viewerId).Contains(memberId))
        {
            return null;
        }

        var member = _profiles.MapUser(memberId);
        if (member is null || _users.GetUserById(viewerId) is null)
        {
            return null;
        }

        using var database = _database.CreateDbContext();
        // Two users, a single read, no plugin-owned history or persistent cache.
        var rows = database.UserData.AsNoTracking()
            .Where(row => (row.UserId == memberId || row.UserId == viewerId)
                && (row.Played || row.PlayCount > 0 || row.PlaybackPositionTicks > 0 || row.LastPlayedDate != null || row.IsFavorite))
            .ToList();
        var items = BuildItems(viewerId, memberId, rows, cancellationToken);

        // Do not publish a long-running result after the member was removed from the group.
        return _groups.GetVisibleMemberIds(viewerId).Contains(memberId)
            ? new MemberProfileDto { User = member, Items = items }
            : null;
    }

    internal IReadOnlyList<MemberTitleDto> BuildItems(Guid viewerId, Guid memberId, IReadOnlyList<UserData> rows, CancellationToken cancellationToken = default)
    {
        var viewer = _users.GetUserById(viewerId);
        if (viewer is null)
        {
            return Array.Empty<MemberTitleDto>();
        }

        var memberRows = rows.Where(row => row.UserId == memberId).ToLookup(row => row.ItemId);
        var viewerRows = rows.Where(row => row.UserId == viewerId).ToLookup(row => row.ItemId);
        var titles = new Dictionary<Guid, BaseItem>();
        foreach (var itemId in memberRows.Select(group => group.Key))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = _library.GetItemById<BaseItem>(itemId, viewerId);
            if (item is Episode episode)
            {
                item = episode.SeriesId == Guid.Empty ? null : _library.GetItemById<BaseItem>(episode.SeriesId, viewerId);
            }

            if (item is Movie or Series)
            {
                titles[item.Id] = item;
            }
        }

        var result = new List<MemberTitleDto>();
        foreach (var item in titles.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var isSeries = item is Series;
            IReadOnlyList<BaseItem> progressItems = isSeries
                ? TitleProgressCalculator.GetAvailableEpisodes(_library, viewer, item.Id)
                : new[] { item };
            // The same visible, available episodes form both denominators.
            var theirs = TitleProgressCalculator.Calculate(progressItems, memberRows, isSeries);
            var yours = TitleProgressCalculator.Calculate(progressItems, viewerRows, isSeries);
            var favorite = memberRows[item.Id].Any(row => row.IsFavorite);
            var category = theirs.Completed ? "completed" : theirs.Started ? "started" : favorite ? "favorites" : null;
            if (category is null)
            {
                continue;
            }

            result.Add(new MemberTitleDto
            {
                Id = item.Id,
                Name = item.Name,
                Type = isSeries ? "Series" : "Movie",
                Year = item.ProductionYear,
                HasImage = item.HasImage(ImageType.Primary),
                Category = category,
                Member = theirs,
                You = yours
            });
        }

        return result.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.Id).ToList();
    }
}
