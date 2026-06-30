using System;
using System.IO;
using System.Text;
using System.Xml;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.Didl;
using Jellyfin.Plugin.Dlna.Indexing;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.Playback;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Regression tests for DLNA playback URLs in browse summaries.
/// </summary>
public class DidlPlaybackResourceTests
{
    private static readonly Guid ItemId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void NormalizeDlnaMediaUrl_QuestMode_PreservesDirectPlayParams()
    {
        var url = "http://server/dlna/videos/abc/stream.mkv?Static=true&MediaSourceId=ms1&VideoCodec=h264&DeviceProfileId=xyz";

        var result = DlnaPlaybackUrlHelper.NormalizeDlnaMediaUrl(url, questCompatibilityMode: true);

        Assert.Equal("http://server/dlna/videos/abc/stream.mkv?Static=true&MediaSourceId=ms1", result);
    }

    [Fact]
    public void NormalizeDlnaMediaUrl_QuestMode_StripsTranscodeParamsOnly()
    {
        var url = "http://server/dlna/videos/abc/stream.mkv?Static=true&AudioCodec=aac&Tag=etag";

        var result = DlnaPlaybackUrlHelper.NormalizeDlnaMediaUrl(url, questCompatibilityMode: true);

        Assert.Equal("http://server/dlna/videos/abc/stream.mkv?Static=true&Tag=etag", result);
    }

    [Fact]
    public void NormalizeDlnaMediaUrl_NonQuestBareUrl_AddsQuestionMarkBeforeDlnaHeaders()
    {
        var url = "http://server/dlna/videos/abc/stream.mkv";

        var result = DlnaPlaybackUrlHelper.NormalizeDlnaMediaUrl(url, questCompatibilityMode: false);

        Assert.Equal("http://server/dlna/videos/abc/stream.mkv?dlnaheaders=true", result);
    }

    [Fact]
    public void NormalizeDlnaMediaUrl_NonQuest_AddsDlnaHeaders()
    {
        var url = "http://server/dlna/videos/abc/stream.mkv?Static=true";

        var result = DlnaPlaybackUrlHelper.NormalizeDlnaMediaUrl(url, questCompatibilityMode: false);

        Assert.Equal("http://server/dlna/videos/abc/stream.mkv?Static=true&dlnaheaders=true", result);
    }

    [Fact]
    public void NormalizeDlnaMediaUrl_NonQuest_PreservesTranscodeParameters()
    {
        var url = "http://server/dlna/videos/abc/stream.ts?VideoCodec=h264&AudioCodec=aac&PlaySessionId=session";

        var result = DlnaPlaybackUrlHelper.NormalizeDlnaMediaUrl(url, questCompatibilityMode: false);

        Assert.Equal(url + "&dlnaheaders=true", result);
    }

    [Fact]
    public void BuildDirectPlayVideoStreamUrl_UsesSourceContainer()
    {
        var url = DlnaPlaybackUrlHelper.BuildDirectPlayVideoStreamUrl(
            "http://server",
            ItemId,
            "mkv",
            ItemId.ToString("N"),
            "etag");

        Assert.Equal(
            "http://server/dlna/videos/22222222-2222-2222-2222-222222222222/stream.mkv?Static=true&MediaSourceId=" + ItemId.ToString("N") + "&Tag=etag",
            url);
    }

    [Fact]
    public void SummaryMovie_IncludesRichDirectPlayRes()
    {
        var didl = WriteSummaryPlaybackDidl();

        Assert.Contains("duration=\"", didl, StringComparison.Ordinal);
        Assert.Contains("size=\"1234567890\"", didl, StringComparison.Ordinal);
        Assert.Contains("resolution=\"1920x1080\"", didl, StringComparison.Ordinal);
        Assert.Contains("DLNA.ORG_OP=01", didl, StringComparison.Ordinal);
        Assert.Contains("video/x-matroska", didl, StringComparison.Ordinal);
        Assert.Contains("/dlna/videos/", didl, StringComparison.Ordinal);
        Assert.Contains("stream.mkv?Static=true", didl, StringComparison.Ordinal);
        Assert.DoesNotContain("image/jpeg", didl, StringComparison.Ordinal);
    }

    [Fact]
    public void SummaryVideo_ThreeGp_UsesAndroidCompatibleMimeType()
    {
        var summary = PlaybackTestSupport.CreatePlayableVideoSummary(ItemId, container: "3gp");
        summary.VideoCodec = "h263";
        summary.AudioCodec = "aac";

        using var buffer = new StringWriter();
        using var writer = XmlWriter.Create(
            buffer,
            new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Fragment });
        WriteSummaryPlaybackResource(
            writer,
            summary,
            ensurePlaybackUrlsInBrowse: true,
            DlnaPlaybackMode.Auto,
            questCompatibilityMode: true);
        writer.Flush();

