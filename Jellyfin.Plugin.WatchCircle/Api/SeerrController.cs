using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.WatchCircle.Services;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.WatchCircle.Api;

/// <summary>
/// Server-side Seerr connection and read-only request attribution.
/// </summary>
[ApiController]
[Authorize]
[Route("WatchCircle/Seerr")]
public class SeerrController : ControllerBase
{
    private readonly SeerrService _seerr;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeerrController"/> class.
    /// </summary>
    /// <param name="seerr">Seerr access.</param>
    public SeerrController(SeerrService seerr)
    {
        _seerr = seerr;
    }

    /// <summary>
    /// Gets connection settings without returning the secret.
    /// </summary>
    /// <returns>Admin settings.</returns>
    [HttpGet("Settings")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult<SeerrSettingsDto> GetSettings() => Ok(_seerr.GetSettings());

    /// <summary>
    /// Saves the optional connection.
    /// </summary>
    /// <param name="request">Updated settings.</param>
    /// <returns>Saved settings without the secret.</returns>
    [HttpPost("Settings")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult<SeerrSettingsDto> UpdateSettings([FromBody] UpdateSeerrSettingsRequest request)
    {
        try
        {
            _seerr.UpdateSettings(request);
            return Ok(_seerr.GetSettings());
        }
        catch (ArgumentException)
        {
            return BadRequest("Check the Seerr URL and API key. When changing the URL, enter the key again.");
        }
    }

    /// <summary>
    /// Tests the saved connection without changing Seerr.
    /// </summary>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>Whether the connection works.</returns>
    [HttpPost("Test")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public async Task<ActionResult<bool>> TestConnection(CancellationToken cancellationToken)
    {
        return Ok(await _seerr.TestConnectionAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Gets requesters for an accessible title, without group or playback filtering.
    /// </summary>
    /// <param name="itemId">Jellyfin item identifier.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>Request attribution only.</returns>
    [HttpGet("Requests/{itemId:guid}")]
    public async Task<ActionResult<IReadOnlyList<SeerrRequesterDto>>> GetRequesters(Guid itemId, CancellationToken cancellationToken)
    {
        var claim = User.Claims.FirstOrDefault(value => value.Type.Equals("Jellyfin-UserId", StringComparison.OrdinalIgnoreCase))?.Value;
        if (!Guid.TryParse(claim, out var userId) || userId == Guid.Empty)
        {
            return Unauthorized();
        }

        return Ok(await _seerr.GetRequestersAsync(itemId, userId, cancellationToken).ConfigureAwait(false));
    }
}
