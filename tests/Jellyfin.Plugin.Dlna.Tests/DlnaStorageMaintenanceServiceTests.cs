using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Jellyfin.Plugin.Dlna.Indexing;
using Jellyfin.Plugin.Dlna.Maintenance;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for <see cref="DlnaStorageMaintenanceService"/>.
/// </summary>
public class DlnaStorageMaintenanceServiceTests
{
    [Fact]
    public void ClearAll_DoesNotRebuildIndex()
    {
        using var context = CreateContext();
        context.BrowseNodeCache.Set(
            BrowseCacheTestKeys.Create("root", serverBase: string.Empty),
            new BrowseNodeCacheEntry([], 0));
        context.Service.ClearAll();

        Assert.Equal(0, context.IndexService.RebuildCount);
        Assert.Equal(0, context.Store.GetStatistics().LibraryIndexedCount);
        Assert.Equal(0, context.BrowseCache.GetStatistics().EntryCount);
        Assert.Equal(0, context.BrowseNodeCache.GetStatistics().EntryCount);
        Assert.Equal(0, context.ChildCountCache.GetStatistics().EntryCount);
    }

    [Fact]
    public void ClearBrowseCache_ClearsResponseAndNodeCaches()
    {
        using var context = CreateContext();
        var key = BrowseCacheTestKeys.Create("root", serverBase: string.Empty);
        context.BrowseCache.Set(key, new BrowseCacheEntry("<DIDL-Lite />", 1, 1, 1));
        context.BrowseNodeCache.Set(key, new BrowseNodeCacheEntry([], 0));

        context.Service.ClearBrowseCache();

        Assert.Equal(0, context.BrowseCache.GetStatistics().EntryCount);
        Assert.Equal(0, context.BrowseNodeCache.GetStatistics().EntryCount);
    }

    [Fact]
    public void GetStats_IncludesStoreAndCacheCounts()
    {
        using var context = CreateContext();
        context.Store.MarkLibraryIndexed(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        context.ChildCountCache.Set(null, "root", string.Empty, 5);
        var key = BrowseCacheTestKeys.Create("root", serverBase: string.Empty);
        context.BrowseNodeCache.Set(key, new BrowseNodeCacheEntry([], 0));

        var stats = context.Service.GetStats();

        Assert.Equal(1, stats.IndexDatabase.LibraryIndexedCount);
        Assert.Equal(1, stats.ChildCountCache.EntryCount);
        Assert.Equal(1, stats.BrowseNodeCache.EntryCount);
    }

    private static MaintenanceTestContext CreateContext()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "dlna-maint-test-" + Guid.NewGuid().ToString("N"));
        var store = new VirtualIndexStore(new TestApplicationPaths(tempDir));
        var browseCache = new BrowseResponseCache(() => new Configuration.DlnaPluginConfiguration
        {
            EnableBrowseResponseCache = true,
            BrowseResponseCacheTtlSeconds = 0
        });
        var browseNodeCache = new BrowseNodeCache(() => new Configuration.DlnaPluginConfiguration
        {
            EnableBrowseNodeCache = true,
            BrowseNodeCacheTtlSeconds = 0
        });
        var childCountCache = new ChildCountCache();
        var indexService = new FakeIndexService(store);
        var prewarmService = new FakePrewarmService();
        var browseMetrics = new BrowseMetrics();
        var libraryNotifier = new LibraryChangeNotifier(null!, null!);
        var service = new DlnaStorageMaintenanceService(
            browseCache,
            browseNodeCache,
            childCountCache,
            store,
            indexService,
            prewarmService,
            browseMetrics,
            libraryNotifier,
            NullLogger<DlnaStorageMaintenanceService>.Instance);

