using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Confirmation of a previously reviewed, immutable deletion plan.</summary>
public sealed class CleanupDeleteRequest
{
    /// <summary>Gets or sets the reviewed plan identifier.</summary>
    public Guid PlanId { get; set; }
}
