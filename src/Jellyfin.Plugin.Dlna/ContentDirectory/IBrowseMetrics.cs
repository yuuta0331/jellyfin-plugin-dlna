namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Aggregated DLNA Browse metrics.
/// </summary>
public interface IBrowseMetrics
{
    /// <summary>
    /// Records a completed browse operation.
    /// </summary>
    /// <param name="cacheHitKind">Which browse cache layer served the response.</param>
    /// <param name="indexHit">Whether a virtual index was used.</param>
    /// <param name="totalMs">Total elapsed milliseconds.</param>
    void RecordBrowse(BrowseCacheHitKind cacheHitKind, bool indexHit, long totalMs);

    /// <summary>
    /// Records a cache invalidation.
    /// </summary>
    /// <param name="full">Whether the invalidation was full.</param>
    void RecordInvalidation(bool full);

    /// <summary>
    /// Gets cache hit rate between 0 and 1.
    /// </summary>
    double CacheHitRate { get; }

    /// <summary>
    /// Gets layer 4 response cache hit rate between 0 and 1.
    /// </summary>
    double ResponseCacheHitRate { get; }

    /// <summary>
    /// Gets layer 3 node cache hit rate between 0 and 1.
    /// </summary>
    double NodeCacheHitRate { get; }

    /// <summary>
    /// Gets index hit rate between 0 and 1.
    /// </summary>
    double IndexHitRate { get; }

    /// <summary>
    /// Gets the total browse count.
    /// </summary>
    long BrowseCount { get; }

    /// <summary>
    /// Gets the number of invalidation events recorded.
    /// </summary>
    long InvalidationCount { get; }
}
