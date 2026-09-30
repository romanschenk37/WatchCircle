using System;

namespace Jellyfin.Plugin.WatchCircle.Api;

/// <summary>A movie or series and both users' existing progress.</summary>
public class MemberTitleDto
{
    /// <summary>Gets or sets the Jellyfin identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the title.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the media type.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the release year.</summary>
    public int? Year { get; set; }

    /// <summary>Gets or sets a value indicating whether a poster exists.</summary>
    public bool HasImage { get; set; }

    /// <summary>Gets or sets the exclusive category: started, completed or favorites.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the member's progress.</summary>
    public LibraryProgressDto Member { get; set; } = new();

    /// <summary>Gets or sets the viewer's progress.</summary>
    public LibraryProgressDto You { get; set; } = new();
}
