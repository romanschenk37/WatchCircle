using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Title identity and decision revision at confirmation time.</summary>
internal sealed class PlanTarget
{
    public Guid EntryId { get; set; }

    public Guid Generation { get; set; }

    public Guid NominationId { get; set; }

    public long Revision { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;
}
