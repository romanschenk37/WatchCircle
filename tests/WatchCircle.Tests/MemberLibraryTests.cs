using System.Security.Claims;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.WatchCircle.Abstractions;
using Jellyfin.Plugin.WatchCircle.Api;
using Jellyfin.Plugin.WatchCircle.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace WatchCircle.Tests;

public class MemberLibraryTests
{
    private readonly User _viewer = new("Viewer", "auth", "reset");
    private readonly Guid _memberId = Guid.NewGuid();
    private readonly Mock<IGroupMembershipService> _groups = new();
    private readonly Mock<IUserProfileService> _profiles = new();
    private readonly Mock<ILibraryManager> _library = new();
    private readonly Mock<IUserManager> _users = new();
    private readonly Mock<IDbContextFactory<JellyfinDbContext>> _database = new(MockBehavior.Strict);
    private readonly MemberLibraryService _service;

    public MemberLibraryTests()
    {
        _groups.Setup(value => value.GetVisibleMemberIds(_viewer.Id)).Returns(new[] { _memberId });
        _users.Setup(value => value.GetUserById(_viewer.Id)).Returns(_viewer);
        _profiles.Setup(value => value.MapUser(_memberId, 88)).Returns(new GroupUserDto { Id = _memberId, Name = "Member" });
        _service = new MemberLibraryService(_groups.Object, _profiles.Object, _library.Object, _users.Object, _database.Object);
    }

    [Fact]
    public void ProfilesOutsideGroupsAndSelfNeverReadHistory()
    {
        Assert.Null(_service.GetProfile(_viewer.Id, Guid.NewGuid(), default));
        Assert.Null(_service.GetProfile(_viewer.Id, _viewer.Id, default));
        Assert.Null(_service.GetProfile(Guid.Empty, _memberId, default));
        _groups.Setup(value => value.GetVisibleMemberIds(_viewer.Id)).Returns(Array.Empty<Guid>());
        Assert.Null(_service.GetProfile(_viewer.Id, _memberId, default));
        _database.VerifyNoOtherCalls();
    }

    [Fact]
    public void MissingAccountsNeverReadHistory()
    {
        _profiles.Setup(value => value.MapUser(_memberId, 88)).Returns((GroupUserDto?)null);
        Assert.Null(_service.GetProfile(_viewer.Id, _memberId, default));
        _database.VerifyNoOtherCalls();
    }

