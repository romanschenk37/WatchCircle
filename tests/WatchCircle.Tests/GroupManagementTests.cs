using System.Xml.Serialization;
using Jellyfin.Data.Events.Users;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.WatchCircle;
using Jellyfin.Plugin.WatchCircle.Api;
using Jellyfin.Plugin.WatchCircle.Configuration;
using Jellyfin.Plugin.WatchCircle.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace WatchCircle.Tests;

public class GroupManagementTests
{
    private readonly TestPlugin _plugin = new();
    private readonly HashSet<Guid> _users = new();
    private readonly Mock<IUserManager> _userManager = new();
    private readonly GroupManagementService _groups;

    public GroupManagementTests()
    {
        _userManager.Setup(manager => manager.GetUsersIds()).Returns(() => _users.ToArray());
        _groups = new GroupManagementService(_userManager.Object);
    }

    [Fact]
    public void ExistingConfigurationDefaultsToManualMembership()
    {
        var userId = Guid.NewGuid();
        var xml = $"<WatchGroup><Id>{Guid.NewGuid()}</Id><Name>Friends</Name><MemberUserIds><guid>{userId}</guid></MemberUserIds></WatchGroup>";
        var group = (WatchGroup)new XmlSerializer(typeof(WatchGroup)).Deserialize(new StringReader(xml))!;
        Assert.False(group.AutoAddNewUsers);
        Assert.Equal(new[] { userId }, group.MemberUserIds);
    }

    [Fact]
    public async Task RegisteredConsumerAddsNewUserOnlyToOptedInGroupsAndPersists()
    {
        var first = AddGroup(true);
        var second = AddGroup(true);
        var manual = AddGroup(false);
        var user = new User("New viewer", "auth", "reset");
        _users.Add(user.Id);
        var services = new ServiceCollection();
        services.AddSingleton(_userManager.Object);
        new PluginServiceRegistrator().RegisterServices(services, Mock.Of<IServerApplicationHost>());
        using var provider = services.BuildServiceProvider();
        var consumer = provider.GetRequiredService<IEventConsumer<UserCreatedEventArgs>>();

        await consumer.OnEvent(new UserCreatedEventArgs(user));

        Assert.Contains(user.Id, first.MemberUserIds);
        Assert.Contains(user.Id, second.MemberUserIds);
        Assert.Empty(manual.MemberUserIds);
        Assert.Equal(1, _plugin.SaveCount);
        var saved = _plugin.ReadSaved();
        Assert.Equal(2, saved.Groups.Count(group => group.MemberUserIds.Contains(user.Id)));
        Assert.True(saved.Groups[0].AutoAddNewUsers);
    }

    [Fact]
    public void EnablingDoesNotBackfillExistingAccountsAndDisablingKeepsMembers()
    {
        var existing = Guid.NewGuid();
        var next = Guid.NewGuid();
        _users.UnionWith(new[] { existing, next });
        var group = AddGroup(false);

        _groups.UpdateGroup(group.Id, new UpdateGroupRequest { AutoAddNewUsers = true });
        Assert.Empty(group.MemberUserIds);
        _groups.AddNewUser(next);
        _groups.UpdateGroup(group.Id, new UpdateGroupRequest { AutoAddNewUsers = false });
        _groups.AddNewUser(Guid.NewGuid());

        Assert.Equal(new[] { next }, group.MemberUserIds);
        Assert.False(_plugin.ReadSaved().Groups[0].AutoAddNewUsers);
    }

    [Fact]
    public void RepeatedCreationNotificationDoesNotDuplicateMembershipOrSave()
    {
        var group = AddGroup(true);
        var userId = Guid.NewGuid();
        _groups.AddNewUser(userId);
        _groups.AddNewUser(userId);
        _groups.AddNewUser(Guid.Empty);
        Assert.Equal(new[] { userId }, group.MemberUserIds);
        Assert.Equal(1, _plugin.SaveCount);
    }

    [Fact]
    public void SavingAnOpenPagePreservesUsersAddedInTheMeantime()
    {
        var removed = Guid.NewGuid();
        var selected = Guid.NewGuid();
        var newcomer = Guid.NewGuid();
        _users.UnionWith(new[] { removed, selected, newcomer });
        var group = AddGroup(true);
        group.MemberUserIds.Add(removed);
        var pageSnapshot = _groups.GetGroups().Single();
        _groups.AddNewUser(newcomer);

        _groups.UpdateGroup(group.Id, new UpdateGroupRequest
        {
            AddedUserIds = new[] { selected },
            RemovedUserIds = pageSnapshot.MemberUserIds,
            AutoAddNewUsers = true
        });

        Assert.Equal(2, group.MemberUserIds.Count);
        Assert.Contains(selected, group.MemberUserIds);
        Assert.Contains(newcomer, group.MemberUserIds);
        Assert.DoesNotContain(removed, group.MemberUserIds);
    }

