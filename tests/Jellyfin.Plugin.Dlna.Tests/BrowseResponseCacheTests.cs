using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for <see cref="BrowseResponseCache"/>.
/// </summary>
public class BrowseResponseCacheTests
{
  private static DlnaPluginConfiguration EnabledConfig() => new()
  {
    EnableBrowseResponseCache = true,
    BrowseResponseCacheTtlSeconds = 0
  };

  [Fact]
  public void TryGet_MatchingKey_ReturnsCachedEntry()
  {
    var cache = new BrowseResponseCache(() => EnabledConfig());
    var key = BrowseCacheTestKeys.Create();
    var entry = new BrowseCacheEntry("<DIDL-Lite />", 3, 3, 1);

    cache.Set(key, entry);

    Assert.True(cache.TryGet(key, out var cached));
    Assert.Equal(entry, cached);
  }

  [Fact]
  public void TryGet_ConsecutiveAccessWithSameKey_HitsOnSecondAccess()
  {
    var cache = new BrowseResponseCache(() => EnabledConfig());
    var key = BrowseCacheTestKeys.Create("movies_all");
    var entry = new BrowseCacheEntry("<DIDL-Lite />", 10, 10, 1);

    Assert.False(cache.TryGet(key, out _));

    cache.Set(key, entry);

    Assert.True(cache.TryGet(key, out var firstHit));
    Assert.Equal(entry, firstHit);
    Assert.True(cache.TryGet(key, out var secondHit));
    Assert.Equal(entry, secondHit);
  }

  [Fact]
  public void TryGet_DifferentServerBase_Misses()
  {
    var cache = new BrowseResponseCache(() => EnabledConfig());
    var key = BrowseCacheTestKeys.Create("movies_all");
    cache.Set(key, new BrowseCacheEntry("<DIDL-Lite />", 1, 1, 1));

    var otherKey = key with { ServerBase = "http://client.example" };

    Assert.False(cache.TryGet(otherKey, out _));
  }

  [Fact]
  public void Set_LoopbackServerBase_DoesNotStoreEntry()
  {
    var cache = new BrowseResponseCache(() => EnabledConfig());
    var key = BrowseCacheTestKeys.Create("movies_all", serverBase: "http://127.0.0.1");

    cache.Set(key, new BrowseCacheEntry("<DIDL-Lite />", 1, 1, 1));

    Assert.Equal(0, cache.GetStatistics().EntryCount);
  }

  [Fact]
  public void TryGet_DifferentConfigFingerprint_Misses()
  {
    var cache = new BrowseResponseCache(() => EnabledConfig());
    var key = BrowseCacheTestKeys.Create("0");
    cache.Set(key, new BrowseCacheEntry("<DIDL-Lite />", 1, 1, 1));

    var otherKey = key with { ConfigFingerprint = 99 };

    Assert.False(cache.TryGet(otherKey, out _));
  }

  [Fact]
  public void InvalidateAll_ClearsEntries()
  {
    var cache = new BrowseResponseCache(() => EnabledConfig());
    var key = BrowseCacheTestKeys.Create("0");
    cache.Set(key, new BrowseCacheEntry("<DIDL-Lite />", 1, 1, 1));

    cache.InvalidateAll();

    Assert.False(cache.TryGet(key, out _));
  }
}
