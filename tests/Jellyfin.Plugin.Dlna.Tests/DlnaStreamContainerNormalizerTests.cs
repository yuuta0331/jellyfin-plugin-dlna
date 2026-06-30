using Jellyfin.Plugin.Dlna.Model;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for <see cref="DlnaStreamContainerNormalizer"/>.
/// </summary>
public class DlnaStreamContainerNormalizerTests
{
    [Theory]
    [InlineData("mov,mp4,m4a,3gp,3g2,mj2", "mov")]
    [InlineData(".mp4", "mp4")]
    [InlineData("mkv", "mkv")]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("  .webm  ", "webm")]
    [InlineData(".MP4", "mp4")]
    public void NormalizeRouteContainer_HandlesProbeAndDotPrefixes(string? input, string? expected)
    {
        Assert.Equal(expected, DlnaStreamContainerNormalizer.NormalizeRouteContainer(input));
    }

    [Theory]
    [InlineData("mov,mp4,m4a", true)]
    [InlineData("mp4", false)]
    [InlineData(null, false)]
    public void IsDlnaCapabilityProbe_DetectsCommaSeparatedLists(string? input, bool expected)
    {
        Assert.Equal(expected, DlnaStreamContainerNormalizer.IsDlnaCapabilityProbe(input));
    }
}
