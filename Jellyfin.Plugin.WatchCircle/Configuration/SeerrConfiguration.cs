namespace Jellyfin.Plugin.WatchCircle.Configuration;

/// <summary>
/// Optional server-side connection to Seerr.
/// </summary>
public class SeerrConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether request attribution is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the Seerr base URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key, used only by the Jellyfin server.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
