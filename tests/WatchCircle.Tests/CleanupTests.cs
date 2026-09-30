using System.Net;
using System.Reflection;
using System.Text.Json;
using Jellyfin.Plugin.WatchCircle.Api;
using Jellyfin.Plugin.WatchCircle.Cleanup;
using MediaBrowser.Common.Api;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace WatchCircle.Tests;

public sealed class CleanupTests
{
    [Fact]
    public async Task UnknownHistoryGetsFullGraceAndRepeatedScansKeepTheDeadline()
    {
        using var env = new Scenario();
        await env.Service.EvaluateAsync(default);
        Assert.Null(env.Entry.NominationId);
        env.Time.Now = env.Time.Now.AddMonths(3).AddDays(-1);
        await env.Service.EvaluateAsync(default);
        Assert.Null(env.Entry.NominationId);
        env.Time.Now = env.Time.Now.AddDays(1);
        await env.Service.EvaluateAsync(default);
        var nomination = env.Entry.NominationId;
        var deadline = env.Entry.DeleteAt;
        Assert.Equal(env.Time.Now.AddDays(30), deadline);
        env.Time.Now = env.Time.Now.AddDays(10);
        await env.Service.EvaluateAsync(default);
        Assert.Equal(nomination, env.Entry.NominationId);
        Assert.Equal(deadline, env.Entry.DeleteAt);
    }

    [Fact]
    public async Task BannerIncludesAnsweredNominationsAndLetsUserChangeIndifference()
    {
        using var env = new Scenario(); await env.Due();
        var nomination = env.Entry.NominationId!.Value;
        env.Service.Reply(env.Entry.Id, env.User, new() { NominationId = nomination, Answer = "Indifferent" });
        Assert.Empty(Json(env.Service.Pending(env.User)).EnumerateArray());
        var status = Json(env.Service.ItemStatuses(env.User, new[] { env.Media.ItemId }));
        Assert.Equal("Indifferent", status[0].GetProperty("Answer").GetString());
        Assert.Equal(nomination, env.Entry.NominationId);
        env.Service.Reply(env.Entry.Id, env.User, new() { NominationId = nomination, Answer = "Keep" });
        Assert.Null(env.Entry.NominationId);
        Assert.Equal(2, env.Store.Read().Replies.Count);
        Assert.All(env.Store.Read().Replies, reply => Assert.False(reply.Active));
        Assert.Throws<InvalidOperationException>(() => env.Service.Reply(env.Entry.Id, env.User, new() { NominationId = nomination, Answer = "Indifferent" }));
        Assert.Empty(Json(env.Service.ItemStatuses(env.User, new[] { env.Media.ItemId })).EnumerateArray());
    }

    [Fact]
    public async Task SeriesEpisodeStatusUsesSeriesNominationAndEnforcesAccess()
    {
        using var env = new Scenario(true); await env.Due();
        var episode = env.Library.States[0].ItemId;
        var status = Json(env.Service.ItemStatuses(env.User, new[] { episode }));
        Assert.Equal(env.Media.ItemId, status[0].GetProperty("RootItemId").GetGuid());
        env.Library.Denied.Add(episode);
        Assert.Empty(Json(env.Service.ItemStatuses(env.User, new[] { episode })).EnumerateArray());
        env.Library.Denied.Add(env.Media.ItemId);
        Assert.Empty(Json(env.Service.Pending(env.User)).EnumerateArray());
        Assert.Throws<UnauthorizedAccessException>(() => env.Service.Reply(env.Entry.Id, env.User, new() { NominationId = env.Entry.NominationId!.Value, Answer = "Keep" }));
    }