    [Fact]
    public void ManualRemovalSurvivesLaterUserCreationAndConfigurationReload()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        _users.UnionWith(new[] { first, second });
        var group = AddGroup(true);
        _groups.AddNewUser(first);
        _groups.UpdateGroup(group.Id, new UpdateGroupRequest { RemovedUserIds = new[] { first }, AutoAddNewUsers = true });
        _plugin.ReloadSaved();
        _groups.AddNewUser(second);
        var restored = _groups.GetGroups().Single();
        Assert.DoesNotContain(first, restored.MemberUserIds);
        Assert.Equal(new[] { second }, restored.MemberUserIds);
    }

    [Fact]
    public void GroupChangesPreserveOtherGroupsAndWatchTogetherSettings()
    {
        var optedIn = AddGroup(true);
        var history = _plugin.Configuration.WatchTogether;
        var existingGroups = _plugin.Configuration.Groups;
        var created = _groups.CreateGroup("  Manual  ");
        Assert.NotNull(created);
        Assert.Null(_groups.CreateGroup("manual"));
        _groups.AddNewUser(Guid.NewGuid());
        Assert.True(_groups.DeleteGroup(created.Value));
        Assert.Same(history, _plugin.Configuration.WatchTogether);
        Assert.Same(existingGroups, _plugin.Configuration.Groups);
        Assert.Single(optedIn.MemberUserIds);
        Assert.Single(_plugin.Configuration.Groups);
    }

    [Fact]
    public void OverlappingGroupsStillDeduplicateVisibleMembers()
    {
        var viewer = Guid.NewGuid();
        var newcomer = Guid.NewGuid();
        AddGroup(true).MemberUserIds.Add(viewer);
        AddGroup(true).MemberUserIds.Add(viewer);
        _groups.AddNewUser(newcomer);
        var members = new GroupMembershipService().GetVisibleMemberIds(viewer);
        Assert.Equal(new[] { newcomer }, members);
    }

    [Fact]
    public void OnlyDirectlySharedGroupsGrantVisibility()
    {
        var viewer = Guid.NewGuid();
        var firstBuddy = Guid.NewGuid();
        var secondBuddy = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        AddGroup(false).MemberUserIds.AddRange(new[] { viewer, firstBuddy });
        AddGroup(false).MemberUserIds.AddRange(new[] { viewer, firstBuddy, secondBuddy });
        AddGroup(false).MemberUserIds.AddRange(new[] { firstBuddy, outsider });

        var members = new GroupMembershipService().GetVisibleMemberIds(viewer);

        Assert.Equal(new[] { firstBuddy, secondBuddy }.Order(), members.Order());
        Assert.Empty(new GroupMembershipService().GetVisibleMemberIds(Guid.NewGuid()));
    }

    [Fact]
    public void UnknownGroupsAndInvalidManualUserIdsDoNotChangeMemberships()
    {
        var group = AddGroup(false);
        Assert.False(_groups.UpdateGroup(Guid.NewGuid(), new UpdateGroupRequest()));
        Assert.False(_groups.DeleteGroup(Guid.NewGuid()));
        Assert.Equal(0, _plugin.SaveCount);
        _groups.UpdateGroup(group.Id, new UpdateGroupRequest { AddedUserIds = new[] { Guid.Empty, Guid.NewGuid() } });
        Assert.Empty(group.MemberUserIds);
    }

    [Fact]
    public async Task ConcurrentUserCreationAndSettingsSavesDoNotLoseMembers()
    {
        var group = AddGroup(true);
        var newIds = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToArray();
        _users.UnionWith(newIds);
        await Task.WhenAll(newIds.Select(id => Task.Run(() =>
        {
            _groups.AddNewUser(id);
            _groups.UpdateGroup(group.Id, new UpdateGroupRequest { AutoAddNewUsers = true });
            _groups.GetGroups();
        })));
        Assert.Equal(newIds.Order(), group.MemberUserIds.Order());
        Assert.Equal(newIds.Order(), _plugin.ReadSaved().Groups.Single().MemberUserIds.Order());
    }

    private WatchGroup AddGroup(bool autoAdd)
    {
        var group = new WatchGroup { Name = Guid.NewGuid().ToString(), AutoAddNewUsers = autoAdd };
        _plugin.Configuration.Groups.Add(group);
        return group;
    }

    private sealed class TestPlugin : Plugin
    {
        public TestPlugin()
            : base(CreatePaths(), Mock.Of<IXmlSerializer>())
        {
            Configuration = new PluginConfiguration();
        }

        public int SaveCount { get; private set; }

        private string _savedXml = string.Empty;

        public override void SaveConfiguration()
        {
            using var writer = new StringWriter();
            new XmlSerializer(typeof(PluginConfiguration)).Serialize(writer, Configuration);
            _savedXml = writer.ToString();
            SaveCount++;
        }

        public PluginConfiguration ReadSaved() => (PluginConfiguration)new XmlSerializer(typeof(PluginConfiguration)).Deserialize(new StringReader(_savedXml))!;

        public void ReloadSaved() => Configuration = ReadSaved();

        private static IApplicationPaths CreatePaths()
        {
            var paths = new Mock<IApplicationPaths>();
            paths.SetupGet(value => value.PluginsPath).Returns(Path.GetTempPath());
            return paths.Object;
        }
    }
}
