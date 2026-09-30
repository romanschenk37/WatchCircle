using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Jellyfin.Plugin.WatchCircle.Infrastructure;

/// <summary>
/// Registers the WatchCircle script injector middleware in the ASP.NET pipeline.
/// </summary>
public class WatchCircleScriptInjectorStartup : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<WatchCircleScriptInjectorMiddleware>();
            next(app);
        };
    }
}
