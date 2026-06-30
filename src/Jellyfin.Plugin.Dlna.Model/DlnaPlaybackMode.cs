namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Controls how DLNA playback streams are negotiated.
/// </summary>
public enum DlnaPlaybackMode
{
    /// <summary>
    /// Jellyfin default: DirectPlay, then DirectStream (remux), then Transcode.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Prefer direct play; remux is disabled but transcoding may still be used.
    /// </summary>
    PreferDirectPlay = 1,

    /// <summary>
    /// Only true direct play is allowed; incompatible items omit playback URLs.
    /// </summary>
    DirectPlayOnly = 2
}
