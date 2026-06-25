using System;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Jellyfin.Plugin.Dlna.Didl;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for browse timing log output.
/// </summary>
public class BrowseTimingScopeTests
{
    [Fact]
    public void ToLogSummary_ContainsExpectedFields()
    {
        using var scope = new BrowseTimingScope
        {
            ObjectId = "series_abc",
            StubTypeName = "Series",
            CacheHit = false,
            IndexHit = true,
            Items = 42,
            XmlBytes = 1024
        };

        scope.AddQueryMs(10);
        scope.AddIndexMs(5);
        scope.AddDtoMs(20);
        scope.AddDidlMs(30);

        var summary = scope.ToLogSummary();

        Assert.Contains("ObjectId=series_abc", summary, StringComparison.Ordinal);
        Assert.Contains("StubType=Series", summary, StringComparison.Ordinal);
        Assert.Contains("CacheHit=False", summary, StringComparison.Ordinal);
        Assert.Contains("IndexHit=True", summary, StringComparison.Ordinal);
        Assert.Contains("QueryMs=10", summary, StringComparison.Ordinal);
        Assert.Contains("IndexMs=5", summary, StringComparison.Ordinal);
        Assert.Contains("Items=42", summary, StringComparison.Ordinal);
        Assert.Contains("XmlBytes=1024", summary, StringComparison.Ordinal);
    }
}
