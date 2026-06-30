namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Resolved device profile and playback flags for DLNA streaming.
/// </summary>
public sealed class DlnaStreamingProfileContext
{
    /// <summary>
    /// Gets the device profile to use for stream negotiation.
    /// </summary>
    public required DlnaDeviceProfile Profile { get; init; }

    /// <summary>
    /// Gets a value indicating whether direct play should be forced regardless of codec compatibility.
    /// </summary>
    public bool ForceDirectPlay { get; init; }

    /// <summary>
    /// Gets the active playback mode.
    /// </summary>
    public DlnaPlaybackMode PlaybackMode { get; init; }
}
