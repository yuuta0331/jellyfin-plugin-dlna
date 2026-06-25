using System;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Jellyfin.Plugin.Dlna.Didl;
using Jellyfin.Plugin.Dlna.Indexing;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for DLNA image selection rules.
/// </summary>
public class DlnaImageResolverTests
{
    private static DlnaPluginConfiguration CreateConfig(
        DlnaImagePresentation virtualList = DlnaImagePresentation.Poster,
        DlnaImagePresentation search = DlnaImagePresentation.Poster,
        EpisodeListImageSource episodeList = EpisodeListImageSource.Episode)
        => new()
        {
            VirtualListImagePresentation = virtualList,
            SearchImagePresentation = search,
            EpisodeListImageSource = episodeList
        };

    [Fact]
    public void ResolveSummary_MovieVirtualList_PosterPrefersPrimary()
    {
        var itemId = Guid.NewGuid();
        var summary = new ItemSummaryRecord
        {
            ItemId = itemId,
            ItemType = BaseItemKind.Movie,
            PrimaryImageItemId = itemId,
            PrimaryImageTag = "primary-tag",
            ThumbImageItemId = itemId,
            ThumbImageTag = "thumb-tag"
        };

        var resolved = DlnaImageResolver.Resolve(summary, DlnaImageBrowseContext.VirtualList, CreateConfig());

        Assert.NotNull(resolved);
        Assert.Equal(ImageType.Primary, resolved!.Type);
        Assert.Equal("primary-tag", resolved.ImageTag);
    }

    [Fact]
    public void ResolveSummary_MovieVirtualList_ThumbnailPrefersThumb()
    {
        var itemId = Guid.NewGuid();
        var summary = new ItemSummaryRecord
        {
            ItemId = itemId,
            ItemType = BaseItemKind.Movie,
            PrimaryImageItemId = itemId,
            PrimaryImageTag = "primary-tag",
            ThumbImageItemId = itemId,
            ThumbImageTag = "thumb-tag"
        };

        var resolved = DlnaImageResolver.Resolve(
            summary,
            DlnaImageBrowseContext.VirtualList,
            CreateConfig(virtualList: DlnaImagePresentation.Thumbnail));

        Assert.NotNull(resolved);
        Assert.Equal(ImageType.Thumb, resolved!.Type);
        Assert.Equal("thumb-tag", resolved.ImageTag);
    }

    [Fact]
    public void ResolveSummary_MovieVirtualList_ThumbnailFallsBackToPrimary()
    {
        var itemId = Guid.NewGuid();
        var summary = new ItemSummaryRecord
        {
            ItemId = itemId,
            ItemType = BaseItemKind.Movie,
            PrimaryImageItemId = itemId,
            PrimaryImageTag = "primary-tag"
        };

        var resolved = DlnaImageResolver.Resolve(
            summary,
            DlnaImageBrowseContext.VirtualList,
            CreateConfig(virtualList: DlnaImagePresentation.Thumbnail));

        Assert.NotNull(resolved);
        Assert.Equal(ImageType.Primary, resolved!.Type);
    }

    [Fact]
    public void ResolveSummary_EpisodeList_AlwaysPrefersThumb()
    {
        var itemId = Guid.NewGuid();
        var summary = new ItemSummaryRecord
        {
            ItemId = itemId,
            ItemType = BaseItemKind.Episode,
            PrimaryImageItemId = itemId,
            PrimaryImageTag = "primary-tag",
            ThumbImageItemId = itemId,
            ThumbImageTag = "thumb-tag"
        };

        var resolved = DlnaImageResolver.Resolve(summary, DlnaImageBrowseContext.EpisodeList, CreateConfig());

        Assert.NotNull(resolved);
        Assert.Equal(ImageType.Thumb, resolved!.Type);
    }

    [Fact]
    public void ResolveSummary_EpisodeList_EpisodeSource_FallsBackToSeriesThumbWhenEpisodeMissing()
    {
        var episodeId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var summary = new ItemSummaryRecord
        {
            ItemId = episodeId,
            ItemType = BaseItemKind.Episode,
            ThumbImageItemId = seriesId,
            ThumbImageTag = "series-thumb-tag"
        };

        var resolved = DlnaImageResolver.Resolve(
            summary,
            DlnaImageBrowseContext.EpisodeList,
            CreateConfig(episodeList: EpisodeListImageSource.Episode));

        Assert.NotNull(resolved);
        Assert.Equal(ImageType.Thumb, resolved!.Type);
        Assert.Equal(seriesId, resolved.ItemId);
        Assert.Equal("series-thumb-tag", resolved.ImageTag);
    }

    [Fact]
    public void ResolveSummary_EpisodeList_EpisodeSource_UsesEpisodeOwnedThumb()
    {
        var episodeId = Guid.NewGuid();
        var summary = new ItemSummaryRecord
        {
            ItemId = episodeId,
            ItemType = BaseItemKind.Episode,
            ThumbImageItemId = episodeId,
            ThumbImageTag = "episode-thumb-tag"
        };

        var resolved = DlnaImageResolver.Resolve(
            summary,
            DlnaImageBrowseContext.EpisodeList,
            CreateConfig(episodeList: EpisodeListImageSource.Episode));

        Assert.NotNull(resolved);
        Assert.Equal(ImageType.Thumb, resolved!.Type);
        Assert.Equal(episodeId, resolved.ItemId);
        Assert.Equal("episode-thumb-tag", resolved.ImageTag);
    }

