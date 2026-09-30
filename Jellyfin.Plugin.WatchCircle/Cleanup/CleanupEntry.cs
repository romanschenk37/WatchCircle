using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>A title's lifecycle, with separate availability and interaction clocks.</summary>
internal sealed class CleanupEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public CleanupMedia Media { get; set; } = new();

    public bool Present { get; set; } = true;

    public Guid Generation { get; set; } = Guid.NewGuid();

    public bool Protected { get; set; }

    public DateTimeOffset Baseline { get; set; }

    public DateTimeOffset? LastInteraction { get; set; }

    public Guid? LastUserId { get; set; }

    public string LastKind { get; set; } = "ObservationStart";

    public long Revision { get; set; }

    public Guid? NominationId { get; set; }

    public DateTimeOffset? NominatedAt { get; set; }

    public DateTimeOffset? DeleteAt { get; set; }

    public string? Error { get; set; }
}