    [Fact]
    public void EndpointAuthenticatesViewerAndDoesNotDiscloseNonMembers()
    {
        var controller = new MemberProfileController(_service) { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };
        Assert.IsType<UnauthorizedResult>(controller.GetProfile(_memberId, default).Result);
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("Jellyfin-UserId", _viewer.Id.ToString()) }, "test"));
        Assert.IsType<NotFoundResult>(controller.GetProfile(Guid.NewGuid(), default).Result);
    }

    [Fact]
    public void FavoritesAreExclusiveAndEachMovieComparesIndependentUserProgress()
    {
        var started = Movie("Started");
        var completed = Movie("Completed");
        var favorite = Movie("Favorite");
        var rows = new[]
        {
            Row(started.Id, position: 30, favorite: true), Row(completed.Id, played: true, favorite: true), Row(favorite.Id, favorite: true),
            Row(started.Id, user: _viewer.Id, position: 60), Row(completed.Id, user: _viewer.Id, position: 10), Row(favorite.Id, user: _viewer.Id, played: true)
        };
        var result = _service.BuildItems(_viewer.Id, _memberId, rows);
        Assert.Equal(3, result.Count);
        var first = Assert.Single(result, item => item.Category == "started");
        Assert.Equal(30d, first.Member.Percent);
        Assert.Equal(60d, first.You.Percent);
        var second = Assert.Single(result, item => item.Category == "completed");
        Assert.Equal(100d, second.Member.Percent);
        Assert.Equal(10d, second.You.Percent);
        var third = Assert.Single(result, item => item.Category == "favorites");
        Assert.False(third.Member.Started);
        Assert.True(third.You.Completed);
    }

    [Fact]
    public void LastPlayAndPlayCountCountAsStartedEvenWithoutResumePosition()
    {
        var one = Movie("Last play");
        var two = Movie("Count");
        var a = Row(one.Id); a.LastPlayedDate = DateTime.UtcNow;
        var b = Row(two.Id); b.PlayCount = 1;
        Assert.All(_service.BuildItems(_viewer.Id, _memberId, new[] { a, b }), item => Assert.Equal("started", item.Category));
        Assert.Equal(2, _service.BuildItems(_viewer.Id, _memberId, new[] { a, b }).Count);
    }

    [Fact]
    public void SeriesCombinesAllEpisodesInsteadOfTreatingOneFinishedEpisodeAsFinishedSeries()
    {
        var (series, episodes) = Series(3);
        var result = Assert.Single(_service.BuildItems(_viewer.Id, _memberId, new[]
        {
            Row(series.Id, favorite: true), Row(episodes[0].Id, played: true), Row(episodes[1].Id, position: 50),
            Row(episodes[2].Id, user: _viewer.Id, played: true)
        }));
        Assert.Equal("started", result.Category);
        Assert.Equal(series.Id, result.Id);
        Assert.Equal(3, result.Member.TotalEpisodes);
        Assert.Equal(1, result.Member.CompletedEpisodes);
        Assert.Equal(50d, result.Member.Percent);
        Assert.Equal(100d / 3, result.You.Percent!.Value, 6);
        _library.Verify(value => value.GetItemList(It.Is<InternalItemsQuery>(q => q.User == _viewer && q.ParentId == series.Id && q.IsVirtualItem == false && q.IsMissing == false)), Times.Once);
    }

    [Fact]
    public void SeriesIsCompletedOnlyWhenAllAvailableEpisodesIncludingSpecialsArePlayed()
    {
        var (series, episodes) = Series(2);
        episodes[1].ParentIndexNumber = 0;
        var rows = episodes.Select(episode => Row(episode.Id, played: true)).ToList();
        rows.Add(Row(series.Id, favorite: true));
        var result = Assert.Single(_service.BuildItems(_viewer.Id, _memberId, rows));
        Assert.Equal("completed", result.Category);
        Assert.Equal(100d, result.Member.Percent);
        Assert.Equal(2, result.Member.CompletedEpisodes);
        Assert.False(result.You.Started);
    }

    [Fact]
    public void AddingAnAvailableEpisodeMovesSeriesBackToStarted()
    {
        var (series, episodes) = Series(2);
        var rows = episodes.Select(episode => Row(episode.Id, played: true)).ToArray();
        Assert.Equal("completed", Assert.Single(_service.BuildItems(_viewer.Id, _memberId, rows)).Category);
        var extra = new Episode { Id = Guid.NewGuid(), SeriesId = series.Id, RunTimeTicks = 100 };
        _library.Setup(value => value.GetItemList(It.IsAny<InternalItemsQuery>())).Returns(episodes.Append(extra).Cast<BaseItem>().ToArray());
        Assert.Equal("started", Assert.Single(_service.BuildItems(_viewer.Id, _memberId, rows)).Category);
    }

    [Fact]
    public void InaccessibleTitlesAndEpisodesAreNotDisclosedOrCounted()
    {
        var hidden = Guid.NewGuid();
        var (series, episodes) = Series(1);
        var hiddenEpisode = new Episode { Id = Guid.NewGuid(), SeriesId = series.Id };
        var rows = new[] { Row(hidden, played: true), Row(hiddenEpisode.Id, played: true), Row(series.Id, favorite: true) };
        var result = Assert.Single(_service.BuildItems(_viewer.Id, _memberId, rows));
        Assert.Equal(series.Id, result.Id);
        Assert.Equal("favorites", result.Category);
        Assert.Equal(1, result.Member.TotalEpisodes);
        Assert.False(result.Member.Started);
    }

    [Fact]
    public void DuplicateDataDoesNotDuplicateTitlesOrEpisodeCounts()
    {
        var (_, episodes) = Series(2);
        var result = Assert.Single(_service.BuildItems(_viewer.Id, _memberId, new[] { Row(episodes[0].Id, position: 10), Row(episodes[0].Id, played: true) }));
        Assert.Equal(1, result.Member.CompletedEpisodes);
        Assert.Equal(50d, result.Member.Percent);
    }

    [Fact]
    public void UnknownRuntimeAndEmptySeriesDoNotInventCompletion()
    {
        var movie = Movie("Unknown"); movie.RunTimeTicks = null;
        var (series, _) = Series(0);
        var result = _service.BuildItems(_viewer.Id, _memberId, new[] { Row(movie.Id, position: 10), Row(series.Id, favorite: true) });
        Assert.Null(result.Single(item => item.Id == movie.Id).Member.Percent);
        Assert.False(result.Single(item => item.Id == movie.Id).Member.Completed);
        Assert.Equal("favorites", result.Single(item => item.Id == series.Id).Category);
    }

    [Fact]
    public void ResumeAtEndDoesNotOverrideJellyfinPlayedFlag()
    {
        var movie = Movie("End");
        var result = Assert.Single(_service.BuildItems(_viewer.Id, _memberId, new[] { Row(movie.Id, position: 200) }));
        Assert.Equal("started", result.Category);
        Assert.InRange(result.Member.Percent!.Value, 99, 99.999);
    }

    private Movie Movie(string name)
    {
        var movie = new Movie { Id = Guid.NewGuid(), Name = name, RunTimeTicks = 100 };
        _library.Setup(value => value.GetItemById<BaseItem>(movie.Id, _viewer.Id)).Returns(movie);
        return movie;
    }

    private (Series Series, Episode[] Episodes) Series(int count)
    {
        var series = new Series { Id = Guid.NewGuid(), Name = "Series" };
        _library.Setup(value => value.GetItemById<BaseItem>(series.Id, _viewer.Id)).Returns(series);
        var episodes = Enumerable.Range(1, count).Select(index => new Episode { Id = Guid.NewGuid(), SeriesId = series.Id, ParentIndexNumber = 1, IndexNumber = index, RunTimeTicks = 100 }).ToArray();
        foreach (var episode in episodes) _library.Setup(value => value.GetItemById<BaseItem>(episode.Id, _viewer.Id)).Returns(episode);
        _library.Setup(value => value.GetItemList(It.Is<InternalItemsQuery>(query => query.ParentId == series.Id))).Returns(episodes);
        return (series, episodes);
    }

    private UserData Row(Guid item, Guid? user = null, bool played = false, long position = 0, bool favorite = false) => new()
    {
        ItemId = item, UserId = user ?? _memberId, Played = played, PlaybackPositionTicks = position, IsFavorite = favorite,
        CustomDataKey = string.Empty, Item = null!, User = null!
    };
}
