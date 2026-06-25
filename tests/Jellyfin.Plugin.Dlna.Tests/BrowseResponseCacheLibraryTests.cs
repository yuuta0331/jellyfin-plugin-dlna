using System;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for selective browse cache invalidation.
/// </summary>
public class BrowseResponseCacheLibraryTests
{
    [Fact]
    public void InvalidateLibrary_RemovesMatchingObjectIdsOnly()
    {
        var libraryId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var cache = new BrowseResponseCache(() => new DlnaPluginConfiguration { EnableBrowseResponseCache = true });

        var libraryKey = BrowseCacheTestKeys.Create($"series_{libraryId:N}");
        var otherKey = BrowseCacheTestKeys.Create("series_bbbbbbbbbbbbbbbbbbbbbbbbbbbb");

        cache.Set(libraryKey, new BrowseCacheEntry("<DIDL-Lite />", 1, 1, 1));
        cache.Set(otherKey, new BrowseCacheEntry("<DIDL-Lite />", 1, 1, 1));

        cache.InvalidateLibrary(libraryId);

        Assert.False(cache.TryGet(libraryKey, out _));
        Assert.True(cache.TryGet(otherKey, out _));
    }

    [Fact]
    public void InvalidatePattern_RemovesMatchingObjectIdPrefixOnly()
    {
        var cache = new BrowseResponseCache(() => new DlnaPluginConfiguration { EnableBrowseResponseCache = true });

        var matchingKey = BrowseCacheTestKeys.Create("recentlyaddedseries_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        var otherKey = BrowseCacheTestKeys.Create("recentlyaddedepisodes_bbbbbbbbbbbbbbbbbbbbbbbbbbbb");

        cache.Set(matchingKey, new BrowseCacheEntry("<DIDL-Lite />", 1, 1, 1));
        cache.Set(otherKey, new BrowseCacheEntry("<DIDL-Lite />", 1, 1, 1));

        cache.InvalidatePattern("recentlyaddedseries_");

        Assert.False(cache.TryGet(matchingKey, out _));
        Assert.True(cache.TryGet(otherKey, out _));
    }
}
