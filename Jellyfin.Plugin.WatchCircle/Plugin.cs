using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.WatchCircle.Configuration;
using Jellyfin.Plugin.WatchCircle.Infrastructure;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Plugin.WatchCircle;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "WatchCircle";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("33f774e1-5cb2-4aca-a349-f6ec2bd82c7f");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override void UpdateConfiguration(BasePluginConfiguration configuration)
    {
        PluginConfigurationLock.Run(() => base.UpdateConfiguration(configuration));
    }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                DisplayName = Name,
                EnableInMainMenu = true,
                MenuIcon = "groups",
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        ];
    }

    /// <inheritdoc />
    public override void OnUninstalling()
    {
        var injector = new WebScriptInjector(ApplicationPaths, NullLogger<WebScriptInjector>.Instance);
        injector.Remove();
        base.OnUninstalling();
    }
}
