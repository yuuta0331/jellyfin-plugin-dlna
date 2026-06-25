using System;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for browse cache statistics.
/// </summary>
public class BrowseResponseCacheStatisticsTests
{
    [Fact]
    public void GetStatistics_ReturnsEntryCountAndEstimatedBytes()
    {
        var cache = new BrowseResponseCache(() => new DlnaPluginConfiguration
        {
            EnableBrowseResponseCache = true,
            BrowseResponseCacheTtlSeconds = 0
        });
        var key = BrowseCacheTestKeys.Create("series_abc");

        cache.Set(key, new BrowseCacheEntry("<DIDL-Lite>payload</DIDL-Lite>", 1, 1, 1));

        var stats = cache.GetStatistics();

        Assert.Equal(1, stats.EntryCount);
        Assert.True(stats.EstimatedBytes > 0);
    }
}
