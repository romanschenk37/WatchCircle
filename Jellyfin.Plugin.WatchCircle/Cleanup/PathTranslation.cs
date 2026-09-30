using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Equivalent roots as seen by Jellyfin and the external service.</summary>
public sealed class PathTranslation
{
    /// <summary>Gets or sets the Jellyfin-visible root.</summary>
    public string Jellyfin { get; set; } = string.Empty;

    /// <summary>Gets or sets the Radarr/Sonarr-visible root.</summary>
    public string Arr { get; set; } = string.Empty;
}
