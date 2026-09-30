using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Reviewable immutable deletion scope, invalidated by any newer decision.</summary>
internal sealed class CleanupPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; }

    public Guid UserId { get; set; }

    public List<PlanTarget> Targets { get; set; } = new();
}
