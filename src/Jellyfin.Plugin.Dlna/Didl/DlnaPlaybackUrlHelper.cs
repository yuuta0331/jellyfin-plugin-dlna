using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Indexing;
using Jellyfin.Plugin.Dlna.Model;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.MediaInfo;

namespace Jellyfin.Plugin.Dlna.Didl;

/// <summary>
/// Builds DLNA playback URLs and DIDL <c>res</c> elements for indexed browse summaries.
/// </summary>
public static class DlnaPlaybackUrlHelper
{
    private const string NsDidl = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";

    private static readonly HashSet<string> PreservedQueryParams = new(StringComparer.OrdinalIgnoreCase)
    {
        "Static",
        "MediaSourceId",
        "Tag",
        "dlnaheaders"
    };

    /// <summary>
    /// Gets whether browse summaries should include playback URLs.
    /// </summary>
    public static bool ShouldEnsurePlaybackUrlsInBrowse(DlnaPluginConfiguration? config)
        => config?.EnsurePlaybackUrlsInBrowse != false;

    /// <summary>
    /// Normalizes a DLNA media URL for the active compatibility mode.
    /// </summary>
    public static string NormalizeDlnaMediaUrl(string url, bool questCompatibilityMode)
    {
        if (!questCompatibilityMode)
        {
            if (url.Contains("dlnaheaders=", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            return url.Contains('?', StringComparison.Ordinal)
                ? url + "&dlnaheaders=true"
                : url + "?dlnaheaders=true";
        }

        var queryIndex = url.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex < 0)
        {
            return url;
        }

        var basePath = url[..queryIndex];
        var query = url[(queryIndex + 1)..];
        var preserved = new List<string>();

        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var equalsIndex = part.IndexOf('=', StringComparison.Ordinal);
            var key = equalsIndex >= 0 ? part[..equalsIndex] : part;
            if (!PreservedQueryParams.Contains(key))
            {
                continue;
            }

            if (string.Equals(key, "dlnaheaders", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            preserved.Add(part);
        }

        return preserved.Count == 0 ? basePath : basePath + "?" + string.Join('&', preserved);
    }

    /// <summary>
    /// Builds a direct-play video stream URL using the source container.
    /// </summary>
    public static string BuildDirectPlayVideoStreamUrl(
        string serverAddress,
        Guid itemId,
        string? container,
        string? mediaSourceId,
        string? mediaSourceTag)
    {
        var extension = NormalizeContainerExtension(container);
        var baseUrl = serverAddress.TrimEnd('/');
        var url = string.Format(
            CultureInfo.InvariantCulture,
            "{0}/dlna/videos/{1}/stream.{2}?Static=true",
            baseUrl,
            itemId,
            extension);

        if (!string.IsNullOrWhiteSpace(mediaSourceId))
        {
            url += "&MediaSourceId=" + Uri.EscapeDataString(mediaSourceId);
        }

        if (!string.IsNullOrWhiteSpace(mediaSourceTag))
        {
            url += "&Tag=" + Uri.EscapeDataString(mediaSourceTag);
        }

        return url;
    }

    /// <summary>
    /// Builds a direct-play audio stream URL using the source container.
    /// </summary>
    public static string BuildDirectPlayAudioStreamUrl(
        string serverAddress,
        Guid itemId,
        string? container,
        string? mediaSourceId,
        string? mediaSourceTag)
    {
        var extension = NormalizeContainerExtension(container, "mp3");
        var baseUrl = serverAddress.TrimEnd('/');
        var url = string.Format(
            CultureInfo.InvariantCulture,
            "{0}/dlna/audio/{1}/stream.{2}?Static=true",
            baseUrl,
            itemId,
            extension);

        if (!string.IsNullOrWhiteSpace(mediaSourceId))
        {
            url += "&MediaSourceId=" + Uri.EscapeDataString(mediaSourceId);
        }

        if (!string.IsNullOrWhiteSpace(mediaSourceTag))
        {
            url += "&Tag=" + Uri.EscapeDataString(mediaSourceTag);
        }

        return url;
    }

    /// <summary>
    /// Builds a query-free auto-negotiated video stream URL for summary browse output.
    /// </summary>
    public static string BuildLightweightVideoStreamUrl(string serverAddress, Guid itemId)
    {
        var baseUrl = serverAddress.TrimEnd('/');
        return string.Format(CultureInfo.InvariantCulture, "{0}/dlna/videos/{1}/stream.mp4", baseUrl, itemId);
    }

    /// <summary>
    /// Builds a query-free auto-negotiated audio stream URL for summary browse output.
    /// </summary>
    public static string BuildLightweightAudioStreamUrl(string serverAddress, Guid itemId)
    {
        var baseUrl = serverAddress.TrimEnd('/');
        return string.Format(CultureInfo.InvariantCulture, "{0}/dlna/audio/{1}/stream.mp3", baseUrl, itemId);
    }

    /// <summary>
    /// Returns true when the item type is playable media in browse summaries.
    /// </summary>
    public static bool IsPlayableSummaryItem(BaseItemKind itemType)
        => itemType is BaseItemKind.Movie or BaseItemKind.Episode or BaseItemKind.Video or BaseItemKind.Audio or BaseItemKind.MusicVideo;

    /// <summary>
    /// Writes a playback <c>res</c> element for indexed browse summaries.
    /// </summary>
    public static void WriteSummaryPlaybackResource(
        XmlWriter writer,
        ItemSummaryRecord summary,
        DlnaDeviceProfile profile,
        DlnaPlaybackMode playbackMode,
        string serverAddress,
        bool questCompatibilityMode,
        bool ensurePlaybackUrlsInBrowse)
    {
        if (!ensurePlaybackUrlsInBrowse || !IsPlayableSummaryItem(summary.ItemType))
        {
            return;
        }

        if (playbackMode == DlnaPlaybackMode.DirectPlayOnly && !summary.SupportsDirectPlay)
        {
            return;
        }

        if (!summary.SupportsDirectPlay)
        {
            WriteAutoNegotiatedResource(writer, summary, serverAddress, questCompatibilityMode);
            return;
        }

        writer.WriteStartElement(string.Empty, "res", NsDidl);

        switch (summary.ItemType)
        {
            case BaseItemKind.Audio:
                WriteAudioResourceAttributes(writer, summary, profile);
                writer.WriteString(
                    NormalizeDlnaMediaUrl(
                        BuildDirectPlayAudioStreamUrl(
                            serverAddress,
                            summary.ItemId,
                            summary.Container,
                            summary.MediaSourceId,
                            summary.MediaSourceTag),
                        questCompatibilityMode));
                break;
            default:
                WriteVideoResourceAttributes(writer, summary, profile);
                writer.WriteString(
                    NormalizeDlnaMediaUrl(
                        BuildDirectPlayVideoStreamUrl(
                            serverAddress,
                            summary.ItemId,
                            summary.Container,
                            summary.MediaSourceId,
                            summary.MediaSourceTag),
                        questCompatibilityMode));
                break;
        }

        writer.WriteFullEndElement();
    }

    private static void WriteAutoNegotiatedResource(
        XmlWriter writer,
        ItemSummaryRecord summary,
        string serverAddress,
        bool questCompatibilityMode)
    {
        writer.WriteStartElement(string.Empty, "res", NsDidl);

        if (summary.ItemType == BaseItemKind.Audio)
        {
            writer.WriteAttributeString("protocolInfo", "http-get:*:audio/mpeg:*");
            writer.WriteString(
                NormalizeDlnaMediaUrl(
                    BuildLightweightAudioStreamUrl(serverAddress, summary.ItemId),
                    questCompatibilityMode));
        }
        else
        {
            writer.WriteAttributeString("protocolInfo", "http-get:*:video/mp4:*");
            writer.WriteString(
                NormalizeDlnaMediaUrl(
                    BuildLightweightVideoStreamUrl(serverAddress, summary.ItemId),
                    questCompatibilityMode));
        }

        writer.WriteFullEndElement();
    }

    private static void WriteVideoResourceAttributes(XmlWriter writer, ItemSummaryRecord summary, DlnaDeviceProfile profile)
    {
        if (summary.RunTimeTicks is > 0)
        {
            writer.WriteAttributeString(
                "duration",
                TimeSpan.FromTicks(summary.RunTimeTicks.Value).ToString("c", CultureInfo.InvariantCulture));
        }

        if (summary.FileSize is > 0)
        {
            writer.WriteAttributeString("size", summary.FileSize.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (summary.VideoWidth is > 0 && summary.VideoHeight is > 0)
        {
            writer.WriteAttributeString(
                "resolution",
                string.Format(CultureInfo.InvariantCulture, "{0}x{1}", summary.VideoWidth.Value, summary.VideoHeight.Value));
        }

        if (summary.TotalBitrate is > 0)
        {
            writer.WriteAttributeString("bitrate", summary.TotalBitrate.Value.ToString(CultureInfo.InvariantCulture));
        }

        var container = NormalizeContainerExtension(summary.Container);
        var mimeType = GetVideoMimeType(container);
        var contentFeatures = ContentFeatureBuilder.BuildVideoHeader(
            profile,
            container,
            summary.VideoCodec,
            summary.AudioCodec,
            summary.VideoWidth,
            summary.VideoHeight,
            null,
            summary.TotalBitrate,
            TransportStreamTimestamp.None,
            isDirectStream: true,
            summary.RunTimeTicks ?? 0,
            null,
            VideoRangeType.SDR,
            null,
            0,
            null,
            TranscodeSeekInfo.Auto,
            null,
            null,
            null,
            null,
            null,
            0,
            null,
            null).FirstOrDefault() ?? "DLNA.ORG_OP=01;DLNA.ORG_CI=0";

        writer.WriteAttributeString("protocolInfo", string.Format(CultureInfo.InvariantCulture, "http-get:*:{0}:{1}", mimeType, contentFeatures));
    }

    private static void WriteAudioResourceAttributes(XmlWriter writer, ItemSummaryRecord summary, DlnaDeviceProfile profile)
    {
        if (summary.RunTimeTicks is > 0)
        {
            writer.WriteAttributeString(
                "duration",
                TimeSpan.FromTicks(summary.RunTimeTicks.Value).ToString("c", CultureInfo.InvariantCulture));
        }

        if (summary.FileSize is > 0)
        {
            writer.WriteAttributeString("size", summary.FileSize.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (summary.TotalBitrate is > 0)
        {
            writer.WriteAttributeString("bitrate", summary.TotalBitrate.Value.ToString(CultureInfo.InvariantCulture));
        }

        var container = NormalizeContainerExtension(summary.Container, "mp3");
        var mimeType = GetAudioMimeType(container);
        var contentFeatures = ContentFeatureBuilder.BuildAudioHeader(
            profile,
            container,
            summary.AudioCodec,
            summary.TotalBitrate,
            null,
            null,
            null,
            isDirectStream: true,
            summary.RunTimeTicks ?? 0,
            TranscodeSeekInfo.Auto);

        writer.WriteAttributeString("protocolInfo", string.Format(CultureInfo.InvariantCulture, "http-get:*:{0}:{1}", mimeType, contentFeatures));
    }

    private static string NormalizeContainerExtension(string? container, string fallback = "mp4")
    {
        if (string.IsNullOrWhiteSpace(container))
        {
            return fallback;
        }

        return container.Trim().TrimStart('.').ToLowerInvariant();
    }

    private static string GetVideoMimeType(string container)
        => container switch
        {
            "mkv" => "video/x-matroska",
            "webm" => "video/webm",
            "3gp" or "3g2" => "video/3gpp",
            "ts" or "mpegts" => "video/vnd.dlna.mpeg-tts",
            "avi" => "video/avi",
            _ => "video/mp4"
        };

    private static string GetAudioMimeType(string container)
        => container switch
        {
            "flac" => "audio/flac",
            "aac" => "audio/aac",
            "m4a" or "mp4" => "audio/mp4",
            "ogg" or "oga" or "opus" => "audio/ogg",
            "amr" => "audio/amr",
            "wav" => "audio/wav",
            _ => "audio/mpeg"
        };
}
