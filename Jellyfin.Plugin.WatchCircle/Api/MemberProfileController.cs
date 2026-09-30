using System;
using System.Linq;
using System.Threading;
using Jellyfin.Plugin.WatchCircle.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.WatchCircle.Api;

/// <summary>Group-restricted, read-only profiles.</summary>
[ApiController]
[Authorize]
[Route("WatchCircle/Profiles")]
public class MemberProfileController : ControllerBase
{
    private readonly MemberLibraryService _library;

    /// <summary>Initializes a new instance of the <see cref="MemberProfileController"/> class.</summary>
    /// <param name="library">Read-only comparisons.</param>
    public MemberProfileController(MemberLibraryService library) => _library = library;

    /// <summary>Gets the existing progress and favorites of a shared-group member.</summary>
    /// <param name="memberId">Requested member.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>The profile, or a non-disclosing 404 if not in a common group.</returns>
    [HttpGet("{memberId:guid}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public ActionResult<MemberProfileDto> GetProfile(Guid memberId, CancellationToken cancellationToken)
    {
        var claim = User.Claims.FirstOrDefault(value => value.Type.Equals("Jellyfin-UserId", StringComparison.OrdinalIgnoreCase))?.Value;
        if (!Guid.TryParse(claim, out var viewerId) || viewerId == Guid.Empty)
        {
            return Unauthorized();
        }

        var profile = _library.GetProfile(viewerId, memberId, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }
}
