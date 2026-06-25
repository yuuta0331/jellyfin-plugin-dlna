using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Jellyfin.Plugin.Dlna.Indexing;
using Jellyfin.Plugin.Dlna.Model;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Maintenance;

/// <summary>
/// Coordinates DLNA storage and cache maintenance operations.
/// </summary>
public sealed class DlnaStorageMaintenanceService : IDlnaStorageMaintenanceService
{
    private readonly IBrowseResponseCache _browseResponseCache;
    private readonly IBrowseNodeCache _browseNodeCache;
    private readonly ChildCountCache _childCountCache;
    private readonly IVirtualIndexStore _indexStore;
    private readonly IDlnaVirtualIndexService _indexService;
    private readonly IDlnaBrowsePrewarmService _prewarmService;
    private readonly IBrowseMetrics _browseMetrics;
    private readonly LibraryChangeNotifier _libraryChangeNotifier;
    private readonly ILogger<DlnaStorageMaintenanceService> _logger;
    private int _maintenanceRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaStorageMaintenanceService"/> class.
    /// </summary>
    public DlnaStorageMaintenanceService(
        IBrowseResponseCache browseResponseCache,
        IBrowseNodeCache browseNodeCache,
        ChildCountCache childCountCache,
        IVirtualIndexStore indexStore,
        IDlnaVirtualIndexService indexService,
        IDlnaBrowsePrewarmService prewarmService,
        IBrowseMetrics browseMetrics,
        LibraryChangeNotifier libraryChangeNotifier,
        ILogger<DlnaStorageMaintenanceService> logger)
    {
        _browseResponseCache = browseResponseCache;
        _browseNodeCache = browseNodeCache;
        _childCountCache = childCountCache;
        _indexStore = indexStore;
        _indexService = indexService;
        _prewarmService = prewarmService;
        _browseMetrics = browseMetrics;
        _libraryChangeNotifier = libraryChangeNotifier;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsMaintenanceRunning => Volatile.Read(ref _maintenanceRunning) > 0;

    /// <inheritdoc />
    public DlnaStorageStatsDto GetStats()
    {
        var storeStats = _indexStore.GetStatistics();

        return new DlnaStorageStatsDto
        {
            IndexDatabase = new IndexDatabaseStatsDto
            {
                DatabasePath = storeStats.DatabasePath,
                FileSizeBytes = storeStats.FileSizeBytes,
                LibraryIndexedCount = storeStats.LibraryIndexedCount,
                ItemSummaryCount = storeStats.ItemSummaryCount,
                VirtualListCount = storeStats.VirtualListCount,
                KanaRowCount = storeStats.KanaRowCount,
                FacetIndexCount = storeStats.FacetIndexCount,
                IndexedLibraryIds = storeStats.IndexedLibraryIds
            },
            BrowseCache = MapBrowseCacheStats(_browseResponseCache.GetStatistics()),
            BrowseNodeCache = MapBrowseCacheStats(_browseNodeCache.GetStatistics()),
            ChildCountCache = new ChildCountCacheStatsDto
            {
                EntryCount = _childCountCache.GetStatistics().EntryCount
            },
            BrowseMetrics = new BrowseMetricsStatsDto
            {
                BrowseCount = _browseMetrics.BrowseCount,
                CacheHitRate = _browseMetrics.CacheHitRate,
                ResponseCacheHitRate = _browseMetrics.ResponseCacheHitRate,
                NodeCacheHitRate = _browseMetrics.NodeCacheHitRate,
                IndexHitRate = _browseMetrics.IndexHitRate,
                InvalidationCount = _browseMetrics.InvalidationCount
            },
            IndexGeneration = _indexService.Generation.Value,
            LibraryGeneration = _libraryChangeNotifier.LibraryGeneration,
            IsMaintenanceRunning = IsMaintenanceRunning
        };
    }

    /// <inheritdoc />
    public void ClearBrowseCache()
    {
        _browseResponseCache.InvalidateAll();
        _browseNodeCache.InvalidateAll();
        _logger.LogInformation("DLNA browse response and node caches cleared via maintenance API");
    }

    /// <inheritdoc />
    public void ClearChildCountCache()
    {
        _childCountCache.InvalidateAll();
        _logger.LogInformation("DLNA child count cache cleared via maintenance API");
    }

    /// <inheritdoc />
    public void ClearIndex()
    {
        _indexService.InvalidateAll();
        _logger.LogInformation("DLNA virtual index cleared via maintenance API");
    }

    /// <inheritdoc />
    public void ClearAll()
    {
        _browseResponseCache.InvalidateAll();
        _browseNodeCache.InvalidateAll();
        _childCountCache.InvalidateAll();
        _indexService.InvalidateAll();
        _logger.LogInformation("DLNA browse caches, child count cache, and virtual index cleared via maintenance API");
    }

    /// <inheritdoc />
    public bool RebuildIndexAsync(bool prewarm)
    {
        if (!TryBeginMaintenance())
        {
            return false;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _indexService.RebuildAllAsync(null, CancellationToken.None).ConfigureAwait(false);
                if (prewarm)
                {
                    await _prewarmService.PrewarmAsync(null, CancellationToken.None).ConfigureAwait(false);
                }

                _logger.LogInformation("DLNA index rebuild completed via maintenance API Prewarm={Prewarm}", prewarm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DLNA index rebuild failed via maintenance API");
            }
            finally
            {
                EndMaintenance();
            }
        });

        return true;
    }

    /// <inheritdoc />
    public bool ClearAndRebuildAsync(bool prewarm)
    {
        if (!TryBeginMaintenance())
        {
            return false;
        }

        ClearAll();

        _ = Task.Run(async () =>
        {
            try
            {
                await _indexService.RebuildAllAsync(null, CancellationToken.None).ConfigureAwait(false);
                if (prewarm)
                {
                    await _prewarmService.PrewarmAsync(null, CancellationToken.None).ConfigureAwait(false);
                }

                _logger.LogInformation("DLNA clear and rebuild completed via maintenance API Prewarm={Prewarm}", prewarm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DLNA clear and rebuild failed via maintenance API");
            }
            finally
            {
                EndMaintenance();
            }
        });

        return true;
    }

    private static BrowseCacheStatsDto MapBrowseCacheStats(BrowseCacheStatistics statistics)
        => new()
        {
            EntryCount = statistics.EntryCount,
            EstimatedBytes = statistics.EstimatedBytes
        };

    private bool TryBeginMaintenance()
        => Interlocked.CompareExchange(ref _maintenanceRunning, 1, 0) == 0;

    private void EndMaintenance()
        => Interlocked.Exchange(ref _maintenanceRunning, 0);
}
