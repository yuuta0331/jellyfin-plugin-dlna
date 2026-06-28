using System;
using System.IO;
using System.Text;
using System.Xml;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Didl;
using Jellyfin.Plugin.Dlna.Indexing;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

public class MixedLibraryPlaybackTests
{
    [Fact]
    public void SummaryMovie_IncludesVideoRes_ForMixedLibraryPlaybackPath()
    {
        var didl = WriteSummaryPlaybackDidl(BaseItemKind.Movie);

        Assert.Contains("protocolInfo=\"http-get:*:video/mp4:*\"", didl, StringComparison.Ordinal);
        Assert.Contains("/dlna/videos/", didl, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SummaryMovie_RespectsEnsurePlaybackUrlsSetting(bool ensurePlaybackUrlsInBrowse)
    {
        var didl = WriteSummaryPlaybackDidl(BaseItemKind.Movie, ensurePlaybackUrlsInBrowse);

        if (ensurePlaybackUrlsInBrowse)
        {
            Assert.Contains("protocolInfo=\"http-get:*:video/mp4:*\"", didl, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("protocolInfo=\"http-get:*:video/mp4:*\"", didl, StringComparison.Ordinal);
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

        var summary = new ItemSummaryRecord
        {
            ItemId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ItemType = BaseItemKind.Movie,
            Name = "Mixed Library Movie"
        };

        DlnaPlaybackUrlHelper.WriteSummaryPlaybackResource(
            writer,
            summary,
            "http://server",
            questCompatibilityMode: questMode,
            ensurePlaybackUrlsInBrowse: true);
        writer.Flush();

        var didl = buffer.ToString();
        if (questMode)
        {
            Assert.Contains("http://server/dlna/videos/33333333-3333-3333-3333-333333333333/stream.mp4", didl, StringComparison.Ordinal);
            Assert.DoesNotContain("?", didl, StringComparison.Ordinal);
        }
        else
        {
            Assert.Contains("?dlnaheaders=true", didl, StringComparison.Ordinal);
        }
    }

    private static string WriteSummaryPlaybackDidl(BaseItemKind itemType, bool ensurePlaybackUrlsInBrowse = true)
    {
        using var buffer = new StringWriter();
        var settings = new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Fragment };
        using var writer = XmlWriter.Create(buffer, settings);

        var summary = new ItemSummaryRecord
        {
            ItemId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ItemType = itemType,
            Name = "Test Movie"
        };

        DlnaPlaybackUrlHelper.WriteSummaryPlaybackResource(
            writer,
            summary,
            "http://server",
            questCompatibilityMode: true,
            ensurePlaybackUrlsInBrowse);

        writer.Flush();
        return buffer.ToString();
    }
}
