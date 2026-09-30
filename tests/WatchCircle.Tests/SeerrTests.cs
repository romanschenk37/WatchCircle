using System.Net;
using System.Text;
using System.Text.Json;
using Jellyfin.Plugin.WatchCircle.Api;
using Jellyfin.Plugin.WatchCircle.Configuration;
using Jellyfin.Plugin.WatchCircle.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace WatchCircle.Tests;

public class SeerrTests : IDisposable
{
    private readonly GroupManagementTests.TestPlugin _plugin = new();
    private readonly Mock<ILibraryManager> _library = new();
    private readonly StubHandler _handler = new();
    private readonly HttpClient _client;
    private readonly SeerrService _service;
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Guid _viewerId = Guid.NewGuid();

    public SeerrTests()
    {
        _client = new HttpClient(_handler);
        _service = new SeerrService(_client, _library.Object, NullLogger<SeerrService>.Instance);
        _plugin.Configuration.Seerr = new SeerrConfiguration { Enabled = true, Url = "https://seerr.example/base", ApiKey = "test-secret" };
        _library.Setup(value => value.GetItemById<BaseItem>(_itemId, _viewerId)).Returns(new Movie { Id = _itemId, ProviderIds = new() { ["Tmdb"] = "123" } });
    }

    [Fact]
    public async Task RequestersDoNotRequireGroupMembershipOrPlaybackAndAreDeduplicatedBySeerrId()
    {
        _handler.Json = """
            {"mediaInfo":{"requests":[
              {"requestedBy":{"id":7,"username":"Alex"},"seasons":[{"seasonNumber":2}]},
              {"requestedBy":{"id":7,"username":"Alex"},"seasons":[{"seasonNumber":1},{"seasonNumber":2}]},
              {"requestedBy":{"id":8,"username":"Alex"}},
              {"requestedBy":{"id":9,"jellyfinUsername":"Nora","email":"private@example.org","jellyfinAuthToken":"do-not-return"}}
            ]}}
            """;

        var result = await _service.GetRequestersAsync(_itemId, _viewerId, default);

        Assert.Empty(_plugin.Configuration.Groups);
        Assert.Equal(3, result.Count);
        Assert.Equal(new[] { 1, 2 }, result.Single(user => user.Id == 7).Seasons);
        Assert.Equal(2, result.Count(user => user.Name == "Alex"));
        Assert.Equal("https://seerr.example/base/api/v1/movie/123", _handler.LastUrl);
        Assert.Equal("test-secret", _handler.LastApiKey);
        Assert.Equal(HttpMethod.Get, _handler.LastMethod);
        var json = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("private@example.org", json);
        Assert.DoesNotContain("do-not-return", json);
        Assert.Equal(0, _plugin.SaveCount);
    }

    [Fact]
    public async Task SeriesUsesTvEndpointAndReturnsRequestedSeasonNumbers()
    {
        _library.Setup(value => value.GetItemById<BaseItem>(_itemId, _viewerId)).Returns(new Series { Id = _itemId, ProviderIds = new() { ["Tmdb"] = "456" } });
        _handler.Json = """{"mediaInfo":{"requests":[{"requestedBy":{"id":2,"displayName":"Anna"},"seasons":[{"seasonNumber":3}]}]}}""";

        var result = await _service.GetRequestersAsync(_itemId, _viewerId, default);

        Assert.Equal("https://seerr.example/base/api/v1/tv/456", _handler.LastUrl);
        Assert.Equal(new[] { 3 }, Assert.Single(result).Seasons);
    }

    [Fact]
    public async Task DisabledIntegrationAndMissingTmdbIdDoNotContactSeerr()
    {
        _plugin.Configuration.Seerr.Enabled = false;
        Assert.Empty(await _service.GetRequestersAsync(_itemId, _viewerId, default));
        _plugin.Configuration.Seerr.Enabled = true;
        _library.Setup(value => value.GetItemById<BaseItem>(_itemId, _viewerId)).Returns(new Movie { Id = _itemId });
        Assert.Empty(await _service.GetRequestersAsync(_itemId, _viewerId, default));
        Assert.Equal(0, _handler.Calls);
    }

