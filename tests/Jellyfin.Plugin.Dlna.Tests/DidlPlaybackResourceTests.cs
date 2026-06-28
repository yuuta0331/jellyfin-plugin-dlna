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

/// <summary>
/// Regression tests for DLNA playback URLs in browse summaries.
/// </summary>
public class DidlPlaybackResourceTests
{
    [Fact]
    public void NormalizeDlnaMediaUrl_QuestMode_StripsQuery()
    {
        var url = "http://server/dlna/videos/abc/stream.mp4?Static=true&DeviceProfileId=xyz";

        var result = DlnaPlaybackUrlHelper.NormalizeDlnaMediaUrl(url, questCompatibilityMode: true);

        Assert.Equal("http://server/dlna/videos/abc/stream.mp4", result);
    }

    [Fact]
    public void NormalizeDlnaMediaUrl_NonQuestBareUrl_AddsQuestionMarkBeforeDlnaHeaders()
    {
        var url = "http://server/dlna/videos/abc/stream.mp4";

        var result = DlnaPlaybackUrlHelper.NormalizeDlnaMediaUrl(url, questCompatibilityMode: false);

        Assert.Equal("http://server/dlna/videos/abc/stream.mp4?dlnaheaders=true", result);
    }

    [Fact]
    public void NormalizeDlnaMediaUrl_NonQuest_AddsDlnaHeaders()
    {
        var url = "http://server/dlna/videos/abc/stream.mp4?Static=true";

        var result = DlnaPlaybackUrlHelper.NormalizeDlnaMediaUrl(url, questCompatibilityMode: false);

        Assert.Equal("http://server/dlna/videos/abc/stream.mp4?Static=true&dlnaheaders=true", result);
    }

    [Fact]
    public void BuildLightweightVideoStreamUrl_UsesMp4Container()
    {
        var itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var url = DlnaPlaybackUrlHelper.BuildLightweightVideoStreamUrl("http://server", itemId);

        Assert.Equal("http://server/dlna/videos/11111111-1111-1111-1111-111111111111/stream.mp4", url);
    }

    [Fact]
    public void SummaryMovie_IncludesVideoRes_ExcludesImageRes()
    {
        var didl = WriteSummaryPlaybackDidl(BaseItemKind.Movie);

        Assert.Contains("protocolInfo=\"http-get:*:video/mp4:*\"", didl, StringComparison.Ordinal);
        Assert.Contains("/dlna/videos/", didl, StringComparison.Ordinal);
        Assert.DoesNotContain("image/jpeg", didl, StringComparison.Ordinal);
    }

    [Fact]
    public void SummaryMovie_VideoResBeforeAlbumArt()
    {
        using var buffer = new StringWriter();
        var settings = new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Fragment };
        using var writer = XmlWriter.Create(buffer, settings);

        var summary = new ItemSummaryRecord
        {
            ItemId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ItemType = BaseItemKind.Movie,
            Name = "Test Movie"
        };

        DlnaPlaybackUrlHelper.WriteSummaryPlaybackResource(
            writer,
            summary,
            "http://server",
            questCompatibilityMode: true,
            ensurePlaybackUrlsInBrowse: true);
        writer.WriteElementString("upnp", "albumArtURI", "urn:schemas-upnp-org:metadata-1-0/upnp/", "http://server/Items/image.jpg");
        writer.Flush();

        var didl = buffer.ToString();
        var videoResIndex = didl.IndexOf("protocolInfo=\"http-get:*:video/mp4:*\"", StringComparison.Ordinal);
        var albumArtIndex = didl.IndexOf("albumArtURI", StringComparison.Ordinal);

        Assert.True(videoResIndex < albumArtIndex);
    }

    [Fact]
    public void SummaryMovie_PlaybackDisabled_OmitsVideoRes()
    {
        var didl = WriteSummaryPlaybackDidl(BaseItemKind.Movie, ensurePlaybackUrlsInBrowse: false);

        Assert.DoesNotContain("protocolInfo=\"http-get:*:video/mp4:*\"", didl, StringComparison.Ordinal);
    }

    [Fact]
    public void IsBareDlnaStreamRequest_EmptyQuery_ReturnsTrue()
    {
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Request.Path = "/dlna/videos/abc/stream.mp4";
        var request = new Jellyfin.Plugin.Dlna.Playback.Model.DlnaVideoRequestDto
        {
            Id = Guid.NewGuid()
        };

        Assert.True(Jellyfin.Plugin.Dlna.Playback.StreamingHelpers.IsBareDlnaStreamRequest(context.Request, request));
    }

    [Fact]
    public void IsBareDlnaStreamRequest_WithStaticParam_ReturnsFalse()
    {
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Request.Path = "/dlna/videos/abc/stream.mp4";
        context.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString("?Static=true");
        var request = new Jellyfin.Plugin.Dlna.Playback.Model.DlnaVideoRequestDto
        {
            Id = Guid.NewGuid()
        };

        Assert.False(Jellyfin.Plugin.Dlna.Playback.StreamingHelpers.IsBareDlnaStreamRequest(context.Request, request));
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
            Name = "Test Movie",
            PrimaryImageItemId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            PrimaryImageTag = "tag"
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
