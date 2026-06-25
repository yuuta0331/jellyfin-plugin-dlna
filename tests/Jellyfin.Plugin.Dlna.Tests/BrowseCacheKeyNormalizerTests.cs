using Jellyfin.Plugin.Dlna.ContentDirectory;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for <see cref="BrowseCacheKeyNormalizer"/>.
/// </summary>
public class BrowseCacheKeyNormalizerTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(" dc:title ", "dc:title")]
    public void NormalizeSortCriteria_TrimsAndCollapsesEmpty(string? input, string expected)
    {
        Assert.Equal(expected, BrowseCacheKeyNormalizer.NormalizeSortCriteria(input));
    }

    [Theory]
    [InlineData(null, "*")]
    [InlineData("", "*")]
    [InlineData("   ", "*")]
    [InlineData(" dc:title ", "dc:title")]
    public void NormalizeFilter_TrimsAndDefaultsToStar(string? input, string expected)
    {
        Assert.Equal(expected, BrowseCacheKeyNormalizer.NormalizeFilter(input));
    }

    [Theory]
    [InlineData("http://localhost:8096", true)]
    [InlineData("http://server.example:8096", false)]
    public void IsLoopbackServerBase_DetectsLoopbackHosts(string serverBase, bool expected)
    {
        Assert.Equal(expected, BrowseCacheKeyNormalizer.IsLoopbackServerBase(serverBase));
    }
}
