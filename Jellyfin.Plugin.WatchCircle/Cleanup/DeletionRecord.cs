using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Durable deletion journal; uncertain operations are verified before any retry.</summary>
internal sealed class DeletionRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EntryId { get; set; }

    public Guid Generation { get; set; }

    public Guid NominationId { get; set; }

    public CleanupMedia Media { get; set; } = new();

    public int ArrId { get; set; }

    public string ArrUrl { get; set; } = string.Empty;

    public string ArrPath { get; set; } = string.Empty;

    public string[] Paths { get; set; } = Array.Empty<string>();

    public string Phase { get; set; } = "Prepared";

    public DateTimeOffset At { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }

    public string? Error { get; set; }
}
