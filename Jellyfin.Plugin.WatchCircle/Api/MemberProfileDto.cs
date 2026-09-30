using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.WatchCircle.Api;

/// <summary>Read-only library comparison for a group member.</summary>
public class MemberProfileDto
{
    /// <summary>Gets or sets the member.</summary>
    public GroupUserDto User { get; set; } = new();

    /// <summary>Gets or sets the titles visible to the viewer.</summary>
    public IReadOnlyList<MemberTitleDto> Items { get; set; } = Array.Empty<MemberTitleDto>();
}
