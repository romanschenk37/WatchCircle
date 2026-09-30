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

/// <summary>Jellyfin boundary, replaceable by isolated test libraries.</summary>
internal interface ICleanupLibrary
{
    IReadOnlyList<CleanupMedia> Inventory();

    IReadOnlyList<CleanupUserState> ReadStates(CleanupMedia media);

    IReadOnlyList<CleanupUser> Users();

    bool CanAccess(Guid itemId, Guid userId);

    Guid RootItemId(Guid itemId);

    bool IsPlaying(IReadOnlyList<CleanupEntry> scope);

    bool FilesAbsent(string[] paths);

    Task RefreshAsync(CancellationToken cancellationToken);

    void RestorePlayed(Guid userId, Guid itemId, bool played);

    object Libraries();
}
