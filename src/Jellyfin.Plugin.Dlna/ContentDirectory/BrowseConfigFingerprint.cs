using System.Collections.Concurrent;
using System;
using Jellyfin.Plugin.Dlna.Configuration;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Computes a fingerprint of browse-affecting plugin configuration.
/// </summary>
public static class BrowseConfigFingerprint
{
    /// <summary>
    /// Computes a fingerprint for the supplied configuration.
    /// </summary>
    /// <param name="config">The plugin configuration.</param>
    /// <returns>A fingerprint value.</returns>
    public static int Compute(DlnaPluginConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var hash = new HashCode();
        hash.Add(config.EnableQuestCompatibilityMode);
        hash.Add(config.EnsurePlaybackUrlsInBrowse);
        hash.Add(config.ChildCountCalculation);
        hash.Add(config.EnableChildCountCache);
        hash.Add(config.RespectRequestedCount);
        hash.Add(config.EnableStrictTotalMatches);
        hash.Add(config.MaxBrowseItemsPerResponse);
        hash.Add(config.MaxRecentlyAddedItems);
        hash.Add(config.MaxSeriesListItems);
        hash.Add(config.EnableRecentlyAddedEpisodes);
        hash.Add(config.EnableRecentlyAddedSeries);
        hash.Add(config.EnableRecentlyReleasedEpisodes);
        hash.Add(config.EnableRecentlyReleasedSeries);
        hash.Add(config.EnableCurrentlyAiring);
        hash.Add(config.EnableRecentlyAddedMovies);
        hash.Add(config.EnableRecentlyReleasedMovies);
        hash.Add(config.EnableThreeDMovies);
        hash.Add(config.EnableFourKMovies);
        hash.Add(config.EnableEightKMovies);
        hash.Add(config.EnableVrMovies);
        hash.Add(config.EnableEightKVrMovies);
        hash.Add(config.EnableBrowseByKana);
        hash.Add(config.EnableBrowseByYear);
        hash.Add(config.EnableExtras);
        hash.Add(config.EnableKanaPrefixStripping);
        hash.Add(config.KanaTitlePrefixes.Length);
        hash.Add(config.EnableVirtualFolderIndex);
        hash.Add(config.EnableRecentlyUpdatedSeries);
        hash.Add(config.EnableBrowseByStudio);
        hash.Add(config.EnableBrowseByTag);
        hash.Add(config.EnableBrowseByRating);
        hash.Add(config.LargeFolderRangeSplitThreshold);
        hash.Add(config.RangeFolderSize);
        hash.Add(config.EnableIndexGenre);
        hash.Add(config.EnableIndexYear);
        hash.Add(config.EnableIndexRecentlyReleasedEpisodes);
        hash.Add(config.EnableIndexRecentlyReleasedMovies);
        hash.Add(config.EnableIndexRecentlyReleasedSeries);
        hash.Add(config.EnableIndexSeriesList);
        hash.Add(config.EnableIndexMoviesList);
        hash.Add(config.EnableIndexSeasonList);
        hash.Add(config.EnableIndexEpisodeList);
        hash.Add(config.PrewarmFacetItemFolders);
        hash.Add(config.EnableItemSummaryBrowse);
        hash.Add(config.VirtualListImagePresentation);
        hash.Add(config.SearchImagePresentation);
        hash.Add(config.EpisodeListImageSource);
        hash.Add(config.EnableBrowseNodeCache);
        hash.Add(config.BrowseNodeCacheTtlSeconds);
        hash.Add(config.EnableIndexMusicGenre);
        hash.Add(config.EnableIndexPerson);
        hash.Add(config.EnableBrowseByPerson);
        hash.Add(config.EnableIndexRecentlyModifiedEpisodes);
        hash.Add(config.EnableIndexRecentlyModifiedMovies);
        hash.Add(config.EnableIndexRecentlyModifiedSeries);
        hash.Add(config.EnableRecentlyModifiedEpisodes);
        hash.Add(config.EnableRecentlyModifiedMovies);
        hash.Add(config.EnableRecentlyModifiedSeries);
        hash.Add(config.PrewarmHierarchyFolders);
        hash.Add(config.PrewarmHierarchyMaxSeries);
        hash.Add(config.PrewarmHierarchyMaxSeasonsPerSeries);
        return hash.ToHashCode();
    }
}
