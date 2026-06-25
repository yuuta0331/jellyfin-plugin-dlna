using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Jellyfin.Plugin.Dlna.Profiles;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests that default DLNA device profiles use a stable cache identity.
/// </summary>
public class DefaultProfileCacheIdentityTests
{
    [Fact]
    public void DefaultProfile_UsesStableIdAcrossInstances()
    {
        var first = new DefaultProfile();
        var second = new DefaultProfile();

        Assert.Equal(DefaultProfile.ProfileId, first.Id);
        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public void BrowseResponseCache_HitsWhenDefaultProfileIdIsStable()
    {
        var cache = new BrowseResponseCache(() => new DlnaPluginConfiguration
        {
            EnableBrowseResponseCache = true,
            BrowseResponseCacheTtlSeconds = 0
        });

        var key = BrowseCacheTestKeys.Create(
            "movies_all",
            DefaultProfile.ProfileId.ToString()!);

        cache.Set(key, new BrowseCacheEntry("<DIDL-Lite />", 1, 1, 1));

        Assert.True(cache.TryGet(key, out var cached));
        Assert.Equal("<DIDL-Lite />", cached.DidlXml);
    }
}
