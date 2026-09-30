using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.WatchCircle.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using Xunit;

namespace WatchCircle.Tests;

public class TitleProgressTests
{
    [Fact]
    public void SeriesSeasonAndSingleEpisodeUseTheirOwnDenominators()
    {
        var episodes = new[] { Episode(1, 1), Episode(1, 2), Episode(2, 1), Episode(2, 2) };
        // Skipped season 1 episode 2; the current episode is halfway watched.
        var rows = new[] { Row(episodes[0], true), Row(episodes[2], false, 50) }.ToLookup(row => row.ItemId);
        var series = TitleProgressCalculator.Calculate(episodes, rows, true);
        var season = TitleProgressCalculator.Calculate(episodes.Skip(2).ToArray(), rows, true);
        var episode = TitleProgressCalculator.Calculate(new[] { episodes[2] }, rows, false);
        Assert.Equal(4, series.TotalEpisodes);
        Assert.Equal(1, series.CompletedEpisodes);
        Assert.Equal(37.5, series.Percent);
        Assert.Equal(2, season.TotalEpisodes);
        Assert.Equal(0, season.CompletedEpisodes);
        Assert.Equal(25, season.Percent);
        Assert.Equal(50, episode.Percent);
        Assert.Equal(2, series.Episode!.SeasonIndexNumber);
        Assert.Equal(1, series.Episode.EpisodeIndexNumber);
        Assert.False(series.Episode.Played);
    }

    [Fact]
    public void SeeingLastNumberedEpisodeDoesNotImplySeeingEarlierEpisodes()
    {
        var episodes = new[] { Episode(1, 1), Episode(1, 2), Episode(1, 3) };
        var rows = new[] { Row(episodes[2], true) }.ToLookup(row => row.ItemId);
        var result = TitleProgressCalculator.Calculate(episodes, rows, true);
        Assert.Equal(1, result.CompletedEpisodes);
        Assert.Equal(100d / 3, result.Percent!.Value, 6);
        Assert.False(result.Completed);
        Assert.True(result.Episode!.Played);
        Assert.Equal(3, result.Episode.EpisodeIndexNumber);
    }

    [Fact]
    public void UnknownRuntimeAndHistoryOnlyStillHaveAnUnambiguousStartedEpisode()
    {
        var episode = Episode(0, 3); episode.RunTimeTicks = null;
        var row = Row(episode, false); row.LastPlayedDate = DateTime.UtcNow;
        var result = TitleProgressCalculator.Calculate(new[] { episode }, new[] { row }.ToLookup(value => value.ItemId), true);
        Assert.True(result.Started);
        Assert.False(result.Completed);
        Assert.Equal(0, result.CompletedEpisodes);
        Assert.Equal(0, result.Percent);
        Assert.Equal(0, result.Episode!.SeasonIndexNumber);
        Assert.False(result.Episode.Played);
    }

    [Fact]
    public void UnplayedResumeAtEndCannotCompleteSeriesAndMissingUserDataMeansZero()
    {
        var episode = Episode(1, 1);
        var result = TitleProgressCalculator.Calculate(new[] { episode }, new[] { Row(episode, false, 150) }.ToLookup(value => value.ItemId), true);
        Assert.False(result.Completed);
        Assert.Equal(0, result.CompletedEpisodes);
        Assert.InRange(result.Percent!.Value, 99, 99.999);
        var untouched = TitleProgressCalculator.Calculate(new[] { episode }, Array.Empty<UserData>().ToLookup(value => value.ItemId), true);
        Assert.Equal(1, untouched.TotalEpisodes);
        Assert.Equal(0, untouched.Percent);
        Assert.Null(untouched.Episode);
    }

    private static Episode Episode(int season, int episode) => new()
    {
        Id = Guid.NewGuid(), ParentIndexNumber = season, IndexNumber = episode, RunTimeTicks = 100
    };

    private static UserData Row(BaseItem episode, bool played, long position = 0) => new()
    {
        ItemId = episode.Id, UserId = Guid.NewGuid(), Played = played, PlaybackPositionTicks = position,
        CustomDataKey = string.Empty, Item = null!, User = null!
    };
}
