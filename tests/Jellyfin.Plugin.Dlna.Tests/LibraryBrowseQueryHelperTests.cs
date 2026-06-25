using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Jellyfin.Plugin.Dlna.Indexing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for library browse query helpers and mixed library folder lists.
/// </summary>
public class LibraryBrowseQueryHelperTests
{
    [Theory]
    [InlineData(CollectionType.unknown, true, true, true, false)]
    [InlineData(CollectionType.movies, false, false, true, false)]
    [InlineData(CollectionType.tvshows, false, true, false, false)]
    [InlineData(CollectionType.homevideos, false, false, false, true)]
    [InlineData(CollectionType.musicvideos, false, false, false, false, true)]
    public void LibraryTypeHelpers_IdentifyCollectionTypes(
        CollectionType collectionType,
        bool mixed,
        bool tv,
        bool movie,
        bool homeVideos,
        bool musicVideos = false)
    {
        var library = CreateLibrary(collectionType);

        Assert.Equal(mixed, LibraryBrowseQueryHelper.IsMixedLibrary(library));
        Assert.Equal(tv, LibraryBrowseQueryHelper.SupportsTvVirtualFolders(library));
        Assert.Equal(movie, LibraryBrowseQueryHelper.SupportsMovieVirtualFolders(library));
        Assert.Equal(homeVideos, LibraryBrowseQueryHelper.IsHomeVideosLibrary(library));
        Assert.Equal(musicVideos, LibraryBrowseQueryHelper.IsMusicVideosLibrary(library));
    }

    [Fact]
    public void IsDlnaLibraryView_IncludesCollectionFolderWithNullCollectionType()
    {
        var library = CreateLibrary(null);

        Assert.True(LibraryBrowseQueryHelper.IsDlnaLibraryView(library));
    }

    [Fact]
    public void ShouldRouteLibraryByCollectionType_RoutesFolderStubAtLibraryRoot()
    {
        var library = CreateLibrary(null);

        Assert.True(LibraryBrowseQueryHelper.ShouldRouteLibraryByCollectionType(library, StubType.Folder));
        Assert.True(LibraryBrowseQueryHelper.ShouldRouteLibraryByCollectionType(library, null));
    }

    [Fact]
    public void IsMixedLibrary_TreatsNullCollectionTypeAsMixed()
    {
        var library = CreateLibrary(null);

        Assert.True(LibraryBrowseQueryHelper.IsMixedLibrary(library));
        Assert.True(LibraryBrowseQueryHelper.SupportsTvVirtualFolders(library));
        Assert.True(LibraryBrowseQueryHelper.SupportsMovieVirtualFolders(library));
    }

    [Fact]
    public void GetGenreBrowseItemTypes_ReturnsMovieAndSeries_ForMixedLibrary()
    {
        var library = CreateLibrary(CollectionType.unknown);

        var types = LibraryBrowseQueryHelper.GetGenreBrowseItemTypes(library);

        Assert.Equal(2, types.Count);
        Assert.Contains(BaseItemKind.Movie, types);
        Assert.Contains(BaseItemKind.Series, types);
    }

    [Fact]
    public void GetGenreBrowseItemTypes_ReturnsMovieOnly_ForMoviesLibrary()
    {
        var library = CreateLibrary(CollectionType.movies);

        var types = LibraryBrowseQueryHelper.GetGenreBrowseItemTypes(library);

        Assert.Single(types);
        Assert.Equal(BaseItemKind.Movie, types[0]);
    }

    [Fact]
    public void BuildMixedRootFolderList_IncludesSeriesMoviesAndSingleGenresStub()
    {
        var library = CreateLibrary(CollectionType.unknown);
        var config = new DlnaPluginConfiguration();
        var store = new StubIndexStore(library.Id);
        var indexService = new StubIndexService(library.Id, ready: false);

        var items = MixedLibraryBrowseHelper.BuildMixedRootFolderList(config, library, store, indexService);

        Assert.Contains(items, i => i.StubType == StubType.Series);
        Assert.Contains(items, i => i.StubType == StubType.Movies);
        Assert.Single(items, i => i.StubType == StubType.Genres);
        Assert.Single(items, i => i.StubType == StubType.BrowseByKana);
    }

