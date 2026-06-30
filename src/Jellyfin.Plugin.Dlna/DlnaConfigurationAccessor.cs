using Jellyfin.Plugin.Dlna.Configuration;

namespace Jellyfin.Plugin.Dlna;

/// <summary>
/// Provides access to the active DLNA plugin configuration.
/// </summary>
internal static class DlnaConfigurationAccessor
{
    /// <summary>
    /// Gets the current plugin configuration.
    /// </summary>
    public static DlnaPluginConfiguration Current
        => DlnaPlugin.Instance?.Configuration ?? new DlnaPluginConfiguration();
}
