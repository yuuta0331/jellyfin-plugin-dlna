using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for <see cref="BrowseNodeCache"/>.
/// </summary>
public class BrowseNodeCacheTests
{
    private static DlnaPluginConfiguration EnabledConfig() => new()
    {
        EnableBrowseNodeCache = true,
        BrowseNodeCacheTtlSeconds = 0
    };

    [Fact]
    public void TryGet_MatchingKey_ReturnsCachedEntry()
    {
        var cache = new BrowseNodeCache(() => EnabledConfig());
        var key = BrowseCacheTestKeys.Create("series_root");
        var nodes = new List<BrowseNodeRecord>
        {
            new("item1", "Title", "object.item.videoItem", false, null, "series_root")
        };
        var entry = new BrowseNodeCacheEntry(nodes, 1);

        cache.Set(key, entry);

        Assert.True(cache.TryGet(key, out var cached));
        Assert.Equal(entry, cached);
    }

    [Fact]
    public void GetStatistics_ReturnsEntryCountAndEstimatedBytes()
    {
        var cache = new BrowseNodeCache(() => EnabledConfig());
        var key = BrowseCacheTestKeys.Create("series_root");
        var nodes = new List<BrowseNodeRecord>
        {
            new("item1", "Title", "object.item.videoItem", false, null, "series_root")
        };
        cache.Set(key, new BrowseNodeCacheEntry(nodes, 1));

        var stats = cache.GetStatistics();

        Assert.Equal(1, stats.EntryCount);
        Assert.True(stats.EstimatedBytes > 0);
    }

    [Fact]
    public void InvalidateLibrary_RemovesMatchingEntries()
    {
        var libraryId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var cache = new BrowseNodeCache(() => EnabledConfig());
        var key = BrowseCacheTestKeys.Create(libraryId.ToString("N"));
        cache.Set(key, new BrowseNodeCacheEntry([], 0));

        cache.InvalidateLibrary(libraryId);

        Assert.False(cache.TryGet(key, out _));
    }
}
