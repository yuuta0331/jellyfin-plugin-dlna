using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Dlna.Playback;

/// <summary>
/// Result of evaluating playback-mode guards before the transcode path is entered.
/// </summary>
public sealed class EarlyPlaybackGuardResult
{
    /// <summary>
    /// Gets a response that should be returned immediately, if any.
    /// </summary>
    public ActionResult? EarlyResponse { get; init; }

    /// <summary>
    /// Gets a value indicating whether streaming state should be rebuilt after upgrading to static playback.
    /// </summary>
    public bool RequiresStateRefresh { get; init; }
}
