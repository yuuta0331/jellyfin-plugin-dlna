using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Jellyfin.Plugin.Dlna.Indexing;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for index browse helper facet helpers.
/// </summary>
public class IndexBrowseHelperFacetTests
{
    [Fact]
    public void IsFacetIndexEnabled_RespectsSeasonAndEpisodeFlags()
    {
        var config = new DlnaPluginConfiguration
        {
            EnableIndexSeasonList = true,
            EnableIndexEpisodeList = false
        };

        Assert.True(IndexBrowseHelper.IsFacetIndexEnabled(config, FacetType.SeasonOfSeries));
        Assert.False(IndexBrowseHelper.IsFacetIndexEnabled(config, FacetType.EpisodeOfSeason));
    }

    [Fact]
    public void IsVirtualListBrowseEnabled_RespectsSeriesAndMoviesFlags()
    {
        var config = new DlnaPluginConfiguration
        {
            EnableIndexSeriesList = false,
            EnableIndexMoviesList = true
        };

        Assert.False(IndexBrowseHelper.IsVirtualListBrowseEnabled(config, VirtualListType.SeriesAll));
        Assert.True(IndexBrowseHelper.IsVirtualListBrowseEnabled(config, VirtualListType.MoviesAll));
    }

    [Fact]
    public void IsFacetIndexEnabled_RespectsGenreAndYearFlags()
    {
        var config = new DlnaPluginConfiguration
        {
            EnableIndexGenre = false,
            EnableIndexYear = true
        };

        Assert.False(IndexBrowseHelper.IsFacetIndexEnabled(config, FacetType.Genre));
        Assert.True(IndexBrowseHelper.IsFacetIndexEnabled(config, FacetType.Year));
    }

    [Fact]
    public void TryGetYearFolders_ReturnsDescendingYearStubs()
    {
        var libraryId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var library = new Folder { Id = libraryId, Name = "Anime" };
        var store = new FakeIndexStore(libraryId);
        var indexService = new FakeIndexService(libraryId, ready: true);
        var config = new DlnaPluginConfiguration { EnableVirtualFolderIndex = true, EnableIndexYear = true };

        var folders = IndexBrowseHelper.TryGetYearFolders(store, indexService, config, library, null);

        Assert.NotNull(folders);
        Assert.Equal(2, folders!.Count);
        Assert.Equal(2024, folders[0].ProductionYear);
        Assert.Equal(2023, folders[1].ProductionYear);
    }

    [Fact]
    public void TryGetYearFolders_ReturnsNull_WhenIndexDisabled()
    {
        var libraryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var library = new Folder { Id = libraryId, Name = "Anime" };
        var store = new FakeIndexStore(libraryId);
        var indexService = new FakeIndexService(libraryId, ready: true);
        var config = new DlnaPluginConfiguration { EnableIndexYear = false };

        var folders = IndexBrowseHelper.TryGetYearFolders(store, indexService, config, library, null);

        Assert.Null(folders);
    }

    private sealed class FakeIndexService : IDlnaVirtualIndexService
    {
        private readonly Guid _libraryId;
        private readonly bool _ready;

        public FakeIndexService(Guid libraryId, bool ready)
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

        public System.Threading.Tasks.Task<IReadOnlyList<Guid>> TryRebuildLibrariesAsync(
            IReadOnlyList<Guid> libraryIds,
            System.Threading.CancellationToken cancellationToken)
            => System.Threading.Tasks.Task.FromResult<IReadOnlyList<Guid>>(libraryIds);

        public void InvalidateAll()
        {
        }

        public void InvalidateLibrary(Guid libraryId)
        {
        }
    }

    private sealed class FakeIndexStore : IVirtualIndexStore
    {
        private readonly Guid _libraryId;

        public FakeIndexStore(Guid libraryId)
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

        public void ReplaceVirtualList(Guid libraryId, VirtualListType listType, IReadOnlyList<Guid> itemIds)
        {
        }

        public IReadOnlyList<Guid> GetVirtualList(Guid libraryId, VirtualListType listType) => [];

        public int GetVirtualListCount(Guid libraryId, VirtualListType listType) => 0;

        public void ReplaceTitleBrowseGroup(Guid libraryId, Jellyfin.Data.Enums.BaseItemKind itemType, string groupId, IReadOnlyList<Guid> itemIds)
        {
        }

        public IReadOnlyList<Guid> GetTitleBrowseGroup(Guid libraryId, Jellyfin.Data.Enums.BaseItemKind itemType, string groupId) => [];

        public IReadOnlyList<TitleBrowseGroupCount> GetTitleBrowseGroupCounts(Guid libraryId, Jellyfin.Data.Enums.BaseItemKind itemType) => [];

        public void ReplaceKanaRow(Guid libraryId, Jellyfin.Data.Enums.BaseItemKind itemType, int rowIndex, IReadOnlyList<Guid> itemIds)
        {
        }

        public IReadOnlyList<Guid> GetKanaRow(Guid libraryId, Jellyfin.Data.Enums.BaseItemKind itemType, int rowIndex) => [];

        public void ReplaceFacets(Guid libraryId, FacetType facetType, IReadOnlyDictionary<string, IReadOnlyList<Guid>> entries)
        {
        }

        public IReadOnlyList<FacetKeyCount> GetFacetKeys(Guid libraryId, FacetType facetType)
        {
            if (libraryId != _libraryId || facetType != FacetType.Year)
            {
                return [];
            }

            return [new FacetKeyCount("2024", 1), new FacetKeyCount("2023", 1)];
        }

        public IReadOnlyList<Guid> GetFacetItems(Guid libraryId, FacetType facetType, string facetKey) => [];

        public IReadOnlyDictionary<Guid, ItemSummaryRecord> GetItemSummaries(Guid libraryId, IReadOnlyList<Guid> itemIds) => new Dictionary<Guid, ItemSummaryRecord>();

        public int GetSeriesCount(Guid libraryId) => 0;
    }
}
