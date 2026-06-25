using System;
using System.IO;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Indexing;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using Episode = MediaBrowser.Controller.Entities.TV.Episode;
using MusicAlbum = MediaBrowser.Controller.Entities.Audio.MusicAlbum;

namespace Jellyfin.Plugin.Dlna.Didl;

/// <summary>
/// Selects poster vs thumbnail images for DLNA browse output.
/// </summary>
public static class DlnaImageResolver
{
    /// <summary>
    /// Resolves an image for a Jellyfin item.
    /// </summary>
    public static DlnaResolvedImage? Resolve(
        BaseItem item,
        DlnaImageBrowseContext context,
        DlnaPluginConfiguration config,
        IImageProcessor imageProcessor,
        ILibraryManager libraryManager,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(imageProcessor);
        ArgumentNullException.ThrowIfNull(libraryManager);

        var presentation = GetEffectivePresentation(item.GetBaseItemKind(), context, config);
        return ResolveFromItem(item, presentation, context, config, imageProcessor, libraryManager, logger);
    }

    /// <summary>
    /// Resolves an image from indexed summary metadata.
    /// </summary>
    public static DlnaResolvedImage? Resolve(
        ItemSummaryRecord summary,
        DlnaImageBrowseContext context,
        DlnaPluginConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(config);

        var presentation = GetEffectivePresentation(summary.ItemType, context, config);
        return ResolveFromSummary(summary, presentation, context, config);
    }

    /// <summary>
    /// Collects primary and thumbnail metadata for index storage.
    /// </summary>
    public static void PopulateSummaryImages(
        BaseItem item,
        ItemSummaryRecord summary,
        IImageProcessor imageProcessor,
        ILibraryManager libraryManager,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(imageProcessor);
        ArgumentNullException.ThrowIfNull(libraryManager);

        PopulateImageSlot(item, ImageType.Primary, imageProcessor, logger, out var primaryItemId, out var primaryTag, out var primaryWidth, out var primaryHeight);
        if (!string.IsNullOrEmpty(primaryTag))
        {
            summary.PrimaryImageItemId = primaryItemId;
            summary.PrimaryImageTag = primaryTag;
            summary.PrimaryWidth = primaryWidth;
            summary.PrimaryHeight = primaryHeight;
        }

        PopulateImageSlot(item, ImageType.Thumb, imageProcessor, logger, out var thumbItemId, out var thumbTag, out var thumbWidth, out var thumbHeight);
        if (!string.IsNullOrEmpty(thumbTag))
        {
            summary.ThumbImageItemId = thumbItemId;
            summary.ThumbImageTag = thumbTag;
            summary.ThumbWidth = thumbWidth;
            summary.ThumbHeight = thumbHeight;
        }

        if (item is Episode episode && episode.SeriesId != Guid.Empty)
        {
            var series = libraryManager.GetItemById(episode.SeriesId);
            if (series is not null)
            {
                if (string.IsNullOrEmpty(summary.PrimaryImageTag))
                {
                    PopulateImageSlot(series, ImageType.Primary, imageProcessor, logger, out var seriesPrimaryItemId, out var seriesPrimaryTag, out var seriesPrimaryWidth, out var seriesPrimaryHeight);
                    if (!string.IsNullOrEmpty(seriesPrimaryTag))
                    {
                        summary.PrimaryImageItemId = seriesPrimaryItemId;
                        summary.PrimaryImageTag = seriesPrimaryTag;
                        summary.PrimaryWidth = seriesPrimaryWidth;
                        summary.PrimaryHeight = seriesPrimaryHeight;
                    }
                }

                if (string.IsNullOrEmpty(summary.ThumbImageTag))
                {
                    PopulateImageSlot(series, ImageType.Thumb, imageProcessor, logger, out var seriesThumbItemId, out var seriesThumbTag, out var seriesThumbWidth, out var seriesThumbHeight);
                    if (!string.IsNullOrEmpty(seriesThumbTag))
                    {
                        summary.ThumbImageItemId = seriesThumbItemId;
                        summary.ThumbImageTag = seriesThumbTag;
                        summary.ThumbWidth = seriesThumbWidth;
                        summary.ThumbHeight = seriesThumbHeight;
                    }
                    else if (string.IsNullOrEmpty(summary.ThumbImageTag) && !string.IsNullOrEmpty(summary.PrimaryImageTag))
                    {
                        summary.ThumbImageItemId = summary.PrimaryImageItemId;
                        summary.ThumbImageTag = summary.PrimaryImageTag;
                        summary.ThumbWidth = summary.PrimaryWidth;
                        summary.ThumbHeight = summary.PrimaryHeight;
                    }
                }
            }
        }
    }