        return new MaintenanceTestContext(tempDir, store, browseCache, browseNodeCache, childCountCache, indexService, service);
    }

    private sealed class MaintenanceTestContext : IDisposable
    {
        public MaintenanceTestContext(
            string tempDir,
            VirtualIndexStore store,
            BrowseResponseCache browseCache,
            BrowseNodeCache browseNodeCache,
            ChildCountCache childCountCache,
            FakeIndexService indexService,
            DlnaStorageMaintenanceService service)
        {
            TempDir = tempDir;
            Store = store;
            BrowseCache = browseCache;
            BrowseNodeCache = browseNodeCache;
            ChildCountCache = childCountCache;
            IndexService = indexService;
            Service = service;
        }

        public string TempDir { get; }

        public VirtualIndexStore Store { get; }

        public BrowseResponseCache BrowseCache { get; }

        public BrowseNodeCache BrowseNodeCache { get; }

        public ChildCountCache ChildCountCache { get; }

        public FakeIndexService IndexService { get; }

        public DlnaStorageMaintenanceService Service { get; }

        public void Dispose()
        {
            Store.Dispose();
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(TempDir))
            {
                Directory.Delete(TempDir, true);
            }
        }
    }

    private sealed class FakeIndexService : IDlnaVirtualIndexService
    {
        private readonly IVirtualIndexStore _store;

        public FakeIndexService(IVirtualIndexStore store)
        {
            _store = store;
            Generation = new DlnaIndexGeneration();
        }

        public int RebuildCount { get; private set; }

        public DlnaIndexGeneration Generation { get; }

        public bool IsReady(Guid libraryId) => _store.IsLibraryIndexed(libraryId);

        public Task RebuildAllAsync(IProgress<double>? progress, CancellationToken cancellationToken)
        {
            RebuildCount++;
            return Task.CompletedTask;
        }

        public Task RebuildLibraryAsync(Guid libraryId, CancellationToken cancellationToken)
        {
            RebuildCount++;
            return Task.CompletedTask;
        }

        public void InvalidateAll()
        {
            _store.ClearAll();
            Generation.Increment();
        }

        public void InvalidateLibrary(Guid libraryId)
        {
            _store.ClearLibrary(libraryId);
            Generation.Increment();
        }
    }

    private sealed class FakePrewarmService : IDlnaBrowsePrewarmService
    {
        public Task PrewarmAsync(Guid? libraryId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class TestApplicationPaths : IApplicationPaths
    {
        public TestApplicationPaths(string pluginConfigurationsPath)
        {
            PluginConfigurationsPath = pluginConfigurationsPath;
            ProgramDataPath = pluginConfigurationsPath;
            ConfigurationDirectoryPath = pluginConfigurationsPath;
            DataPath = pluginConfigurationsPath;
            PluginsPath = pluginConfigurationsPath;
            LogDirectoryPath = pluginConfigurationsPath;
            CachePath = pluginConfigurationsPath;
            WebPath = pluginConfigurationsPath;
            ImageCachePath = pluginConfigurationsPath;
            TempPath = pluginConfigurationsPath;
            TranscodingTempPath = pluginConfigurationsPath;
            MetadataPath = pluginConfigurationsPath;
            RootCachePath = pluginConfigurationsPath;
            TraysPath = pluginConfigurationsPath;
            ProgramSystemPath = pluginConfigurationsPath;
            SystemConfigurationFilePath = Path.Combine(pluginConfigurationsPath, "system.xml");
            TempDirectory = pluginConfigurationsPath;
            VirtualDataPath = pluginConfigurationsPath;
            TrickplayPath = pluginConfigurationsPath;
            BackupPath = pluginConfigurationsPath;
        }

        public string PluginConfigurationsPath { get; }

        public string ProgramDataPath { get; }

        public string ConfigurationDirectoryPath { get; }

        public string DataPath { get; }

        public string PluginsPath { get; }

        public string LogDirectoryPath { get; }

        public string CachePath { get; }

        public string WebPath { get; }

        public string ImageCachePath { get; }

        public string TempPath { get; }

        public string TranscodingTempPath { get; }

        public string MetadataPath { get; }

        public string RootCachePath { get; }

        public string TraysPath { get; }

        public string ProgramSystemPath { get; }

        public string SystemConfigurationFilePath { get; }

        public string TempDirectory { get; }

        public string VirtualDataPath { get; }

        public string TrickplayPath { get; }

        public string BackupPath { get; }

        public void MakeSanityCheckOrThrow()
        {
        }

        public void CreateAndCheckMarker(string path, string markerName, bool recursive = false)
        {
        }
    }
}
