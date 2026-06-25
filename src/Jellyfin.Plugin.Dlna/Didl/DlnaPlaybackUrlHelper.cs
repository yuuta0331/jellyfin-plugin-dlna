using System;
using System.Globalization;
using System.Xml;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Indexing;

namespace Jellyfin.Plugin.Dlna.Didl;

/// <summary>
/// Builds lightweight DLNA playback URLs for indexed browse summaries.
/// </summary>
public static class DlnaPlaybackUrlHelper
{
    private const string NsDidl = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";

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
        if (questCompatibilityMode)
        {
            var queryIndex = url.IndexOf('?', StringComparison.Ordinal);
            if (queryIndex >= 0)
            {
                return url[..queryIndex];
            }

            return url;
        }

        return url + "&dlnaheaders=true";
    }

    /// <summary>
    /// Builds a query-free video stream URL for summary browse output.
    /// </summary>
    public static string BuildLightweightVideoStreamUrl(string serverAddress, Guid itemId)
    {
        var baseUrl = serverAddress.TrimEnd('/');
        return string.Format(CultureInfo.InvariantCulture, "{0}/dlna/videos/{1}/stream.mp4", baseUrl, itemId);
    }

    /// <summary>
    /// Builds a query-free audio stream URL for summary browse output.
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
        => itemType is BaseItemKind.Movie or BaseItemKind.Episode or BaseItemKind.Video or BaseItemKind.Audio;

    /// <summary>
    /// Writes a lightweight playback <c>res</c> element for indexed browse summaries.
    /// </summary>
    public static void WriteSummaryPlaybackResource(
        XmlWriter writer,
        ItemSummaryRecord summary,
        string serverAddress,
        bool questCompatibilityMode,
        bool ensurePlaybackUrlsInBrowse)
    {
        if (!ensurePlaybackUrlsInBrowse || !IsPlayableSummaryItem(summary.ItemType))
        {
            return;
        }

        writer.WriteStartElement(string.Empty, "res", NsDidl);

        switch (summary.ItemType)
        {
            case BaseItemKind.Audio:
                writer.WriteAttributeString("protocolInfo", "http-get:*:audio/mpeg:*");
                writer.WriteString(NormalizeDlnaMediaUrl(BuildLightweightAudioStreamUrl(serverAddress, summary.ItemId), questCompatibilityMode));
                break;
            default:
                writer.WriteAttributeString("protocolInfo", "http-get:*:video/mp4:*");
                writer.WriteString(NormalizeDlnaMediaUrl(BuildLightweightVideoStreamUrl(serverAddress, summary.ItemId), questCompatibilityMode));
                break;
        }

        writer.WriteFullEndElement();
    }
}
