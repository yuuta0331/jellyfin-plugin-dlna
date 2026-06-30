using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Session;
using StreamInfo = MediaBrowser.Model.Dlna.StreamInfo;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// A resolved DLNA playback stream with its Jellyfin play method.
/// </summary>
#pragma warning disable CA1711
public sealed class DlnaResolvedStream
#pragma warning restore CA1711
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaResolvedStream"/> class.
    /// </summary>
    /// <param name="streamInfo">The resolved stream info.</param>
    public DlnaResolvedStream(StreamInfo streamInfo)
    {
        StreamInfo = streamInfo;
    }

    /// <summary>
    /// Gets the resolved stream info.
    /// </summary>
    public StreamInfo StreamInfo { get; }

    /// <summary>
    /// Gets a value indicating whether Jellyfin selected true direct play.
    /// </summary>
    public bool IsDirectPlay => StreamInfo.PlayMethod == PlayMethod.DirectPlay;
}
