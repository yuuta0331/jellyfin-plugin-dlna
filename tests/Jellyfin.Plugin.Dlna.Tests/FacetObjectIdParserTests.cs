using System;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Jellyfin.Plugin.Dlna.Didl;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for facet and range object id parsing.
/// </summary>
public class FacetObjectIdParserTests
{
    private static readonly Guid LibraryId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public void TryParseFacetClientId_Studio_RoundTrips()
    {
        var id = DidlBuilder.GetFacetClientId(StubType.StudioItem, LibraryId, "Kyoto Animation");

        Assert.True(DidlBuilder.TryParseFacetClientId(id, out var libraryId, out var stubType, out var key));
        Assert.Equal(LibraryId, libraryId);
        Assert.Equal(StubType.StudioItem, stubType);
        Assert.Equal("Kyoto Animation", key);
    }

    [Fact]
    public void TryParseFacetClientId_Person_RoundTrips()
    {
        var id = DidlBuilder.GetFacetClientId(StubType.PersonItem, LibraryId, "Actor Name");

        Assert.True(DidlBuilder.TryParseFacetClientId(id, out var libraryId, out var stubType, out var key));
        Assert.Equal(LibraryId, libraryId);
        Assert.Equal(StubType.PersonItem, stubType);
        Assert.Equal("Actor Name", key);
    }

    [Fact]
    public void TryParseSeriesRangeClientId_RoundTrips()
    {
        var id = DidlBuilder.GetSeriesRangeClientId(LibraryId, 0, 500);

        Assert.True(DidlBuilder.TryParseSeriesRangeClientId(id, out var libraryId, out var start, out var end));
        Assert.Equal(LibraryId, libraryId);
        Assert.Equal(0, start);
        Assert.Equal(500, end);
    }
}
