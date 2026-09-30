using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.WatchCircle.Api;
using Jellyfin.Plugin.WatchCircle.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Reads existing Jellyfin state and restores only explicit watched flags.</summary>
internal sealed class CleanupLibrary : ICleanupLibrary
{
    private readonly ILibraryManager _library;
    private readonly IUserManager _users;
    private readonly IUserDataManager _data;
    private readonly ISessionManager _sessions;

    public CleanupLibrary(ILibraryManager library, IUserManager users, IUserDataManager data, ISessionManager sessions)
    {
        _library = library;
        _users = users;
        _data = data;
        _sessions = sessions;
    }

    public IReadOnlyList<CleanupMedia> Inventory()
    {
        var items = _library.GetItemList(new InternalItemsQuery
        {
            Recursive = true, IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series, BaseItemKind.BoxSet },
            IsVirtualItem = false, IsMissing = false, EnableTotalRecordCount = false
        });
        var result = new List<CleanupMedia>();
        foreach (var item in items)
        {
            var collection = item as BoxSet;
            var children = collection?.GetLinkedChildren().Where(value => value is Movie).Select(value => value.Id).Distinct().ToArray() ?? Array.Empty<Guid>();
            var physical = collection is null ? ProgressItems(item) : Array.Empty<BaseItem>();
            var paths = physical.SelectMany(value => value is IHasMediaSources sources
                ? sources.GetMediaSources(false).Select(source => source.Path)
                : new[] { value.Path }).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            long bytes = 0;
            string? problem = null;
            foreach (var path in paths)
            {
                try
                {
                    ArrClient.NormalizePath(path);
                    bytes += new FileInfo(path).Length;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
                {
                    problem = "Some media files are unavailable. Check the library mount and scan before cleanup.";
                }
            }

            var identity = Identities(item);
            if (collection is null && (identity.Length == 0 || physical.Any(value => Identities(value).Length == 0)))
            {
                problem = "Missing stable movie/episode identity. Watched-state backup cannot be guaranteed.";
            }

            result.Add(new CleanupMedia
            {
                ItemId = item.Id, Name = item.Name, Kind = collection is not null ? "Collection" : item is Series ? "Series" : "Movie",
                Identities = identity, Paths = paths, Members = children, Bytes = bytes, Problem = problem,
                LibraryIds = _library.GetCollectionFolders(item).Select(value => value.Id).ToArray()
            });
        }

        foreach (var media in result.Where(value => value.Kind == "Movie"))
        {
            media.Collections = result.Where(value => value.Kind == "Collection" && value.Members.Contains(media.ItemId)).Select(value => value.ItemId).ToArray();
        }

        return result;
    }

    internal static string[] Identities(BaseItem item)
    {
        var kind = item is Movie ? "Movie" : item is Series ? "Series" : item is BoxSet ? "Collection" : item is Episode ? "Episode" : string.Empty;
        var ids = new List<string>();
        foreach (var provider in new[] { "Tmdb", "Tvdb", "Imdb" })
        {
            if (item.ProviderIds.TryGetValue(provider, out var id) && !string.IsNullOrWhiteSpace(id) && !id.Contains(':', StringComparison.Ordinal))
            {
                ids.Add(kind + ":" + provider + ":" + id.Trim().ToLowerInvariant());
            }
        }

        if (item is Episode episode && episode.Series is { } series && episode.ParentIndexNumber is >= 0 && episode.IndexNumber is > 0
            && (!episode.IndexNumberEnd.HasValue || episode.IndexNumberEnd == episode.IndexNumber))
        {
            ids.AddRange(Identities(series).Select(value => "Episode:" + value + ":S" + episode.ParentIndexNumber + ":E:" + episode.IndexNumber));
        }

        return ids.ToArray();
    }

    private BaseItem[] ProgressItems(BaseItem item)
        => item is Series ? _library.GetItemList(new InternalItemsQuery
        {
            ParentId = item.Id, Recursive = true, IncludeItemTypes = new[] { BaseItemKind.Episode },
            IsVirtualItem = false, IsMissing = false, EnableTotalRecordCount = false
        }).DistinctBy(value => value.Id).ToArray() : new[] { item };

