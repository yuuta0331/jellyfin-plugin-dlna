using System.IO;
using System.Xml.Serialization;
using Jellyfin.Plugin.Dlna;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.Profiles;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for <see cref="DlnaDirectPlayHeuristic"/>.
/// </summary>
public class DlnaDirectPlayHeuristicTests
{
    [Fact]
    public void SupportsDirectPlay_Mp4HevcAac_MatchesModernAndroidProfile()
    {
        var profile = CreateModernAndroidStyleProfile();
        var source = CreateVideoSource("mp4", "hevc", "aac");

        Assert.True(DlnaDirectPlayHeuristic.SupportsDirectPlay(profile, source, DlnaProfileType.Video));
    }

    [Fact]
    public void SupportsDirectPlay_MkvHevcAac_MatchesModernAndroidProfile()
    {
        var profile = CreateModernAndroidStyleProfile();
        var source = CreateVideoSource("mkv", "hevc", "aac");

        Assert.True(DlnaDirectPlayHeuristic.SupportsDirectPlay(profile, source, DlnaProfileType.Video));
    }

    [Fact]
    public void SupportsDirectPlay_UnsupportedCodec_ReturnsFalse()
    {
        var profile = CreateModernAndroidStyleProfile();
        var source = CreateVideoSource("avi", "mpeg4", "mp3");

        Assert.False(DlnaDirectPlayHeuristic.SupportsDirectPlay(profile, source, DlnaProfileType.Video));
    }

    [Fact]
    public void SupportsDirectPlay_DefaultProfile_AllowsAnyContainer()
    {
        var profile = new DefaultProfile();
        var source = CreateVideoSource("avi", "mpeg4", "mp3");

        Assert.True(DlnaDirectPlayHeuristic.SupportsDirectPlay(profile, source, DlnaProfileType.Video));
    }

    [Fact]
    public void SupportsDirectPlay_WebmVp8Opus_MatchesModernAndroidProfile()
    {
        var profile = CreateModernAndroidStyleProfile();
        var source = CreateVideoSource("webm", "vp8", "opus");

        Assert.True(DlnaDirectPlayHeuristic.SupportsDirectPlay(profile, source, DlnaProfileType.Video));
    }

    [Fact]
    public void SupportsDirectPlay_WebmVp9Vorbis_MatchesModernAndroidProfile()
    {
        var profile = CreateModernAndroidStyleProfile();
        var source = CreateVideoSource("webm", "vp9", "vorbis");

        Assert.True(DlnaDirectPlayHeuristic.SupportsDirectPlay(profile, source, DlnaProfileType.Video));
    }

    [Fact]
    public void SupportsDirectPlay_WebmH264Aac_IsRejectedByModernAndroidProfile()
    {
        var profile = CreateModernAndroidStyleProfile();
        var source = CreateVideoSource("webm", "h264", "aac");

        Assert.False(DlnaDirectPlayHeuristic.SupportsDirectPlay(profile, source, DlnaProfileType.Video));
    }

    [Theory]
    [InlineData("mp4", "h263", "aac")]
    [InlineData("mp4", "mpeg4", "aac")]
    [InlineData("mp4", "vp9", "opus")]
    [InlineData("mp4", "av1", "flac")]
    [InlineData("mp4", "apv", "aac")]
    [InlineData("3gp", "h263", "amrnb")]
    [InlineData("3g2", "h264", "aac")]
    [InlineData("mkv", "h263", "flac")]
    [InlineData("mkv", "vp8", "opus")]
    [InlineData("ts", "h264", "aac")]
    public void SupportsDirectPlay_AndroidVideoCombinations_ReturnTrue(
        string container,
        string videoCodec,
        string audioCodec)
    {
        var profile = CreateModernAndroidStyleProfile();
        var source = CreateVideoSource(container, videoCodec, audioCodec);

        Assert.True(DlnaDirectPlayHeuristic.SupportsDirectPlay(profile, source, DlnaProfileType.Video));
    }

