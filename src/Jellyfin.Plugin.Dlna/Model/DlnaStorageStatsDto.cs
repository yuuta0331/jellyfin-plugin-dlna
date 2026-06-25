using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Aggregated DLNA storage and cache statistics.
/// </summary>
public class DlnaStorageStatsDto
{
    /// <summary>
    /// Gets or sets index database statistics.
    /// </summary>
    public IndexDatabaseStatsDto IndexDatabase { get; set; } = new();

    /// <summary>
    /// Gets or sets browse response cache statistics.
    /// </summary>
    public BrowseCacheStatsDto BrowseCache { get; set; } = new();

    /// <summary>
    /// Gets or sets browse node cache statistics.
    /// </summary>
    public BrowseCacheStatsDto BrowseNodeCache { get; set; } = new();

    /// <summary>
    /// Gets or sets child count cache statistics.
    /// </summary>
    public ChildCountCacheStatsDto ChildCountCache { get; set; } = new();

    /// <summary>
    /// Gets or sets browse metrics.
    /// </summary>
    public BrowseMetricsStatsDto BrowseMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the virtual index generation counter.
    /// </summary>
    public int IndexGeneration { get; set; }

    /// <summary>
    /// Gets or sets the library generation counter.
    /// </summary>
    public int LibraryGeneration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether maintenance is running.
    /// </summary>
    public bool IsMaintenanceRunning { get; set; }
}

/// <summary>
/// SQLite index database statistics.
/// </summary>
public class IndexDatabaseStatsDto
{
    /// <summary>
    /// Gets or sets the database file path.
    /// </summary>
    public string DatabasePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the number of indexed libraries.
    /// </summary>
    public int LibraryIndexedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of item summary rows.
    /// </summary>
    public int ItemSummaryCount { get; set; }

    /// <summary>
    /// Gets or sets the number of virtual list rows.
    /// </summary>
    public int VirtualListCount { get; set; }

    /// <summary>
    /// Gets or sets the number of kana row entries.
    /// </summary>
    public int KanaRowCount { get; set; }

    /// <summary>
    /// Gets or sets the number of facet index rows.
    /// </summary>
    public int FacetIndexCount { get; set; }

    /// <summary>
    /// Gets or sets indexed library ids.
    /// </summary>
    public IReadOnlyList<Guid> IndexedLibraryIds { get; set; } = Array.Empty<Guid>();
}

/// <summary>
/// Browse response cache statistics.
/// </summary>
public class BrowseCacheStatsDto
{
    /// <summary>
    /// Gets or sets the number of cached entries.
    /// </summary>
    public int EntryCount { get; set; }

    /// <summary>
    /// Gets or sets the estimated memory usage in bytes.
    /// </summary>
    public long EstimatedBytes { get; set; }
}

/// <summary>
/// Child count cache statistics.
/// </summary>
public class ChildCountCacheStatsDto
{
    /// <summary>
    /// Gets or sets the number of cached entries.
    /// </summary>
    public int EntryCount { get; set; }
}

/// <summary>
/// Browse metrics snapshot.
/// </summary>
public class BrowseMetricsStatsDto
{
    /// <summary>
    /// Gets or sets the total browse count.
    /// </summary>
    public long BrowseCount { get; set; }

    /// <summary>
    /// Gets or sets the cache hit rate between 0 and 1.
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    /// Gets or sets the layer 4 response cache hit rate between 0 and 1.
    /// </summary>
    public double ResponseCacheHitRate { get; set; }

    /// <summary>
    /// Gets or sets the layer 3 node cache hit rate between 0 and 1.
    /// </summary>
    public double NodeCacheHitRate { get; set; }

    /// <summary>
    /// Gets or sets the index hit rate between 0 and 1.
    /// </summary>
    public double IndexHitRate { get; set; }

    /// <summary>
    /// Gets or sets the invalidation event count.
    /// </summary>
    public long InvalidationCount { get; set; }
}