    [Fact]
    public void ResolveSummary_EpisodeList_SeriesSource_PrefersSeriesThumbOverEpisodeThumb()
    {
        var episodeId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var summary = new ItemSummaryRecord
        {
            ItemId = episodeId,
            ItemType = BaseItemKind.Episode,
            ThumbImageItemId = episodeId,
            ThumbImageTag = "episode-thumb-tag",
            PrimaryImageItemId = seriesId,
            PrimaryImageTag = "series-primary-tag"
        };

        var resolved = DlnaImageResolver.Resolve(
            summary,
            DlnaImageBrowseContext.EpisodeList,
            CreateConfig(episodeList: EpisodeListImageSource.Series));

        Assert.NotNull(resolved);
        Assert.Equal(seriesId, resolved!.ItemId);
        Assert.Equal("series-primary-tag", resolved.ImageTag);
    }

    [Fact]
    public void ResolveSummary_EpisodeList_SeriesSource_UsesSeriesFallbackThumb()
    {
        var episodeId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var summary = new ItemSummaryRecord
        {
            ItemId = episodeId,
            ItemType = BaseItemKind.Episode,
            ThumbImageItemId = seriesId,
            ThumbImageTag = "series-thumb-tag"
        };

        var resolved = DlnaImageResolver.Resolve(
            summary,
            DlnaImageBrowseContext.EpisodeList,
            CreateConfig(episodeList: EpisodeListImageSource.Series));

        Assert.NotNull(resolved);
        Assert.Equal(ImageType.Thumb, resolved!.Type);
        Assert.Equal(seriesId, resolved.ItemId);
        Assert.Equal("series-thumb-tag", resolved.ImageTag);
    }

    [Fact]
    public void ResolveSummary_SeasonList_AlwaysPrefersPrimary()
    {
        var itemId = Guid.NewGuid();
        var summary = new ItemSummaryRecord
        {
            ItemId = itemId,
            ItemType = BaseItemKind.Season,
            IsFolder = true,
            PrimaryImageItemId = itemId,
            PrimaryImageTag = "primary-tag",
            ThumbImageItemId = itemId,
            ThumbImageTag = "thumb-tag"
        };

        var resolved = DlnaImageResolver.Resolve(summary, DlnaImageBrowseContext.SeasonList, CreateConfig());

        Assert.NotNull(resolved);
        Assert.Equal(ImageType.Primary, resolved!.Type);
    }

    [Fact]
    public void ResolveSummary_Search_UsesSearchPresentationSetting()
    {
        var itemId = Guid.NewGuid();
        var summary = new ItemSummaryRecord
        {
            ItemId = itemId,
            ItemType = BaseItemKind.Series,
            PrimaryImageItemId = itemId,
            PrimaryImageTag = "primary-tag",
            ThumbImageItemId = itemId,
            ThumbImageTag = "thumb-tag"
        };

        var poster = DlnaImageResolver.Resolve(summary, DlnaImageBrowseContext.Search, CreateConfig(search: DlnaImagePresentation.Poster));
        var thumb = DlnaImageResolver.Resolve(summary, DlnaImageBrowseContext.Search, CreateConfig(search: DlnaImagePresentation.Thumbnail));

        Assert.Equal(ImageType.Primary, poster!.Type);
        Assert.Equal(ImageType.Thumb, thumb!.Type);
    }

    [Fact]
    public void ResolveSummary_EpisodeInSearch_StillUsesThumb()
    {
        var itemId = Guid.NewGuid();
        var summary = new ItemSummaryRecord
        {
            ItemId = itemId,
            ItemType = BaseItemKind.Episode,
            PrimaryImageItemId = itemId,
            PrimaryImageTag = "primary-tag",
            ThumbImageItemId = itemId,
            ThumbImageTag = "thumb-tag"
        };

        var resolved = DlnaImageResolver.Resolve(
            summary,
            DlnaImageBrowseContext.Search,
            CreateConfig(search: DlnaImagePresentation.Poster));

        Assert.NotNull(resolved);
        Assert.Equal(ImageType.Thumb, resolved!.Type);
    }

    [Theory]
    [InlineData(StubType.Movies, DlnaImageBrowseContext.VirtualList)]
    [InlineData(StubType.Series, DlnaImageBrowseContext.VirtualList)]
    [InlineData(StubType.RecentlyAddedEpisodes, DlnaImageBrowseContext.EpisodeList)]
    [InlineData(StubType.RecentlyReleasedEpisodes, DlnaImageBrowseContext.EpisodeList)]
    [InlineData(StubType.FavoriteEpisodes, DlnaImageBrowseContext.EpisodeList)]
    [InlineData(StubType.RecentlyAddedMovies, DlnaImageBrowseContext.VirtualList)]
    public void BrowseContextMapper_MapsStubTypes(StubType stub, DlnaImageBrowseContext expected)
    {
        Assert.Equal(expected, DlnaImageBrowseContextMapper.FromParent(stub, null));
    }
}
