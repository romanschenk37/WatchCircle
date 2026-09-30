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

/// <summary>Existing user state, used transiently for progress and archive preparation.</summary>
internal sealed class CleanupUserState
{
    public Guid UserId { get; set; }

    public Guid ItemId { get; set; }

    public string[] Identities { get; set; } = Array.Empty<string>();

    public bool Played { get; set; }

    public bool Favorite { get; set; }

    public DateTimeOffset? LastPlayed { get; set; }

    public LibraryProgressDto Progress { get; set; } = new();
}
