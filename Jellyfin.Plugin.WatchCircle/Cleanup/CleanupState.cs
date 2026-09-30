using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Persistent cleanup state, separate from Jellyfin user data and plugin settings.</summary>
internal sealed class CleanupState
{
    public int Schema { get; set; } = 1;

    public CleanupSettings Settings { get; set; } = new();

    public DateTimeOffset? ActivatedAt { get; set; }

    public DateTimeOffset? LastEvaluation { get; set; }

    public string? Error { get; set; }

    public List<CleanupEntry> Entries { get; set; } = new();

    public List<CleanupReply> Replies { get; set; } = new();

    public List<SeenArchive> Archives { get; set; } = new();

    public List<CleanupPlan> Plans { get; set; } = new();

    public List<DeletionRecord> Deletions { get; set; } = new();

    public List<UserObservation> Observations { get; set; } = new();
}
