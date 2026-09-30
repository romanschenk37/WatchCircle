using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Plugin.WatchCircle.Api;

/// <summary>
/// Settings for a new watch group.
/// </summary>
public class CreateGroupRequest
{
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}
