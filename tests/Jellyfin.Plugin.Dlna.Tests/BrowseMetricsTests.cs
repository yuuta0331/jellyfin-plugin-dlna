using Jellyfin.Plugin.Dlna.ContentDirectory;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for <see cref="BrowseMetrics"/>.
/// </summary>
public class BrowseMetricsTests
{
    [Fact]
    public void RecordBrowse_ConsecutiveResponseCacheHits_IncreasesResponseHitRate()
    {
        var metrics = new BrowseMetrics();

        metrics.RecordBrowse(BrowseCacheHitKind.None, indexHit: true, totalMs: 0);
        metrics.RecordBrowse(BrowseCacheHitKind.Response, indexHit: false, totalMs: 0);

        Assert.Equal(2, metrics.BrowseCount);
        Assert.Equal(0.5, metrics.CacheHitRate);
        Assert.Equal(0.5, metrics.ResponseCacheHitRate);
        Assert.Equal(0, metrics.NodeCacheHitRate);
        Assert.Equal(0.5, metrics.IndexHitRate);
    }

    [Fact]
    public void RecordBrowse_NodeCacheHit_IsTrackedSeparately()
    {
        var metrics = new BrowseMetrics();

        metrics.RecordBrowse(BrowseCacheHitKind.Node, indexHit: false, totalMs: 0);

        Assert.Equal(1, metrics.BrowseCount);
        Assert.Equal(1, metrics.CacheHitRate);
        Assert.Equal(0, metrics.ResponseCacheHitRate);
        Assert.Equal(1, metrics.NodeCacheHitRate);
    }
}
