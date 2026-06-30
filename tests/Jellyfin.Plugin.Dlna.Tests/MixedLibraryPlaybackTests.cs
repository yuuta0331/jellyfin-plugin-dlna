using System;
using System.IO;
using System.Text;
using System.Xml;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Jellyfin.Plugin.Dlna.Didl;
using Jellyfin.Plugin.Dlna.Indexing;
using Jellyfin.Plugin.Dlna.Model;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

public class MixedLibraryPlaybackTests
{
    private static readonly Guid ItemId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void SummaryMovie_IncludesVideoRes_ForMixedLibraryPlaybackPath()
    {
        var didl = WriteSummaryPlaybackDidl(BaseItemKind.Movie);

        Assert.Contains("video/x-matroska", didl, StringComparison.Ordinal);
        Assert.Contains("/dlna/videos/", didl, StringComparison.Ordinal);
        Assert.Contains("stream.mkv?Static=true", didl, StringComparison.Ordinal);
    }

    [Fact]
    public void MixedLibraryMoviesBrowse_ParentIdUsesMoviesStub_NotLibraryRoot()
    {
        var libraryId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var moviesParentId = DidlBuilder.GetClientId(libraryId, StubType.Movies);
        var libraryRootId = DidlBuilder.GetClientId(libraryId, null);

        Assert.Equal("movies_" + libraryId.ToString("N"), moviesParentId);
        Assert.NotEqual(libraryRootId, moviesParentId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SummaryMovie_RespectsEnsurePlaybackUrlsSetting(bool ensurePlaybackUrlsInBrowse)
    {
        var didl = WriteSummaryPlaybackDidl(BaseItemKind.Movie, ensurePlaybackUrlsInBrowse);

        if (ensurePlaybackUrlsInBrowse)
        {
            Assert.Contains("stream.mkv", didl, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("<res", didl, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SummaryMovie_NormalizesPlaybackUrlForQuestMode(bool questMode)
    {
        using var buffer = new StringWriter();
        var settings = new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Fragment };
        using var writer = XmlWriter.Create(buffer, settings);

        var summary = PlaybackTestSupport.CreatePlayableVideoSummary(
            Guid.Parse("33333333-3333-3333-3333-333333333333"));

        DlnaPlaybackUrlHelper.WriteSummaryPlaybackResource(
            writer,
            summary,
            PlaybackTestSupport.CreateProfile(),
            DlnaPlaybackMode.Auto,
            "http://server",
            questMode,
            ensurePlaybackUrlsInBrowse: true);
        writer.Flush();

        var didl = buffer.ToString();
        if (questMode)
        {
            Assert.Contains("http://server/dlna/videos/33333333-3333-3333-3333-333333333333/stream.mkv?Static=true", didl, StringComparison.Ordinal);
            Assert.DoesNotContain("VideoCodec=", didl, StringComparison.Ordinal);
        }
        else
        {
            Assert.Contains("dlnaheaders=true", didl, StringComparison.Ordinal);
        }
    }

    private static string WriteSummaryPlaybackDidl(BaseItemKind itemType, bool ensurePlaybackUrlsInBrowse = true)
    {
        using var buffer = new StringWriter();
        var settings = new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Fragment };
        using var writer = XmlWriter.Create(buffer, settings);

        var summary = PlaybackTestSupport.CreatePlayableVideoSummary(ItemId);
        summary.ItemType = itemType;

        DlnaPlaybackUrlHelper.WriteSummaryPlaybackResource(
            writer,
            summary,
            PlaybackTestSupport.CreateProfile(),
            DlnaPlaybackMode.Auto,
            "http://server",
            questCompatibilityMode: true,
            ensurePlaybackUrlsInBrowse);

        writer.Flush();
        return buffer.ToString();
    }
}
