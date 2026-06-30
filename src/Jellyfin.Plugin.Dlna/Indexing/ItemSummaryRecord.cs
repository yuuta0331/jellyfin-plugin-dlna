using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Lightweight item metadata stored in the virtual index.
/// </summary>
public sealed class ItemSummaryRecord
{
    /// <summary>
    /// Gets or sets the item id.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the item type.
    /// </summary>
    public BaseItemKind ItemType { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sort name.
    /// </summary>
    public string SortName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent id.
    /// </summary>
    public Guid ParentId { get; set; }

    /// <summary>
    /// Gets or sets the production year.
    /// </summary>
    public int? ProductionYear { get; set; }

    /// <summary>
    /// Gets or sets the date created ticks.
    /// </summary>
    public long DateCreatedTicks { get; set; }

    /// <summary>
    /// Gets or sets the premiere date ticks.
    /// </summary>
    public long? PremiereDateTicks { get; set; }

    /// <summary>
    /// Gets or sets the index number (season/episode).
    /// </summary>
    public int? IndexNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item is displayed as a folder.
    /// </summary>
    public bool IsFolder { get; set; }

    /// <summary>
    /// Gets or sets the date modified ticks.
    /// </summary>
    public long? DateModifiedTicks { get; set; }

    /// <summary>
    /// Gets or sets the item id that owns the primary image.
    /// </summary>
    public Guid? PrimaryImageItemId { get; set; }

    /// <summary>
    /// Gets or sets the primary image cache tag.
    /// </summary>
    public string? PrimaryImageTag { get; set; }

    /// <summary>
    /// Gets or sets the primary image width.
    /// </summary>
    public int? PrimaryWidth { get; set; }

    /// <summary>
    /// Gets or sets the primary image height.
    /// </summary>
    public int? PrimaryHeight { get; set; }

    /// <summary>
    /// Gets or sets the item id that owns the thumbnail image.
    /// </summary>
    public Guid? ThumbImageItemId { get; set; }

    /// <summary>
    /// Gets or sets the thumbnail image cache tag.
    /// </summary>
    public string? ThumbImageTag { get; set; }

    /// <summary>
    /// Gets or sets the thumbnail image width.
    /// </summary>
    public int? ThumbWidth { get; set; }

    /// <summary>
    /// Gets or sets the thumbnail image height.
    /// </summary>
    public int? ThumbHeight { get; set; }

    /// <summary>
    /// Gets or sets the media runtime in ticks.
    /// </summary>
    public long? RunTimeTicks { get; set; }

    /// <summary>
    /// Gets or sets the media file size in bytes.
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Gets or sets the media container (mkv, mp4, etc.).
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// Gets or sets the video width.
    /// </summary>
    public int? VideoWidth { get; set; }

    /// <summary>
    /// Gets or sets the video height.
    /// </summary>
    public int? VideoHeight { get; set; }

    /// <summary>
    /// Gets or sets the total media bitrate.
    /// </summary>
    public int? TotalBitrate { get; set; }

    /// <summary>
    /// Gets or sets the primary video codec.
    /// </summary>
    public string? VideoCodec { get; set; }

    /// <summary>
    /// Gets or sets the primary audio codec.
    /// </summary>
    public string? AudioCodec { get; set; }

    /// <summary>
    /// Gets or sets the media source id.
    /// </summary>
    public string? MediaSourceId { get; set; }

    /// <summary>
    /// Gets or sets the media source etag.
    /// </summary>
    public string? MediaSourceTag { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether direct play is supported for the indexed profile.
    /// </summary>
    public bool SupportsDirectPlay { get; set; }
}

/// <summary>
/// Facet key with item count.
/// </summary>
public readonly record struct FacetKeyCount(string Key, int Count);
