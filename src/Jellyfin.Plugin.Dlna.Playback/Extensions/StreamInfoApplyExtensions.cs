using System;
using System.Globalization;
using System.Linq;
using Jellyfin.Plugin.Dlna.Playback.Model;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dlna;
using StreamInfo = MediaBrowser.Model.Dlna.StreamInfo;

namespace Jellyfin.Plugin.Dlna.Playback.Extensions;

/// <summary>
/// Applies <see cref="StreamInfo"/> values to DLNA streaming requests.
/// </summary>
public static class StreamInfoApplyExtensions
{
    /// <summary>
    /// Copies optimal stream values from <see cref="StreamInfo"/> into a streaming request.
    /// </summary>
    public static void ApplyToRequest(this StreamInfo streamInfo, StreamingRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(streamInfo);
        ArgumentNullException.ThrowIfNull(request);

        if (request is DlnaVideoRequestDto videoRequest)
        {
            videoRequest.DeviceProfileId = streamInfo.DeviceProfileId;
        }
        else if (request is DlnaStreamingRequestDto streamingRequest)
        {
            streamingRequest.DeviceProfileId = streamInfo.DeviceProfileId;
        }

        request.DeviceId = streamInfo.DeviceId;
        request.MediaSourceId = streamInfo.MediaSourceId;
        request.Static = streamInfo.IsDirectStream;
        request.AudioStreamIndex = streamInfo.AudioStreamIndex;
        request.SubtitleStreamIndex = streamInfo.SubtitleStreamIndex;
        request.AudioSampleRate = streamInfo.AudioSampleRate;
        request.MaxAudioChannels = streamInfo.TranscodingMaxAudioChannels;
        request.StartTimeTicks = streamInfo.StartPositionTicks;
        request.PlaySessionId = streamInfo.PlaySessionId;
        request.LiveStreamId = streamInfo.MediaSource?.LiveStreamId;
        request.Tag = streamInfo.MediaSource?.ETag;
        request.Container = streamInfo.Container;

        if (request is VideoRequestDto video)
        {
            video.VideoCodec = streamInfo.TargetVideoCodec.FirstOrDefault();
            video.AudioCodec = streamInfo.TargetAudioCodec.FirstOrDefault();
            video.VideoBitRate = streamInfo.VideoBitrate;
            video.AudioBitRate = streamInfo.AudioBitrate;
            video.MaxWidth = streamInfo.MaxWidth;
            video.MaxHeight = streamInfo.MaxHeight;
            video.MaxFramerate = streamInfo.MaxFramerate;
            video.RequireAvc = streamInfo.RequireAvc;
            video.RequireNonAnamorphic = streamInfo.RequireNonAnamorphic;
            video.CopyTimestamps = streamInfo.CopyTimestamps;
            video.EnableMpegtsM2TsMode = streamInfo.EnableMpegtsM2TsMode;
            video.SubtitleMethod = streamInfo.SubtitleDeliveryMethod;
            video.TranscodeReasons = streamInfo.TranscodeReasons.ToString();
            video.SubtitleCodec = streamInfo.SubtitleCodecs.Count == 0
                ? null
                : string.Join(',', streamInfo.SubtitleCodecs);

            if (request is DlnaVideoRequestDto dlnaVideo)
            {
                dlnaVideo.EnableSubtitlesInManifest = streamInfo.EnableSubtitlesInManifest;
            }
        }
        else
        {
            request.AudioCodec = streamInfo.TargetAudioCodec.FirstOrDefault();
        }
    }
}
