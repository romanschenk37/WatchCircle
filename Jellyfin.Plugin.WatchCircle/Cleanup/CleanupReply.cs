using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Append-only feedback with independently revocable indifference.</summary>
internal sealed class CleanupReply
{
    public Guid EntryId { get; set; }

    public Guid NominationId { get; set; }

    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public DateTimeOffset At { get; set; }

    public bool Active { get; set; }
}
