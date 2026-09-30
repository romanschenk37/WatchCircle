using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Api;

/// <summary>
/// Explicit membership changes, preserving members added after the settings page was opened.
/// </summary>
public class UpdateGroupRequest
{
    /// <summary>
    /// Gets or sets the users explicitly selected by the administrator.
    /// </summary>
    public IReadOnlyList<Guid> AddedUserIds { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// Gets or sets the users explicitly deselected by the administrator.
    /// </summary>
    public IReadOnlyList<Guid> RemovedUserIds { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// Gets or sets a value indicating whether future new users join this group automatically.
    /// </summary>
    public bool AutoAddNewUsers { get; set; }
}
