namespace Jellyfin.Plugin.WatchCircle.Api;

/// <summary>
/// An administrator's changes to the Seerr connection.
/// </summary>
public class UpdateSeerrSettingsRequest
{
    /// <summary>
    /// Gets or sets a value indicating whether the integration is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the base URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a replacement key. Blank keeps the saved key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to remove the saved key.
    /// </summary>
    public bool ClearApiKey { get; set; }
}