        Assert.Contains("video/3gpp", buffer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void SummaryAudio_Ogg_UsesAndroidCompatibleMimeType()
    {
        var summary = PlaybackTestSupport.CreatePlayableVideoSummary(ItemId, container: "ogg");
        summary.ItemType = BaseItemKind.Audio;
        summary.VideoCodec = null;
        summary.AudioCodec = "opus";

        using var buffer = new StringWriter();
        using var writer = XmlWriter.Create(
            buffer,
            new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Fragment });
        WriteSummaryPlaybackResource(
            writer,
            summary,
            ensurePlaybackUrlsInBrowse: true,
            DlnaPlaybackMode.Auto,
            questCompatibilityMode: true);
        writer.Flush();

        Assert.Contains("audio/ogg", buffer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void SummaryMovie_VideoResBeforeAlbumArt()
    {
        using var buffer = new StringWriter();
        var settings = new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Fragment };
        using var writer = XmlWriter.Create(buffer, settings);

        WriteSummaryPlaybackResource(
            writer,
            PlaybackTestSupport.CreatePlayableVideoSummary(ItemId),
            ensurePlaybackUrlsInBrowse: true,
            DlnaPlaybackMode.Auto,
            questCompatibilityMode: true);
        writer.WriteElementString("upnp", "albumArtURI", "urn:schemas-upnp-org:metadata-1-0/upnp/", "http://server/Items/image.jpg");
        writer.Flush();

        var didl = buffer.ToString();
        var videoResIndex = didl.IndexOf("stream.mkv", StringComparison.Ordinal);
        var albumArtIndex = didl.IndexOf("albumArtURI", StringComparison.Ordinal);

        Assert.True(videoResIndex < albumArtIndex);
    }

    [Fact]
    public void SummaryMovie_PlaybackDisabled_OmitsVideoRes()
    {
        var didl = WriteSummaryPlaybackDidl(ensurePlaybackUrlsInBrowse: false);

        Assert.DoesNotContain("<res", didl, StringComparison.Ordinal);
    }

    [Fact]
    public void SummaryMovie_DirectPlayOnlyWithoutSupport_OmitsVideoRes()
    {
        var didl = WriteSummaryPlaybackDidl(
            playbackMode: DlnaPlaybackMode.DirectPlayOnly,
            supportsDirectPlay: false);

        Assert.DoesNotContain("<res", didl, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(DlnaPlaybackMode.Auto)]
    [InlineData(DlnaPlaybackMode.PreferDirectPlay)]
    public void SummaryMovie_WithoutDirectPlaySupport_UsesNegotiatedStream(DlnaPlaybackMode playbackMode)
    {
        var didl = WriteSummaryPlaybackDidl(
            playbackMode: playbackMode,
            supportsDirectPlay: false);

        Assert.Contains("stream.mp4", didl, StringComparison.Ordinal);
        Assert.DoesNotContain("Static=true", didl, StringComparison.Ordinal);
    }

    [Fact]
    public void IsBareDlnaStreamRequest_EmptyQuery_ReturnsTrue()
    {
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Request.Path = "/dlna/videos/abc/stream.mkv";
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
        context.Request.Path = "/dlna/videos/abc/stream.mkv";
        context.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString("?Static=true");
        var request = new Jellyfin.Plugin.Dlna.Playback.Model.DlnaVideoRequestDto
        {
            Id = Guid.NewGuid()
        };

        Assert.False(Jellyfin.Plugin.Dlna.Playback.StreamingHelpers.IsBareDlnaStreamRequest(context.Request, request));
    }

    [Fact]
    public void IsBareDlnaStreamRequest_ProbeContainerPath_ReturnsTrue()
    {
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Request.Path = "/dlna/videos/abc/stream.mov,mp4,m4a,3gp,3g2,mj2";
        var request = new Jellyfin.Plugin.Dlna.Playback.Model.DlnaVideoRequestDto
        {
            Id = Guid.NewGuid(),
            Container = "mov,mp4,m4a,3gp,3g2,mj2"
        };

        Assert.True(Jellyfin.Plugin.Dlna.Playback.StreamingHelpers.IsBareDlnaStreamRequest(context.Request, request));
    }

    [Fact]
    public void IsDlnaCapabilityProbeRequest_RawMultiExtensionPath_ReturnsTrue()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/dlna/videos/abc/stream.mov,mp4,m4a,3gp,3g2,mj2";

        Assert.True(StreamingHelpers.IsDlnaCapabilityProbeRequest(context.Request));
    }

    [Fact]
    public void EvaluateEarlyPlaybackGuard_NonStaticCapabilityProbe_Returns415WithoutProfileResolution()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/dlna/videos/abc/stream.mov,mp4,m4a,3gp,3g2,mj2";
        var request = new Jellyfin.Plugin.Dlna.Playback.Model.DlnaVideoRequestDto
        {
            Id = Guid.NewGuid(),
            Container = "mov",
            Static = false
        };

        var result = StreamingHelpers.EvaluateEarlyPlaybackGuard(
            request,
            context.Request,
            null!,
            null!,
            null!,
            null!,
            null!);

        var response = Assert.IsType<ObjectResult>(result.EarlyResponse);
        Assert.Equal(StatusCodes.Status415UnsupportedMediaType, response.StatusCode);
        Assert.False(result.RequiresStateRefresh);
    }

    private static string WriteSummaryPlaybackDidl(
        bool ensurePlaybackUrlsInBrowse = true,
        DlnaPlaybackMode playbackMode = DlnaPlaybackMode.Auto,
        bool supportsDirectPlay = true)
    {
        using var buffer = new StringWriter();
        var settings = new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Fragment };
        using var writer = XmlWriter.Create(buffer, settings);

        WriteSummaryPlaybackResource(
            writer,
            PlaybackTestSupport.CreatePlayableVideoSummary(ItemId, supportsDirectPlay: supportsDirectPlay),
            ensurePlaybackUrlsInBrowse,
            playbackMode,
            questCompatibilityMode: true);

        writer.Flush();
        return buffer.ToString();
    }

    private static void WriteSummaryPlaybackResource(
        XmlWriter writer,
        ItemSummaryRecord summary,
        bool ensurePlaybackUrlsInBrowse,
        DlnaPlaybackMode playbackMode,
        bool questCompatibilityMode)
    {
        DlnaPlaybackUrlHelper.WriteSummaryPlaybackResource(
            writer,
            summary,
            PlaybackTestSupport.CreateProfile(),
            playbackMode,
            "http://server",
            questCompatibilityMode,
            ensurePlaybackUrlsInBrowse);
    }
}
