using System;
using System.Collections.Generic;
using System.Threading;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Jellyfin.Plugin.Dlna.Indexing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for <see cref="ContentInvalidationService"/> load-safety behavior.
/// </summary>
public class ContentInvalidationServiceTests
{
    [Fact]
    public void ScheduleLibraryInvalidation_DoesNotClearIndexImmediately()
    {
        using var context = CreateContext();
        var libraryId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        context.Store.MarkLibraryIndexed(libraryId);

        context.Service.ScheduleLibraryInvalidation(libraryId, "test");

        Assert.Equal(1, context.Store.GetStatistics().LibraryIndexedCount);
        Assert.Equal(0, context.BrowseCache.GetStatistics().EntryCount);
        Assert.Contains(libraryId, context.Coordinator.CacheOnlyLibraries);
    }

    [Fact]
    public void InvalidateCachesAndScheduleRebuild_ClearsCachesButNotIndex()
    {
        using var context = CreateContext();
        var libraryId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        context.Store.MarkLibraryIndexed(libraryId);
        var key = BrowseCacheTestKeys.Create("root", serverBase: string.Empty);
        context.BrowseCache.Set(key, new BrowseCacheEntry("<DIDL-Lite />", 1, 1, 1));

        context.Service.InvalidateCachesAndScheduleRebuild("config");

        Assert.Equal(1, context.Store.GetStatistics().LibraryIndexedCount);
        Assert.Equal(0, context.BrowseCache.GetStatistics().EntryCount);
        Assert.NotEmpty(context.Coordinator.CacheOnlyLibraries);
    }

    [Fact]
    public void InvalidateLibrary_ClearsQueuedWorkForLibrary()
    {
        using var context = CreateContext();
        var libraryId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        context.Service.ScheduleLibraryInvalidation(libraryId, "change");

        context.Service.InvalidateLibrary(libraryId, "manual");

        Assert.DoesNotContain(libraryId, context.Service.PendingLibraries);
    }

    [Fact]
    public void InvalidateAll_ClearsAllQueuedWork()
    {
        using var context = CreateContext();
        context.Service.ScheduleLibraryInvalidation(Guid.NewGuid(), "change");
        context.Service.ScheduleLibraryInvalidation(Guid.NewGuid(), "change");

        context.Service.InvalidateAll("manual");

        Assert.Empty(context.Service.PendingLibraries);
    }

    private static TestContext CreateContext()
    {
        var store = new VirtualIndexStore(new TestApplicationPaths(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dlna-inv-" + Guid.NewGuid().ToString("N"))));
        var browseCache = new BrowseResponseCache(() => new DlnaPluginConfiguration
        {
            EnableBrowseResponseCache = true,
            BrowseResponseCacheTtlSeconds = 0
        });
        var browseNodeCache = new BrowseNodeCache(() => new DlnaPluginConfiguration
        {
            EnableBrowseNodeCache = true,
            BrowseNodeCacheTtlSeconds = 0
        });
        var childCountCache = new ChildCountCache();
        var indexService = new RecordingIndexService(store);
        var coordinator = new RecordingCoordinator();
        var service = new ContentInvalidationService(
            browseCache,
            browseNodeCache,
            childCountCache,
            indexService,
            coordinator,
            new BrowseMetrics(),
            NullLogger<ContentInvalidationService>.Instance);

        return new TestContext(store, browseCache, service, coordinator);
    }

    private sealed class TestContext : IDisposable
    {
        public TestContext(
            VirtualIndexStore store,
            BrowseResponseCache browseCache,
            ContentInvalidationService service,
            RecordingCoordinator coordinator)
        {
            Store = store;
            BrowseCache = browseCache;
            Service = service;
            Coordinator = coordinator;
        }

        public VirtualIndexStore Store { get; }

        public BrowseResponseCache BrowseCache { get; }

        public ContentInvalidationService Service { get; }

        public RecordingCoordinator Coordinator { get; }

        public void Dispose() => Store.Dispose();
    }

    private sealed class RecordingCoordinator : IDlnaIndexRebuildCoordinator
    {
        private readonly List<Guid> _dirty = [];
        private readonly List<Guid> _cacheOnly = [];

        public IReadOnlyCollection<Guid> DirtyLibraries => _dirty;

        public IReadOnlyCollection<Guid> PendingLibraries => [.. _dirty, .. _cacheOnly];

        public IReadOnlyCollection<Guid> CacheOnlyLibraries => _cacheOnly;

        public void MarkLibraryDirty(Guid libraryId, string reason)
        {
            if (new DlnaPluginConfiguration().ShouldAutomaticallyRebuildIndex())
            {
                _dirty.Add(libraryId);
            }
            else
            {
                _cacheOnly.Add(libraryId);
            }
        }

        public void MarkAllLibrariesDirty(string reason)
        {
            if (new DlnaPluginConfiguration().ShouldAutomaticallyRebuildIndex())
            {
                _dirty.Add(Guid.Empty);
            }
            else
            {
                _cacheOnly.Add(Guid.Empty);
            }
        }

        public void MarkLibraryForCacheInvalidation(Guid libraryId, string reason) => _cacheOnly.Add(libraryId);

        public void MarkAllLibrariesForCacheInvalidation(string reason) => _cacheOnly.Add(Guid.Empty);

        public void ClearPendingLibrary(Guid libraryId)
        {
            _dirty.Remove(libraryId);
            _cacheOnly.Remove(libraryId);
        }

        public void ClearPendingWork()
        {
            _dirty.Clear();
            _cacheOnly.Clear();
        }
    }

    private sealed class RecordingIndexService : IDlnaVirtualIndexService
    {
        private readonly IVirtualIndexStore _store;

        public RecordingIndexService(IVirtualIndexStore store)
        {
            _store = store;
            Generation = new DlnaIndexGeneration();
        }

        public DlnaIndexGeneration Generation { get; }

        public bool IsReady(Guid libraryId) => _store.IsLibraryIndexed(libraryId);

        public System.Threading.Tasks.Task RebuildAllAsync(IProgress<double>? progress, CancellationToken cancellationToken)
            => System.Threading.Tasks.Task.CompletedTask;

        public System.Threading.Tasks.Task RebuildLibraryAsync(Guid libraryId, CancellationToken cancellationToken)
            => System.Threading.Tasks.Task.CompletedTask;

        public System.Threading.Tasks.Task<IReadOnlyList<Guid>> TryRebuildLibrariesAsync(
            IReadOnlyList<Guid> libraryIds,
            CancellationToken cancellationToken)
            => System.Threading.Tasks.Task.FromResult<IReadOnlyList<Guid>>(libraryIds);

        public void InvalidateAll() => _store.ClearAll();

        public void InvalidateLibrary(Guid libraryId) => _store.ClearLibrary(libraryId);
    }
}