    public IReadOnlyList<CleanupUserState> ReadStates(CleanupMedia media)
    {
        var root = _library.GetItemById(media.ItemId) ?? throw new InvalidOperationException("The library item is no longer available.");
        var items = ProgressItems(root);
        var result = new List<CleanupUserState>();
        foreach (var user in _users.GetUsers())
        {
            var rows = new List<UserData>();
            foreach (var item in items)
            {
                var value = _data.GetUserData(user, item) ?? throw new InvalidOperationException("Jellyfin user data is unavailable; cleanup is blocked.");
                rows.Add(new UserData
                {
                    ItemId = item.Id, UserId = user.Id, Played = value.Played, PlaybackPositionTicks = value.PlaybackPositionTicks,
                    PlayCount = value.PlayCount, LastPlayedDate = value.LastPlayedDate, IsFavorite = value.IsFavorite,
                    CustomDataKey = string.Empty, Item = null!, User = null!
                });
                result.Add(new CleanupUserState
                {
                    UserId = user.Id, ItemId = item.Id, Identities = Identities(item), Played = value.Played, Favorite = value.IsFavorite,
                    LastPlayed = value.LastPlayedDate.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(value.LastPlayedDate.Value, DateTimeKind.Utc)) : null
                });
            }

            var progress = TitleProgressCalculator.Calculate(items, rows.ToLookup(value => value.ItemId), root is Series);
            foreach (var state in result.Where(value => value.UserId == user.Id))
            {
                state.Progress = progress;
            }

            if (root is Series)
            {
                var rootData = _data.GetUserData(user, root) ?? throw new InvalidOperationException("Jellyfin user data is unavailable; cleanup is blocked.");
                result.Add(new CleanupUserState { ItemId = root.Id, UserId = user.Id, Identities = Identities(root), Favorite = rootData.IsFavorite, Played = rootData.Played, Progress = progress });
            }
        }

        return result;
    }

    public IReadOnlyList<CleanupUser> Users() => _users.GetUsers().Select(value => new CleanupUser(value.Id, value.Username)).ToArray();

    public Guid RootItemId(Guid itemId)
    {
        var item = _library.GetItemById(itemId);
        return item is Episode episode ? episode.SeriesId : item is Season season ? season.SeriesId : itemId;
    }

    public bool CanAccess(Guid itemId, Guid userId)
    {
        try
        {
            return _library.GetItemById<BaseItem>(itemId, userId) is not null;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    public bool IsPlaying(IReadOnlyList<CleanupEntry> scope)
    {
        var ids = scope.Select(value => value.Media.ItemId).ToHashSet();
        return _sessions.Sessions.Any(session => session.NowPlayingItem is { } item
            && (ids.Contains(item.Id) || (item.SeriesId.HasValue && ids.Contains(item.SeriesId.Value))));
    }

    public bool FilesAbsent(string[] paths)
    {
        // A disconnected library root is not evidence that the files were deleted.
        var roots = _library.GetVirtualFolders().SelectMany(value => value.Locations).ToArray();
        foreach (var path in paths)
        {
            var root = roots.Where(value => ArrClient.Under(ArrClient.NormalizePath(path), ArrClient.NormalizePath(value))).OrderByDescending(value => value.Length).FirstOrDefault();
            if (root is null || !Directory.Exists(root))
            {
                throw new IOException("Cannot verify deletion while a library mount is unavailable.");
            }

            try
            {
                File.GetAttributes(path);
                return false;
            }
            catch (FileNotFoundException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        return true;
    }

    public Task RefreshAsync(CancellationToken cancellationToken)
        => _library.ValidateMediaLibrary(new Progress<double>(), cancellationToken);

    public void RestorePlayed(Guid userId, Guid itemId, bool played)
    {
        var user = _users.GetUserById(userId) ?? throw new InvalidOperationException("The archived user no longer exists.");
        var item = _library.GetItemById(itemId) ?? throw new InvalidOperationException("The restored media is unavailable.");
        // Preserve position, play count, favorites and dates. Import is not a playback event.
        _data.SaveUserData(user, item, new UpdateUserItemDataDto { Played = played }, UserDataSaveReason.Import);
    }

    public object Libraries() => _library.GetVirtualFolders().Select(value => new { value.ItemId, value.Name }).ToArray();
}
