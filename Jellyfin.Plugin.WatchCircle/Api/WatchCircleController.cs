using System.Collections.Generic;
using Jellyfin.Plugin.WatchCircle.Abstractions;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.WatchCircle.Api;

/// <summary>
/// API endpoints for WatchCircle plugin settings.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.RequiresElevation)]
[Route("WatchCircle")]
[Produces("application/json")]
public class WatchCircleController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchCircleController"/> class.
    /// </summary>
    /// <param name="userProfileService">The user profile service.</param>
    public WatchCircleController(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    /// <summary>
    /// Gets all users with profile image metadata for group member selection.
    /// </summary>
    /// <returns>A list of users.</returns>
    [HttpGet("Users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<GroupUserDto>> GetUsers()
    {
        return Ok(_userProfileService.GetUsersForGroupSettings());
    }
}