    [Fact]
    public async Task InaccessibleTitleDoesNotContactSeerr()
    {
        _library.Setup(value => value.GetItemById<BaseItem>(_itemId, _viewerId)).Throws<UnauthorizedAccessException>();
        Assert.Empty(await _service.GetRequestersAsync(_itemId, _viewerId, default));
        Assert.Empty(await _service.GetRequestersAsync(_itemId, Guid.Empty, default));
        Assert.Equal(0, _handler.Calls);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"mediaInfo\":null}")]
    [InlineData("{\"mediaInfo\":{\"requests\":[{\"requestedBy\":null},{\"requestedBy\":{\"id\":\"bad\"}}]}}")]
    [InlineData("not json")]
    public async Task MissingOrMalformedRequestDataDoesNotBreakTheCard(string json)
    {
        _handler.Json = json;
        Assert.Empty(await _service.GetRequestersAsync(_itemId, _viewerId, default));
    }

    [Fact]
    public async Task ApiFailureAndTimeoutLeaveAttributionEmpty()
    {
        _handler.Status = HttpStatusCode.Unauthorized;
        Assert.Empty(await _service.GetRequestersAsync(_itemId, _viewerId, default));
        Assert.False(await _service.TestConnectionAsync(default));
        _handler.Failure = new TaskCanceledException();
        Assert.Empty(await _service.GetRequestersAsync(_itemId, _viewerId, default));
        Assert.False(await _service.TestConnectionAsync(default));
    }

    [Fact]
    public async Task ConnectionTestUsesAuthenticatedReadOnlyEndpoint()
    {
        _handler.Json = "{\"id\":1}";
        Assert.True(await _service.TestConnectionAsync(default));
        Assert.Equal("https://seerr.example/base/api/v1/auth/me", _handler.LastUrl);
        Assert.Equal(HttpMethod.Get, _handler.LastMethod);
    }

    [Fact]
    public void SettingsPreserveGroupsAndDoNotExposeKey()
    {
        var groups = _plugin.Configuration.Groups;
        _service.UpdateSettings(new UpdateSeerrSettingsRequest { Enabled = true, Url = "https://seerr.example/base/api/v1/" });
        Assert.Same(groups, _plugin.Configuration.Groups);
        Assert.Equal("test-secret", _plugin.Configuration.Seerr.ApiKey);
        Assert.DoesNotContain("test-secret", JsonSerializer.Serialize(_service.GetSettings()));
        Assert.True(_service.GetSettings().HasApiKey);
        Assert.Equal("https://seerr.example/base", _service.GetSettings().Url);
        Assert.True(_plugin.ReadSaved().Seerr.Enabled);

        _service.UpdateSettings(new UpdateSeerrSettingsRequest { Enabled = false, Url = "https://seerr.example/base", ClearApiKey = true });
        Assert.False(_service.GetSettings().HasApiKey);
        Assert.False(_plugin.ReadSaved().Seerr.Enabled);
    }

    [Theory]
    [InlineData("file:///tmp/seerr")]
    [InlineData("https://user:password@seerr.example")]
    [InlineData("https://seerr.example?secret=example")]
    [InlineData("https://seerr.example/#part")]
    public void InvalidConnectionUrlsAreRejected(string url)
    {
        Assert.Throws<ArgumentException>(() => _service.UpdateSettings(new UpdateSeerrSettingsRequest { Enabled = true, Url = url, ApiKey = "new-key" }));
        Assert.Equal(0, _plugin.SaveCount);
    }

    [Fact]
    public void ChangingServerCannotForwardTheSavedKeyWithoutReenteringIt()
    {
        Assert.Throws<ArgumentException>(() => _service.UpdateSettings(new UpdateSeerrSettingsRequest { Enabled = true, Url = "https://another.example" }));
        Assert.Equal("https://seerr.example/base", _service.GetSettings().Url);
        _service.UpdateSettings(new UpdateSeerrSettingsRequest { Enabled = true, Url = "https://another.example", ApiKey = "replacement" });
        Assert.Equal("replacement", _plugin.ReadSaved().Seerr.ApiKey);
    }

    [Fact]
    public async Task EmailOnlyAccountsUseNeutralAttributionInsteadOfExposingAnEmail()
    {
        _handler.Json = """{"mediaInfo":{"requests":[{"requestedBy":{"id":10,"displayName":"private@example.org","email":"private@example.org"}}]}}""";
        var result = await _service.GetRequestersAsync(_itemId, _viewerId, default);
        Assert.Equal("Seerr user 10", Assert.Single(result).Name);
    }

    public void Dispose() => _client.Dispose();

    private sealed class StubHandler : HttpMessageHandler
    {
        public string Json { get; set; } = "{}";
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public Exception? Failure { get; set; }
        public int Calls { get; private set; }
        public string? LastUrl { get; private set; }
        public string? LastApiKey { get; private set; }
        public HttpMethod? LastMethod { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastUrl = request.RequestUri?.ToString();
            LastMethod = request.Method;
            LastApiKey = request.Headers.GetValues("X-Api-Key").Single();
            return Failure is null
                ? Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(Json, Encoding.UTF8, "application/json") })
                : Task.FromException<HttpResponseMessage>(Failure);
        }
    }
}
