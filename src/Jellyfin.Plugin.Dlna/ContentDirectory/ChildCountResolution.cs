using System;
using Jellyfin.Plugin.Dlna.Configuration;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Resolves folder childCount values for DLNA Browse without unnecessary queries.
/// </summary>
public static class ChildCountResolution
{
    /// <summary>
    /// Resolves the childCount for a folder without performing library queries.
    /// </summary>
    /// <param name="config">The plugin configuration.</param>
    /// <param name="isStubFolder">Whether the folder is a virtual stub.</param>
    /// <param name="cachedCount">A cached count, if available.</param>
    /// <returns>The childCount to emit, or null to omit the attribute.</returns>
    public static int? ResolveWithoutQuery(
        DlnaPluginConfiguration config,
        bool isStubFolder,
        int? cachedCount)
    {
        ArgumentNullException.ThrowIfNull(config);

        var mode = GetEffectiveChildCountMode(config);

        if (mode == ChildCountMode.Disabled)
        {
            return null;
        }

        if (isStubFolder)
        {
            return mode == ChildCountMode.Estimate ? 0 : null;
        }

        if (mode == ChildCountMode.Estimate && config.EnableChildCountCache)
        {
            return cachedCount;
        }

        if (mode == ChildCountMode.Estimate)
        {
            return null;
        }

        return null;
    }

    /// <summary>
    /// Returns whether an accurate childCount requires a library query.
    /// </summary>
    /// <param name="config">The plugin configuration.</param>
    /// <param name="isStubFolder">Whether the folder is a virtual stub.</param>
    /// <returns>True when a query is required.</returns>
    public static bool RequiresAccurateQuery(DlnaPluginConfiguration config, bool isStubFolder)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (isStubFolder)
        {
            return false;
        }

        return GetEffectiveChildCountMode(config) == ChildCountMode.Accurate;
    }

    /// <summary>
    /// Gets the effective childCount mode, applying Quest compatibility defaults.
    /// </summary>
    /// <param name="config">The plugin configuration.</param>
    /// <returns>The effective mode.</returns>
    public static ChildCountMode GetEffectiveChildCountMode(DlnaPluginConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.EnableQuestCompatibilityMode)
        {
            return ChildCountMode.Disabled;
        }

        return config.ChildCountCalculation;
    }
}
