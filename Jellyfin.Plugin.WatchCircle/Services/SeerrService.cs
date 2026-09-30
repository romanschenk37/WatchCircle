using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.WatchCircle.Api;
using Jellyfin.Plugin.WatchCircle.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.WatchCircle.Services;

/// <summary>
/// Reads existing Seerr requests without recording request or viewing history.
/// </summary>
public class SeerrService
{
    private static readonly string[] DisplayNameFields = { "username", "jellyfinUsername", "plexUsername", "displayName" };
    private readonly HttpClient _httpClient;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<SeerrService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeerrService"/> class.
    /// </summary>
    /// <param name="httpClient">The bounded, non-redirecting HTTP client.</param>
    /// <param name="libraryManager">Jellyfin library access.</param>
    /// <param name="logger">The logger.</param>
    public SeerrService(HttpClient httpClient, ILibraryManager libraryManager, ILogger<SeerrService> logger)
    {
        _httpClient = httpClient;
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Returns connection settings without the API key.
    /// </summary>
    /// <returns>Safe settings for the admin page.</returns>
    public SeerrSettingsDto GetSettings()
    {
        var settings = GetSnapshot();
        return new SeerrSettingsDto { Enabled = settings.Enabled, Url = settings.Url, HasApiKey = !string.IsNullOrWhiteSpace(settings.ApiKey) };
    }

    /// <summary>
    /// Saves connection settings while preserving groups and other plugin settings.
    /// </summary>
    /// <param name="request">The changes.</param>
    public void UpdateSettings(UpdateSeerrSettingsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var url = NormalizeUrl(request.Url);
        var replacementKey = request.ApiKey?.Trim();
        if (replacementKey?.Contains('\r', StringComparison.Ordinal) == true || replacementKey?.Contains('\n', StringComparison.Ordinal) == true)
        {
            throw new ArgumentException("Invalid API key.", nameof(request));
        }

        PluginConfigurationLock.Run(() =>
        {
            var plugin = Plugin.Instance ?? throw new InvalidOperationException("WatchCircle is not initialized.");
            var current = plugin.Configuration.Seerr ?? new SeerrConfiguration();
            if (current.Url != url && !request.ClearApiKey && string.IsNullOrEmpty(replacementKey) && !string.IsNullOrEmpty(current.ApiKey))
            {
                throw new ArgumentException("Enter the API key again when changing the Seerr URL.", nameof(request));
            }

            var key = request.ClearApiKey ? string.Empty : string.IsNullOrEmpty(replacementKey) ? current.ApiKey : replacementKey;
            if (request.Enabled && (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key)))
            {
                throw new ArgumentException("Seerr URL and API key are required when enabled.", nameof(request));
            }

            plugin.Configuration.Seerr = new SeerrConfiguration { Enabled = request.Enabled, Url = url, ApiKey = key };
            plugin.SaveConfiguration();
        });
    }

    /// <summary>
    /// Checks the saved connection using an authenticated, read-only endpoint.
    /// </summary>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>Whether Seerr accepts the connection and key.</returns>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        var settings = GetSnapshot();
        if (string.IsNullOrEmpty(settings.Url) || string.IsNullOrEmpty(settings.ApiKey))
        {
            return false;
        }

