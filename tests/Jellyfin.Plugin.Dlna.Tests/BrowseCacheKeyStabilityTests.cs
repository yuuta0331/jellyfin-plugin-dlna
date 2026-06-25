using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests that browse cache keys stay stable across index/library generation changes.
/// </summary>
public class BrowseCacheKeyStabilityTests
{
    [Fact]
    public void ToString_IncludesServerBaseAndOmitsGenerationCounters()
    {
        var key = BrowseCacheTestKeys.Create("movies_all");

        var text = key.ToString();

        Assert.Contains(BrowseCacheTestKeys.ServerBase, text, System.StringComparison.Ordinal);
        Assert.Contains("movies_all", text, System.StringComparison.Ordinal);
        Assert.EndsWith("|42|0|", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void TryGet_RemainsValidAfterNormalizedSortAndFilter()
    {
        var cache = new BrowseResponseCache(() => new DlnaPluginConfiguration
        {
            EnableBrowseResponseCache = true,
            BrowseResponseCacheTtlSeconds = 0
        });

        var key = new BrowseCacheKey(
            null,
            "root",
            "BrowseDirectChildren",
            "   ",
            "  * ",
            "profile",
            BrowseCacheTestKeys.ServerBase,
            42,
            0,
            null);
        var entry = new BrowseCacheEntry("<DIDL-Lite />", 1, 1, 1);
        cache.Set(key, entry);

        var lookupKey = key with
        {
            SortCriteria = string.Empty,
            Filter = "*"
        };

        Assert.True(cache.TryGet(lookupKey, out var cached));
        Assert.Equal(entry, cached);
    }
}
