using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;
using Jellyfin.Plugin.WatchCircle.Cleanup;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace WatchCircle.Tests;

/// <summary>Opt-in tests against disposable, loopback-only Arr instances. Never use production data.</summary>
public sealed class CleanupLiveArrTests
{
    [LocalArrTheory]
    [InlineData("radarr", 17878, "6.3.0.10514")]
    [InlineData("sonarr", 18989, "4.0.20.3014")]
    public async Task RealServiceDeletesGeneratedVideoIntoItsRecycleBin(string name, int port, string version)
    {
        var root = Path.GetFullPath(Environment.GetEnvironmentVariable("WATCHCIRCLE_ARR_TEST_ROOT")!);
        Assert.EndsWith(Path.Combine("artifacts", "cleanup-live"), root);
        var fixture = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "fixtures.json")))
            .RootElement.EnumerateArray().Single(value => value.GetProperty("name").GetString() == name);
        var file = Path.GetFullPath(fixture.GetProperty("file").GetString()!);
        var mediaRoot = Path.Combine(root, name == "radarr" ? "test-movies" : "test-series") + Path.DirectorySeparatorChar;
        Assert.StartsWith(mediaRoot, file); Assert.True(File.Exists(file));
        var existingRecycled = Directory.GetFiles(Path.Combine(root, name + "-recycle"), Path.GetFileName(file), SearchOption.AllDirectories)
            .ToDictionary(path => path, File.GetLastWriteTimeUtc);
        var dataPath = Path.Combine(root, name + "-data");
        var configuration = XDocument.Load(Path.Combine(dataPath, "config.xml"));
        Assert.Equal("127.0.0.1", configuration.Root!.Element("BindAddress")!.Value);
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.Add("X-Api-Key", configuration.Root.Element("ApiKey")!.Value);
        var url = "http://127.0.0.1:" + port;
        var status = await http.GetFromJsonAsync<JsonElement>(url + "/api/v3/system/status");
        Assert.Equal(dataPath, status.GetProperty("appData").GetString()); Assert.Equal(version, status.GetProperty("version").GetString());
        var files = await http.GetFromJsonAsync<JsonElement>(url + "/api/v3/" + (name == "radarr" ? "moviefile?movieId=" : "episodefile?seriesId=") + fixture.GetProperty("id").GetInt32());
        Assert.Single(files.EnumerateArray());
        var item = new CleanupMedia { ItemId = Guid.NewGuid(), Kind = name == "radarr" ? "Movie" : "Series", Name = "Generated test video",
            Paths = new[] { file }, Identities = new[] { (name == "radarr" ? "Movie:Tmdb:" : "Series:Tvdb:") + fixture.GetProperty("itemId").GetInt32() }, LibraryIds = new[] { Guid.NewGuid() } };
        var library = new TestLibrary(item);
        var clock = new Clock();
        var store = new CleanupStore(Path.Combine(root, name + "-workflow-" + Guid.NewGuid() + ".json"));
        store.Change(state =>
        {
            var connection = new ArrConnection { Url = url, ApiKey = configuration.Root.Element("ApiKey")!.Value };
            state.Settings = new() { Enabled = true, LibraryIds = item.LibraryIds, Radarr = connection, Sonarr = connection };
            return true;
        });
        using var arrHttp = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(30) };
        using var service = new CleanupService(store, library, new ArrClient(arrHttp), clock, NullLogger<CleanupService>.Instance);
        await service.EvaluateAsync(default); clock.Now = clock.Now.AddMonths(3); await service.EvaluateAsync(default); clock.Now = clock.Now.AddDays(30);
        var entry = Assert.Single(store.Read().Entries);
        var plan = service.Plan(entry.Id, library.User);
        await service.ExecutePlanAsync(plan.Id, library.User, default);
        for (var attempt = 0; attempt < 10 && store.Read().Deletions.SingleOrDefault()?.Phase != "Deleted"; attempt++)
        {
            await Task.Delay(500); await service.EvaluateAsync(default);
        }

        var job = Assert.Single(store.Read().Deletions);
        Assert.True(job.Phase == "Deleted", job.Error ?? store.Read().Entries[0].Error);
        Assert.False(File.Exists(file)); Assert.True(library.Refreshed);
        Assert.True(Assert.Single(store.Read().Archives).Played);
        var recycled = Directory.GetFiles(Path.Combine(root, name + "-recycle"), Path.GetFileName(file), SearchOption.AllDirectories)
            .Where(path => !existingRecycled.TryGetValue(path, out var before) || File.GetLastWriteTimeUtc(path) != before).ToArray();
        Assert.Single(recycled); Assert.Equal(4432552, new FileInfo(recycled[0]).Length);
        using var absent = await http.GetAsync(url + "/api/v3/" + (name == "radarr" ? "movie/" : "series/") + fixture.GetProperty("id").GetInt32());
        Assert.Equal(System.Net.HttpStatusCode.NotFound, absent.StatusCode);
    }

    private sealed class LocalArrTheoryAttribute : TheoryAttribute
    {
        public LocalArrTheoryAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WATCHCIRCLE_ARR_TEST_ROOT")))
                Skip = "Requires generated fixtures in isolated local Radarr/Sonarr instances; see docs/library-cleanup.md.";
        }
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    // Arr runs unmodified. This boundary stands in for a Jellyfin server and its completed library scan.
    private sealed class TestLibrary : ICleanupLibrary
    {
        private readonly CleanupMedia _media;
        public Guid User = Guid.NewGuid();
        public bool Refreshed;
        public TestLibrary(CleanupMedia media) => _media = media;
        public IReadOnlyList<CleanupMedia> Inventory() => File.Exists(_media.Paths[0]) ? new[] { CleanupStore.Clone(_media) } : Array.Empty<CleanupMedia>();
        public IReadOnlyList<CleanupUserState> ReadStates(CleanupMedia media) => new[] { new CleanupUserState { ItemId = _media.ItemId, UserId = User, Played = true,
            Identities = _media.Kind == "Movie" ? _media.Identities : new[] { "Episode:" + _media.Identities[0] + ":S1:E:1" } } };
        public IReadOnlyList<CleanupUser> Users() => new[] { new CleanupUser(User, "Test user") };
        public bool CanAccess(Guid itemId, Guid userId) => userId == User;
        public Guid RootItemId(Guid itemId) => _media.ItemId;
        public bool IsPlaying(IReadOnlyList<CleanupEntry> scope) => false;
        public bool FilesAbsent(string[] paths) => paths.All(path => !File.Exists(path));
        public Task RefreshAsync(CancellationToken token) { Refreshed = true; return Task.CompletedTask; }
        public void RestorePlayed(Guid userId, Guid itemId, bool played) => throw new NotSupportedException();
        public object Libraries() => Array.Empty<object>();
    }
}
