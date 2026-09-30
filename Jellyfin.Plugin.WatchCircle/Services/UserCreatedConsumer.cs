using System.Threading.Tasks;
using Jellyfin.Data.Events.Users;
using MediaBrowser.Controller.Events;

namespace Jellyfin.Plugin.WatchCircle.Services;

/// <summary>
/// Enrolls new accounts using Jellyfin's existing user-created event.
/// </summary>
public class UserCreatedConsumer : IEventConsumer<UserCreatedEventArgs>
{
    private readonly GroupManagementService _groups;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserCreatedConsumer"/> class.
    /// </summary>
    /// <param name="groups">The group management service.</param>
    public UserCreatedConsumer(GroupManagementService groups)
    {
        _groups = groups;
    }

    /// <inheritdoc />
    public Task OnEvent(UserCreatedEventArgs eventArgs)
    {
        _groups.AddNewUser(eventArgs.Argument.Id);
        return Task.CompletedTask;
    }
}