    [Fact]
    public async Task OnlyTheInteractingUsersIndifferenceIsResetAndHistoryIsRetained()
    {
        using var env = new Scenario(); await env.Due();
        foreach (var user in env.Library.Accounts)
            env.Service.Reply(env.Entry.Id, user.Id, new() { NominationId = env.Entry.NominationId!.Value, Answer = "Indifferent" });
        env.Observe(env.User, true, false, UserDataSaveReason.PlaybackProgress);
        Assert.Null(env.Entry.NominationId);
        Assert.False(env.Store.Read().Replies.Single(reply => reply.UserId == env.User).Active);
        Assert.True(env.Store.Read().Replies.Single(reply => reply.UserId != env.User).Active);
        env.Time.Now = env.Time.Now.AddMonths(3); await env.Service.EvaluateAsync(default);
        Assert.Single(Json(env.Service.Pending(env.User)).EnumerateArray());
        Assert.Empty(Json(env.Service.Pending(env.OtherUser)).EnumerateArray());
    }

    [Fact]
    public async Task FavoriteRemovalAndImportAreNotActivityButANewFavoriteIs()
    {
        using var env = new Scenario(); await env.Due();
        env.Observe(env.User, false, false, UserDataSaveReason.Import);
        Assert.NotNull(env.Entry.NominationId);
        env.Observe(env.User, false, true, UserDataSaveReason.UpdateUserRating);
        Assert.Null(env.Entry.NominationId);
        env.Time.Now = env.Time.Now.AddMonths(3); await env.Service.EvaluateAsync(default);
        var nomination = env.Entry.NominationId;
        env.Observe(env.User, false, true, UserDataSaveReason.UpdateUserRating);
        Assert.Equal(nomination, env.Entry.NominationId);
        env.Observe(env.User, false, false, UserDataSaveReason.UpdateUserRating);
        Assert.Equal(nomination, env.Entry.NominationId);
    }

    [Fact]
    public void MovieActivityPropagatesAcrossEveryDirectCollectionButNotTransitively()
    {
        var state = new CleanupState { Settings = new() { Enabled = true } };
        CleanupEntry a = Entry("1"), b = Entry("2"), c = Entry("3"), d = Entry("4");
        CleanupEntry Collection(params CleanupEntry[] members) => new() { Media = new() { Kind = "Collection", ItemId = Guid.NewGuid(), Members = members.Select(value => value.Media.ItemId).ToArray() } };
        state.Entries.AddRange(new[] { a, b, c, d, Collection(a, b), Collection(a, c), Collection(c, d) });
        foreach (var value in state.Entries) value.NominationId = Guid.NewGuid();
        CleanupRules.Interact(state, a, Guid.NewGuid(), "Playback", DateTimeOffset.UtcNow);
        Assert.Null(a.NominationId); Assert.Null(b.NominationId); Assert.Null(c.NominationId);
        Assert.NotNull(d.NominationId);
    }

    [Fact]
    public async Task ProtectionCancelsPlansAndUnprotectionGivesFreshWarning()
    {
        using var env = new Scenario(); await env.Due();
        var plan = env.Service.Plan(env.Entry.Id, env.User);
        env.Service.Protect(env.Entry.Id, true);
        await env.Service.ExecutePlanAsync(plan.Id, env.User, default);
        Assert.Equal(0, env.Handler.Deletes);
        env.Time.Now = env.Time.Now.AddYears(1); await env.Service.EvaluateAsync(default);
        Assert.Null(env.Entry.NominationId);
        env.Service.Protect(env.Entry.Id, false);
        await env.Service.EvaluateAsync(default); Assert.Null(env.Entry.NominationId);
        env.Time.Now = env.Time.Now.AddMonths(3); await env.Service.EvaluateAsync(default);
        Assert.Equal(env.Time.Now.AddDays(30), env.Entry.DeleteAt);
    }

