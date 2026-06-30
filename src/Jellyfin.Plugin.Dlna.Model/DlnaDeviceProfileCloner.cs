using MediaBrowser.Model.Dlna;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Helpers for cloning device profiles without mutating cached instances.
/// </summary>
public static class DlnaDeviceProfileCloner
{
    /// <summary>
    /// Returns a copy of the profile with all transcoding profiles removed.
    /// </summary>
    /// <param name="source">The source profile.</param>
    /// <returns>A profile that only allows direct play paths.</returns>
    public static DlnaDeviceProfile WithoutTranscoding(DlnaDeviceProfile source)
    {
        return new DlnaDeviceProfile
        {
            Identification = source.Identification,
            FriendlyName = source.FriendlyName,
            Manufacturer = source.Manufacturer,
            ManufacturerUrl = source.ManufacturerUrl,
            ModelName = source.ModelName,
            ModelDescription = source.ModelDescription,
            ModelNumber = source.ModelNumber,
            ModelUrl = source.ModelUrl,
            SerialNumber = source.SerialNumber,
            EnableAlbumArtInDidl = source.EnableAlbumArtInDidl,
            EnableSingleAlbumArtLimit = source.EnableSingleAlbumArtLimit,
            EnableSingleSubtitleLimit = source.EnableSingleSubtitleLimit,
            SupportedMediaTypes = source.SupportedMediaTypes,
            UserId = source.UserId,
            AlbumArtPn = source.AlbumArtPn,
            MaxAlbumArtWidth = source.MaxAlbumArtWidth,
            MaxAlbumArtHeight = source.MaxAlbumArtHeight,
            MaxIconWidth = source.MaxIconWidth,
            MaxIconHeight = source.MaxIconHeight,
            SonyAggregationFlags = source.SonyAggregationFlags,
            ProtocolInfo = source.ProtocolInfo,
            TimelineOffsetSeconds = source.TimelineOffsetSeconds,
            RequiresPlainVideoItems = source.RequiresPlainVideoItems,
            RequiresPlainFolders = source.RequiresPlainFolders,
            EnableMSMediaReceiverRegistrar = source.EnableMSMediaReceiverRegistrar,
            IgnoreTranscodeByteRangeRequests = source.IgnoreTranscodeByteRangeRequests,
            XmlRootAttributes = source.XmlRootAttributes,
            Name = source.Name,
            MaxStreamingBitrate = source.MaxStreamingBitrate,
            MaxStaticBitrate = source.MaxStaticBitrate,
            MusicStreamingTranscodingBitrate = source.MusicStreamingTranscodingBitrate,
            MaxStaticMusicBitrate = source.MaxStaticMusicBitrate,
            DirectPlayProfiles = source.DirectPlayProfiles,
            TranscodingProfiles = [],
            ContainerProfiles = source.ContainerProfiles,
            CodecProfiles = source.CodecProfiles,
            ResponseProfiles = source.ResponseProfiles,
            SubtitleProfiles = source.SubtitleProfiles
        };
    }
}
