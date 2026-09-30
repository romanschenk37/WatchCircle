using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>Strict Arr API v3 mapping and removal; never deletes filesystem content itself.</summary>
internal sealed class ArrClient
{
    private readonly HttpClient _http;

    public ArrClient(HttpClient http)
    {
        _http = http;
    }

    internal static string NormalizePath(string value)
    {
        var path = value.Replace('\\', '/').TrimEnd('/');
        if (string.IsNullOrWhiteSpace(path) || path.Split('/').Any(part => part is "." or "..")
            || !(path.StartsWith('/') || (path.Length > 2 && char.IsLetter(path[0]) && path[1] == ':' && path[2] == '/')))
        {
            throw new InvalidOperationException("A media path is not absolute or contains traversal segments.");
        }

        return path.Length > 1 && path[1] == ':' ? path.ToUpperInvariant() : path;
    }

    internal static bool Under(string path, string root)
        => path == root || path.StartsWith(root + "/", StringComparison.Ordinal);

    internal static string Translate(string path, ArrConnection connection)
    {
        path = NormalizePath(path);
        var candidates = connection.Paths.Select(value => new { From = NormalizePath(value.Jellyfin), To = NormalizePath(value.Arr) })
            .Where(value => Under(path, value.From)).OrderByDescending(value => value.From.Length).ToArray();
        if (candidates.Length == 0)
        {
            return path;
        }

        if (candidates.Count(value => value.From == candidates[0].From) != 1)
        {
            throw new InvalidOperationException("Ambiguous path mappings.");
        }

        return candidates[0].To + path[candidates[0].From.Length..];
    }

    internal async Task<string> TestAsync(ArrConnection connection, CancellationToken cancellationToken)
    {
        using var json = await ReadAsync(connection, "system/status", cancellationToken).ConfigureAwait(false);
        return json.RootElement.GetProperty("version").GetString() ?? "Unknown";
    }

    internal async Task<ArrMatch> MatchAsync(CleanupMedia media, ArrConnection connection, CancellationToken cancellationToken)
    {
        var series = media.Kind == "Series";
        var identityPrefix = series ? "Series:Tvdb:" : "Movie:Tmdb:";
        var provider = media.Identities.SingleOrDefault(value => value.StartsWith(identityPrefix, StringComparison.Ordinal));
        if (provider is null || !int.TryParse(provider[identityPrefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var providerId))
        {
            throw new InvalidOperationException(series ? "A unique TVDB series ID is required." : "A unique TMDB movie ID is required.");
        }

        using var list = await ReadAsync(connection, series ? "series" : "movie", cancellationToken).ConfigureAwait(false);
        var matches = list.RootElement.EnumerateArray().Where(value => value.TryGetProperty(series ? "tvdbId" : "tmdbId", out var id) && id.GetInt32() == providerId).ToArray();
        if (matches.Length != 1)
        {
            throw new InvalidOperationException("No unique Radarr/Sonarr entry matches this provider ID.");
        }

        var entry = matches[0];
        var match = new ArrMatch { Id = entry.GetProperty("id").GetInt32(), Path = NormalizePath(entry.GetProperty("path").GetString()!) };
        using var files = await ReadAsync(connection, (series ? "episodefile?seriesId=" : "moviefile?movieId=") + match.Id.ToString(CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
        var managed = files.RootElement.EnumerateArray().Select(value => NormalizePath(value.TryGetProperty("path", out var path)
            && !string.IsNullOrWhiteSpace(path.GetString()) ? path.GetString()! : match.Path + "/" + value.GetProperty("relativePath").GetString())).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var jellyfin = media.Paths.Select(value => Translate(value, connection)).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (managed.Length == 0 || !managed.SequenceEqual(jellyfin, StringComparer.Ordinal) || managed.Any(value => !Under(value, match.Path)))
        {
            throw new InvalidOperationException("Managed files do not exactly match Jellyfin. Check path mappings and scan both libraries first.");
        }

        return match;
    }

    // Called while the decision lock is held. The HTTP request is dispatched before
    // another decision can be committed; the network response is awaited outside it.
    internal async Task DeleteAsync(CleanupMedia media, ArrConnection connection, int id, CancellationToken cancellationToken)
    {
        var path = (media.Kind == "Series" ? "series/" : "movie/") + id.ToString(CultureInfo.InvariantCulture)
            + "?deleteFiles=true&" + (media.Kind == "Series" ? "addImportListExclusion=" : "addImportExclusion=")
            + (connection.AddImportExclusion ? "true" : "false");
        using var request = Request(connection, HttpMethod.Delete, path);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    internal async Task<bool> IsRemovedAsync(CleanupMedia media, ArrConnection connection, int id, CancellationToken cancellationToken)
    {
        using var request = Request(connection, HttpMethod.Get, (media.Kind == "Series" ? "series/" : "movie/") + id.ToString(CultureInfo.InvariantCulture));
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return true;
        }

        response.EnsureSuccessStatusCode();
        return false;
    }

    private async Task<JsonDocument> ReadAsync(ArrConnection connection, string path, CancellationToken cancellationToken)
    {
        using var request = Request(connection, HttpMethod.Get, path);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
    }

    private static HttpRequestMessage Request(ArrConnection connection, HttpMethod method, string path)
    {
        if (string.IsNullOrWhiteSpace(connection.Url) || string.IsNullOrWhiteSpace(connection.ApiKey))
        {
            throw new InvalidOperationException("Configure the required Radarr/Sonarr connection first.");
        }

        var request = new HttpRequestMessage(method, connection.Url.TrimEnd('/') + "/api/v3/" + path);
        request.Headers.Add("X-Api-Key", connection.ApiKey);
        return request;
    }
}
