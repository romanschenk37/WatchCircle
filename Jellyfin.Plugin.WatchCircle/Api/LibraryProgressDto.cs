namespace Jellyfin.Plugin.WatchCircle.Api;

/// <summary>Whole-title progress derived from Jellyfin user data.</summary>
public class LibraryProgressDto
{
    /// <summary>Gets or sets a value indicating whether any playback is recorded.</summary>
    public bool Started { get; set; }

    /// <summary>Gets or sets a value indicating whether the movie or all available episodes are played.</summary>
    public bool Completed { get; set; }

    /// <summary>Gets or sets the percentage, or null when the runtime is unknown.</summary>
    public double? Percent { get; set; }

    /// <summary>Gets or sets the number of completed episodes.</summary>
    public int CompletedEpisodes { get; set; }

    /// <summary>Gets or sets the number of available episodes.</summary>
    public int TotalEpisodes { get; set; }

    /// <summary>Gets or sets the furthest started episode, using the detail card's progress rules.</summary>
    public WatchProgressDto? Episode { get; set; }

    /// <summary>Gets or sets the current movie position.</summary>
    public long PositionTicks { get; set; }

    /// <summary>Gets or sets the movie runtime.</summary>
    public long RuntimeTicks { get; set; }
}
