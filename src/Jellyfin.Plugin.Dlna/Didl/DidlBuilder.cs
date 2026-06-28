using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Jellyfin.Plugin.Dlna.Extensions;
using Jellyfin.Plugin.Dlna.Indexing;
using Jellyfin.Plugin.Dlna.Model;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;
using Episode = MediaBrowser.Controller.Entities.TV.Episode;
using Genre = MediaBrowser.Controller.Entities.Genre;
using MediaOptions = MediaBrowser.Model.Dlna.MediaOptions;
using Movie = MediaBrowser.Controller.Entities.Movies.Movie;
using MusicAlbum = MediaBrowser.Controller.Entities.Audio.MusicAlbum;
using Season = MediaBrowser.Controller.Entities.TV.Season;
using Series = MediaBrowser.Controller.Entities.TV.Series;
using StreamBuilder = MediaBrowser.Model.Dlna.StreamBuilder;
using StreamInfo = MediaBrowser.Model.Dlna.StreamInfo;
using SubtitleDeliveryMethod = MediaBrowser.Model.Dlna.SubtitleDeliveryMethod;
using SubtitleStreamInfo = MediaBrowser.Model.Dlna.SubtitleStreamInfo;
using XmlAttribute = Jellyfin.Plugin.Dlna.Model.XmlAttribute;

namespace Jellyfin.Plugin.Dlna.Didl;

/// <summary>
/// Defines the <see cref="DidlBuilder" />.
/// </summary>
public class DidlBuilder
{
    private const string NsDidl = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
    private const string NsDc = "http://purl.org/dc/elements/1.1/";
    private const string NsUpnp = "urn:schemas-upnp-org:metadata-1-0/upnp/";
    private const string NsDlna = "urn:schemas-dlna-org:metadata-1-0/";

    private readonly DlnaDeviceProfile _profile;
    private readonly IImageProcessor _imageProcessor;
    private readonly string _serverAddress;
    private readonly string? _accessToken;
    private readonly User? _user;
    private readonly IUserDataManager _userDataManager;
    private readonly ILocalizationManager _localization;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly ILogger _logger;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DidlBuilder"/> class.
    /// </summary>
    /// <param name="profile">The <see cref="DlnaDeviceProfile"/>.</param>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <param name="imageProcessor">Instance of the <see cref="IImageProcessor"/> interface.</param>
    /// <param name="serverAddress">The server address.</param>
    /// <param name="accessToken">The access token.</param>
    /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    public DidlBuilder(
        DlnaDeviceProfile profile,
        User? user,
        IImageProcessor imageProcessor,
        string serverAddress,
        string? accessToken,
        IUserDataManager userDataManager,
        ILocalizationManager localization,
        IMediaSourceManager mediaSourceManager,
        ILogger logger,
        IMediaEncoder mediaEncoder,
        ILibraryManager libraryManager)
    {
        _profile = profile;
        _user = user;
        _imageProcessor = imageProcessor;
        _serverAddress = serverAddress;
        _accessToken = accessToken;
        _userDataManager = userDataManager;
        _localization = localization;
        _mediaSourceManager = mediaSourceManager;
        _logger = logger;
        _mediaEncoder = mediaEncoder;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Gets the normalized DLNA media URL.
    /// <param name="url">The URL to normalize.</param>
    /// </summary>
    public static string NormalizeDlnaMediaUrl(string url)
        => DlnaPlaybackUrlHelper.NormalizeDlnaMediaUrl(
            url,
            DlnaPlugin.Instance?.Configuration.EnableQuestCompatibilityMode == true);

    /// <summary>
    /// Gets the item DIDL.
    /// <param name="item">The <see cref="BaseItem"/>.</param>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <param name="context">The <see cref="BaseItem"/> context.</param>
    /// <param name="deviceId">The device id.</param>
    /// <param name="filter">The <see cref="Filter"/>.</param>
    /// <param name="streamInfo">The <see cref="StreamInfo" />.</param>
    /// </summary>
    public string GetItemDidl(BaseItem item, User? user, BaseItem? context, string deviceId, Filter filter, StreamInfo streamInfo)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            CloseOutput = false,
            OmitXmlDeclaration = true,
            ConformanceLevel = ConformanceLevel.Fragment
        };

