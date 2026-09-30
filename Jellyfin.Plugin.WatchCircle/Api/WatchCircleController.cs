using System;
using System.Collections.Generic;
using Jellyfin.Plugin.WatchCircle.Abstractions;
using Jellyfin.Plugin.WatchCircle.Configuration;
using Jellyfin.Plugin.WatchCircle.Services;
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
    private readonly GroupManagementService _groups;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchCircleController"/> class.
    /// </summary>
    /// <param name="userProfileService">The user profile service.</param>
    /// <param name="groups">The group management service.</param>
    public WatchCircleController(IUserProfileService userProfileService, GroupManagementService groups)
    {
        _userProfileService = userProfileService;
        _groups = groups;
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

    /// <summary>
    /// Gets current group settings without exposing other plugin configuration.
    /// </summary>
    /// <returns>The watch groups.</returns>
    [HttpGet("Groups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<WatchGroup>> GetGroups() => Ok(_groups.GetGroups());

    /// <summary>
    /// Creates a watch group.
    /// </summary>
    /// <param name="request">The group settings.</param>
    /// <returns>The new group identifier.</returns>
    [HttpPost("Groups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<Guid> CreateGroup([FromBody] CreateGroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
        {
            return BadRequest("Group name must contain between 1 and 100 characters.");
        }

        var groupId = _groups.CreateGroup(request.Name);
        return groupId.HasValue ? Ok(groupId.Value) : Conflict("A group with this name already exists.");
    }

    /// <summary>
    /// Saves membership changes and automatic enrollment for one group.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="request">The settings to change.</param>
    /// <returns>No content when saved.</returns>
    [HttpPost("Groups/{groupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult UpdateGroup(Guid groupId, [FromBody] UpdateGroupRequest request)
    {
        return _groups.UpdateGroup(groupId, request) ? NoContent() : NotFound();
    }

    /// <summary>
    /// Deletes a group.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <returns>No content when deleted.</returns>
    [HttpDelete("Groups/{groupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult DeleteGroup(Guid groupId)
    {
        return _groups.DeleteGroup(groupId) ? NoContent() : NotFound();
    }
}
