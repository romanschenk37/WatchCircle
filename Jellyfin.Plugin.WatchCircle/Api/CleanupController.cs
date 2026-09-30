using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.WatchCircle.Cleanup;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.WatchCircle.Api;

/// <summary>Cleanup feedback for accessible titles, with separate elevated administration.</summary>
[ApiController]
[Authorize]
[Route("WatchCircle/Cleanup")]
public sealed class CleanupController : ControllerBase
{
    private readonly CleanupService _cleanup;
    private readonly IAuthorizationService _authorization;

    /// <summary>Initializes a new instance of the <see cref="CleanupController"/> class.</summary>
    /// <param name="cleanup">Optional cleanup workflow.</param>
    /// <param name="authorization">Jellyfin's administrator policy.</param>
    public CleanupController(CleanupService cleanup, IAuthorizationService authorization)
    {
        _cleanup = cleanup;
        _authorization = authorization;
    }

    private Guid UserId => Guid.TryParse(User.Claims.FirstOrDefault(value => value.Type.Equals("Jellyfin-UserId", StringComparison.OrdinalIgnoreCase))?.Value, out var id) ? id : Guid.Empty;

    /// <summary>Gets feature availability and menu permissions.</summary>
    /// <returns>Availability without library or account details.</returns>
    [HttpGet("Status")]
    public async Task<ActionResult> Status()
    {
        var admin = await _authorization.AuthorizeAsync(User, Policies.RequiresElevation).ConfigureAwait(false);
        return Ok(new { Enabled = _cleanup.Enabled, IsAdmin = admin.Succeeded });
    }

    /// <summary>Gets only this user's open, accessible nominations.</summary>
    /// <returns>Pending feedback.</returns>
    [HttpGet("Pending")]
    public ActionResult Pending() => UserId == Guid.Empty ? Unauthorized() : Ok(_cleanup.Pending(UserId));

    /// <summary>Gets accessible nominations, including previously answered ones, for posters and detail banners.</summary>
    /// <param name="ids">Visible Jellyfin items, at most 200.</param>
    /// <returns>Current status and only the current user's response.</returns>
    [HttpPost("Items/Status")]
    public ActionResult ItemStatuses([FromBody] Guid[] ids)
        => UserId == Guid.Empty ? Unauthorized() : ids.Length > 200 ? BadRequest() : Ok(_cleanup.ItemStatuses(UserId, ids));

    /// <summary>Records this user's answer to a still-active nomination.</summary>
    /// <param name="id">Cleanup entry.</param>
    /// <param name="request">Nomination and answer.</param>
    /// <returns>Success or a stale-view conflict.</returns>
    [HttpPost("Replies/{id:guid}")]
    public ActionResult Reply(Guid id, [FromBody] CleanupReplyRequest request)
    {
        if (UserId == Guid.Empty)
        {
            return Unauthorized();
        }

        return Change(() => _cleanup.Reply(id, UserId, request));
    }

    /// <summary>Gets settings without secret values.</summary>
    /// <returns>Settings and available libraries.</returns>
    [HttpGet("Admin/Settings")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult Settings() => Ok(_cleanup.GetSettings());

    /// <summary>Saves explicit cleanup settings independently of existing groups.</summary>
    /// <param name="settings">Desired settings.</param>
    /// <returns>Success or validation errors.</returns>
    [HttpPost("Admin/Settings")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult Settings([FromBody] CleanupSettings settings) => Change(() => _cleanup.SaveSettings(settings));

    /// <summary>Tests an already configured connection without modifying the external service.</summary>
    /// <param name="service">Radarr or Sonarr.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>Service version.</returns>
    [HttpPost("Admin/Test/{service}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public async Task<ActionResult> Test(string service, CancellationToken cancellationToken)
    {
        if (service is not ("Radarr" or "Sonarr"))
        {
            return BadRequest();
        }

        return await RunAsync(async () => new { Version = await _cleanup.TestAsync(service, cancellationToken).ConfigureAwait(false) }).ConfigureAwait(false);
    }

    /// <summary>Gets all cleanup lifecycles and feedback, restricted to administrators.</summary>
    /// <returns>Admin overview.</returns>
    [HttpGet("Admin/Items")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult Items() => Ok(_cleanup.AdminView());

    /// <summary>Gets every user's progress, including the administrator and unstarted accounts.</summary>
    /// <param name="id">Cleanup entry.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>All-user details.</returns>
    [HttpGet("Admin/Items/{id:guid}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public Task<ActionResult> Details(Guid id, CancellationToken cancellationToken)
        => RunAsync(() => _cleanup.AdminDetailsAsync(id, UserId, cancellationToken));

    /// <summary>Evaluates the configured libraries without bypassing deadlines or protections.</summary>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>Updated overview.</returns>
    [HttpPost("Admin/Evaluate")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public Task<ActionResult> Evaluate(CancellationToken cancellationToken)
        => RunAsync(async () =>
        {
            await _cleanup.EvaluateAsync(cancellationToken).ConfigureAwait(false);
            return _cleanup.AdminView();
        });

    /// <summary>Changes permanent title or collection protection.</summary>
    /// <param name="id">Cleanup entry.</param>
    /// <param name="request">Explicit protection state.</param>
    /// <returns>Success.</returns>
    [HttpPost("Admin/Protection/{id:guid}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult Protect(Guid id, [FromBody] CleanupProtectionRequest request) => Change(() => _cleanup.Protect(id, request.Protected));

    /// <summary>Gets protection for an administrator's detail-page control.</summary>
    /// <param name="id">Jellyfin item identifier.</param>
    /// <returns>Protection state.</returns>
    [HttpGet("Admin/Protection/Item/{id:guid}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult ItemProtection(Guid id) => Ok(_cleanup.ProtectionForItem(id));

    /// <summary>Prepares the exact scope for manual confirmation.</summary>
    /// <param name="id">Title or collection entry.</param>
    /// <returns>Expiring confirmation plan.</returns>
    [HttpPost("Admin/Plan/{id:guid}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult Plan(Guid id)
    {
        try
        {
            return Ok(_cleanup.Plan(id, UserId));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Error = CleanupService.SafeError(ex) });
        }
    }

    /// <summary>Executes a reviewed plan, revalidating each title immediately before removal.</summary>
    /// <param name="request">Reviewed plan identifier.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>Updated per-title outcomes.</returns>
    [HttpPost("Admin/Delete")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public Task<ActionResult> Delete([FromBody] CleanupDeleteRequest request, CancellationToken cancellationToken)
        => RunAsync(async () =>
        {
            await _cleanup.ExecutePlanAsync(request.PlanId, UserId, cancellationToken).ConfigureAwait(false);
            return _cleanup.AdminView();
        });

    private ActionResult Change(Action action)
    {
        try
        {
            action();
            return Ok(new { Success = true });
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or System.IO.IOException)
        {
            return Conflict(new { Error = CleanupService.SafeError(ex) });
        }
    }

    private async Task<ActionResult> RunAsync(Func<Task<object>> action)
    {
        try
        {
            return Ok(await action().ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or System.IO.IOException or System.Net.Http.HttpRequestException or OperationCanceledException)
        {
            return Conflict(new { Error = CleanupService.SafeError(ex) });
        }
    }
}
