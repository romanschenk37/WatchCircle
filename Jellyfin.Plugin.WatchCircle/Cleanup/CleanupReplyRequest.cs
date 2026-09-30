using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>A response bound to a particular nomination; stale views cannot resurrect it.</summary>
public sealed class CleanupReplyRequest
{
    /// <summary>Gets or sets the current nomination identifier.</summary>
    public Guid NominationId { get; set; }

    /// <summary>Gets or sets either Keep or Indifferent.</summary>
    public string Answer { get; set; } = string.Empty;
}
