using System;
using Jellyfin.Plugin.Dlna.Configuration;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Resolves DLNA Browse paging from plugin configuration and request parameters.
/// </summary>
public static class BrowsePagingResolver
{
    /// <summary>
    /// Resolves browse paging for a DLNA Browse request.
    /// </summary>
    /// <param name="config">The plugin configuration.</param>
    /// <param name="requestedCount">The client RequestedCount, if any.</param>
    /// <param name="startIndex">The client StartingIndex, if any.</param>
    /// <returns>The resolved paging context.</returns>
    public static BrowsePagingContext Resolve(DlnaPluginConfiguration config, int? requestedCount, int? startIndex)
    {
        ArgumentNullException.ThrowIfNull(config);

        var quest = config.EnableQuestCompatibilityMode;
        var respect = !quest && config.RespectRequestedCount;
        var strict = !quest && config.EnableStrictTotalMatches;
        var maxBrowse = config.MaxBrowseItemsPerResponse;

        int? limit = null;
        if (respect)
        {
            if (requestedCount.HasValue && requestedCount.Value > 0)
            {
                limit = Math.Min(requestedCount.Value, maxBrowse);
            }
            else
            {
                limit = maxBrowse;
            }
        }

        return new BrowsePagingContext
        {
            StartIndex = startIndex ?? 0,
            Limit = limit,
            StrictTotalMatches = strict
        };
    }
}