        using (StringWriter builder = new StringWriterWithEncoding(Encoding.UTF8))
        {
            // If this using are changed to single lines, then write.Flush needs to be appended before the return.
            using (var writer = XmlWriter.Create(builder, settings))
            {
                // writer.WriteStartDocument();

                writer.WriteStartElement(string.Empty, "DIDL-Lite", NsDidl);

                writer.WriteAttributeString("xmlns", "dc", null, NsDc);
                writer.WriteAttributeString("xmlns", "dlna", null, NsDlna);
                writer.WriteAttributeString("xmlns", "upnp", null, NsUpnp);
                // didl.SetAttribute("xmlns:sec", NS_SEC);

                WriteXmlRootAttributes(_profile, writer);

                WriteItemElement(writer, item, user, context, null, deviceId, filter, streamInfo);

                writer.WriteFullEndElement();
                // writer.WriteEndDocument();
            }

            return builder.ToString();
        }
    }

    /// <summary>
    /// Writes XML attributes of a profile the item DIDL.
    /// <param name="profile">The <see cref="DlnaDeviceProfile"/>.</param>
    /// <param name="writer">The <see cref="XmlWriter"/>.</param>
    /// </summary>
    public static void WriteXmlRootAttributes(DlnaDeviceProfile profile, XmlWriter writer)
    {
        foreach (var att in profile.XmlRootAttributes)
        {
            var parts = att.Name.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                writer.WriteAttributeString(parts[0], parts[1], null, att.Value);
            }
            else
            {
                writer.WriteAttributeString(att.Name, att.Value);
            }
        }
    }

    /// <summary>
    /// Writes an XML item element.
    /// <param name="writer">The <see cref="XmlWriter"/>.</param>
    /// <param name="item">The <see cref="BaseItem"/>.</param>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <param name="context">The <see cref="BaseItem"/> context.</param>
    /// <param name="contextStubType">The <see cref="StubType"/> of the context.</param>
    /// <param name="deviceId">The device id.</param>
    /// <param name="filter">The <see cref="Filter"/>.</param>
    /// <param name="streamInfo">The <see cref="StreamInfo" />.</param>
    /// <param name="imageContext">The image browse context.</param>
    /// </summary>
    public void WriteItemElement(
        XmlWriter writer,
        BaseItem item,
        User? user,
        BaseItem? context,
        StubType? contextStubType,
        string deviceId,
        Filter filter,
        StreamInfo? streamInfo = null,
        DlnaImageBrowseContext imageContext = DlnaImageBrowseContext.Default)
    {
        var clientId = GetClientId(item, null);

        writer.WriteStartElement(string.Empty, "item", NsDidl);

        writer.WriteAttributeString("restricted", "1");
        writer.WriteAttributeString("id", clientId);

        if (context is not null)
        {
            writer.WriteAttributeString("parentID", GetClientId(context, contextStubType));
        }
        else
        {
            var parent = item.DisplayParentId;
            if (!parent.Equals(default))
            {
                writer.WriteAttributeString("parentID", GetClientId(parent, null));
            }
        }

        AddGeneralProperties(item, null, context, writer, filter);

        AddSamsungBookmarkInfo(item, user, writer, streamInfo);

        // refID?
        // storeAttribute(itemNode, object, ClassProperties.REF_ID, false);

        if (item is IHasMediaSources)
        {
            switch (item.MediaType)
            {
                case MediaType.Audio:
                    AddAudioResource(writer, item, deviceId, filter, streamInfo);
                    break;
                case MediaType.Video:
                    AddVideoResource(writer, item, deviceId, filter, streamInfo);
                    break;
            }
        }

        AddCover(item, contextStubType, imageContext, writer);
        writer.WriteFullEndElement();
    }

    private void AddVideoResource(XmlWriter writer, BaseItem video, string deviceId, Filter filter, StreamInfo? streamInfo = null)
    {
        if (streamInfo is null)
        {
            var sources = _mediaSourceManager.GetStaticMediaSources(video, true, _user);

            streamInfo = new StreamBuilder(_mediaEncoder, DlnaPluginLog.VerboseDependencyLogger(_logger)).GetOptimalVideoStream(new MediaOptions
            {
                ItemId = video.Id,
                MediaSources = sources.ToArray(),
                Profile = _profile,
                DeviceId = deviceId,
                MaxBitrate = _profile.MaxStreamingBitrate
            }) ?? throw new InvalidOperationException("No optimal video stream found");
        }

        var targetWidth = streamInfo.TargetWidth;
        var targetHeight = streamInfo.TargetHeight;
        var targetVideoCodec = streamInfo.TargetVideoCodec.FirstOrDefault();
        var targetAudioCodec = streamInfo.TargetAudioCodec.FirstOrDefault();

        var contentFeatureList = ContentFeatureBuilder.BuildVideoHeader(
            _profile,
            streamInfo.Container,
            targetVideoCodec,
            targetAudioCodec,
            targetWidth,
            targetHeight,
            streamInfo.TargetVideoBitDepth,
            streamInfo.TargetVideoBitrate,
            streamInfo.TargetTimestamp,
            streamInfo.IsDirectStream,
            streamInfo.RunTimeTicks ?? 0,
            streamInfo.TargetVideoProfile,
            streamInfo.TargetVideoRangeType,
            streamInfo.TargetVideoLevel,
            streamInfo.TargetFramerate ?? 0,
            streamInfo.TargetPacketLength,
            streamInfo.TranscodeSeekInfo,
            streamInfo.IsTargetAnamorphic,
            streamInfo.IsTargetInterlaced,
            streamInfo.TargetRefFrames,
            streamInfo.TargetVideoStreamCount,
            streamInfo.TargetAudioStreamCount,
            streamInfo.GetStreamCount(),
            streamInfo.TargetVideoCodecTag,
            streamInfo.IsTargetAVC);

        foreach (var contentFeature in contentFeatureList)
        {
            AddVideoResource(writer, filter, contentFeature, streamInfo);
        }

        var subtitleProfiles = streamInfo.GetSubtitleProfiles(_mediaEncoder, false, _serverAddress, _accessToken);

        foreach (var subtitle in subtitleProfiles)
        {
            if (subtitle.DeliveryMethod != SubtitleDeliveryMethod.External)
            {
                continue;
            }

            var subtitleAdded = AddSubtitleElement(writer, subtitle);

            if (subtitleAdded && _profile.EnableSingleSubtitleLimit)
            {
                break;
            }
        }
    }

    private bool AddSubtitleElement(XmlWriter writer, SubtitleStreamInfo info)
    {
        var subtitleProfile = _profile.SubtitleProfiles
            .FirstOrDefault(i => string.Equals(info.Format, i.Format, StringComparison.OrdinalIgnoreCase)
                                 && i.Method == SubtitleDeliveryMethod.External);

        if (subtitleProfile is null)
        {
            return false;
        }

        var subtitleMode = subtitleProfile.DidlMode;

        if (string.Equals(subtitleMode, "CaptionInfoEx", StringComparison.OrdinalIgnoreCase))
        {
            // <sec:CaptionInfoEx sec:type="srt">http://192.168.1.3:9999/video.srt</sec:CaptionInfoEx>
            // <sec:CaptionInfo sec:type="srt">http://192.168.1.3:9999/video.srt</sec:CaptionInfo>

            writer.WriteStartElement("sec", "CaptionInfoEx", null);
            writer.WriteAttributeString("sec", "type", null, info.Format.ToLowerInvariant());

            writer.WriteString(info.Url);
            writer.WriteFullEndElement();
        }
        else if (string.Equals(subtitleMode, "smi", StringComparison.OrdinalIgnoreCase))
        {
            writer.WriteStartElement(string.Empty, "res", NsDidl);

            writer.WriteAttributeString("protocolInfo", "http-get:*:smi/caption:*");

            writer.WriteString(info.Url);
            writer.WriteFullEndElement();
        }
        else
        {
            writer.WriteStartElement(string.Empty, "res", NsDidl);
            var protocolInfo = string.Format(
                CultureInfo.InvariantCulture,
                "http-get:*:text/{0}:*",
                info.Format.ToLowerInvariant());
            writer.WriteAttributeString("protocolInfo", protocolInfo);

            writer.WriteString(info.Url);
            writer.WriteFullEndElement();
        }

        return true;
    }

    private void AddVideoResource(XmlWriter writer, Filter filter, string contentFeatures, StreamInfo streamInfo)
    {
        writer.WriteStartElement(string.Empty, "res", NsDidl);

        var url = NormalizeDlnaMediaUrl(streamInfo.ToDlnaUrl(_serverAddress, _accessToken));

        var mediaSource = streamInfo.MediaSource;

        if (mediaSource?.RunTimeTicks.HasValue == true)
        {
            writer.WriteAttributeString("duration", TimeSpan.FromTicks(mediaSource.RunTimeTicks.Value).ToString("c", CultureInfo.InvariantCulture));
        }

        if (filter.Contains("res@size"))
        {
            if (streamInfo.IsDirectStream || streamInfo.EstimateContentLength)
            {
                var size = streamInfo.TargetSize;

                if (size.HasValue)
                {
                    writer.WriteAttributeString("size", size.Value.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        var totalBitrate = streamInfo.TargetTotalBitrate;
        var targetSampleRate = streamInfo.TargetAudioSampleRate;
        var targetChannels = streamInfo.TargetAudioChannels;

        var targetWidth = streamInfo.TargetWidth;
        var targetHeight = streamInfo.TargetHeight;

        if (targetChannels.HasValue)
        {
            writer.WriteAttributeString("nrAudioChannels", targetChannels.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (filter.Contains("res@resolution"))
        {
            if (targetWidth.HasValue && targetHeight.HasValue)
            {
                writer.WriteAttributeString(
                    "resolution",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}x{1}",
                        targetWidth.Value,
                        targetHeight.Value));
            }
        }

        if (targetSampleRate.HasValue)
        {
            writer.WriteAttributeString("sampleFrequency", targetSampleRate.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (totalBitrate.HasValue)
        {
            writer.WriteAttributeString("bitrate", totalBitrate.Value.ToString(CultureInfo.InvariantCulture));
        }

        var mediaProfile = _profile.GetVideoMediaProfile(
            streamInfo.Container,
            streamInfo.TargetAudioCodec.FirstOrDefault(),
            streamInfo.TargetVideoCodec.FirstOrDefault(),
            streamInfo.TargetAudioBitrate,
            targetWidth,
            targetHeight,
            streamInfo.TargetVideoBitDepth,
            streamInfo.TargetVideoProfile,
            streamInfo.TargetVideoRangeType,
            streamInfo.TargetVideoLevel,
            streamInfo.TargetFramerate ?? 0,
            streamInfo.TargetPacketLength,
            streamInfo.TargetTimestamp,
            streamInfo.IsTargetAnamorphic,
            streamInfo.IsTargetInterlaced,
            streamInfo.TargetRefFrames,
            streamInfo.TargetVideoStreamCount,
            streamInfo.TargetAudioStreamCount,
            streamInfo.GetStreamCount(),
            streamInfo.TargetVideoCodecTag,
            streamInfo.IsTargetAVC);

        var filename = url[..url.IndexOf('?', StringComparison.Ordinal)];

        var mimeType = mediaProfile is null || string.IsNullOrEmpty(mediaProfile.MimeType)
            ? MimeTypes.GetMimeType(filename)
            : mediaProfile.MimeType;

        writer.WriteAttributeString(
            "protocolInfo",
            string.Format(
                CultureInfo.InvariantCulture,
                "http-get:*:{0}:{1}",
                mimeType,
                contentFeatures));

        writer.WriteString(url);

        writer.WriteFullEndElement();
    }

    private string GetDisplayName(BaseItem item, StubType? itemStubType, BaseItem? context, ServerItem? serverItem = null)
    {
        if (serverItem?.StubType == StubType.BrowseByKanaRow)
        {
            var config = DlnaPlugin.Instance?.Configuration;
            if (config is not null && serverItem.TitleBrowseGroupId is string groupId)
            {
                var preset = TitleBrowseConfigurationHelper.ResolvePreset(config, serverItem.LibraryScopeId ?? item.Id);
                return TitleBrowseHelper.GetGroupLabel(preset, groupId, IsJapanese());
            }

            if (serverItem.KanaRowIndex is int kanaRow)
            {
                var preset = TitleBrowsePresetDefaults.CreateJapaneseKanaPreset();
                return TitleBrowseHelper.GetGroupLabel(preset, TitleBrowsePresetDefaults.LegacyRowIndexToGroupId(kanaRow), IsJapanese());
            }
        }

        if (serverItem?.StubType == StubType.BrowseByYearItem && serverItem.ProductionYear is int year)
        {
            return year.ToString(CultureInfo.InvariantCulture);
        }

        if (serverItem?.FacetKey is string facetName
            && serverItem.StubType is StubType.StudioItem or StubType.TagItem or StubType.RatingItem or StubType.PersonItem)
        {
            return facetName;
        }

        if (serverItem?.StubType == StubType.SeriesRange
            && serverItem.RangeStart is int rangeStart
            && serverItem.RangeEnd is int rangeEnd)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{rangeStart + 1:D4}-{rangeEnd:D4}");
        }

        if (itemStubType.HasValue)
        {
            switch (itemStubType.Value)
            {
                case StubType.Latest: return _localization.GetLocalizedString("Latest");
                case StubType.Playlists: return _localization.GetLocalizedString("Playlists");
                case StubType.AlbumArtists: return _localization.GetLocalizedString("HeaderAlbumArtists");
                case StubType.Albums: return _localization.GetLocalizedString("Albums");
                case StubType.Artists: return _localization.GetLocalizedString("Artists");
                case StubType.Songs: return _localization.GetLocalizedString("Songs");
                case StubType.Genres: return _localization.GetLocalizedString("Genres");
                case StubType.FavoriteAlbums: return _localization.GetLocalizedString("HeaderFavoriteAlbums");
                case StubType.FavoriteArtists: return _localization.GetLocalizedString("HeaderFavoriteArtists");
                case StubType.FavoriteSongs: return _localization.GetLocalizedString("HeaderFavoriteSongs");
                case StubType.ContinueWatching: return _localization.GetLocalizedString("HeaderContinueWatching");
                case StubType.Movies: return _localization.GetLocalizedString("Movies");
                case StubType.Collections: return _localization.GetLocalizedString("Collections");
                case StubType.Favorites: return _localization.GetLocalizedString("Favorites");
                case StubType.NextUp: return _localization.GetLocalizedString("HeaderNextUp");
                case StubType.FavoriteSeries: return _localization.GetLocalizedString("HeaderFavoriteShows");
                case StubType.FavoriteEpisodes: return _localization.GetLocalizedString("HeaderFavoriteEpisodes");
                case StubType.Series: return _localization.GetLocalizedString("Shows");
                case StubType.RecentlyAddedEpisodes:
                    return IsJapanese() ? "最近追加されたエピソード" : "Recently Added Episodes";
                case StubType.RecentlyAddedSeries:
                    return IsJapanese() ? "最近追加されたシリーズ" : "Recently Added Series";
                case StubType.RecentlyReleasedEpisodes:
                    return IsJapanese() ? "最近リリースされたエピソード" : "Recently Released Episodes";
                case StubType.RecentlyAddedMovies:
                    return IsJapanese() ? "最近追加された映画" : "Recently Added Movies";
                case StubType.RecentlyReleasedMovies:
                    return IsJapanese() ? "最近リリースされた映画" : "Recently Released Movies";
                case StubType.RecentlyReleasedSeries:
                    return IsJapanese() ? "最近リリースされたシリーズ" : "Recently Released Series";
                case StubType.RecentlyUpdatedSeries:
                    return IsJapanese() ? "最近更新されたシリーズ" : "Recently Updated Series";
                case StubType.CurrentlyAiring:
                    return IsJapanese() ? "放送中" : "Currently Airing";
                case StubType.BrowseByKana:
                {
                    var config = DlnaPlugin.Instance?.Configuration;
                    if (config is not null)
                    {
                        var libraryId = serverItem?.LibraryScopeId ?? item.Id;
                        var preset = TitleBrowseConfigurationHelper.ResolvePreset(config, libraryId);
                        return IsJapanese() ? preset.NameJa : preset.NameEn;
                    }

                    return IsJapanese() ? "頭文字別" : "Browse By Title";
                }
                case StubType.BrowseByStudio:
                    return IsJapanese() ? "スタジオ別" : "Browse By Studio";
                case StubType.BrowseByTag:
                    return IsJapanese() ? "タグ別" : "Browse By Tag";
                case StubType.BrowseByRating:
                    return IsJapanese() ? "レーティング別" : "Browse By Rating";
                case StubType.BrowseByYear:
                    return IsJapanese() ? "年別" : "Browse By Year";
                case StubType.BrowseByPerson:
                    return IsJapanese() ? "出演者別" : "Browse By Person";
                case StubType.RecentlyModifiedSeries:
                    return IsJapanese() ? "最近メタデータ更新（シリーズ）" : "Recently Modified Series";
                case StubType.RecentlyModifiedMovies:
                    return IsJapanese() ? "最近メタデータ更新（映画）" : "Recently Modified Movies";
                case StubType.RecentlyModifiedEpisodes:
                    return IsJapanese() ? "最近メタデータ更新（エピソード）" : "Recently Modified Episodes";
                case StubType.ThreeDMovies:
                    return IsJapanese() ? "3D映画" : "3D Movies";
                case StubType.FourKMovies:
                    return IsJapanese() ? "4K映画" : "4K Movies";
                case StubType.EightKMovies:
                    return IsJapanese() ? "8K映画" : "8K Movies";
                case StubType.VrMovies:
                    return IsJapanese() ? "VR動画" : "VR Videos";
                case StubType.EightKVrMovies:
                    return IsJapanese() ? "8K VR動画" : "8K VR Videos";
                case StubType.Extras:
                    if (item is Movie)
                    {
                        return item.Name + (IsJapanese() ? " - 特典映像" : " - Extras");
                    }
                    return IsJapanese() ? "特典映像" : "Extras";
                case StubType.Videos:
                    return IsJapanese() ? "ビデオ" : "Videos";
                case StubType.Photos:
                    return _localization.GetLocalizedString("Photos");
                case StubType.MusicVideos:
                    return IsJapanese() ? "ミュージックビデオ" : "Music Videos";
            }
        }

        var displayName = item is Episode episode
            ? GetEpisodeDisplayName(episode, context)
            : item.Name;

        if (item is Video video)
        {
            try
            {
                var tagsList = new System.Collections.Generic.List<string>();

                // 1. 3D Tagging
                if (DlnaPlugin.Instance?.Configuration.EnableAuto3DTagging == true)
                {
                    var format = video.Video3DFormat;
                    if (format.HasValue)
                    {
                        var tag = format.Value switch
                        {
                            Video3DFormat.HalfSideBySide => "3D.HSBS",
                            Video3DFormat.FullSideBySide => "3D.FSBS",
                            Video3DFormat.HalfTopAndBottom => "3D.HOU",
                            Video3DFormat.FullTopAndBottom => "3D.FOU",
                            _ => "3D"
                        };
                        tagsList.Add(tag);
                    }
                }

                // 2. VR Tagging (VR180 / VR360)
                if (DlnaPlugin.Instance?.Configuration.EnableAutoVrTagging == true)
                {
                    if (IsVrVideo(video))
                    {
                        var path = video.Path;
                        if (!string.IsNullOrEmpty(path))
                        {
                            var fileName = System.IO.Path.GetFileName(path);
                            if (fileName.Contains("180", StringComparison.OrdinalIgnoreCase))
                            {
                                tagsList.Add("VR180");
                            }
                            else if (fileName.Contains("360", StringComparison.OrdinalIgnoreCase))
                            {
                                tagsList.Add("VR360");
                            }
                            else
                            {
                                tagsList.Add("VR");
                            }
                        }
                        else
                        {
                            tagsList.Add("VR");
                        }
                    }
                }

                // 3. Resolution Tagging (4K / 8K)
                if (DlnaPlugin.Instance?.Configuration.EnableAutoResolutionTagging == true)
                {
                    var w = video.Width;
                    var h = video.Height;
                    if (w >= 7000 || h >= 4000)
                    {
                        tagsList.Add("8K");
                    }
                    else if (w >= 3800 || h >= 2000)
                    {
                        tagsList.Add("4K");
                    }
                }

                if (tagsList.Count > 0)
                {
                    displayName = $"{displayName} [{string.Join(".", tagsList)}]";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tagging display name for {ItemName}", item.Name);
            }
        }

        return displayName;
    }

    private static bool IsVrVideo(Video video)
    {
        if (video.Tags != null && video.Tags.Any(t => t.Contains("vr", StringComparison.OrdinalIgnoreCase) 
                                                     || t.Contains("180", StringComparison.OrdinalIgnoreCase) 
                                                     || t.Contains("360", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var path = video.Path;
        if (!string.IsNullOrEmpty(path))
        {
            var fileName = System.IO.Path.GetFileName(path);
            if (fileName.Contains("vr180", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("vr360", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("180_sbs", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("360_sbs", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("_180", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("_360", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsJapanese()
    {
        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.OrdinalIgnoreCase)
            || CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (_user != null)
        {
            if (_user.AudioLanguagePreference != null && _user.AudioLanguagePreference.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (_user.SubtitleLanguagePreference != null && _user.SubtitleLanguagePreference.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets episode display name appropriate for the given context.
    /// </summary>
    /// <remarks>
    /// If context is a season, this will return a string containing just episode number and name.
    /// Otherwise the result will include series names and season number.
    /// </remarks>
    /// <param name="episode">The episode.</param>
    /// <param name="context">Current context.</param>
    /// <returns>Formatted name of the episode.</returns>
    private string GetEpisodeDisplayName(Episode episode, BaseItem? context)
    {
        string[] components;

        if (context is Season season)
        {
            // This is a special embedded within a season
            if (episode.ParentIndexNumber.HasValue && episode.ParentIndexNumber.Value == 0
                                                   && season.IndexNumber.HasValue && season.IndexNumber.Value != 0)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("ValueSpecialEpisodeName"),
                    episode.Name);
            }

            // inside a season use simple format (ex. '12 - Episode Name')
            var epNumberName = GetEpisodeIndexFullName(episode);
            components = [epNumberName, episode.Name];
        }
        else
        {
            // outside a season include series and season details (ex. 'TV Show - S05E11 - Episode Name')
            var epNumberName = GetEpisodeNumberDisplayName(episode);
            components = [episode.SeriesName, epNumberName, episode.Name];
        }

        return string.Join(" - ", components.Where(NotNullOrWhiteSpace));
    }

    /// <summary>
    /// Gets complete episode number.
    /// </summary>
    /// <param name="episode">The episode.</param>
    /// <returns>For single episodes returns just the number. For double episodes - current and ending numbers.</returns>
    private static string GetEpisodeIndexFullName(Episode episode)
    {
        var name = string.Empty;
        if (episode.IndexNumber.HasValue)
        {
            name += episode.IndexNumber.Value.ToString("00", CultureInfo.InvariantCulture);

            if (episode.IndexNumberEnd.HasValue)
            {
                name += "-" + episode.IndexNumberEnd.Value.ToString("00", CultureInfo.InvariantCulture);
            }
        }

        return name;
    }

    private string GetEpisodeNumberDisplayName(Episode episode)
    {
        var name = string.Empty;
        var seasonNumber = episode.Season?.IndexNumber;

        if (seasonNumber.HasValue)
        {
            name = "S" + seasonNumber.Value.ToString("00", CultureInfo.InvariantCulture);
        }

        var indexName = GetEpisodeIndexFullName(episode);

        if (!string.IsNullOrWhiteSpace(indexName))
        {
            name += "E" + indexName;
        }

        return name;
    }

    private bool NotNullOrWhiteSpace(string s) => !string.IsNullOrWhiteSpace(s);

    private void AddAudioResource(XmlWriter writer, BaseItem audio, string deviceId, Filter filter, StreamInfo? streamInfo = null)
    {
        writer.WriteStartElement(string.Empty, "res", NsDidl);

        if (streamInfo is null)
        {
            var sources = _mediaSourceManager.GetStaticMediaSources(audio, true, _user);

            streamInfo = new StreamBuilder(_mediaEncoder, DlnaPluginLog.VerboseDependencyLogger(_logger)).GetOptimalAudioStream(new MediaOptions
            {
                ItemId = audio.Id,
                MediaSources = sources.ToArray(),
                Profile = _profile,
                DeviceId = deviceId
            }) ?? throw new InvalidOperationException("No optimal audio stream found");
        }

        var url = NormalizeDlnaMediaUrl(streamInfo.ToDlnaUrl(_serverAddress, _accessToken));

        var mediaSource = streamInfo.MediaSource;

        if (mediaSource?.RunTimeTicks is not null)
        {
            writer.WriteAttributeString("duration", TimeSpan.FromTicks(mediaSource.RunTimeTicks.Value).ToString("c", CultureInfo.InvariantCulture));
        }

        if (filter.Contains("res@size"))
        {
            if (streamInfo.IsDirectStream || streamInfo.EstimateContentLength)
            {
                var size = streamInfo.TargetSize;

                if (size.HasValue)
                {
                    writer.WriteAttributeString("size", size.Value.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        var targetAudioBitrate = streamInfo.TargetAudioBitrate;
        var targetSampleRate = streamInfo.TargetAudioSampleRate;
        var targetChannels = streamInfo.TargetAudioChannels;
        var targetAudioBitDepth = streamInfo.TargetAudioBitDepth;

        if (targetChannels.HasValue)
        {
            writer.WriteAttributeString("nrAudioChannels", targetChannels.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (targetSampleRate.HasValue)
        {
            writer.WriteAttributeString("sampleFrequency", targetSampleRate.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (targetAudioBitrate.HasValue)
        {
            writer.WriteAttributeString("bitrate", targetAudioBitrate.Value.ToString(CultureInfo.InvariantCulture));
        }

        var mediaProfile = _profile.GetAudioMediaProfile(
            streamInfo.Container,
            streamInfo.TargetAudioCodec.FirstOrDefault(),
            targetChannels,
            targetAudioBitrate,
            targetSampleRate,
            targetAudioBitDepth);

        var filename = url[..url.IndexOf('?', StringComparison.Ordinal)];

        var mimeType = mediaProfile is null || string.IsNullOrEmpty(mediaProfile.MimeType)
            ? MimeTypes.GetMimeType(filename)
            : mediaProfile.MimeType;

        var contentFeatures = ContentFeatureBuilder.BuildAudioHeader(
            _profile,
            streamInfo.Container?.FirstOrDefault().ToString(),
            streamInfo.TargetAudioCodec.FirstOrDefault(),
            targetAudioBitrate,
            targetSampleRate,
            targetChannels,
            targetAudioBitDepth,
            streamInfo.IsDirectStream,
            streamInfo.RunTimeTicks ?? 0,
            streamInfo.TranscodeSeekInfo);

        writer.WriteAttributeString(
            "protocolInfo",
            string.Format(
                CultureInfo.InvariantCulture,
                "http-get:*:{0}:{1}",
                mimeType,
                contentFeatures));

        writer.WriteString(url);

        writer.WriteFullEndElement();
    }

    /// <summary>
    /// Gets a value indicating whether the id is a root id.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns><c>true</c> if the id is a root id; otherwise, <c>false</c>.</returns>
    public static bool IsIdRoot(string id)
        => string.IsNullOrWhiteSpace(id)
           || string.Equals(id, "0", StringComparison.OrdinalIgnoreCase)
           // Samsung sometimes uses 1 as root
           || string.Equals(id, "1", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Writes an XML folder element from a <see cref="ServerItem"/>.
    /// </summary>
    internal void WriteFolderElement(XmlWriter writer, ServerItem serverItem, BaseItem? context, int? childCount, Filter filter, string? requestedId = null, DlnaImageBrowseContext imageContext = DlnaImageBrowseContext.Default)
    {
        if (serverItem.IsSummaryBacked)
        {
            WriteSummaryFolderElement(writer, serverItem, context, childCount, filter, requestedId, imageContext);
            return;
        }

        WriteFolderElement(writer, serverItem.Item, serverItem.StubType, context, childCount, filter, requestedId, serverItem, imageContext);
    }

    /// <summary>
    /// Writes a minimal DIDL element from an indexed item summary.
    /// </summary>
    internal void WriteSummaryElement(XmlWriter writer, ServerItem serverItem, BaseItem? context, int? childCount, Filter filter, DlnaImageBrowseContext imageContext)
    {
        if (serverItem.Summary!.IsFolder)
        {
            WriteSummaryFolderElement(writer, serverItem, context, childCount, filter, null, imageContext);
        }
        else
        {
            WriteSummaryItemElement(writer, serverItem, context, filter, imageContext);
        }
    }

    private void WriteSummaryFolderElement(XmlWriter writer, ServerItem serverItem, BaseItem? context, int? childCount, Filter filter, string? requestedId, DlnaImageBrowseContext imageContext)
    {
        var summary = serverItem.Summary!;
        writer.WriteStartElement(string.Empty, "container", NsDidl);
        writer.WriteAttributeString("restricted", "1");
        writer.WriteAttributeString("searchable", "1");
        if (childCount.HasValue)
        {
            writer.WriteAttributeString("childCount", childCount.Value.ToString(CultureInfo.InvariantCulture));
        }

        var clientId = GetClientId(serverItem);
        if (string.Equals(requestedId, "0", StringComparison.Ordinal))
        {
            writer.WriteAttributeString("id", "0");
            writer.WriteAttributeString("parentID", "-1");
        }
        else
        {
            writer.WriteAttributeString("id", clientId);
            writer.WriteAttributeString("parentID", context is not null ? GetClientId(context, null) : GetClientId(summary.ParentId, null));
        }

        AddValue(writer, "dc", "title", summary.Name, NsDc);
        WriteSummaryObjectClass(writer, summary);

        if (filter.Contains("dc:date") && summary.PremiereDateTicks is long premiereTicks)
        {
            AddValue(writer, "dc", "date", new DateTime(premiereTicks).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), NsDc);
        }

        AddSummaryCover(summary, imageContext, writer);

        writer.WriteFullEndElement();
    }

    private void WriteSummaryItemElement(XmlWriter writer, ServerItem serverItem, BaseItem? context, Filter filter, DlnaImageBrowseContext imageContext)
    {
        var summary = serverItem.Summary!;
        writer.WriteStartElement(string.Empty, "item", NsDidl);
        writer.WriteAttributeString("restricted", "1");
        writer.WriteAttributeString("id", GetClientId(serverItem));
        if (context is not null)
        {
            writer.WriteAttributeString("parentID", GetClientId(context, null));
        }
        else if (summary.ParentId != Guid.Empty)
        {
            writer.WriteAttributeString("parentID", GetClientId(summary.ParentId, null));
        }

        AddValue(writer, "dc", "title", GetSummaryDisplayName(summary), NsDc);
        WriteSummaryObjectClass(writer, summary);

        if (filter.Contains("dc:date") && summary.PremiereDateTicks is long premiereTicks)
        {
            AddValue(writer, "dc", "date", new DateTime(premiereTicks).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), NsDc);
        }

        var config = DlnaPlugin.Instance.Configuration;
        DlnaPlaybackUrlHelper.WriteSummaryPlaybackResource(
            writer,
            summary,
            _serverAddress,
            config.EnableQuestCompatibilityMode,
            DlnaPlaybackUrlHelper.ShouldEnsurePlaybackUrlsInBrowse(config));

        AddSummaryCover(summary, imageContext, writer);

        writer.WriteFullEndElement();
    }

    private void AddSummaryCover(ItemSummaryRecord summary, DlnaImageBrowseContext imageContext, XmlWriter writer)
    {
        var config = DlnaPlugin.Instance.Configuration;
        var resolved = DlnaImageResolver.Resolve(summary, imageContext, config);
        if (resolved is null)
        {
            return;
        }

        WriteCoverElementsForSummary(summary, resolved, writer);
    }

    private void WriteCoverElementsForSummary(ItemSummaryRecord summary, DlnaResolvedImage resolved, XmlWriter writer)
    {
        var albumArtUrlInfo = GetImageUrl(
            resolved,
            _profile.MaxAlbumArtWidth ?? 10000,
            _profile.MaxAlbumArtHeight ?? 10000,
            "jpg",
            out _);

        writer.WriteStartElement("upnp", "albumArtURI", NsUpnp);
        if (!string.IsNullOrEmpty(_profile.AlbumArtPn))
        {
            writer.WriteAttributeString("dlna", "profileID", NsDlna, _profile.AlbumArtPn);
        }

        writer.WriteString(albumArtUrlInfo.Url);
        writer.WriteFullEndElement();

        var iconUrlInfo = GetImageUrl(
            resolved,
            _profile.MaxIconWidth ?? 48,
            _profile.MaxIconHeight ?? 48,
            "jpg",
            out _);
        writer.WriteElementString("upnp", "icon", NsUpnp, iconUrlInfo.Url);

        if (!_profile.EnableAlbumArtInDidl && DlnaPlaybackUrlHelper.IsPlayableSummaryItem(summary.ItemType))
        {
            return;
        }

        if (!_profile.EnableSingleAlbumArtLimit)
        {
            AddImageResElement(resolved, writer, 4096, 4096, "jpg", "JPEG_LRG");
            AddImageResElement(resolved, writer, 1024, 768, "jpg", "JPEG_MED");
            AddImageResElement(resolved, writer, 640, 480, "jpg", "JPEG_SM");
            AddImageResElement(resolved, writer, 4096, 4096, "png", "PNG_LRG");
            AddImageResElement(resolved, writer, 160, 160, "png", "PNG_TN");
        }

        AddImageResElement(resolved, writer, 160, 160, "jpg", "JPEG_TN");
    }

    private void WriteSummaryObjectClass(XmlWriter writer, ItemSummaryRecord summary)
    {
        writer.WriteStartElement("upnp", "class", NsUpnp);
        writer.WriteString(GetSummaryUpnpClass(summary));
        writer.WriteFullEndElement();
    }

    private string GetSummaryUpnpClass(ItemSummaryRecord summary)
    {
        if (summary.IsFolder)
        {
            if (_profile.RequiresPlainFolders)
            {
                return "object.container.storageFolder";
            }

            return summary.ItemType switch
            {
                BaseItemKind.Series or BaseItemKind.Season or BaseItemKind.BoxSet => "object.container.album.videoAlbum",
                BaseItemKind.MusicAlbum => "object.container.album.musicAlbum",
                BaseItemKind.MusicArtist => "object.container.person.musicArtist",
                BaseItemKind.Playlist => "object.container.playlistContainer",
                _ => "object.container.storageFolder"
            };
        }

        return summary.ItemType switch
        {
            BaseItemKind.Audio => "object.item.audioItem.musicTrack",
            BaseItemKind.Movie when !_profile.RequiresPlainVideoItems => "object.item.videoItem.movie",
            BaseItemKind.Episode or BaseItemKind.Movie or BaseItemKind.Video => "object.item.videoItem",
            _ => "object.item"
        };
    }

    private static string GetSummaryDisplayName(ItemSummaryRecord summary)
    {
        if (summary.ItemType == BaseItemKind.Episode && summary.IndexNumber is int indexNumber)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{indexNumber:D2}. {summary.Name}");
        }

        return summary.Name;
    }

    /// <summary>
    /// Creates a lightweight browse node record for layer-3 caching.
    /// </summary>
    internal BrowseNodeRecord CreateBrowseNodeRecord(ServerItem serverItem, BaseItem? context, string parentClientId, int? childCount, DlnaImageBrowseContext imageContext)
    {
        var (albumArtUri, iconUri) = GetCoverUris(serverItem, imageContext);

        if (serverItem.IsSummaryBacked)
        {
            var summary = serverItem.Summary!;
            return new BrowseNodeRecord(
                GetClientId(serverItem),
                summary.IsFolder ? summary.Name : GetSummaryDisplayName(summary),
                GetSummaryUpnpClass(summary),
                summary.IsFolder,
                childCount,
                parentClientId,
                albumArtUri,
                iconUri);
        }

        var item = serverItem.Item;
        var isFolder = item.IsDisplayedAsFolder || serverItem.StubType.HasValue;
        return new BrowseNodeRecord(
            GetClientId(serverItem),
            GetDisplayName(item, serverItem.StubType, context, serverItem),
            GetUpnpClassString(item, serverItem.StubType),
            isFolder,
            childCount,
            parentClientId,
            albumArtUri,
            iconUri);
    }

    private string GetUpnpClassString(BaseItem item, StubType? stubType)
    {
        using var buffer = new StringWriter();
        using var writer = XmlWriter.Create(buffer, new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Fragment });
        WriteObjectClass(writer, item, stubType);
        var xml = buffer.ToString();
        var start = xml.IndexOf('>', StringComparison.Ordinal) + 1;
        var end = xml.LastIndexOf('<');
        return start > 0 && end > start ? xml[start..end] : "object.item";
    }

    /// <summary>
    /// Writes a cached browse node element.
    /// </summary>
    internal static void WriteBrowseNodeElement(XmlWriter writer, BrowseNodeRecord node)
    {
        var elementName = node.IsFolder ? "container" : "item";
        writer.WriteStartElement(string.Empty, elementName, NsDidl);
        writer.WriteAttributeString("restricted", "1");
        if (node.IsFolder)
        {
            writer.WriteAttributeString("searchable", "1");
            if (node.ChildCount.HasValue)
            {
                writer.WriteAttributeString("childCount", node.ChildCount.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        writer.WriteAttributeString("id", node.ClientId);
        if (!string.IsNullOrEmpty(node.ParentId))
        {
            writer.WriteAttributeString("parentID", node.ParentId);
        }

        writer.WriteStartElement("dc", "title", NsDc);
        writer.WriteString(node.Title);
        writer.WriteFullEndElement();

        writer.WriteStartElement("upnp", "class", NsUpnp);
        writer.WriteString(node.UpnpClass);
        writer.WriteFullEndElement();

        if (!string.IsNullOrEmpty(node.AlbumArtUri))
        {
            writer.WriteElementString("upnp", "albumArtURI", NsUpnp, node.AlbumArtUri);
        }

        if (!string.IsNullOrEmpty(node.IconUri))
        {
            writer.WriteElementString("upnp", "icon", NsUpnp, node.IconUri);
        }

        writer.WriteFullEndElement();
    }

    /// <summary>
    /// Writes an XML folder element.
    /// </summary>
    public void WriteFolderElement(XmlWriter writer, BaseItem folder, StubType? stubType, BaseItem? context, int? childCount, Filter filter, string? requestedId = null, DlnaImageBrowseContext imageContext = DlnaImageBrowseContext.Default)
    {
        WriteFolderElement(writer, folder, stubType, context, childCount, filter, requestedId, null, imageContext);
    }

    private void WriteFolderElement(XmlWriter writer, BaseItem folder, StubType? stubType, BaseItem? context, int? childCount, Filter filter, string? requestedId, ServerItem? serverItem, DlnaImageBrowseContext imageContext = DlnaImageBrowseContext.Default)
    {
        writer.WriteStartElement(string.Empty, "container", NsDidl);

        writer.WriteAttributeString("restricted", "1");
        writer.WriteAttributeString("searchable", "1");
        if (childCount.HasValue)
        {
            writer.WriteAttributeString("childCount", childCount.Value.ToString(CultureInfo.InvariantCulture));
        }

        var clientId = serverItem is not null
            ? GetClientId(serverItem)
            : GetScopedClientId(folder, stubType, context);

        if (string.Equals(requestedId, "0", StringComparison.Ordinal))
        {
            writer.WriteAttributeString("id", "0");
            writer.WriteAttributeString("parentID", "-1");
        }
        else
        {
            writer.WriteAttributeString("id", clientId);

            if (context is not null)
            {
                writer.WriteAttributeString("parentID", GetClientId(context, null));
            }
            else
            {
                var parent = folder.DisplayParentId;
                if (parent.Equals(default))
                {
                    writer.WriteAttributeString("parentID", "0");
                }
                else
                {
                    writer.WriteAttributeString("parentID", GetClientId(parent, null));
                }
            }
        }

        AddGeneralProperties(folder, stubType, context, writer, filter, serverItem);

        AddCover(folder, stubType, imageContext, writer);

        writer.WriteFullEndElement();
    }

    private void AddSamsungBookmarkInfo(BaseItem item, User? user, XmlWriter writer, StreamInfo? streamInfo)
    {
        if (!item.SupportsPositionTicksResume || item is Folder)
        {
            return;
        }

        XmlAttribute? secAttribute = null;
        foreach (var attribute in _profile.XmlRootAttributes)
        {
            if (string.Equals(attribute.Name, "xmlns:sec", StringComparison.OrdinalIgnoreCase))
            {
                secAttribute = attribute;
                break;
            }
        }

        // Not a samsung device or no user data
        if (secAttribute is null || user is null)
        {
            return;
        }

        var userdata = _userDataManager.GetUserData(user, item)!;
        var playbackPositionTicks = (streamInfo is not null && streamInfo.StartPositionTicks > 0) ? streamInfo.StartPositionTicks : userdata.PlaybackPositionTicks;

        if (playbackPositionTicks > 0)
        {
            var elementValue = string.Format(
                CultureInfo.InvariantCulture,
                "BM={0}",
                Convert.ToInt32(TimeSpan.FromTicks(playbackPositionTicks).TotalSeconds));
            AddValue(writer, "sec", "dcmInfo", elementValue, secAttribute.Value);
        }
    }

    private void AddCommonFields(BaseItem item, StubType? itemStubType, BaseItem? context, XmlWriter writer, Filter filter, ServerItem? serverItem = null)
    {
        // Don't filter on dc:title because not all devices will include it in the filter
        // MediaMonkey for example won't display content without a title
        // if (filter.Contains("dc:title"))
        {
            AddValue(writer, "dc", "title", GetDisplayName(item, itemStubType, context, serverItem), NsDc);
        }

        WriteObjectClass(writer, item, itemStubType);

        if (filter.Contains("dc:date"))
        {
            if (item.PremiereDate.HasValue)
            {
                AddValue(writer, "dc", "date", item.PremiereDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), NsDc);
            }
        }

        if (filter.Contains("upnp:genre"))
        {
            foreach (var genre in item.Genres)
            {
                AddValue(writer, "upnp", "genre", genre, NsUpnp);
            }
        }

        foreach (var studio in item.Studios)
        {
            AddValue(writer, "upnp", "publisher", studio, NsUpnp);
        }

        if (item is not Folder)
        {
            if (filter.Contains("dc:description"))
            {
                var desc = item.Overview;

                if (!string.IsNullOrWhiteSpace(desc))
                {
                    AddValue(writer, "dc", "description", desc, NsDc);
                }
            }

            // if (filter.Contains("upnp:longDescription"))
            // {
            //    if (!string.IsNullOrWhiteSpace(item.Overview))
            //    {
            //        AddValue(writer, "upnp", "longDescription", item.Overview, NsUpnp);
            //    }
            // }
        }

        if (!string.IsNullOrEmpty(item.OfficialRating))
        {
            if (filter.Contains("dc:rating"))
            {
                AddValue(writer, "dc", "rating", item.OfficialRating, NsDc);
            }

            if (filter.Contains("upnp:rating"))
            {
                AddValue(writer, "upnp", "rating", item.OfficialRating, NsUpnp);
            }
        }

        AddPeople(item, writer);
    }

    private void WriteObjectClass(XmlWriter writer, BaseItem item, StubType? stubType)
    {
        // More types here
        // http://oss.linn.co.uk/repos/Public/LibUpnpCil/DidlLite/UpnpAv/Test/TestDidlLite.cs

        writer.WriteStartElement("upnp", "class", NsUpnp);

        if (item.IsDisplayedAsFolder || stubType.HasValue)
        {
            string? classType = null;

            if (!_profile.RequiresPlainFolders)
            {
                if (item is MusicAlbum)
                {
                    classType = "object.container.album.musicAlbum";
                }
                else if (item is MusicArtist)
                {
                    classType = "object.container.person.musicArtist";
                }
                else if (item is Series || item is Season || item is BoxSet || item is Video)
                {
                    classType = "object.container.album.videoAlbum";
                }
                else if (item is Playlist)
                {
                    classType = "object.container.playlistContainer";
                }
                else if (item is PhotoAlbum)
                {
                    classType = "object.container.album.photoAlbum";
                }
            }

            writer.WriteString(classType ?? "object.container.storageFolder");
        }
        else if (item.MediaType == MediaType.Audio)
        {
            writer.WriteString("object.item.audioItem.musicTrack");
        }
        else if (item.MediaType == MediaType.Photo)
        {
            writer.WriteString("object.item.imageItem.photo");
        }
        else if (item.MediaType == MediaType.Video)
        {
            if (!_profile.RequiresPlainVideoItems && item is Movie)
            {
                writer.WriteString("object.item.videoItem.movie");
            }
            else if (!_profile.RequiresPlainVideoItems && item is MusicVideo)
            {
                writer.WriteString("object.item.videoItem.musicVideoClip");
            }
            else
            {
                writer.WriteString("object.item.videoItem");
            }
        }
        else if (item is MusicGenre)
        {
            writer.WriteString(_profile.RequiresPlainFolders ? "object.container.storageFolder" : "object.container.genre.musicGenre");
        }
        else if (item is Genre)
        {
            writer.WriteString(_profile.RequiresPlainFolders ? "object.container.storageFolder" : "object.container.genre");
        }
        else
        {
            writer.WriteString("object.item");
        }

        writer.WriteFullEndElement();
    }

    private void AddPeople(BaseItem item, XmlWriter writer)
    {
        if (!item.SupportsPeople)
        {
            return;
        }

        var types = new[]
        {
            PersonKind.Director,
            PersonKind.Writer,
            PersonKind.Producer,
            PersonKind.Composer,
            PersonKind.Creator
        };

        // Seeing some LG models locking up due content with large lists of people
        // The actual issue might just be due to processing a more metadata than it can handle
        var people = _libraryManager.GetPeople(
            new InternalPeopleQuery
            {
                ItemId = item.Id,
                Limit = 6
            });

        foreach (var actor in people)
        {
            var type = types.FirstOrDefault(i => i == actor.Type || string.Equals(actor.Role, i.ToString(), StringComparison.OrdinalIgnoreCase));
            if (type == PersonKind.Unknown)
            {
                type = PersonKind.Actor;
            }

            AddValue(writer, "upnp", type.ToString().ToLowerInvariant(), actor.Name, NsUpnp);
        }
    }

    private void AddGeneralProperties(BaseItem item, StubType? itemStubType, BaseItem? context, XmlWriter writer, Filter filter, ServerItem? serverItem = null)
    {
        AddCommonFields(item, itemStubType, context, writer, filter, serverItem);

        var hasAlbumArtists = item as IHasAlbumArtist;

        if (item is IHasArtist hasArtists)
        {
            foreach (var artist in hasArtists.Artists)
            {
                AddValue(writer, "upnp", "artist", artist, NsUpnp);
                AddValue(writer, "dc", "creator", artist, NsDc);

                // If it doesn't support album artists (musicvideo), then tag as both
                if (hasAlbumArtists is null)
                {
                    AddAlbumArtist(writer, artist);
                }
            }
        }

        if (hasAlbumArtists is not null)
        {
            foreach (var albumArtist in hasAlbumArtists.AlbumArtists)
            {
                AddAlbumArtist(writer, albumArtist);
            }
        }

        if (!string.IsNullOrWhiteSpace(item.Album))
        {
            AddValue(writer, "upnp", "album", item.Album, NsUpnp);
        }

        if (item.IndexNumber.HasValue)
        {
            AddValue(writer, "upnp", "originalTrackNumber", item.IndexNumber.Value.ToString(CultureInfo.InvariantCulture), NsUpnp);

            if (item is Episode)
            {
                AddValue(writer, "upnp", "episodeNumber", item.IndexNumber.Value.ToString(CultureInfo.InvariantCulture), NsUpnp);
            }
        }
    }

    private void AddAlbumArtist(XmlWriter writer, string name)
    {
        try
        {
            writer.WriteStartElement("upnp", "artist", NsUpnp);
            writer.WriteAttributeString("role", "AlbumArtist");

            writer.WriteString(name);

            writer.WriteFullEndElement();
        }
        catch (XmlException ex)
        {
            _logger.LogError(ex, "Error adding xml value: {Value}", name);
        }
    }

    private void AddValue(XmlWriter writer, string prefix, string name, string value, string namespaceUri)
    {
        try
        {
            writer.WriteElementString(prefix, name, namespaceUri, value);
        }
        catch (XmlException ex)
        {
            _logger.LogError(ex, "Error adding xml value: {Value}", value);
        }
    }

    private void AddCover(BaseItem item, StubType? stubType, DlnaImageBrowseContext imageContext, XmlWriter writer)
    {
        var config = DlnaPlugin.Instance.Configuration;
        var resolved = DlnaImageResolver.Resolve(item, imageContext, config, _imageProcessor, _libraryManager, _logger);
        if (resolved is null)
        {
            return;
        }

        WriteCoverElements(item, stubType, resolved, writer);
    }

    private void WriteCoverElements(BaseItem? item, StubType? stubType, DlnaResolvedImage resolved, XmlWriter writer)
    {
        var albumArtUrlInfo = GetImageUrl(
            resolved,
            _profile.MaxAlbumArtWidth ?? 10000,
            _profile.MaxAlbumArtHeight ?? 10000,
            "jpg",
            out _);

        writer.WriteStartElement("upnp", "albumArtURI", NsUpnp);
        if (!string.IsNullOrEmpty(_profile.AlbumArtPn))
        {
            writer.WriteAttributeString("dlna", "profileID", NsDlna, _profile.AlbumArtPn);
        }

        writer.WriteString(albumArtUrlInfo.Url);
        writer.WriteFullEndElement();

        var iconUrlInfo = GetImageUrl(
            resolved,
            _profile.MaxIconWidth ?? 48,
            _profile.MaxIconHeight ?? 48,
            "jpg",
            out _);
        writer.WriteElementString("upnp", "icon", NsUpnp, iconUrlInfo.Url);

        if (!_profile.EnableAlbumArtInDidl)
        {
            if (item?.MediaType is MediaType.Audio or MediaType.Video)
            {
                if (!stubType.HasValue)
                {
                    return;
                }
            }
        }

        if (!_profile.EnableSingleAlbumArtLimit || item?.MediaType == MediaType.Photo)
        {
            AddImageResElement(resolved, writer, 4096, 4096, "jpg", "JPEG_LRG");
            AddImageResElement(resolved, writer, 1024, 768, "jpg", "JPEG_MED");
            AddImageResElement(resolved, writer, 640, 480, "jpg", "JPEG_SM");
            AddImageResElement(resolved, writer, 4096, 4096, "png", "PNG_LRG");
            AddImageResElement(resolved, writer, 160, 160, "png", "PNG_TN");
        }

        AddImageResElement(resolved, writer, 160, 160, "jpg", "JPEG_TN");
    }

    private (string? AlbumArtUri, string? IconUri) GetCoverUris(ServerItem serverItem, DlnaImageBrowseContext imageContext)
    {
        var config = DlnaPlugin.Instance.Configuration;
        DlnaResolvedImage? resolved = serverItem.IsSummaryBacked
            ? DlnaImageResolver.Resolve(serverItem.Summary!, imageContext, config)
            : DlnaImageResolver.Resolve(serverItem.Item, imageContext, config, _imageProcessor, _libraryManager, _logger);

        if (resolved is null)
        {
            return (null, null);
        }

        var albumArtUri = GetImageUrl(
            resolved,
            _profile.MaxAlbumArtWidth ?? 10000,
            _profile.MaxAlbumArtHeight ?? 10000,
            "jpg",
            out _).Url;
        var iconUri = GetImageUrl(
            resolved,
            _profile.MaxIconWidth ?? 48,
            _profile.MaxIconHeight ?? 48,
            "jpg",
            out _).Url;
        return (albumArtUri, iconUri);
    }

    private void AddImageResElement(
        DlnaResolvedImage imageInfo,
        XmlWriter writer,
        int maxWidth,
        int maxHeight,
        string format,
        string org_Pn)
    {
        var albumartUrlInfo = GetImageUrl(imageInfo, maxWidth, maxHeight, format, out var isDirectStream);

        writer.WriteStartElement(string.Empty, "res", NsDidl);

        var width = albumartUrlInfo.Width ?? maxWidth;
        var height = albumartUrlInfo.Height ?? maxHeight;

        var contentFeatures = ContentFeatureBuilder.BuildImageHeader(_profile, format, width, height, isDirectStream, org_Pn);

        writer.WriteAttributeString(
            "protocolInfo",
            string.Format(
                CultureInfo.InvariantCulture,
                "http-get:*:{0}:{1}",
                MimeTypes.GetMimeType("file." + format),
                contentFeatures));

        writer.WriteAttributeString(
            "resolution",
            string.Format(CultureInfo.InvariantCulture, "{0}x{1}", width, height));

        writer.WriteString(albumartUrlInfo.Url);

        writer.WriteFullEndElement();
    }

    private (string Url, int? Width, int? Height) GetImageUrl(
        DlnaResolvedImage info,
        int maxWidth,
        int maxHeight,
        string format,
        out bool isDirectStream)
    {
        var url = string.Format(
            CultureInfo.InvariantCulture,
            "{0}/Items/{1}/Images/{2}/0/{3}/{4}/{5}/{6}/0/0",
            _serverAddress,
            info.ItemId.ToString("N", CultureInfo.InvariantCulture),
            info.Type,
            info.ImageTag,
            format,
            maxWidth.ToString(CultureInfo.InvariantCulture),
            maxHeight.ToString(CultureInfo.InvariantCulture));

        var width = info.Width;
        var height = info.Height;
        isDirectStream = false;

        if (width.HasValue && height.HasValue)
        {
            var newSize = DrawingUtils.Resize(new ImageDimensions(width.Value, height.Value), 0, 0, maxWidth, maxHeight);

            width = newSize.Width;
            height = newSize.Height;

            var normalizedFormat = format
                .Replace("jpeg", "jpg", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(info.Format, normalizedFormat, StringComparison.OrdinalIgnoreCase))
            {
                isDirectStream = maxWidth >= width.Value && maxHeight >= height.Value;
            }
        }

        isDirectStream = true;

        return (url, width, height);
    }

    /// <summary>
    /// Gets the client id of an <see cref="BaseItem"/> based on the <see cref="StubType"/>.
    /// </summary>
    /// <param name="item">The <see cref="BaseItem"/>.</param>
    /// <param name="stubType">Current <see cref="StubType"/>.</param>
    /// <returns>The client id</returns>
    public static string GetClientId(BaseItem item, StubType? stubType)
    {
        return GetClientId(item.Id, stubType);
    }

    /// <summary>
    /// Gets a client id for an item, encoding library scope for genres browsed under a library folder.
    /// </summary>
    public static string GetScopedClientId(BaseItem item, StubType? stubType, BaseItem? libraryContext)
    {
        if (libraryContext is not null)
        {
            if (item is Genre)
            {
                return GetLibraryScopedGenreClientId(libraryContext.Id, item.Id);
            }

            if (item is MusicGenre)
            {
                return GetLibraryScopedMusicGenreClientId(libraryContext.Id, item.Id);
            }
        }

        return GetClientId(item, stubType);
    }

    /// <summary>
    /// Builds a DLNA object id for a genre within a specific library.
    /// </summary>
    public static string GetLibraryScopedGenreClientId(Guid libraryId, Guid genreId)
        => string.Create(CultureInfo.InvariantCulture, $"genre_{libraryId:N}_{genreId:N}");

    /// <summary>
    /// Builds a DLNA object id for a music genre within a specific library.
    /// </summary>
    public static string GetLibraryScopedMusicGenreClientId(Guid libraryId, Guid genreId)
        => string.Create(CultureInfo.InvariantCulture, $"musicgenre_{libraryId:N}_{genreId:N}");

    /// <summary>
    /// Parses a library-scoped genre object id.
    /// </summary>
    public static bool TryParseLibraryScopedGenreClientId(string id, out Guid libraryId, out Guid genreId)
    {
        libraryId = default;
        genreId = default;

        const string prefix = "genre_";
        if (!id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remainder = id[prefix.Length..];
        var divider = remainder.IndexOf('_', StringComparison.Ordinal);
        if (divider <= 0)
        {
            return false;
        }

        return Guid.TryParse(remainder[..divider], out libraryId)
            && Guid.TryParse(remainder[(divider + 1)..], out genreId);
    }

    /// <summary>
    /// Parses a library-scoped music genre object id.
    /// </summary>
    public static bool TryParseLibraryScopedMusicGenreClientId(string id, out Guid libraryId, out Guid genreId)
    {
        libraryId = default;
        genreId = default;

        const string prefix = "musicgenre_";
        if (!id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remainder = id[prefix.Length..];
        var divider = remainder.IndexOf('_', StringComparison.Ordinal);
        if (divider <= 0)
        {
            return false;
        }

        return Guid.TryParse(remainder[..divider], out libraryId)
            && Guid.TryParse(remainder[(divider + 1)..], out genreId);
    }

    /// <summary>
    /// Gets the client id for a <see cref="ServerItem"/>.
    /// </summary>
    internal static string GetClientId(ServerItem serverItem)
    {
        if (serverItem.IsSummaryBacked)
        {
            return GetClientId(serverItem.Summary!.ItemId, null);
        }

        if (serverItem.StubType == StubType.BrowseByKanaRow
            && serverItem.TitleBrowseGroupId is string groupId
            && serverItem.LibraryScopeId is Guid titleLibraryId)
        {
            return GetTitleBrowseGroupClientId(titleLibraryId, groupId);
        }

        if (serverItem.StubType == StubType.BrowseByKanaRow
            && serverItem.KanaRowIndex is int rowIndex
            && serverItem.LibraryScopeId is Guid kanaLibraryId)
        {
            return GetKanaRowClientId(kanaLibraryId, rowIndex);
        }

        if (serverItem.StubType == StubType.BrowseByYearItem
            && serverItem.ProductionYear is int year
            && serverItem.LibraryScopeId is Guid yearLibraryId)
        {
            return GetYearClientId(yearLibraryId, year);
        }

        if (serverItem.StubType is StubType.StudioItem or StubType.TagItem or StubType.RatingItem or StubType.PersonItem
            && serverItem.LibraryScopeId is Guid facetLibraryId
            && !string.IsNullOrEmpty(serverItem.FacetKey))
        {
            return GetFacetClientId(serverItem.StubType.Value, facetLibraryId, serverItem.FacetKey);
        }

        if (serverItem.StubType == StubType.SeriesRange
            && serverItem.LibraryScopeId is Guid rangeLibraryId
            && serverItem.RangeStart is int rangeStart
            && serverItem.RangeEnd is int rangeEnd)
        {
            return GetSeriesRangeClientId(rangeLibraryId, rangeStart, rangeEnd);
        }

        if (serverItem.LibraryScopeId is Guid scopeId
            && serverItem.Item is Genre)
        {
            return GetLibraryScopedGenreClientId(scopeId, serverItem.Item.Id);
        }

        if (serverItem.LibraryScopeId is Guid musicScopeId
            && serverItem.Item is MusicGenre)
        {
            return GetLibraryScopedMusicGenreClientId(musicScopeId, serverItem.Item.Id);
        }

        return GetClientId(serverItem.Item.Id, serverItem.StubType);
    }

    /// <summary>
    /// Builds a DLNA object id for a title browse group within a library.
    /// </summary>
    public static string GetTitleBrowseGroupClientId(Guid libraryId, string groupId)
        => string.Create(CultureInfo.InvariantCulture, $"titlegroup_{libraryId:N}_{groupId}");

    /// <summary>
    /// Builds a DLNA object id for a kana row within a library.
    /// </summary>
    public static string GetKanaRowClientId(Guid libraryId, int rowIndex)
        => string.Create(CultureInfo.InvariantCulture, $"kanarow_{libraryId:N}_{rowIndex}");

    /// <summary>
    /// Parses a title browse group object id.
    /// </summary>
    public static bool TryParseTitleBrowseGroupClientId(string id, out Guid libraryId, out string groupId)
    {
        libraryId = default;
        groupId = string.Empty;

        const string prefix = "titlegroup_";
        if (!id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remainder = id[prefix.Length..];
        var divider = remainder.IndexOf('_', StringComparison.Ordinal);
        if (divider <= 0)
        {
            return false;
        }

        if (!Guid.TryParse(remainder[..divider], out libraryId))
        {
            return false;
        }

        groupId = remainder[(divider + 1)..];
        return !string.IsNullOrEmpty(groupId);
    }

    /// <summary>
    /// Builds a DLNA object id for a production year within a library.
    /// </summary>
    public static string GetYearClientId(Guid libraryId, int year)
        => string.Create(CultureInfo.InvariantCulture, $"year_{libraryId:N}_{year}");

    /// <summary>
    /// Builds a DLNA object id for a facet folder within a library.
    /// </summary>
    public static string GetFacetClientId(StubType facetStubType, Guid libraryId, string facetKey)
    {
        var prefix = facetStubType switch
        {
            StubType.StudioItem => "studio",
            StubType.TagItem => "tag",
            StubType.RatingItem => "rating",
            StubType.PersonItem => "person",
            _ => "facet"
        };

        return string.Create(CultureInfo.InvariantCulture, $"{prefix}_{libraryId:N}_{facetKey}");
    }

    /// <summary>
    /// Builds a DLNA object id for a series range folder.
    /// </summary>
    public static string GetSeriesRangeClientId(Guid libraryId, int rangeStart, int rangeEnd)
        => string.Create(CultureInfo.InvariantCulture, $"range_{libraryId:N}_{rangeStart}_{rangeEnd}");

    /// <summary>
    /// Parses a kana row object id.
    /// </summary>
    public static bool TryParseKanaRowClientId(string id, out Guid libraryId, out int rowIndex)
    {
        libraryId = default;
        rowIndex = -1;

        const string prefix = "kanarow_";
        if (!id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remainder = id[prefix.Length..];
        var divider = remainder.IndexOf('_', StringComparison.Ordinal);
        if (divider <= 0)
        {
            return false;
        }

        if (!Guid.TryParse(remainder[..divider], out libraryId))
        {
            return false;
        }

        return int.TryParse(remainder[(divider + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out rowIndex)
            && rowIndex >= 0;
    }

    /// <summary>
    /// Parses a year browse object id.
    /// </summary>
    public static bool TryParseYearClientId(string id, out Guid libraryId, out int year)
    {
        libraryId = default;
        year = 0;

        const string prefix = "year_";
        if (!id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remainder = id[prefix.Length..];
        var divider = remainder.IndexOf('_', StringComparison.Ordinal);
        if (divider <= 0)
        {
            return false;
        }

        return Guid.TryParse(remainder[..divider], out libraryId)
            && int.TryParse(remainder[(divider + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out year);
    }

    /// <summary>
    /// Parses a facet browse object id.
    /// </summary>
    public static bool TryParseFacetClientId(string id, out Guid libraryId, out StubType facetStubType, out string facetKey)
    {
        libraryId = default;
        facetStubType = default;
        facetKey = string.Empty;

        string? prefix = null;
        if (id.StartsWith("studio_", StringComparison.OrdinalIgnoreCase))
        {
            prefix = "studio_";
            facetStubType = StubType.StudioItem;
        }
        else if (id.StartsWith("tag_", StringComparison.OrdinalIgnoreCase))
        {
            prefix = "tag_";
            facetStubType = StubType.TagItem;
        }
        else if (id.StartsWith("rating_", StringComparison.OrdinalIgnoreCase))
        {
            prefix = "rating_";
            facetStubType = StubType.RatingItem;
        }
        else if (id.StartsWith("person_", StringComparison.OrdinalIgnoreCase))
        {
            prefix = "person_";
            facetStubType = StubType.PersonItem;
        }

        if (prefix is null)
        {
            return false;
        }

        var remainder = id[prefix.Length..];
        var divider = remainder.IndexOf('_', StringComparison.Ordinal);
        if (divider <= 0 || divider >= remainder.Length - 1)
        {
            return false;
        }

        if (!Guid.TryParse(remainder[..divider], out libraryId))
        {
            return false;
        }

        facetKey = remainder[(divider + 1)..];
        return facetKey.Length > 0;
    }

    /// <summary>
    /// Parses a series range browse object id.
    /// </summary>
    public static bool TryParseSeriesRangeClientId(string id, out Guid libraryId, out int rangeStart, out int rangeEnd)
    {
        libraryId = default;
        rangeStart = 0;
        rangeEnd = 0;

        const string prefix = "range_";
        if (!id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remainder = id[prefix.Length..];
        var firstDivider = remainder.IndexOf('_', StringComparison.Ordinal);
        if (firstDivider <= 0)
        {
            return false;
        }

        if (!Guid.TryParse(remainder[..firstDivider], out libraryId))
        {
            return false;
        }

        var tail = remainder[(firstDivider + 1)..];
        var secondDivider = tail.IndexOf('_', StringComparison.Ordinal);
        if (secondDivider <= 0)
        {
            return false;
        }

        return int.TryParse(tail[..secondDivider], NumberStyles.Integer, CultureInfo.InvariantCulture, out rangeStart)
            && int.TryParse(tail[(secondDivider + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out rangeEnd)
            && rangeEnd > rangeStart;
    }

    /// <summary>
    /// Gets the client id of an <see cref="Guid"/> based on the <see cref="StubType"/>.
    /// </summary>
    /// <param name="idValue">The <see cref="Guid"/>.</param>
    /// <param name="stubType">Current <see cref="StubType"/>.</param>
    /// <returns>The client id</returns>
    public static string GetClientId(Guid idValue, StubType? stubType)
    {
        var id = idValue.ToString("N", CultureInfo.InvariantCulture);

        if (stubType.HasValue)
        {
            id = stubType.Value.ToString().ToLowerInvariant() + "_" + id;
        }

        return id;
    }
}
