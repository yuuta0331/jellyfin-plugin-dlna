namespace Jellyfin.Plugin.Dlna.Configuration;

/// <summary>
/// Preferred image style for DLNA browse output.
/// </summary>
public enum DlnaImagePresentation
{
    /// <summary>
    /// Use poster images (Primary).
    /// </summary>
    Poster = 0,

    /// <summary>
    /// Use thumbnail images (Thumb).
    /// </summary>
    Thumbnail = 1
}
