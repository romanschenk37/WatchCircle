using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Explicit permanent protection change.</summary>
public sealed class CleanupProtectionRequest
{
    /// <summary>Gets or sets a value indicating whether permanent protection is required.</summary>
    public bool Protected { get; set; }
}
