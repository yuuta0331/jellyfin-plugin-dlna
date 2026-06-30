using System;
using System.IO;
using System.Xml;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Jellyfin.Plugin.Dlna.Didl;
using Jellyfin.Plugin.Dlna.Indexing;
using Jellyfin.Plugin.Dlna.Model;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Playback URL and parent container ID matrix for virtual-folder browse paths.
/// </summary>
public class VirtualFolderPlaybackMatrixTests
{
    private static readonly Guid LibraryId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid ItemId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Theory]
    [InlineData(BaseItemKind.Movie, "video/x-matroska", "/dlna/videos/")]
    [InlineData(BaseItemKind.Episode, "video/x-matroska", "/dlna/videos/")]
    [InlineData(BaseItemKind.Video, "video/x-matroska", "/dlna/videos/")]
    [InlineData(BaseItemKind.MusicVideo, "video/x-matroska", "/dlna/videos/")]
    [InlineData(BaseItemKind.Audio, "audio/mpeg", "/dlna/audio/")]
    public void PlayableSummaryItem_IncludesStreamRes(BaseItemKind itemType, string protocolFragment, string urlFragment)
    {
        Assert.True(DlnaPlaybackUrlHelper.IsPlayableSummaryItem(itemType));

        var didl = WriteSummaryPlaybackDidl(itemType);

        Assert.Contains(protocolFragment, didl, StringComparison.Ordinal);
        Assert.Contains(urlFragment, didl, StringComparison.Ordinal);
        Assert.Contains("Static=true", didl, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(StubType.Movies)]
    [InlineData(StubType.Series)]
    [InlineData(StubType.RecentlyAddedMovies)]
    [InlineData(StubType.RecentlyAddedEpisodes)]
    [InlineData(StubType.MusicVideos)]
    [InlineData(StubType.Videos)]
    public void GetClientId_WithStub_PrefixesStubName(StubType stubType)
    {
        var clientId = DidlBuilder.GetClientId(LibraryId, stubType);

        Assert.StartsWith(stubType.ToString().ToLowerInvariant() + "_", clientId, StringComparison.Ordinal);
        Assert.EndsWith(LibraryId.ToString("N"), clientId, StringComparison.Ordinal);
    }

    [Fact]
    public void MixedLibraryMoviesBrowse_ParentClientId_UsesMoviesStubPrefix()
    {
        var moviesContainerId = DidlBuilder.GetClientId(LibraryId, StubType.Movies);
        var libraryRootId = DidlBuilder.GetClientId(LibraryId, null);

        Assert.Equal("movies_" + LibraryId.ToString("N"), moviesContainerId);
        Assert.Equal(LibraryId.ToString("N"), libraryRootId);
        Assert.NotEqual(moviesContainerId, libraryRootId);
    }

    [Fact]
    public void BrowseConfigFingerprint_ChangesWhenSchemaVersionChanges()
    {
        var config = new DlnaPluginConfiguration();
        var fingerprint = BrowseConfigFingerprint.Compute(config);

        Assert.NotEqual(0, fingerprint);
        Assert.Equal(BrowseConfigFingerprint.BrowseCacheSchemaVersion, 2);
    }

    [Theory]
    [InlineData(BaseItemKind.Series)]
    [InlineData(BaseItemKind.Season)]
    [InlineData(BaseItemKind.Photo)]
    [InlineData(BaseItemKind.MusicAlbum)]
    public void NonPlayableFolderTypes_AreExcludedFromSummaryPlayback(BaseItemKind itemType)
    {
        Assert.False(DlnaPlaybackUrlHelper.IsPlayableSummaryItem(itemType));

        var didl = WriteSummaryPlaybackDidl(itemType);

        Assert.DoesNotContain("stream.mkv", didl, StringComparison.Ordinal);
        Assert.DoesNotContain("stream.mp3", didl, StringComparison.Ordinal);
    }

    [Fact]
    public void SummaryMovie_NonQuestPlaybackUrl_UsesQuestionMarkBeforeDlnaHeaders()
    {
        var didl = WriteSummaryPlaybackDidl(BaseItemKind.Movie, questMode: false);

        Assert.Contains("dlnaheaders=true", didl, StringComparison.Ordinal);
        Assert.DoesNotContain("VideoCodec=", didl, StringComparison.Ordinal);
    }

    private static string WriteSummaryPlaybackDidl(BaseItemKind itemType, bool questMode = true)
    {
        using var buffer = new StringWriter();
        var settings = new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Fragment };
        using var writer = XmlWriter.Create(buffer, settings);

        var summary = PlaybackTestSupport.CreatePlayableVideoSummary(ItemId);
        summary.ItemType = itemType;
        if (itemType == BaseItemKind.Audio)
        {
            summary.Container = "mp3";
            summary.VideoCodec = null;
            summary.VideoWidth = null;
            summary.VideoHeight = null;
        }

        DlnaPlaybackUrlHelper.WriteSummaryPlaybackResource(
            writer,
            summary,
            PlaybackTestSupport.CreateProfile(),
            DlnaPlaybackMode.Auto,
            "http://server",
            questCompatibilityMode: questMode,
            ensurePlaybackUrlsInBrowse: true);

        writer.Flush();
        return buffer.ToString();
    }
}
