using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.WatchCircle.Api;
using Jellyfin.Plugin.WatchCircle.Configuration;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.WatchCircle.Services;

/// <summary>
/// Applies group changes without replacing unrelated settings or concurrent membership additions.
/// </summary>
public class GroupManagementService
{
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupManagementService"/> class.
    /// </summary>
    /// <param name="userManager">The Jellyfin user manager.</param>
    public GroupManagementService(IUserManager userManager)
    {
        _userManager = userManager;
    }

    /// <summary>
    /// Gets detached group settings for the administration page.
    /// </summary>
    /// <returns>The current groups.</returns>
    public IReadOnlyList<WatchGroup> GetGroups()
    {
        return PluginConfigurationLock.Run(() => GetPlugin().Configuration.Groups
            .Select(group => new WatchGroup
            {
                Id = group.Id,
                Name = group.Name,
                AutoAddNewUsers = group.AutoAddNewUsers,
                MemberUserIds = group.MemberUserIds?.ToList() ?? new List<Guid>()
            })
            .ToList());
    }

    /// <summary>
    /// Creates an empty group with automatic enrollment disabled.
    /// </summary>
    /// <param name="name">The group name.</param>
    /// <returns>The new group's identifier, or null if the name already exists.</returns>
    public Guid? CreateGroup(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmedName = name.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(trimmedName.Length, 100);

        return PluginConfigurationLock.Run<Guid?>(() =>
        {
            var plugin = GetPlugin();
            if (plugin.Configuration.Groups.Any(group => group.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            var group = new WatchGroup { Name = trimmedName };
            plugin.Configuration.Groups.Add(group);
            plugin.SaveConfiguration();
            return group.Id;
        });
    }

    /// <summary>
    /// Applies only explicitly changed memberships and the automatic enrollment setting.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="request">The requested changes.</param>
    /// <returns>Whether the group exists.</returns>
    public bool UpdateGroup(Guid groupId, UpdateGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return PluginConfigurationLock.Run(() =>
        {
            var plugin = GetPlugin();
            var group = plugin.Configuration.Groups.FirstOrDefault(item => item.Id == groupId);
            if (group is null)
            {
                return false;
            }

            var existingUserIds = _userManager.GetUsersIds().ToHashSet();
            var memberIds = group.MemberUserIds?.ToHashSet() ?? new HashSet<Guid>();
            memberIds.UnionWith((request.AddedUserIds ?? Array.Empty<Guid>()).Where(existingUserIds.Contains));
            memberIds.ExceptWith(request.RemovedUserIds ?? Array.Empty<Guid>());
            memberIds.Remove(Guid.Empty);
            group.MemberUserIds = memberIds.ToList();
            group.AutoAddNewUsers = request.AutoAddNewUsers;
            plugin.SaveConfiguration();
            return true;
        });
    }

    /// <summary>
    /// Deletes one group while preserving other plugin settings.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <returns>Whether a group was deleted.</returns>
    public bool DeleteGroup(Guid groupId)
    {
        return PluginConfigurationLock.Run(() =>
        {
            var plugin = GetPlugin();
            if (plugin.Configuration.Groups.RemoveAll(group => group.Id == groupId) == 0)
            {
                return false;
            }

            plugin.SaveConfiguration();
            return true;
        });
    }

    /// <summary>
    /// Adds a newly created Jellyfin user to each group with automatic enrollment enabled.
    /// </summary>
    /// <param name="userId">The new user's identifier.</param>
    public void AddNewUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return;
        }

        PluginConfigurationLock.Run(() =>
        {
            var plugin = GetPlugin();
            var changed = false;
            foreach (var group in plugin.Configuration.Groups.Where(group => group.AutoAddNewUsers))
            {
                group.MemberUserIds ??= new List<Guid>();
                if (!group.MemberUserIds.Contains(userId))
                {
                    group.MemberUserIds.Add(userId);
                    changed = true;
                }
            }

            if (changed)
            {
                plugin.SaveConfiguration();
            }
        });
    }

    private static Plugin GetPlugin() => Plugin.Instance ?? throw new InvalidOperationException("WatchCircle is not initialized.");
}
