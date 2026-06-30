using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Indexing;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for <see cref="BrowsePrewarmPaths"/> scope selection.
/// </summary>
public class BrowsePrewarmPathsTests
{
    [Fact]
    public void GetQuestMinimalObjectIds_ReturnsFewerPathsThanFullScope()
    {
        var libraryId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var library = new StubTvLibrary(libraryId);
        var store = new VirtualIndexStore(new TestApplicationPaths(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dlna-prewarm-" + Guid.NewGuid().ToString("N"))));
        var indexService = new ReadyIndexService(libraryId);
        store.MarkLibraryIndexed(libraryId);

        var config = new DlnaPluginConfiguration
        {
            EnableIndexMoviesList = true,
            EnableIndexSeriesList = true,
            EnableRecentlyAddedSeries = true,
            EnableIndexGenre = true,
            PrewarmFacetItemFolders = true,
            PrewarmHierarchyFolders = true
        };

        try
        {
            var minimal = BrowsePrewarmPaths.GetObjectIdsForScope(
                config,
                library,
                store,
                indexService,
                libraryManager: null!);

            config.PrewarmScope = PrewarmScope.Full;
            var full = BrowsePrewarmPaths.GetObjectIds(
                config,
                library,
                store,
                indexService,
                libraryManager: null!);

            Assert.True(minimal.Count < full.Count);
            Assert.Contains(libraryId.ToString("N"), minimal);
        }
        finally
        {
            store.Dispose();
        }
    }

    private sealed class StubTvLibrary : BaseItem, IHasCollectionType
    {
        public StubTvLibrary(Guid id)
        {
            Id = id;
            Name = "TV";
        }

        public CollectionType? CollectionType => Jellyfin.Data.Enums.CollectionType.tvshows;
    }

    private sealed class ReadyIndexService : IDlnaVirtualIndexService
    {
        private readonly Guid _libraryId;

        public ReadyIndexService(Guid libraryId)
        {
            _libraryId = libraryId;
            Generation = new DlnaIndexGeneration();
        }

        public DlnaIndexGeneration Generation { get; }

        public bool IsReady(Guid libraryId) => libraryId == _libraryId;

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
}