    private static DlnaResolvedImage? ResolveFromItem(
        BaseItem item,
        DlnaImagePresentation presentation,
        DlnaImageBrowseContext context,
        DlnaPluginConfiguration config,
        IImageProcessor imageProcessor,
        ILibraryManager libraryManager,
        ILogger? logger)
    {
        if (item is Audio audioItem)
        {
            var album = audioItem.AlbumEntity;
            if (album is not null)
            {
                var albumImage = TryCreateImage(album, ImageType.Primary, imageProcessor, logger)
                    ?? TryCreateImage(album, ImageType.Thumb, imageProcessor, logger);
                if (albumImage is not null)
                {
                    return albumImage;
                }
            }
        }

        var preferred = presentation == DlnaImagePresentation.Poster ? ImageType.Primary : ImageType.Thumb;
        var fallback = presentation == DlnaImagePresentation.Poster ? ImageType.Thumb : ImageType.Primary;

        if (item is Episode episodeForSeriesPreference
            && context == DlnaImageBrowseContext.EpisodeList
            && config.EpisodeListImageSource == EpisodeListImageSource.Series
            && episodeForSeriesPreference.SeriesId != Guid.Empty)
        {
            var series = libraryManager.GetItemById(episodeForSeriesPreference.SeriesId);
            if (series is not null)
            {
                var seriesResolved = TryCreateImage(series, preferred, imageProcessor, logger)
                    ?? TryCreateImage(series, fallback, imageProcessor, logger);
                if (seriesResolved is not null)
                {
                    return seriesResolved;
                }
            }
        }

        var resolved = TryCreateImage(item, preferred, imageProcessor, logger)
            ?? TryCreateImage(item, fallback, imageProcessor, logger);

        if (resolved is not null)
        {
            return resolved;
        }

        if (item.HasImage(ImageType.Backdrop) && item is Channel)
        {
            return TryCreateImage(item, ImageType.Backdrop, imageProcessor, logger);
        }

        if (item is MusicAlbum or Playlist)
        {
            return null;
        }

        var parentWithImage = GetFirstParentWithImageBelowUserRoot(item);
        if (parentWithImage is not null)
        {
            return TryCreateImage(parentWithImage, ImageType.Primary, imageProcessor, logger)
                ?? TryCreateImage(parentWithImage, ImageType.Thumb, imageProcessor, logger);
        }

        if (item is Episode episode && episode.SeriesId != Guid.Empty)
        {
            var series = libraryManager.GetItemById(episode.SeriesId);
            if (series is not null)
            {
                return ResolveFromItem(series, presentation, context, config, imageProcessor, libraryManager, logger);
            }
        }

        return null;
    }

    private static DlnaResolvedImage? ResolveFromSummary(
        ItemSummaryRecord summary,
        DlnaImagePresentation presentation,
        DlnaImageBrowseContext context,
        DlnaPluginConfiguration config)
    {
        var preferPrimary = presentation == DlnaImagePresentation.Poster;

        if (summary.ItemType == BaseItemKind.Episode && context == DlnaImageBrowseContext.EpisodeList)
        {
            if (config.EpisodeListImageSource == EpisodeListImageSource.Episode)
            {
                return ResolveFromSummarySlots(summary, preferPrimary, summary.ItemId, requireOwnedByItem: true)
                    ?? ResolveFromSummarySlots(summary, preferPrimary, summary.ItemId, requireOwnedByItem: null);
            }

            return ResolveFromSummarySlots(summary, preferPrimary, summary.ItemId, requireOwnedByItem: false)
                ?? ResolveFromSummarySlots(summary, preferPrimary, summary.ItemId, requireOwnedByItem: true);
        }

        return ResolveFromSummarySlots(summary, preferPrimary, summary.ItemId, requireOwnedByItem: null);
    }

    private static DlnaResolvedImage? ResolveFromSummarySlots(
        ItemSummaryRecord summary,
        bool preferPrimary,
        Guid ownerItemId,
        bool? requireOwnedByItem)
    {
        if (preferPrimary)
        {
            return CreateFromSummarySlotIfMatches(
                    ownerItemId,
                    requireOwnedByItem,
                    summary.PrimaryImageItemId,
                    summary.PrimaryImageTag,
                    ImageType.Primary,
                    summary.PrimaryWidth,
                    summary.PrimaryHeight)
                ?? CreateFromSummarySlotIfMatches(
                    ownerItemId,
                    requireOwnedByItem,
                    summary.ThumbImageItemId,
                    summary.ThumbImageTag,
                    ImageType.Thumb,
                    summary.ThumbWidth,
                    summary.ThumbHeight);
        }

        return CreateFromSummarySlotIfMatches(
                ownerItemId,
                requireOwnedByItem,
                summary.ThumbImageItemId,
                summary.ThumbImageTag,
                ImageType.Thumb,
                summary.ThumbWidth,
                summary.ThumbHeight)
            ?? CreateFromSummarySlotIfMatches(
                ownerItemId,
                requireOwnedByItem,
                summary.PrimaryImageItemId,
                summary.PrimaryImageTag,
                ImageType.Primary,
                summary.PrimaryWidth,
                summary.PrimaryHeight);
    }

