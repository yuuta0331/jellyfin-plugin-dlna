using System.Threading;
using Jellyfin.Plugin.Dlna.Configuration;

namespace Jellyfin.Plugin.Dlna;

/// <summary>
/// Thread-safe cache of <see cref="DlnaPluginConfiguration.EnableDebugLogging"/>.
/// </summary>
public sealed class DlnaDebugLoggingState
{
    private int _isEnabled;

    /// <summary>
    /// Gets the active singleton instance registered in DI.
    /// </summary>
    public static DlnaDebugLoggingState? Current { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaDebugLoggingState"/> class.
    /// </summary>
    public DlnaDebugLoggingState()
    {
        Current = this;
    }

    /// <summary>
    /// Gets a value indicating whether verbose DLNA debug logging is enabled.
    /// </summary>
    public bool IsEnabled => Volatile.Read(ref _isEnabled) != 0;

    /// <summary>
    /// Synchronizes the cached flag from plugin configuration.
    /// </summary>
    /// <param name="configuration">The plugin configuration.</param>
    public void SyncFrom(DlnaPluginConfiguration configuration)
    {
        var enabled = configuration.EnableDebugLogging;
        Volatile.Write(ref _isEnabled, enabled ? 1 : 0);
    }
}
