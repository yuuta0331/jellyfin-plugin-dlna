using System;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Dlna.Didl;

/// <summary>
/// Resolved DLNA cover image metadata.
/// </summary>
public sealed class DlnaResolvedImage
{
    /// <summary>
    /// Gets or sets the item id that owns the image.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the image type.
    /// </summary>
    public ImageType Type { get; set; }

    /// <summary>
    /// Gets or sets the image cache tag.
    /// </summary>
    public string? ImageTag { get; set; }

    /// <summary>
    /// Gets or sets the image width.
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Gets or sets the image height.
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Gets or sets the image file format extension (jpg/png).
    /// </summary>
    public string Format { get; set; } = "jpg";
}
