using Jellyfin.Plugin.Dlna.Model;

namespace Jellyfin.Plugin.Dlna.Maintenance;

/// <summary>
/// Coordinates DLNA storage and cache maintenance operations.
/// </summary>
public interface IDlnaStorageMaintenanceService
{
    /// <summary>
    /// Gets a value indicating whether maintenance is running.
    /// </summary>
    bool IsMaintenanceRunning { get; }

    /// <summary>
    /// Gets aggregated storage statistics.
    /// </summary>
    /// <returns>Storage statistics.</returns>
    DlnaStorageStatsDto GetStats();

    /// <summary>
    /// Clears the browse response cache.
    /// </summary>
    void ClearBrowseCache();

    /// <summary>
    /// Clears the child count cache.
    /// </summary>
    void ClearChildCountCache();

    /// <summary>
    /// Clears the virtual index database.
    /// </summary>
    void ClearIndex();

    /// <summary>
    /// Clears all caches and the index without rebuilding.
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Starts an index rebuild in the background.
    /// </summary>
    /// <param name="prewarm">Whether to prewarm browse responses after rebuilding.</param>
    /// <returns>True when the rebuild was started.</returns>
    bool RebuildIndexAsync(bool prewarm);

    /// <summary>
    /// Clears all storage and starts a rebuild in the background.
    /// </summary>
    /// <param name="prewarm">Whether to prewarm browse responses after rebuilding.</param>
    /// <returns>True when the operation was started.</returns>
    bool ClearAndRebuildAsync(bool prewarm);
}