        try
        {
            using var result = await ReadAsync(settings, "auth/me", cancellationToken).ConfigureAwait(false);
            return result.RootElement.ValueKind == JsonValueKind.Object && result.RootElement.TryGetProperty("id", out var id)
                && id.ValueKind == JsonValueKind.Number && id.TryGetInt32(out var userId) && userId > 0;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// Reads all requesters for a title the current Jellyfin user can access.
    /// </summary>
    /// <param name="itemId">The Jellyfin item.</param>
    /// <param name="userId">The authenticated Jellyfin user.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>One entry per Seerr requester, independent of watch groups or playback.</returns>
    public async Task<IReadOnlyList<SeerrRequesterDto>> GetRequestersAsync(Guid itemId, Guid userId, CancellationToken cancellationToken)
    {
        var settings = GetSnapshot();
        if (!settings.Enabled || string.IsNullOrEmpty(settings.Url) || string.IsNullOrEmpty(settings.ApiKey) || userId == Guid.Empty)
        {
            return Array.Empty<SeerrRequesterDto>();
        }

        BaseItem? item;
        try
        {
            item = _libraryManager.GetItemById<BaseItem>(itemId, userId);
        }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException or InvalidOperationException)
        {
            return Array.Empty<SeerrRequesterDto>();
        }

        int? seasonNumber = null;
        if (item is Season season)
        {
            seasonNumber = season.IndexNumber;
            item = season.Series;
        }
        else if (item is Episode episode)
        {
            seasonNumber = episode.ParentIndexNumber;
            item = episode.Series;
        }

        if (item is not Movie && item is not Series)
        {
            return Array.Empty<SeerrRequesterDto>();
        }

        if (!item.ProviderIds.TryGetValue("Tmdb", out var providerId)
            || !int.TryParse(providerId, NumberStyles.None, CultureInfo.InvariantCulture, out var tmdbId) || tmdbId <= 0)
        {
            return Array.Empty<SeerrRequesterDto>();
        }

        var mediaType = item is Movie ? "movie" : "tv";
        try
        {
            using var result = await ReadAsync(settings, mediaType + "/" + tmdbId.ToString(CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
            return ParseRequesters(result.RootElement, seasonNumber);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException)
        {
            _logger.LogDebug("Seerr request attribution is temporarily unavailable for item {ItemId}", itemId);
            return Array.Empty<SeerrRequesterDto>();
        }
    }

    private static SeerrConfiguration GetSnapshot()
    {
        return PluginConfigurationLock.Run(() =>
        {
            var settings = Plugin.Instance?.Configuration.Seerr ?? new SeerrConfiguration();
            return new SeerrConfiguration { Enabled = settings.Enabled, Url = settings.Url, ApiKey = settings.ApiKey };
        });
    }

    private static string NormalizeUrl(string? value)
    {
        var url = (value ?? string.Empty).Trim().TrimEnd('/');
        if (url.Length == 0)
        {
            return string.Empty;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException("Use an HTTP or HTTPS Seerr URL without credentials, query or fragment.", nameof(value));
        }

        return url.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase) ? url[..^7] : url;
    }

    private static IReadOnlyList<SeerrRequesterDto> ParseRequesters(JsonElement root, int? seasonNumber)
    {
        var requesters = new Dictionary<int, SeerrRequesterDto>();
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("mediaInfo", out var media) || media.ValueKind != JsonValueKind.Object
            || !media.TryGetProperty("requests", out var requests) || requests.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<SeerrRequesterDto>();
        }

        foreach (var request in requests.EnumerateArray())
        {
            if (request.ValueKind != JsonValueKind.Object || !request.TryGetProperty("requestedBy", out var user) || user.ValueKind != JsonValueKind.Object
                || !user.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.Number || !id.TryGetInt32(out var userId) || userId <= 0)
            {
                continue;
            }

            var seasons = new List<int>();
            if (request.TryGetProperty("seasons", out var seasonEntries) && seasonEntries.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in seasonEntries.EnumerateArray())
                {
                    if (entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("seasonNumber", out var number)
                        && number.ValueKind == JsonValueKind.Number && number.TryGetInt32(out var value))
                    {
                        seasons.Add(value);
                    }
                }
            }

            if (seasonNumber.HasValue && !seasons.Contains(seasonNumber.Value))
            {
                continue;
            }

            if (!requesters.TryGetValue(userId, out var requester))
            {
                var name = DisplayNameFields
                    .Select(field => user.TryGetProperty(field, out var text) && text.ValueKind == JsonValueKind.String ? text.GetString() : null)
                    .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text) && !text.Contains('@', StringComparison.Ordinal));
                requester = new SeerrRequesterDto { Id = userId, Name = name ?? "Seerr user " + userId.ToString(CultureInfo.InvariantCulture) };
                requesters.Add(userId, requester);
            }

            requester.Seasons = requester.Seasons.Concat(seasons).Distinct().OrderBy(number => number).ToArray();
        }

        return requesters.Values.OrderBy(user => user.Name, StringComparer.OrdinalIgnoreCase).ThenBy(user => user.Id).ToList();
    }

    private async Task<JsonDocument> ReadAsync(SeerrConfiguration settings, string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, settings.Url + "/api/v1/" + path);
        request.Headers.Add("X-Api-Key", settings.ApiKey);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonDocument.Parse(content);
    }
}
