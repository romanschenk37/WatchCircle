using System.Threading.Tasks;
using Jellyfin.Data.Events.Users;
using MediaBrowser.Controller.Events;

namespace Jellyfin.Plugin.WatchCircle.Services;

/// <summary>Removes deleted accounts using Jellyfin's existing user-deleted event.</summary>
public sealed class UserDeletedConsumer : IEventConsumer<UserDeletedEventArgs>
{
    private readonly GroupManagementService _groups;

    /// <summary>Initializes a new instance of the <see cref="UserDeletedConsumer"/> class.</summary>
    /// <param name="groups">The group management service.</param>
    public UserDeletedConsumer(GroupManagementService groups)
    {
        _groups = groups;
    }

    /// <inheritdoc />
    public Task OnEvent(UserDeletedEventArgs eventArgs)
    {
        _groups.RemoveDeletedUser(eventArgs.Argument.Id);
        return Task.CompletedTask;
    }
}
