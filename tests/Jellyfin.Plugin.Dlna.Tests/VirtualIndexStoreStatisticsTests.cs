using System;
using System.IO;
using Jellyfin.Plugin.Dlna.Indexing;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for <see cref="VirtualIndexStore.GetStatistics"/>.
/// </summary>
public class VirtualIndexStoreStatisticsTests
{
    [Fact]
    public void GetStatistics_ReturnsCountsAndFileSize()
    {
        var libraryId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tempDir = Path.Combine(Path.GetTempPath(), "dlna-index-test-" + Guid.NewGuid().ToString("N"));

        try
        {
            using var store = new VirtualIndexStore(new TestApplicationPaths(tempDir));
            store.MarkLibraryIndexed(libraryId);
            store.ReplaceVirtualList(libraryId, VirtualListType.SeriesAll, [itemId]);

            var stats = store.GetStatistics();

            Assert.Contains("dlna-index.db", stats.DatabasePath, StringComparison.Ordinal);
            Assert.True(stats.FileSizeBytes > 0);
            Assert.Equal(1, stats.LibraryIndexedCount);
            Assert.Equal(1, stats.VirtualListCount);
            Assert.Single(stats.IndexedLibraryIds);
            Assert.Equal(libraryId, stats.IndexedLibraryIds[0]);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
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
