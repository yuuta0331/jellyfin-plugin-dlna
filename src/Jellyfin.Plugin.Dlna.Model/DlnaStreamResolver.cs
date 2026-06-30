using System;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MediaOptions = MediaBrowser.Model.Dlna.MediaOptions;
using StreamBuilder = MediaBrowser.Model.Dlna.StreamBuilder;
using StreamInfo = MediaBrowser.Model.Dlna.StreamInfo;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Resolves optimal DLNA streams with playback-mode aware MediaOptions.
/// </summary>
public static class DlnaStreamResolver
{
    /// <summary>
    /// Resolves the optimal video stream for the given playback mode.
    /// </summary>
    public static DlnaResolvedStream? ResolveVideo(
        IMediaEncoder mediaEncoder,
        DlnaDeviceProfile profile,
        MediaSourceInfo[] sources,
        Guid itemId,
        DlnaPlaybackMode playbackMode,
        string? deviceId = null,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(mediaEncoder);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(sources);

        var effectiveProfile = ApplyPlaybackModeToProfile(profile, playbackMode);
        var streamInfo = CreateBuilder(mediaEncoder, logger).GetOptimalVideoStream(
            BuildMediaOptions(effectiveProfile, sources, itemId, playbackMode, deviceId));

        return Finalize(streamInfo, playbackMode);
    }

    /// <summary>
    /// Resolves the optimal audio stream for the given playback mode.
    /// </summary>
    public static DlnaResolvedStream? ResolveAudio(
        IMediaEncoder mediaEncoder,
        DlnaDeviceProfile profile,
        MediaSourceInfo[] sources,
        Guid itemId,
        DlnaPlaybackMode playbackMode,
        string? deviceId = null,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(mediaEncoder);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(sources);

        var effectiveProfile = ApplyPlaybackModeToProfile(profile, playbackMode);
        var streamInfo = CreateBuilder(mediaEncoder, logger).GetOptimalAudioStream(
            BuildMediaOptions(effectiveProfile, sources, itemId, playbackMode, deviceId));

        return Finalize(streamInfo, playbackMode);
    }

    /// <summary>
    /// Returns true when the item can be direct-played for the given profile and mode.
    /// </summary>
    public static bool SupportsDirectPlay(
        IMediaEncoder mediaEncoder,
        DlnaDeviceProfile profile,
        MediaSourceInfo[] sources,
        Guid itemId,
        DlnaPlaybackMode playbackMode,
        ILogger? logger = null)
        => ResolveVideo(mediaEncoder, profile, sources, itemId, playbackMode, null, logger)?.IsDirectPlay == true;

    private static StreamBuilder CreateBuilder(IMediaEncoder mediaEncoder, ILogger? logger)
        => new(mediaEncoder, logger ?? NullLogger.Instance);

    private static MediaOptions BuildMediaOptions(
        DlnaDeviceProfile profile,
        MediaSourceInfo[] sources,
        Guid itemId,
        DlnaPlaybackMode playbackMode,
        string? deviceId)
    {
        var options = new MediaOptions
        {
            ItemId = itemId,
            MediaSources = sources,
            Profile = profile,
            DeviceId = deviceId,
            MaxBitrate = profile.MaxStreamingBitrate
        };

        switch (playbackMode)
        {
            case DlnaPlaybackMode.PreferDirectPlay:
                options.EnableDirectPlay = true;
                options.EnableDirectStream = false;
                break;
            case DlnaPlaybackMode.DirectPlayOnly:
                options.EnableDirectPlay = true;
                options.EnableDirectStream = false;
                options.ForceDirectPlay = true;
                break;
        }

        return options;
    }

    private static DlnaDeviceProfile ApplyPlaybackModeToProfile(DlnaDeviceProfile profile, DlnaPlaybackMode playbackMode)
        => playbackMode == DlnaPlaybackMode.DirectPlayOnly
            ? DlnaDeviceProfileCloner.WithoutTranscoding(profile)
            : profile;

    private static DlnaResolvedStream? Finalize(StreamInfo? streamInfo, DlnaPlaybackMode playbackMode)
    {
        if (streamInfo is null)
        {
            return null;
        }

        if (playbackMode == DlnaPlaybackMode.DirectPlayOnly && streamInfo.PlayMethod != PlayMethod.DirectPlay)
        {
            return null;
        }

        return new DlnaResolvedStream(streamInfo);
    }
}
