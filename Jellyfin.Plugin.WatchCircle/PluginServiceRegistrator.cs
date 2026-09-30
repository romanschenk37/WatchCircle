using System;
using System.Net.Http;
using Jellyfin.Data.Events.Users;
using Jellyfin.Plugin.WatchCircle.Abstractions;
using Jellyfin.Plugin.WatchCircle.Cleanup;
using Jellyfin.Plugin.WatchCircle.Infrastructure;
using Jellyfin.Plugin.WatchCircle.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.WatchCircle;

/// <summary>
/// Registers WatchCircle services with the Jellyfin DI container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHttpClient("WatchCircle.Arr", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.MaxResponseContentBufferSize = 16 * 1024 * 1024;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        serviceCollection.AddSingleton<CleanupService>();
        serviceCollection.AddHostedService(provider => provider.GetRequiredService<CleanupService>());
        serviceCollection.AddHttpClient<SeerrService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
            client.MaxResponseContentBufferSize = 2 * 1024 * 1024;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        serviceCollection.AddSingleton<GroupManagementService>();
        serviceCollection.AddSingleton<MemberLibraryService>();
        serviceCollection.AddSingleton<IEventConsumer<UserCreatedEventArgs>, UserCreatedConsumer>();
        serviceCollection.AddSingleton<IGroupMembershipService, GroupMembershipService>();
        serviceCollection.AddSingleton<IUserProfileService, UserProfileService>();
        serviceCollection.AddSingleton<IItemWatchProgressService, ItemWatchProgressService>();
        serviceCollection.AddSingleton<IWatchCircleOverlayService, WatchCircleOverlayService>();
        serviceCollection.AddSingleton<IWatchTogetherHistoryService, WatchTogetherHistoryService>();
        serviceCollection.AddSingleton<IWatchTogetherQueueService, WatchTogetherQueueService>();
        serviceCollection.AddSingleton<WebScriptInjector>();
        serviceCollection.AddSingleton<IStartupFilter, WatchCircleScriptInjectorStartup>();
    }
}
