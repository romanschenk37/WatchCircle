using System;
using System.IO;
using System.Text.Json;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Atomic, durable transactions; unreadable or unwritable state disables destructive work.</summary>
internal sealed class CleanupStore
{
    private readonly object _gate = new();
    private readonly string _path;
    private CleanupState _state = new();
    private string? _fault;

    public CleanupStore(IApplicationPaths paths)
        : this(Path.Combine(paths.PluginConfigurationsPath, "WatchCircle", "cleanup-state.json"))
    {
    }

    internal CleanupStore(string path)
    {
        _path = path;
        try
        {
            if (File.Exists(path))
            {
                _state = JsonSerializer.Deserialize<CleanupState>(File.ReadAllBytes(path)) ?? throw new InvalidDataException();
                if (_state.Schema != 1)
                {
                    throw new InvalidDataException();
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _fault = "Cleanup state cannot be read. Restore the plugin backup before enabling cleanup.";
        }
    }

    internal string? Fault => _fault;

    internal T Locked<T>(Func<T> action)
    {
        lock (_gate)
        {
            return action();
        }
    }

    internal CleanupState Read() => Locked(() => Clone(_state));

    internal T Change<T>(Func<CleanupState, T> change)
    {
        return Locked(() =>
        {
            if (_fault is not null)
            {
                throw new InvalidOperationException(_fault);
            }

            var next = Clone(_state);
            var result = change(next);
            var temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    JsonSerializer.Serialize(file, next);
                    file.Flush(flushToDisk: true);
                }

                if (File.Exists(_path))
                {
                    File.Copy(_path, _path + ".bak", overwrite: true);
                }

                File.Move(temporary, _path, overwrite: true);
                _state = next;
                return result;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _fault = "Cleanup state could not be saved. Deletion is blocked until storage is repaired and Jellyfin restarted.";
                throw new IOException(_fault, ex);
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        });
    }

    internal static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value))!;
}