    private static DlnaResolvedImage? CreateFromSummarySlotIfMatches(
        Guid ownerItemId,
        bool? requireOwnedByItem,
        Guid? imageItemId,
        string? tag,
        ImageType type,
        int? width,
        int? height)
    {
        if (!MatchesOwnerFilter(imageItemId, ownerItemId, requireOwnedByItem))
        {
            return null;
        }

        return CreateFromSummarySlot(imageItemId, tag, type, width, height);
    }

    private static bool MatchesOwnerFilter(Guid? imageItemId, Guid ownerItemId, bool? requireOwnedByItem)
    {
        if (!imageItemId.HasValue || imageItemId.Value == Guid.Empty)
        {
            return false;
        }

        if (requireOwnedByItem is null)
        {
            return true;
        }

        return requireOwnedByItem.Value
            ? imageItemId.Value == ownerItemId
            : imageItemId.Value != ownerItemId;
    }

    private static DlnaResolvedImage? CreateFromSummarySlot(
        Guid? itemId,
        string? tag,
        ImageType type,
        int? width,
        int? height)
    {
        if (!itemId.HasValue || itemId.Value == Guid.Empty || string.IsNullOrEmpty(tag))
        {
            return null;
        }

        return new DlnaResolvedImage
        {
            ItemId = itemId.Value,
            Type = type,
            ImageTag = tag,
            Width = width,
            Height = height,
            Format = "jpg"
        };
    }

    private static void PopulateImageSlot(
        BaseItem item,
        ImageType type,
        IImageProcessor imageProcessor,
        ILogger? logger,
        out Guid itemId,
        out string? tag,
        out int? width,
        out int? height)
    {
        itemId = item.Id;
        tag = null;
        width = null;
        height = null;

        if (!item.HasImage(type))
        {
            return;
        }

        var resolved = TryCreateImage(item, type, imageProcessor, logger);
        if (resolved is null)
        {
            return;
        }

        itemId = resolved.ItemId;
        tag = resolved.ImageTag;
        width = resolved.Width;
        height = resolved.Height;
    }

    private static DlnaResolvedImage? TryCreateImage(BaseItem item, ImageType type, IImageProcessor imageProcessor, ILogger? logger)
    {
        if (!item.HasImage(type))
        {
            return null;
        }

        var imageInfo = item.GetImageInfo(type, 0);
        string? tag = null;
        try
        {
            tag = imageProcessor.GetImageCacheTag(item, type);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting image cache tag for {ItemId} {ImageType}", item.Id, type);
        }

        if (string.IsNullOrEmpty(tag))
        {
            return null;
        }

        int? width = imageInfo.Width;
        int? height = imageInfo.Height;
        if (width is 0 or -1)
        {
            width = null;
        }

        if (height is 0 or -1)
        {
            height = null;
        }

        var inputFormat = (Path.GetExtension(imageInfo.Path) ?? string.Empty)
            .TrimStart('.')
            .Replace("jpeg", "jpg", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(inputFormat))
        {
            inputFormat = "jpg";
        }

        return new DlnaResolvedImage
        {
            ItemId = item.Id,
            Type = type,
            ImageTag = tag,
            Width = width,
            Height = height,
            Format = inputFormat
        };
    }

    private static BaseItem? GetFirstParentWithImageBelowUserRoot(BaseItem item)
    {
        if (item.HasImage(ImageType.Primary))
        {
            return item;
        }

        var parent = item.GetParent();
        if (parent is UserRootFolder)
        {
            return null;
        }

        if (parent is Folder folder && folder.IsRoot)
        {
            return null;
        }

        return parent is null ? null : GetFirstParentWithImageBelowUserRoot(parent);
    }

    private static DlnaImagePresentation GetEffectivePresentation(
        BaseItemKind itemType,
        DlnaImageBrowseContext context,
        DlnaPluginConfiguration config)
    {
        if (itemType == BaseItemKind.Episode)
        {
            return DlnaImagePresentation.Thumbnail;
        }

        if (itemType == BaseItemKind.Season)
        {
            return DlnaImagePresentation.Poster;
        }

        if (itemType is BaseItemKind.MusicAlbum or BaseItemKind.MusicArtist or BaseItemKind.BoxSet)
        {
            return DlnaImagePresentation.Poster;
        }

        if (itemType is BaseItemKind.Movie or BaseItemKind.Series)
        {
            return context switch
            {
                DlnaImageBrowseContext.VirtualList => config.VirtualListImagePresentation,
                DlnaImageBrowseContext.Search => config.SearchImagePresentation,
                _ => DlnaImagePresentation.Poster
            };
        }

        return context switch
        {
            DlnaImageBrowseContext.VirtualList => config.VirtualListImagePresentation,
            DlnaImageBrowseContext.Search => config.SearchImagePresentation,
            DlnaImageBrowseContext.EpisodeList => DlnaImagePresentation.Thumbnail,
            DlnaImageBrowseContext.SeasonList => DlnaImagePresentation.Poster,
            DlnaImageBrowseContext.MusicList => DlnaImagePresentation.Poster,
            _ => DlnaImagePresentation.Thumbnail
        };
    }
}
