using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Api;

/// <summary>
/// Minimal attribution for an existing Seerr request.
/// </summary>
public class SeerrRequesterDto
{
    /// <summary>
    /// Gets or sets the Seerr user identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the requester's display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets requested seasons, empty for a movie.
    /// </summary>
    public IReadOnlyList<int> Seasons { get; set; } = Array.Empty<int>();
}
