using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Statistics for the virtual index SQLite store.
/// </summary>
/// <param name="DatabasePath">The database file path.</param>
/// <param name="FileSizeBytes">The database file size in bytes.</param>
/// <param name="LibraryIndexedCount">The number of indexed libraries.</param>
/// <param name="ItemSummaryCount">The number of item summary rows.</param>
/// <param name="VirtualListCount">The number of virtual list rows.</param>
/// <param name="KanaRowCount">The number of kana row entries.</param>
/// <param name="FacetIndexCount">The number of facet index rows.</param>
/// <param name="IndexedLibraryIds">Indexed library ids.</param>
public readonly record struct IndexStoreStatistics(
    string DatabasePath,
    long FileSizeBytes,
    int LibraryIndexedCount,
    int ItemSummaryCount,
    int VirtualListCount,
    int KanaRowCount,
    int FacetIndexCount,
    IReadOnlyList<Guid> IndexedLibraryIds);