    [Theory]
    [InlineData("keep")]
    [InlineData("protect")]
    [InlineData("playback")]
    [InlineData("active")]
    [InlineData("disable-auto")]
    [InlineData("new-collection-protection")]
    public async Task ChangesAtTheLastExternalLookupPreventDeletion(string kind)
    {
        using var env = new Scenario(); await env.Due();
        env.Store.Change(state => state.Settings.AutomaticDeletion = true);
        var plan = env.Service.Plan(env.Entry.Id, Guid.Empty);
        env.Handler.BeforeSecondFileLookup = () =>
        {
            if (kind == "keep") env.Service.Reply(env.Entry.Id, env.User, new() { NominationId = env.Entry.NominationId!.Value, Answer = "Keep" });
            if (kind == "protect") env.Service.Protect(env.Entry.Id, true);
            if (kind == "playback") env.Observe(env.User, false, false, UserDataSaveReason.PlaybackStart);
            if (kind == "active") env.Library.Playing = true;
            if (kind == "disable-auto") env.Store.Change(state => state.Settings.AutomaticDeletion = false);
            if (kind == "new-collection-protection")
            {
                var collection = new CleanupMedia { Kind = "Collection", ItemId = Guid.NewGuid(), Members = new[] { env.Media.ItemId } };
                env.Library.Media.Add(collection);
                env.Store.Change(state => { state.Entries.Add(new() { Media = CleanupStore.Clone(collection), Protected = true }); return true; });
            }
        };
        await env.Service.ExecutePlanAsync(plan.Id, Guid.Empty, default);
        Assert.Equal(0, env.Handler.Deletes); Assert.True(File.Exists(env.Media.Paths[0]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeletesOnlyMappedTestFilesAndRestoresIndividualWatchedFlags(bool series)
    {
        using var env = new Scenario(series); await env.Due();
        env.Library.States[0].Played = true;
        await env.Delete();
        Assert.Equal(1, env.Handler.Deletes); Assert.All(env.Media.Paths, path => Assert.False(File.Exists(path)));
        Assert.Equal("Deleted", Assert.Single(env.Store.Read().Deletions).Phase);
        Assert.Equal(env.Library.Accounts.Count * (series ? 2 : 1), env.Store.Read().Archives.Count);
        Assert.Contains("deleteFiles=true", env.Handler.DeleteUrl);
        Assert.Contains(series ? "addImportListExclusion=false" : "addImportExclusion=false", env.Handler.DeleteUrl);
        env.Readd(series);
        env.Restart(); await env.Service.EvaluateAsync(default);
        Assert.True(env.Library.States[0].Played);
        Assert.All(env.Library.States.Skip(1), state => Assert.False(state.Played));
        Assert.Single(env.Library.Restores); Assert.Null(env.Entry.NominationId);
        await env.Service.EvaluateAsync(default); Assert.Single(env.Library.Restores);
    }

    [Fact]
    public async Task NewerExplicitUnwatchWinsAgainstArchiveWithNewItemId()
    {
        using var env = new Scenario(); await env.Due(); env.Library.States[0].Played = true;
        await env.Delete(); env.Readd(false); env.Restart(); env.Time.Now = env.Time.Now.AddMinutes(1);
        var value = env.Library.States[0];
        env.Service.Observe(env.Media.ItemId, value.ItemId, value.UserId, value.Identities, false, false, UserDataSaveReason.TogglePlayed);
        await env.Service.EvaluateAsync(default);
        Assert.False(env.Library.States[0].Played); Assert.Empty(env.Library.Restores);
    }

    [Fact]
    public async Task SuccessfulHttpWithoutFileRemovalIsIncompleteAndRestartOnlyVerifies()
    {
        using var env = new Scenario(); await env.Due(); env.Handler.LeaveFiles = true;
        await env.Delete(); Assert.Equal("Failed", Assert.Single(env.Store.Read().Deletions).Phase);
        env.Restart(); await env.Service.EvaluateAsync(default);
        Assert.Equal(1, env.Handler.Deletes); Assert.Equal("Failed", Assert.Single(env.Store.Read().Deletions).Phase);
        // The isolated service finishes its delayed removal after the plugin restarted.
        foreach (var path in env.Media.Paths) File.Delete(path);
        env.Library.Media.Clear();
        await env.Service.EvaluateAsync(default);
        Assert.Equal(1, env.Handler.Deletes); Assert.Equal("Deleted", Assert.Single(env.Store.Read().Deletions).Phase);
    }

    [Fact]
    public async Task UnavailableServiceOrMismatchedFilesNeverDelete()
    {
        using var env = new Scenario(); await env.Due(); env.Handler.Fail = true;
        await env.Delete(); Assert.Equal(0, env.Handler.Deletes); Assert.NotNull(env.Entry.Error);
        env.Handler.Fail = false; env.Handler.ExtraFile = true;
        await env.Delete(); Assert.Equal(0, env.Handler.Deletes); Assert.True(File.Exists(env.Media.Paths[0]));
    }

    [Fact]
    public async Task FailedBackupBlocksDispatchAndCorruptStateCannotBeOverwritten()
    {
        using var env = new Scenario(); await env.Due();
        var plan = env.Service.Plan(env.Entry.Id, env.User);
        using (var locked = new FileStream(env.StatePath + ".bak", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            await Assert.ThrowsAsync<IOException>(() => env.Service.ExecutePlanAsync(plan.Id, env.User, default));
        }
        Assert.Equal(0, env.Handler.Deletes); Assert.NotNull(env.Store.Fault);
        File.WriteAllText(env.StatePath, "{");
        var store = new CleanupStore(env.StatePath);
        Assert.NotNull(store.Fault); Assert.Throws<InvalidOperationException>(() => store.Change(state => state.Settings.Enabled = true));
        Assert.Equal("{", File.ReadAllText(env.StatePath));
    }

    [Fact]
    public async Task AdminHasAllUsersIncludingUnstartedAndSecretsStayServerSide()
    {
        using var env = new Scenario(); await env.Service.EvaluateAsync(default);
        var details = Json(await env.Service.AdminDetailsAsync(env.Entry.Id, env.User, default));
        var users = details.GetProperty("Users").EnumerateArray().ToArray();
        Assert.Equal(2, users.Length); Assert.Contains(users, user => user.GetProperty("Id").GetGuid() == env.User);
        Assert.All(users, user => Assert.False(user.GetProperty("Progress").GetProperty("Started").GetBoolean()));
        var settings = Json(env.Service.GetSettings());
        Assert.Equal("", settings.GetProperty("Settings").GetProperty("Radarr").GetProperty("ApiKey").GetString());
        Assert.DoesNotContain("test-secret", JsonSerializer.Serialize(env.Service.AdminView()));
    }

    [Fact]
    public void EveryAdministrativeEndpointRequiresJellyfinElevation()
    {
        foreach (var method in typeof(CleanupController).GetMethods())
        {
            if (method.GetCustomAttributes<HttpMethodAttribute>().Any(route => route.Template?.StartsWith("Admin/") == true))
                Assert.Contains(method.GetCustomAttributes<AuthorizeAttribute>(), attribute => attribute.Policy == Policies.RequiresElevation);
        }
    }

    [Fact]
    public void PathMappingIsBoundedAndProviderContradictionsAreRejected()
    {
        var connection = new ArrConnection { Paths = new[] { new PathTranslation { Jellyfin = "/media", Arr = "/movies" } } };
        Assert.Equal("/movies/a/movie.mkv", ArrClient.Translate("/media/a/movie.mkv", connection));
        Assert.Equal("/media2/a.mkv", ArrClient.Translate("/media2/a.mkv", connection));
        Assert.Throws<InvalidOperationException>(() => ArrClient.NormalizePath("/media/../secrets"));
        Assert.False(CleanupRules.SameIdentity(new[] { "Movie:Tmdb:1", "Movie:Imdb:tt1" }, new[] { "Movie:Tmdb:1", "Movie:Imdb:tt2" }));
        Assert.False(CleanupRules.SameIdentity(new[] { "Episode:Tmdb:1", "Episode:Series:Tvdb:12:S1:E:1" }, new[] { "Episode:Tmdb:1", "Episode:Series:Tvdb:13:S1:E:1" }));
    }

    [Fact]
    public async Task ProtectionSurvivesRestartAndNewJellyfinIds()
    {
        using var env = new Scenario(); await env.Service.EvaluateAsync(default);
        env.Service.Protect(env.Entry.Id, true);
        env.Readd(false); env.Restart(); await env.Service.EvaluateAsync(default);
        Assert.True(env.Entry.Protected); Assert.Null(env.Entry.NominationId);
    }

    [Fact]
    public async Task BackupFromAbortedPreparationDoesNotRestoreState()
    {
        using var env = new Scenario(); await env.Due(); env.Library.States[0].Played = true;
        env.Handler.BeforeSecondFileLookup = () => env.Service.Protect(env.Entry.Id, true);
        await env.Delete(); Assert.NotEmpty(env.Store.Read().Archives); Assert.Empty(env.Store.Read().Deletions);
        env.Readd(false); await env.Service.EvaluateAsync(default);
        Assert.Empty(env.Library.Restores);
    }

    [Fact]
    public async Task AmbiguousRestoredEpisodesNeverReceiveAnotherEpisodesState()
    {
        using var env = new Scenario(true); await env.Due(); env.Library.States[0].Played = true;
        await env.Delete(); env.Readd(true);
        var duplicate = CleanupStore.Clone(env.Library.States[0]); duplicate.ItemId = Guid.NewGuid(); env.Library.States.Add(duplicate);
        await env.Service.EvaluateAsync(default);
        Assert.Empty(env.Library.Restores); Assert.Contains(env.Store.Read().Archives, archive => archive.Error is not null);
    }

    [Fact]
    public async Task DisabledOrUnselectedLibrariesCannotBeDeleted()
    {
        using var env = new Scenario(); await env.Due();
        env.Store.Change(state => state.Settings.LibraryIds = Array.Empty<Guid>());
        Assert.Throws<InvalidOperationException>(() => env.Service.Plan(env.Entry.Id, env.User));
        await env.Service.EvaluateAsync(default); Assert.Null(env.Entry.NominationId);
        env.Store.Change(state => state.Settings.Enabled = false);
        env.Observe(env.User, false, false, UserDataSaveReason.PlaybackStart);
        Assert.Null(env.Entry.LastInteraction);
    }

    [Fact]
    public async Task CollectionPlanRefusesToIncludeAProtectedMovie()
    {
        using var env = new Scenario(); await env.Due();
        var collection = new CleanupEntry { Media = new() { ItemId = Guid.NewGuid(), Kind = "Collection", Members = new[] { env.Media.ItemId } } };
        env.Store.Change(state => { state.Entries.Add(collection); return true; });
        env.Service.Protect(env.Entry.Id, true);
        Assert.Throws<InvalidOperationException>(() => env.Service.Plan(collection.Id, env.User));
    }

    [Fact]
    public async Task ExpiredConfirmationCannotExecute()
    {
        using var env = new Scenario(); await env.Due(); var plan = env.Service.Plan(env.Entry.Id, env.User);
        env.Time.Now = env.Time.Now.AddMinutes(16);
        await Assert.ThrowsAsync<InvalidOperationException>(() => env.Service.ExecutePlanAsync(plan.Id, env.User, default));
        Assert.Equal(0, env.Handler.Deletes);
    }

    [Fact]
    public void RestoreUsesOnlyJellyfinsPlayedFieldAndImportReason()
    {
        var users = new Moq.Mock<MediaBrowser.Controller.Library.IUserManager>();
        var library = new Moq.Mock<MediaBrowser.Controller.Library.ILibraryManager>();
        var data = new Moq.Mock<MediaBrowser.Controller.Library.IUserDataManager>();
        var user = new Jellyfin.Database.Implementations.Entities.User("Test", "auth", "reset");
        var movie = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid() };
        users.Setup(value => value.GetUserById(user.Id)).Returns(user);
        library.Setup(value => value.GetItemById(movie.Id)).Returns(movie);
        var gateway = new CleanupLibrary(library.Object, users.Object, data.Object, null!);
        gateway.RestorePlayed(user.Id, movie.Id, true);
        data.Verify(value => value.SaveUserData(user, movie,
            Moq.It.Is<MediaBrowser.Model.Dto.UpdateUserItemDataDto>(dto => dto.Played == true && dto.PlayCount == null && dto.PlaybackPositionTicks == null && dto.LastPlayedDate == null && dto.IsFavorite == null),
            UserDataSaveReason.Import), Moq.Times.Once);
        data.VerifyNoOtherCalls();
    }

    private static CleanupEntry Entry(string id) => new() { Media = new() { Kind = "Movie", ItemId = Guid.NewGuid(), Identities = new[] { "Movie:Tmdb:" + id } } };
    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value);

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Scenario : IDisposable
    {
        public readonly string DirectoryPath = Path.Combine(Path.GetTempPath(), "watchcircle-cleanup-test-" + Guid.NewGuid());
        public readonly Clock Time = new();
        public readonly Library Library = new();
        public readonly Handler Handler;
        public readonly Guid User = Guid.NewGuid(), OtherUser = Guid.NewGuid(), LibraryId = Guid.NewGuid();
        public CleanupStore Store;
        public CleanupService Service;
        public CleanupMedia Media;
        public string StatePath => Path.Combine(DirectoryPath, "cleanup-state.json");
        public CleanupEntry Entry => Store.Read().Entries.Single(entry => entry.Media.Kind != "Collection");
        public Scenario(bool series = false)
        {
            Directory.CreateDirectory(DirectoryPath);
            var mediaFolder = Path.Combine(DirectoryPath, "test-media"); Directory.CreateDirectory(mediaFolder);
            Media = new CleanupMedia { ItemId = Guid.NewGuid(), Name = "Isolated test title", Kind = series ? "Series" : "Movie", LibraryIds = new[] { LibraryId },
                Identities = new[] { series ? "Series:Tvdb:123" : "Movie:Tmdb:123" },
                Paths = Enumerable.Range(1, series ? 2 : 1).Select(n => Path.Combine(mediaFolder, "test-" + n + ".mkv")).ToArray() };
            foreach (var path in Media.Paths) File.WriteAllBytes(path, new byte[] { 0x1a, 0x45, 0xdf, 0xa3 });
            Library.Media.Add(Media); Library.Accounts.Add(new(User, "Administrator")); Library.Accounts.Add(new(OtherUser, "Anna"));
            PopulateStates(series, false);
            Store = new CleanupStore(StatePath);
            Store.Change(state => { state.Settings = new() { Enabled = true, LibraryIds = new[] { LibraryId },
                Radarr = new() { Url = "http://isolated.test", ApiKey = "test-secret" }, Sonarr = new() { Url = "http://isolated.test", ApiKey = "test-secret" } }; return true; });
            Handler = new Handler(this); Service = NewService();
        }
        private CleanupService NewService() => new(Store, Library, new ArrClient(new HttpClient(Handler)), Time, NullLogger<CleanupService>.Instance);
        public void Restart() { Service.Dispose(); Store = new CleanupStore(StatePath); Service = NewService(); }
        public async Task Due() { await Service.EvaluateAsync(default); Time.Now = Time.Now.AddMonths(3); await Service.EvaluateAsync(default); Time.Now = Time.Now.AddDays(30); }
        public Task Delete() => Service.ExecutePlanAsync(Service.Plan(Entry.Id, User).Id, User, default);
        public void Observe(Guid user, bool played, bool favorite, UserDataSaveReason reason)
        { var value = Library.States.First(state => state.UserId == user); Service.Observe(Media.ItemId, value.ItemId, user, value.Identities, played, favorite, reason); }
        public void Readd(bool series)
        {
            Media = CleanupStore.Clone(Media); Media.ItemId = Guid.NewGuid();
            Media.Paths = Media.Paths.Select(path => Path.Combine(DirectoryPath, "readded-" + Path.GetFileName(path))).ToArray();
            foreach (var path in Media.Paths) File.WriteAllText(path, "readded test media");
            Library.Media.Clear(); Library.Media.Add(Media); Library.States.Clear(); PopulateStates(series, true);
        }
        private void PopulateStates(bool series, bool newEpisode)
        {
            var items = Enumerable.Range(1, series ? (newEpisode ? 3 : 2) : 1).Select(n => (Id: series ? Guid.NewGuid() : Media.ItemId,
                Identity: series ? "Episode:Series:Tvdb:123:S1:E:" + n : "Movie:Tmdb:123")).ToArray();
            foreach (var user in Library.Accounts)
                foreach (var item in items) Library.States.Add(new() { ItemId = item.Id, UserId = user.Id, Identities = new[] { item.Identity }, Progress = new() });
        }
        public void Dispose()
        {
            Service.Dispose(); Handler.Dispose();
            // Every test owns this unique temporary directory and no external service is involved.
            var absolute = Path.GetFullPath(DirectoryPath);
            Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar + "watchcircle-cleanup-test-", absolute);
            Directory.Delete(absolute, true);
        }
    }

    private sealed class Library : ICleanupLibrary
    {
        public List<CleanupMedia> Media = new();
        public List<CleanupUserState> States = new();
        public List<CleanupUser> Accounts = new();
        public HashSet<Guid> Denied = new();
        public List<(Guid User, Guid Item)> Restores = new();
        public bool Playing;
        public IReadOnlyList<CleanupMedia> Inventory() => CleanupStore.Clone(Media);
        public IReadOnlyList<CleanupUserState> ReadStates(CleanupMedia media) => States;
        public IReadOnlyList<CleanupUser> Users() => Accounts;
        public bool CanAccess(Guid itemId, Guid userId) => !Denied.Contains(itemId);
        public Guid RootItemId(Guid id) => States.Any(value => value.ItemId == id) ? Media[0].ItemId : id;
        public bool IsPlaying(IReadOnlyList<CleanupEntry> scope) => Playing;
        public bool FilesAbsent(string[] paths) => paths.All(path => !File.Exists(path));
        public Task RefreshAsync(CancellationToken token) { Media.RemoveAll(media => media.Paths.All(path => !File.Exists(path))); return Task.CompletedTask; }
        public void RestorePlayed(Guid userId, Guid itemId, bool played)
        { States.Single(value => value.UserId == userId && value.ItemId == itemId).Played = played; Restores.Add((userId, itemId)); }
        public object Libraries() => Array.Empty<object>();
    }

    private sealed class Handler : HttpMessageHandler
    {
        private readonly Scenario _env;
        private int _files;
        private bool _removed;
        public int Deletes;
        public string? DeleteUrl;
        public bool Fail, ExtraFile, LeaveFiles;
        public Action? BeforeSecondFileLookup;
        public Handler(Scenario env) => _env = env;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("isolated.test", request.RequestUri!.Host);
            Assert.Equal("test-secret", request.Headers.GetValues("X-Api-Key").Single());
            if (Fail) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            var path = request.RequestUri.AbsolutePath;
            if (request.Method == HttpMethod.Delete)
            {
                Deletes++; DeleteUrl = request.RequestUri.ToString(); _removed = true;
                if (!LeaveFiles) foreach (var file in _env.Media.Paths) File.Delete(file);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
            }
            object data;
            if (path.EndsWith("/17")) return Task.FromResult(new HttpResponseMessage(_removed ? HttpStatusCode.NotFound : HttpStatusCode.OK) { Content = new StringContent("{}") });
            if (path.EndsWith("file"))
            {
                if (++_files == 2) BeforeSecondFileLookup?.Invoke();
                data = _env.Media.Paths.Concat(ExtraFile ? new[] { Path.Combine(_env.DirectoryPath, "unexpected.mkv") } : Array.Empty<string>()).Select(file => new { path = file }).ToArray();
            }
            else data = new[] { new { id = 17, tmdbId = 123, tvdbId = 123, path = Path.GetDirectoryName(_env.Media.Paths[0]) } };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(data)) });
        }
    }
}
