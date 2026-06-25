using System;
using System.Collections.Generic;
using System.IO;
using Jellyfin.Plugin.Dlna.Indexing;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for genre and year facet storage.
/// </summary>
public class VirtualIndexStoreFacetTests
{
    [Fact]
    public void GenreAndYearFacets_RoundTrip()
    {
        var libraryId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var itemA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var itemB = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var tempDir = Path.Combine(Path.GetTempPath(), "dlna-index-test-" + Guid.NewGuid().ToString("N"));

        try
        {
            using var store = new VirtualIndexStore(new TestApplicationPaths(tempDir));

            store.ReplaceFacets(
                libraryId,
                FacetType.Genre,
                new Dictionary<string, IReadOnlyList<Guid>>
                {
                    ["Action"] = [itemA],
                    ["Drama"] = [itemB]
                });

            store.ReplaceFacets(
                libraryId,
                FacetType.Year,
                new Dictionary<string, IReadOnlyList<Guid>>
                {
                    ["2024"] = [itemA],
                    ["2023"] = [itemB]
                });

            var genreKeys = store.GetFacetKeys(libraryId, FacetType.Genre);
            Assert.Equal(2, genreKeys.Count);
            Assert.Equal([itemA], store.GetFacetItems(libraryId, FacetType.Genre, "Action"));
            Assert.Equal([itemB], store.GetFacetItems(libraryId, FacetType.Genre, "Drama"));

            var yearKeys = store.GetFacetKeys(libraryId, FacetType.Year);
            Assert.Equal(2, yearKeys.Count);
            Assert.Equal([itemA], store.GetFacetItems(libraryId, FacetType.Year, "2024"));
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

    [Fact]
    public void RecentlyReleasedVirtualLists_RoundTrip()
    {
        var libraryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var episodeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var tempDir = Path.Combine(Path.GetTempPath(), "dlna-index-test-" + Guid.NewGuid().ToString("N"));

        try
        {
            using var store = new VirtualIndexStore(new TestApplicationPaths(tempDir));

            store.ReplaceVirtualList(libraryId, VirtualListType.RecentlyReleasedEpisodes, [episodeId]);
            store.ReplaceVirtualList(libraryId, VirtualListType.RecentlyReleasedSeries, [episodeId]);
            store.ReplaceVirtualList(libraryId, VirtualListType.RecentlyReleasedMovies, [episodeId]);

            Assert.Equal([episodeId], store.GetVirtualList(libraryId, VirtualListType.RecentlyReleasedEpisodes));
            Assert.Equal([episodeId], store.GetVirtualList(libraryId, VirtualListType.RecentlyReleasedSeries));
            Assert.Equal([episodeId], store.GetVirtualList(libraryId, VirtualListType.RecentlyReleasedMovies));
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
