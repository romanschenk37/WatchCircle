using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Minimal library identity and physical scope used for a cleanup decision.</summary>
internal sealed class CleanupMedia
{
    public Guid ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string[] Identities { get; set; } = Array.Empty<string>();

    public string[] Paths { get; set; } = Array.Empty<string>();

    public IReadOnlyList<Guid> LibraryIds { get; set; } = Array.Empty<Guid>();

    public Guid[] Collections { get; set; } = Array.Empty<Guid>();

    public Guid[] Members { get; set; } = Array.Empty<Guid>();

    public long Bytes { get; set; }

    public string? Problem { get; set; }
}
