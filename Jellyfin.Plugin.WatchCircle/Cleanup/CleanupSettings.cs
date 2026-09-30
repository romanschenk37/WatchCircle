using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Optional cleanup settings; secrets are omitted from API responses.</summary>
public sealed class CleanupSettings
{
    /// <summary>Gets or sets a value indicating whether cleanup records interactions and evaluates titles.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets a value indicating whether automatic deletion is explicitly enabled.</summary>
    public bool AutomaticDeletion { get; set; }

    /// <summary>Gets or sets calendar months of inactivity.</summary>
    public int InactivityMonths { get; set; } = 3;

    /// <summary>Gets or sets days between nomination and deletion.</summary>
    public int WarningDays { get; set; } = 30;

    /// <summary>Gets or sets the legacy interval used to seed Jellyfin's default scheduled task trigger.</summary>
    public int IntervalHours { get; set; } = 24;

    /// <summary>Gets or sets selected Jellyfin library identifiers.</summary>
    public IReadOnlyList<Guid> LibraryIds { get; set; } = Array.Empty<Guid>();

    /// <summary>Gets or sets the Radarr connection.</summary>
    public ArrConnection Radarr { get; set; } = new();

    /// <summary>Gets or sets the Sonarr connection.</summary>
    public ArrConnection Sonarr { get; set; } = new();
}
