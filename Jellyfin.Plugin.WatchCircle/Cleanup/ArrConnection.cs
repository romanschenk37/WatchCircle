using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Server-side Arr connection and explicit path translations.</summary>
public sealed class ArrConnection
{
    /// <summary>Gets or sets the service URL including an optional base path.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets a replacement secret; empty preserves the saved secret.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether a secret is configured, for safe API responses.</summary>
    public bool HasApiKey { get; set; }

    /// <summary>Gets or sets a value indicating whether the saved secret should be removed.</summary>
    public bool ClearApiKey { get; set; }

    /// <summary>Gets or sets a value indicating whether deletion should create an import-list exclusion.</summary>
    public bool AddImportExclusion { get; set; }

    /// <summary>Gets or sets server-to-Arr path mappings.</summary>
    public IReadOnlyList<PathTranslation> Paths { get; set; } = Array.Empty<PathTranslation>();
}
