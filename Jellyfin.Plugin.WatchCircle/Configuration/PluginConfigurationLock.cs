using System;

namespace Jellyfin.Plugin.WatchCircle.Configuration;

/// <summary>
/// Serializes changes to the shared plugin configuration.
/// </summary>
internal static class PluginConfigurationLock
{
    private static readonly object SyncRoot = new();

    internal static void Run(Action action)
    {
        lock (SyncRoot)
        {
            action();
        }
    }

    internal static T Run<T>(Func<T> action)
    {
        lock (SyncRoot)
        {
            return action();
        }
    }
}
