namespace Jellyfin.Plugin.WatchCircle.Api;

/// <summary>
/// Seerr connection settings without the saved secret.
/// </summary>
public class SeerrSettingsDto
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
    /// Gets or sets a value indicating whether a key is saved.
    /// </summary>
    public bool HasApiKey { get; set; }
}
