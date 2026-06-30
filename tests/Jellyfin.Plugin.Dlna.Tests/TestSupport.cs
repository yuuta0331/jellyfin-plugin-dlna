using System;
using System.IO;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.Indexing;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.Profiles;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Shared test helpers.
/// </summary>
internal static class PlaybackTestSupport
{
    public static DefaultProfile CreateProfile() => new();

    public static ItemSummaryRecord CreatePlayableVideoSummary(
        Guid itemId,
        string container = "mkv",
        bool supportsDirectPlay = true)
        => new()
        {
            ItemId = itemId,
            ItemType = BaseItemKind.Movie,
            Name = "Test Movie",
            Container = container,
            RunTimeTicks = TimeSpan.FromMinutes(24).Ticks,
            FileSize = 1_234_567_890,
            VideoWidth = 1920,
            VideoHeight = 1080,
            TotalBitrate = 8_000_000,
            VideoCodec = "hevc",
            AudioCodec = "aac",
            MediaSourceId = itemId.ToString("N"),
            MediaSourceTag = "etag",
            SupportsDirectPlay = supportsDirectPlay
        };
}

/// <summary>
/// Shared test helpers.
/// </summary>
internal sealed class TestApplicationPaths : IApplicationPaths
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