    [Theory]
    [InlineData("3gp", "hevc", "aac")]
    [InlineData("webm", "vp8", "aac")]
    public void SupportsDirectPlay_InvalidAndroidVideoCombinations_ReturnFalse(
        string container,
        string videoCodec,
        string audioCodec)
    {
        var profile = CreateModernAndroidStyleProfile();
        var source = CreateVideoSource(container, videoCodec, audioCodec);

        Assert.False(DlnaDirectPlayHeuristic.SupportsDirectPlay(profile, source, DlnaProfileType.Video));
    }

    [Theory]
    [InlineData("mp3", "mp3")]
    [InlineData("aac", "aac")]
    [InlineData("m4a", "aac")]
    [InlineData("mp4", "opus")]
    [InlineData("flac", "flac")]
    [InlineData("ogg", "vorbis")]
    [InlineData("ogg", "opus")]
    [InlineData("ogg", "flac")]
    [InlineData("wav", "pcm_s16le")]
    [InlineData("amr", "amrnb")]
    public void SupportsDirectPlay_AndroidAudioCombinations_ReturnTrue(string container, string audioCodec)
    {
        var profile = CreateModernAndroidStyleProfile();
        var source = CreateAudioSource(container, audioCodec);

        Assert.True(DlnaDirectPlayHeuristic.SupportsDirectPlay(profile, source, DlnaProfileType.Audio));
    }

    [Theory]
    [InlineData("wav", "aac")]
    [InlineData("amr", "flac")]
    public void SupportsDirectPlay_InvalidAndroidAudioCombinations_ReturnFalse(string container, string audioCodec)
    {
        var profile = CreateModernAndroidStyleProfile();
        var source = CreateAudioSource(container, audioCodec);

        Assert.False(DlnaDirectPlayHeuristic.SupportsDirectPlay(profile, source, DlnaProfileType.Audio));
    }

    [Theory]
    [InlineData("jpeg")]
    [InlineData("png")]
    [InlineData("bmp")]
    [InlineData("gif")]
    [InlineData("webp")]
    [InlineData("heic")]
    [InlineData("avif")]
    public void SupportsDirectPlay_AndroidPhotoContainers_ReturnTrue(string container)
    {
        var profile = CreateModernAndroidStyleProfile();
        var source = new MediaSourceInfo { Container = container };

        Assert.True(DlnaDirectPlayHeuristic.SupportsDirectPlay(profile, source, DlnaProfileType.Photo));
    }

    [Fact]
    public void SupportsDirectPlay_RequiredVideoCodecWithoutVideoStream_ReturnsFalse()
    {
        var profile = CreateModernAndroidStyleProfile();
        var source = new MediaSourceInfo
        {
            Container = "mp4",
            MediaStreams =
            [
                new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Codec = "aac",
                    IsDefault = true
                }
            ]
        };

        Assert.False(DlnaDirectPlayHeuristic.SupportsDirectPlay(profile, source, DlnaProfileType.Video));
    }

    private static DlnaDeviceProfile CreateModernAndroidStyleProfile()
    {
        const string ResourceName = "Jellyfin.Plugin.Dlna.Profiles.Xml.Modern Android.xml";
        using var stream = typeof(DlnaManager).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidDataException($"Embedded profile not found: {ResourceName}");
        var serializer = new XmlSerializer(typeof(DlnaDeviceProfile));
        return (DlnaDeviceProfile)serializer.Deserialize(stream)!;
    }

    private static MediaSourceInfo CreateVideoSource(string container, string videoCodec, string audioCodec)
    {
        return new MediaSourceInfo
        {
            Container = container,
            MediaStreams =
            [
                new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Codec = videoCodec,
                    IsDefault = true
                },
                new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Codec = audioCodec,
                    IsDefault = true
                }
            ]
        };
    }

    private static MediaSourceInfo CreateAudioSource(string container, string audioCodec)
    {
        return new MediaSourceInfo
        {
            Container = container,
            MediaStreams =
            [
                new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Codec = audioCodec,
                    IsDefault = true
                }
            ]
        };
    }
}