    [Theory]
    [InlineData(StubType.Series, true, false, false)]
    [InlineData(StubType.Movies, false, true, false)]
    [InlineData(StubType.Genres, false, false, true)]
    [InlineData(StubType.BrowseByKanaRow, false, false, true)]
    public void MixedStubClassification_CategorizesStubs(
        StubType stub,
        bool tvExclusive,
        bool movieExclusive,
        bool shared)
    {
        Assert.Equal(tvExclusive, MixedLibraryBrowseHelper.IsTvExclusiveStub(stub));
        Assert.Equal(movieExclusive, MixedLibraryBrowseHelper.IsMovieExclusiveStub(stub));
        Assert.Equal(shared, MixedLibraryBrowseHelper.IsSharedBrowseStub(stub));
    }

    private static StubCollectionFolder CreateLibrary(CollectionType? collectionType)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = "Test Library",
            CollectionType = collectionType
        };

    private sealed class StubCollectionFolder : CollectionFolder
    {
    }

    private sealed class StubIndexService : IDlnaVirtualIndexService
    {
        private readonly Guid _libraryId;
        private readonly bool _ready;

        public StubIndexService(Guid libraryId, bool ready)
        {
            _libraryId = libraryId;
            _ready = ready;
        }

        public DlnaIndexGeneration Generation { get; } = new();

        public bool IsReady(Guid libraryId) => _ready && libraryId == _libraryId;

        public System.Threading.Tasks.Task RebuildAllAsync(IProgress<double>? progress, System.Threading.CancellationToken cancellationToken)
            => System.Threading.Tasks.Task.CompletedTask;

        public System.Threading.Tasks.Task RebuildLibraryAsync(Guid libraryId, System.Threading.CancellationToken cancellationToken)
            => System.Threading.Tasks.Task.CompletedTask;

        public void InvalidateAll()
        {
        }

        public void InvalidateLibrary(Guid libraryId)
        {
        }
    }

    private sealed class StubIndexStore : IVirtualIndexStore
    {
        private readonly Guid _libraryId;

        public StubIndexStore(Guid libraryId)
        {
            _libraryId = libraryId;
        }

        public void ClearAll()
        {
        }

        public IndexStoreStatistics GetStatistics()
            => new(string.Empty, 0, 0, 0, 0, 0, 0, Array.Empty<Guid>());

        public string DatabasePath => string.Empty;

        public void ClearLibrary(Guid libraryId)
        {
        }

        public bool IsLibraryIndexed(Guid libraryId) => libraryId == _libraryId;

        public void MarkLibraryIndexed(Guid libraryId)
        {
        }

        public void ReplaceItemSummaries(Guid libraryId, IReadOnlyList<ItemSummaryRecord> summaries)
        {
        }

        public IReadOnlyDictionary<Guid, ItemSummaryRecord> GetItemSummaries(Guid libraryId, IReadOnlyList<Guid> itemIds)
            => new Dictionary<Guid, ItemSummaryRecord>();

        public void ReplaceVirtualList(Guid libraryId, VirtualListType listType, IReadOnlyList<Guid> itemIds)
        {
        }

        public IReadOnlyList<Guid> GetVirtualList(Guid libraryId, VirtualListType listType) => [];

        public void ReplaceKanaRow(Guid libraryId, BaseItemKind itemType, int rowIndex, IReadOnlyList<Guid> itemIds)
        {
        }

        public IReadOnlyList<Guid> GetKanaRow(Guid libraryId, BaseItemKind itemType, int rowIndex) => [];

        public void ReplaceFacets(Guid libraryId, FacetType facetType, IReadOnlyDictionary<string, IReadOnlyList<Guid>> entries)
        {
        }

        public IReadOnlyList<FacetKeyCount> GetFacetKeys(Guid libraryId, FacetType facetType) => [];

        public IReadOnlyList<Guid> GetFacetItems(Guid libraryId, FacetType facetType, string facetKey) => [];

        public int GetSeriesCount(Guid libraryId) => 0;
    }
}
