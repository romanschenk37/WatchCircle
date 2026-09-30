using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Change detection and precedence for manual decisions; no playback history.</summary>
internal sealed class UserObservation
{
    public Guid UserId { get; set; }

    public Guid ItemId { get; set; }

    public string[] Identities { get; set; } = Array.Empty<string>();

    public bool Played { get; set; }

    public bool Favorite { get; set; }

    public DateTimeOffset? DecisionAt { get; set; }
}
