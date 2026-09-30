using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Only the watched flag is archived, per user and movie/episode identity.</summary>
internal sealed class SeenArchive
{
    public Guid EntryId { get; set; }

    public Guid Generation { get; set; }

    public Guid UserId { get; set; }

    public string[] Identities { get; set; } = Array.Empty<string>();

    public bool Played { get; set; }

    public DateTimeOffset CapturedAt { get; set; }

    public List<Guid> RestoredItemIds { get; set; } = new();

    public string? Error { get; set; }
}
