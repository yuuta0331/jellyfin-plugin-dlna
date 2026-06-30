using System;
using Jellyfin.Plugin.Dlna.Model;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Dlna.Configuration;

/// <summary>
/// Defines the <see cref="DlnaPluginConfiguration" />.
/// </summary>
public class DlnaPluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether gets or sets a value to indicate the status of the dlna playTo subsystem.
    /// </summary>
    public bool EnablePlayTo { get; set; } = true;

    /// <summary>
    /// Gets or sets the ssdp client discovery interval time (in seconds).
    /// This is the time after which the server will send a ssdp search request.
    /// </summary>
    public int ClientDiscoveryIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets a value indicating whether to blast alive messages.
    /// </summary>
    public bool BlastAliveMessages { get; set; } = true;

    /// <summary>
    /// Gets or sets the frequency at which ssdp alive notifications are transmitted.
    /// </summary>
    public int AliveMessageIntervalSeconds { get; set; }  = 180;

    /// <summary>
    /// Gets or sets a value indicating whether to send only matched host.
    /// </summary>
    public bool SendOnlyMatchedHost { get; set; } = true;

    /// <summary>
    /// Gets or sets the default user account that the dlna server uses.
    /// </summary>
    public Guid? DefaultUserId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable Recently Added Episodes folder.
    /// </summary>
    public bool EnableRecentlyAddedEpisodes { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable Recently Added Series folder.
    /// </summary>
    public bool EnableRecentlyAddedSeries { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable Recently Released Episodes folder.
    /// </summary>
    public bool EnableRecentlyReleasedEpisodes { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable Recently Added Movies folder.
    /// </summary>
    public bool EnableRecentlyAddedMovies { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable Recently Released Movies folder.
    /// </summary>
    public bool EnableRecentlyReleasedMovies { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable Extras folder.
    /// </summary>
    public bool EnableExtras { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable 3D Movies folder.
    /// </summary>
    public bool EnableThreeDMovies { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to enable auto 3D tagging.
    /// </summary>
    public bool EnableAuto3DTagging { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to enable 4K Movies folder.
    /// </summary>
    public bool EnableFourKMovies { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to enable 8K Movies folder.
    /// </summary>
    public bool EnableEightKMovies { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to enable VR Videos folder.
    /// </summary>
    public bool EnableVrMovies { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to enable 8K VR Videos folder.
    /// </summary>
    public bool EnableEightKVrMovies { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to enable auto resolution tagging (4K/8K).
    /// </summary>
    public bool EnableAutoResolutionTagging { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to enable auto VR tagging (VR180/VR360).
    /// </summary>
    public bool EnableAutoVrTagging { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to enable Recently Released Series folder.
    /// </summary>
    public bool EnableRecentlyReleasedSeries { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable Currently Airing folder.
    /// </summary>
    public bool EnableCurrentlyAiring { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable Browse By Kana folder.
    /// </summary>
    public bool EnableBrowseByKana { get; set; } = true;

    /// <summary>
    /// Gets or sets the active title browse preset id.
    /// </summary>
    public string ActiveTitleBrowsePresetId { get; set; } = TitleBrowsePresetDefaults.AlphabetPresetId;

    /// <summary>
    /// Gets or sets title browse presets (built-in and user-defined).
    /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays - Jellyfin configuration pattern
    public TitleBrowsePreset[] TitleBrowsePresets { get; set; } = TitleBrowsePresetDefaults.CreateBuiltInPresets();

    /// <summary>
    /// Gets or sets per-library title browse overrides.
    /// </summary>
    public LibraryTitleBrowseOverride[] LibraryTitleBrowseOverrides { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether empty virtual folders should be hidden.
    /// </summary>
    public bool HideEmptyVirtualFolders { get; set; } = false;
#pragma warning restore CA1819

    /// <summary>
    /// Gets or sets a value indicating whether to enable Browse By Year folder.
    /// </summary>
    public bool EnableBrowseByYear { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether Quest compatibility mode is enabled.
    /// </summary>
    public bool EnableQuestCompatibilityMode { get; set; } = false;

    /// <summary>
    /// Gets or sets how DLNA playback streams are negotiated.
    /// </summary>
    public DlnaPlaybackMode PlaybackMode { get; set; } = DlnaPlaybackMode.Auto;

    /// <summary>
    /// Gets or sets a value indicating whether DLNA transcoding is disabled for all clients.
    /// Legacy setting; migrated to <see cref="PlaybackMode"/> on load.
    /// </summary>
    public bool DisableDlnaTranscoding { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether direct play should be forced for DLNA streams.
    /// Legacy setting; migrated to <see cref="PlaybackMode"/> on load.
    /// </summary>
    public bool ForceDirectPlay { get; set; } = false;

    /// <summary>
    /// Gets or sets the device profile id to use for all DLNA streaming requests, overriding User-Agent matching.
    /// </summary>
    public string? OverrideDeviceProfileId { get; set; }

    /// <summary>
    /// Gets or sets the device profile id to use when no User-Agent match is found.
    /// </summary>
    public string? FallbackDeviceProfileId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether browse summaries include playback stream URLs.
    /// </summary>
    public bool EnsurePlaybackUrlsInBrowse { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of items returned per Browse response.
    /// </summary>
    public int MaxBrowseItemsPerResponse { get; set; } = 1000;

    /// <summary>
    /// Gets or sets a value indicating whether to respect DLNA RequestedCount.
    /// </summary>
    public bool RespectRequestedCount { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to return strict TotalMatches counts.
    /// </summary>
    public bool EnableStrictTotalMatches { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of items in recently-added virtual folders.
    /// </summary>
    public int MaxRecentlyAddedItems { get; set; } = 300;

    /// <summary>
    /// Gets or sets the maximum number of items in the series list (0 = unlimited).
    /// </summary>
    public int MaxSeriesListItems { get; set; } = 0;

    /// <summary>
    /// Gets or sets how folder childCount attributes are calculated.
    /// </summary>
    public ChildCountMode ChildCountCalculation { get; set; } = ChildCountMode.Estimate;

    /// <summary>
    /// Gets or sets a value indicating whether to cache accurate childCount values.
    /// </summary>
    public bool EnableChildCountCache { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to cache generated Browse DIDL-Lite responses.
    /// </summary>
    public bool EnableBrowseResponseCache { get; set; } = true;

    /// <summary>
    /// Gets or sets the Browse response cache TTL in seconds (0 = until library update).
    /// </summary>
    public int BrowseResponseCacheTtlSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets a value indicating whether virtual folder indexes are enabled.
    /// </summary>
    public bool EnableVirtualFolderIndex { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to rebuild indexes after library changes.
    /// </summary>
    public bool RebuildIndexAfterLibraryScan { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to automatically rebuild indexes after debounced library changes.
    /// </summary>
    public bool EnableAutomaticIndexRebuild { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to warm up indexes on startup.
    /// </summary>
    public bool WarmupIndexOnStartup { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to prewarm Browse XML responses after indexing.
    /// </summary>
    public bool PrewarmBrowseResponses { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to invalidate caches by library scope.
    /// </summary>
    public bool InvalidateByLibraryScope { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether library change invalidation is debounced.
    /// </summary>
    public bool DebounceLibraryChangeInvalidation { get; set; } = true;

    /// <summary>
    /// Gets or sets the debounce interval for library change invalidation in seconds.
    /// </summary>
    public int LibraryChangeDebounceSeconds { get; set; } = 600;

    /// <summary>
    /// Gets or sets a value indicating whether to skip index work while Jellyfin scheduled tasks are running.
    /// </summary>
    public bool SkipIndexWhileServerBusy { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to skip index work when library paths are unavailable.
    /// </summary>
    public bool SkipIndexWhenLibraryPathUnavailable { get; set; } = true;

    /// <summary>
    /// Gets or sets minutes to wait before retrying a deferred index rebuild.
    /// </summary>
    public int IndexRebuildRetryMinutes { get; set; } = 10;

    /// <summary>
    /// Gets or sets the minimum minutes between automatic index rebuild runs.
    /// </summary>
    public int MinIndexIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether to prewarm Browse responses after an automatic index rebuild.
    /// </summary>
    public bool PrewarmAfterLibraryRebuild { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether scheduled nightly prewarm is enabled.
    /// </summary>
    public bool EnableScheduledPrewarm { get; set; } = false;

    /// <summary>
    /// Gets or sets the minimum minutes between prewarm runs.
    /// </summary>
    public int PrewarmMinIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the maximum Browse responses generated per prewarm run.
    /// </summary>
    public int MaxPrewarmResponsesPerRun { get; set; } = 150;

    /// <summary>
    /// Gets or sets which Browse paths are pre-generated during prewarm.
    /// </summary>
    public PrewarmScope PrewarmScope { get; set; } = PrewarmScope.Minimal;

    /// <summary>
    /// Gets or sets a value indicating whether to emit verbose index rebuild logs.
    /// </summary>
    public bool LogIndexDetails { get; set; } = false;

    /// <summary>
    /// Returns whether automatic index rebuild after library changes is enabled.
    /// </summary>
    /// <returns>True when either automatic or legacy rebuild setting is enabled.</returns>
    public bool ShouldAutomaticallyRebuildIndex()
        => EnableAutomaticIndexRebuild || RebuildIndexAfterLibraryScan;

    /// <summary>
    /// Gets or sets a value indicating whether to index recently added episodes.
    /// </summary>
    public bool EnableIndexRecentlyAddedEpisodes { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index recently added series.
    /// </summary>
    public bool EnableIndexRecentlyAddedSeries { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index recently added movies.
    /// </summary>
    public bool EnableIndexRecentlyAddedMovies { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index recently updated series.
    /// </summary>
    public bool EnableIndexRecentlyUpdatedSeries { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index kana browse rows.
    /// </summary>
    public bool EnableIndexKana { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index tags.
    /// </summary>
    public bool EnableIndexTag { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index ratings.
    /// </summary>
    public bool EnableIndexRating { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index studios.
    /// </summary>
    public bool EnableIndexStudio { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index extras.
    /// </summary>
    public bool EnableIndexExtras { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index genres.
    /// </summary>
    public bool EnableIndexGenre { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index production years.
    /// </summary>
    public bool EnableIndexYear { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index recently released episodes.
    /// </summary>
    public bool EnableIndexRecentlyReleasedEpisodes { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index recently released movies.
    /// </summary>
    public bool EnableIndexRecentlyReleasedMovies { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index recently released series.
    /// </summary>
    public bool EnableIndexRecentlyReleasedSeries { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index the full series list.
    /// </summary>
    public bool EnableIndexSeriesList { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index the full movies list.
    /// </summary>
    public bool EnableIndexMoviesList { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index seasons under each series.
    /// </summary>
    public bool EnableIndexSeasonList { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index episodes under each season.
    /// </summary>
    public bool EnableIndexEpisodeList { get; set; } = true;

    /// <summary>
    /// Gets or sets the preferred image style for virtual list browse folders.
    /// </summary>
    public DlnaImagePresentation VirtualListImagePresentation { get; set; } = DlnaImagePresentation.Poster;

    /// <summary>
    /// Gets or sets the preferred image style for DLNA search results.
    /// </summary>
    public DlnaImagePresentation SearchImagePresentation { get; set; } = DlnaImagePresentation.Poster;

    /// <summary>
    /// Gets or sets the image source for episode list browse folders.
    /// </summary>
    public EpisodeListImageSource EpisodeListImageSource { get; set; } = EpisodeListImageSource.Episode;

    /// <summary>
    /// Gets or sets a value indicating whether to use item summaries during browse instead of full DTO loads.
    /// </summary>
    public bool EnableItemSummaryBrowse { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable browse node cache (layer 3).
    /// </summary>
    public bool EnableBrowseNodeCache { get; set; } = true;

    /// <summary>
    /// Gets or sets the browse node cache TTL in seconds (0 = no expiry).
    /// </summary>
    public int BrowseNodeCacheTtlSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets a value indicating whether to index music genres.
    /// </summary>
    public bool EnableIndexMusicGenre { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index persons for browse.
    /// </summary>
    public bool EnableIndexPerson { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show Browse By Person folder.
    /// </summary>
    public bool EnableBrowseByPerson { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index recently modified episodes.
    /// </summary>
    public bool EnableIndexRecentlyModifiedEpisodes { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index recently modified movies.
    /// </summary>
    public bool EnableIndexRecentlyModifiedMovies { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to index recently modified series.
    /// </summary>
    public bool EnableIndexRecentlyModifiedSeries { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show recently modified episodes folder.
    /// </summary>
    public bool EnableRecentlyModifiedEpisodes { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show recently modified movies folder.
    /// </summary>
    public bool EnableRecentlyModifiedMovies { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show recently modified series folder.
    /// </summary>
    public bool EnableRecentlyModifiedSeries { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to prewarm physical series/season folders.
    /// </summary>
    public bool PrewarmHierarchyFolders { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of series folders to prewarm under a library.
    /// </summary>
    public int PrewarmHierarchyMaxSeries { get; set; } = 20;

    /// <summary>
    /// Gets or sets the maximum number of season folders to prewarm per series.
    /// </summary>
    public int PrewarmHierarchyMaxSeasonsPerSeries { get; set; } = 3;

    /// <summary>
    /// Gets or sets a value indicating whether to prewarm facet item folders (studio/tag/rating).
    /// </summary>
    public bool PrewarmFacetItemFolders { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to show Recently Updated Series folder.
    /// </summary>
    public bool EnableRecentlyUpdatedSeries { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show Browse By Studio folder.
    /// </summary>
    public bool EnableBrowseByStudio { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show Browse By Tag folder.
    /// </summary>
    public bool EnableBrowseByTag { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show Browse By Rating folder.
    /// </summary>
    public bool EnableBrowseByRating { get; set; } = true;

    /// <summary>
    /// Gets or sets the series count threshold for range folder splitting.
    /// </summary>
    public int LargeFolderRangeSplitThreshold { get; set; } = 500;

    /// <summary>
    /// Gets or sets the number of series per range folder.
    /// </summary>
    public int RangeFolderSize { get; set; } = 500;

    /// <summary>
    /// Gets or sets a value indicating whether to emit verbose DLNA debug logs.
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    /// Gets the effective playback mode, applying legacy flag migration.
    /// </summary>
    public DlnaPlaybackMode GetEffectivePlaybackMode()
    {
        if (PlaybackMode != DlnaPlaybackMode.Auto)
        {
            return PlaybackMode;
        }

        if (DisableDlnaTranscoding || ForceDirectPlay)
        {
            return DlnaPlaybackMode.DirectPlayOnly;
        }

        return DlnaPlaybackMode.Auto;
    }
}

