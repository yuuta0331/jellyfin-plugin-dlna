using System.Threading;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Thread-safe browse metrics collector.
/// </summary>
public sealed class BrowseMetrics : IBrowseMetrics
{
    private long _browseCount;
    private long _cacheHits;
    private long _responseCacheHits;
    private long _nodeCacheHits;
    private long _indexHits;
    private long _invalidationCount;

    /// <inheritdoc />
    public void RecordBrowse(BrowseCacheHitKind cacheHitKind, bool indexHit, long totalMs)
    {
        Interlocked.Increment(ref _browseCount);
        if (cacheHitKind != BrowseCacheHitKind.None)
        {
            Interlocked.Increment(ref _cacheHits);
        }

        if (cacheHitKind == BrowseCacheHitKind.Response)
        {
            Interlocked.Increment(ref _responseCacheHits);
        }
        else if (cacheHitKind == BrowseCacheHitKind.Node)
        {
            Interlocked.Increment(ref _nodeCacheHits);
        }

        if (indexHit)
        {
            Interlocked.Increment(ref _indexHits);
        }

        _ = totalMs;
    }

    /// <inheritdoc />
    public void RecordInvalidation(bool full)
    {
        Interlocked.Increment(ref _invalidationCount);
        _ = full;
    }

    /// <inheritdoc />
    public double CacheHitRate
    {
        get
        {
            var total = Interlocked.Read(ref _browseCount);
            return total == 0 ? 0 : (double)Interlocked.Read(ref _cacheHits) / total;
        }
    }

    /// <inheritdoc />
    public double IndexHitRate
    {
        get
        {
            var total = Interlocked.Read(ref _browseCount);
            return total == 0 ? 0 : (double)Interlocked.Read(ref _indexHits) / total;
        }
    }

    /// <summary>
    /// Gets layer 4 response cache hit rate between 0 and 1.
    /// </summary>
    public double ResponseCacheHitRate
    {
        get
        {
            var total = Interlocked.Read(ref _browseCount);
            return total == 0 ? 0 : (double)Interlocked.Read(ref _responseCacheHits) / total;
        }
    }

    /// <summary>
    /// Gets layer 3 node cache hit rate between 0 and 1.
    /// </summary>
    public double NodeCacheHitRate
    {
        get
        {
            var total = Interlocked.Read(ref _browseCount);
            return total == 0 ? 0 : (double)Interlocked.Read(ref _nodeCacheHits) / total;
        }
    }

    /// <inheritdoc />
    public long BrowseCount => Interlocked.Read(ref _browseCount);

    /// <inheritdoc />
    public long InvalidationCount => Interlocked.Read(ref _invalidationCount);
}
