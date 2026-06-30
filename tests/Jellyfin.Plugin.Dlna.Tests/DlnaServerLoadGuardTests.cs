using Jellyfin.Plugin.Dlna.Indexing;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for <see cref="DlnaServerLoadGuard"/> busy-task detection.
/// </summary>
public class DlnaServerLoadGuardTests
{
    [Theory]
    [InlineData("Scan Media Library", true)]
    [InlineData("Optimize Database", true)]
    [InlineData("Generate Trickplay Images", true)]
    [InlineData("DLNA Scheduled Browse Prewarm", false)]
    [InlineData("Rebuild DLNA Quest Index", false)]
    public void IsBusyTaskName_MatchesExpected(string taskName, bool expected)
        => Assert.Equal(expected, DlnaServerLoadGuard.IsBusyTaskName(taskName));
}
