using System;
using System.Linq;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Lightweight direct-play eligibility checks against device profile DirectPlayProfiles.
/// Used during indexing to avoid per-item StreamBuilder invocations.
/// </summary>
public static class DlnaDirectPlayHeuristic
{
    /// <summary>
    /// Returns true when the media source likely matches a direct-play profile entry.
    /// </summary>
    public static bool SupportsDirectPlay(
        DlnaDeviceProfile profile,
        MediaSourceInfo mediaSource,
        DlnaProfileType mediaType)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(mediaSource);

        return profile.DirectPlayProfiles
            .Where(p => p.Type == mediaType)
            .Any(p => IsSupported(p, mediaSource, mediaType));
    }

    private static bool IsSupported(DirectPlayProfile profile, MediaSourceInfo source, DlnaProfileType mediaType)
    {
        if (!MatchesContainer(profile, source))
        {
            return false;
        }

        if (mediaType == DlnaProfileType.Video)
        {
            var videoStream = source.VideoStream;
            if (!string.IsNullOrWhiteSpace(profile.VideoCodec))
            {
                if (videoStream is null || !profile.SupportsVideoCodec(videoStream.Codec))
                {
                    return false;
                }
            }

            var audioStream = source.GetDefaultAudioStream(null);
            if (!string.IsNullOrWhiteSpace(profile.AudioCodec))
            {
                if (audioStream is null || !profile.SupportsAudioCodec(audioStream.Codec))
                {
                    return false;
                }
            }
        }
        else if (mediaType == DlnaProfileType.Audio)
        {
            var audioStream = source.GetDefaultAudioStream(null);
            if (!string.IsNullOrWhiteSpace(profile.AudioCodec))
            {
                if (audioStream is null || !profile.SupportsAudioCodec(audioStream.Codec))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool MatchesContainer(DirectPlayProfile profile, MediaSourceInfo source)
    {
        if (string.IsNullOrWhiteSpace(profile.Container))
        {
            return true;
        }

        var containers = SplitValues(source.Container);
        if (containers.Length == 0 && !string.IsNullOrWhiteSpace(source.Path))
        {
            var extension = System.IO.Path.GetExtension(source.Path);
            if (!string.IsNullOrWhiteSpace(extension))
            {
                containers = [extension.TrimStart('.')];
            }
        }

        return containers.Length > 0 && containers.Any(profile.SupportsContainer);
    }

    private static string[] SplitValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => v.TrimStart('.'))
            .Where(v => v.Length > 0)
            .ToArray();
    }
}
