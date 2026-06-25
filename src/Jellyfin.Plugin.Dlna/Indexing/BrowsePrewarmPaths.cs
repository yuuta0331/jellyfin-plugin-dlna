using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Jellyfin.Plugin.Dlna.Didl;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Builds ObjectIDs to prewarm after index rebuild.
/// </summary>
internal static class BrowsePrewarmPaths
{
    internal static IReadOnlyList<string> GetObjectIds(
        DlnaPluginConfiguration config,
        BaseItem library,
        IVirtualIndexStore store,
        IDlnaVirtualIndexService indexService,
        ILibraryManager libraryManager)
    {
        if (!indexService.IsReady(library.Id))
        {
            return [];
        }

        var libraryId = library.Id;
        var isTv = library is IHasCollectionType { CollectionType: CollectionType.tvshows };
        var isMovie = library is IHasCollectionType { CollectionType: CollectionType.movies };
        var isMixed = LibraryBrowseQueryHelper.IsMixedLibrary(library);
        var isMusic = library is IHasCollectionType { CollectionType: CollectionType.music };
        var isHomeVideos = LibraryBrowseQueryHelper.IsHomeVideosLibrary(library);
        var isMusicVideos = LibraryBrowseQueryHelper.IsMusicVideosLibrary(library);
        var ids = new List<string>
        {
            libraryId.ToString("N", CultureInfo.InvariantCulture)
        };

        if (isMixed)
        {
            isTv = true;
            isMovie = true;
        }

        if (config.EnableRecentlyAddedSeries && isTv)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.RecentlyAddedSeries));
        }

        if (config.EnableRecentlyAddedEpisodes && isTv)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.RecentlyAddedEpisodes));
        }

        if (config.EnableRecentlyUpdatedSeries && isTv)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.RecentlyUpdatedSeries));
        }

        if (config.EnableRecentlyReleasedEpisodes && isTv)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.RecentlyReleasedEpisodes));
        }

        if (config.EnableRecentlyReleasedSeries && isTv)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.RecentlyReleasedSeries));
        }

        if (config.EnableRecentlyAddedMovies && isMovie)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.RecentlyAddedMovies));
        }

        if (config.EnableRecentlyReleasedMovies && isMovie)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.RecentlyReleasedMovies));
        }

        if (config.EnableExtras)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.Extras));
        }

        if (config.EnableBrowseByKana)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.BrowseByKana));
            for (var row = 0; row < 10; row++)
            {
                ids.Add(DidlBuilder.GetKanaRowClientId(libraryId, row));
            }
        }

        if (config.EnableBrowseByStudio)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.BrowseByStudio));
        }

        if (config.EnableBrowseByTag)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.BrowseByTag));
        }

        if (config.EnableBrowseByRating)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.BrowseByRating));
        }

        if (isTv || isMovie)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.Genres));
            AddGenrePrewarmPaths(ids, libraryId, store, libraryManager);
        }

        if (config.EnableBrowseByYear)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.BrowseByYear));
            AddYearPrewarmPaths(ids, libraryId, store);
        }

        if (config.PrewarmFacetItemFolders)
        {
            AddFacetItemPrewarmPaths(ids, libraryId, store, config);
        }

        if (isMusic && config.EnableIndexMusicGenre)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.Genres));
            AddMusicGenrePrewarmPaths(ids, libraryId, store, libraryManager);
        }

        if (isTv || isMovie)
        {
            if (config.EnableBrowseByPerson)
            {
                ids.Add(DidlBuilder.GetClientId(libraryId, StubType.BrowseByPerson));
            }

            if (config.EnableRecentlyModifiedSeries && isTv)
            {
                ids.Add(DidlBuilder.GetClientId(libraryId, StubType.RecentlyModifiedSeries));
            }

            if (config.EnableRecentlyModifiedEpisodes && isTv)
            {
                ids.Add(DidlBuilder.GetClientId(libraryId, StubType.RecentlyModifiedEpisodes));
            }

            if (config.EnableRecentlyModifiedMovies && isMovie)
            {
                ids.Add(DidlBuilder.GetClientId(libraryId, StubType.RecentlyModifiedMovies));
            }
        }

        if (config.PrewarmHierarchyFolders && isTv)
        {
            AddHierarchyPrewarmPaths(ids, libraryId, store, config);
        }

        if (isMovie && config.EnableIndexMoviesList)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.Movies));
        }

        if (isTv)
        {
            var seriesCount = store.GetSeriesCount(libraryId);
            if (seriesCount > config.LargeFolderRangeSplitThreshold)
            {
                var size = Math.Max(1, config.RangeFolderSize);
                for (var start = 0; start < seriesCount; start += size)
                {
                    var end = Math.Min(start + size, seriesCount);
                    ids.Add(DidlBuilder.GetSeriesRangeClientId(libraryId, start, end));
                }
            }
            else
            {
                ids.Add(DidlBuilder.GetClientId(libraryId, StubType.Series));
            }
        }

        if (isHomeVideos)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.Latest));
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.Videos));
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.Photos));
            if (config.EnableBrowseByYear)
            {
                ids.Add(DidlBuilder.GetClientId(libraryId, StubType.BrowseByYear));
            }

            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.Favorites));
        }

        if (isMusicVideos)
        {
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.Latest));
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.MusicVideos));
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.Artists));
            ids.Add(DidlBuilder.GetClientId(libraryId, StubType.Genres));
        }

        return ids;
    }

    private static void AddGenrePrewarmPaths(
        ICollection<string> ids,
        Guid libraryId,
        IVirtualIndexStore store,
        ILibraryManager libraryManager)
    {
        foreach (var key in store.GetFacetKeys(libraryId, FacetType.Genre))
        {
            try
            {
                var genre = libraryManager.GetGenre(key.Key);
                if (genre is not null)
                {
                    ids.Add(DidlBuilder.GetLibraryScopedGenreClientId(libraryId, genre.Id));
                }
            }
            catch
            {
                // Skip genres that cannot be resolved during prewarm.
            }
        }
    }

    private static void AddYearPrewarmPaths(ICollection<string> ids, Guid libraryId, IVirtualIndexStore store)
    {
        foreach (var key in store.GetFacetKeys(libraryId, FacetType.Year))
        {
            if (int.TryParse(key.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
            {
                ids.Add(DidlBuilder.GetYearClientId(libraryId, year));
            }
        }
    }

    private static void AddFacetItemPrewarmPaths(
        ICollection<string> ids,
        Guid libraryId,
        IVirtualIndexStore store,
        DlnaPluginConfiguration config)
    {
        if (config.EnableBrowseByStudio && config.EnableIndexStudio)
        {
            foreach (var key in store.GetFacetKeys(libraryId, FacetType.Studio))
            {
                ids.Add(DidlBuilder.GetFacetClientId(StubType.StudioItem, libraryId, key.Key));
            }
        }

        if (config.EnableBrowseByTag && config.EnableIndexTag)
        {
            foreach (var key in store.GetFacetKeys(libraryId, FacetType.Tag))
            {
                ids.Add(DidlBuilder.GetFacetClientId(StubType.TagItem, libraryId, key.Key));
            }
        }

        if (config.EnableBrowseByRating && config.EnableIndexRating)
        {
            foreach (var key in store.GetFacetKeys(libraryId, FacetType.Rating))
            {
                ids.Add(DidlBuilder.GetFacetClientId(StubType.RatingItem, libraryId, key.Key));
            }
        }

        if (config.EnableBrowseByPerson && config.EnableIndexPerson)
        {
            foreach (var key in store.GetFacetKeys(libraryId, FacetType.Person))
            {
                ids.Add(DidlBuilder.GetFacetClientId(StubType.PersonItem, libraryId, key.Key));
            }
        }
    }

    private static void AddMusicGenrePrewarmPaths(
        ICollection<string> ids,
        Guid libraryId,
        IVirtualIndexStore store,
        ILibraryManager libraryManager)
    {
        foreach (var key in store.GetFacetKeys(libraryId, FacetType.MusicGenre))
        {
            try
            {
                var genre = libraryManager.GetMusicGenre(key.Key);
                if (genre is not null)
                {
                    ids.Add(DidlBuilder.GetLibraryScopedMusicGenreClientId(libraryId, genre.Id));
                }
            }
            catch
            {
                // Skip genres that cannot be resolved during prewarm.
            }
        }
    }

    private static void AddHierarchyPrewarmPaths(
        ICollection<string> ids,
        Guid libraryId,
        IVirtualIndexStore store,
        DlnaPluginConfiguration config)
    {
        var seriesIds = store.GetVirtualList(libraryId, VirtualListType.SeriesAll);
        var maxSeries = Math.Max(0, config.PrewarmHierarchyMaxSeries);
        var maxSeasons = Math.Max(0, config.PrewarmHierarchyMaxSeasonsPerSeries);

        foreach (var seriesId in seriesIds.Take(maxSeries))
        {
            ids.Add(seriesId.ToString("N", CultureInfo.InvariantCulture));
            var parentKey = seriesId.ToString("N", CultureInfo.InvariantCulture);
            var seasonIds = store.GetFacetItems(libraryId, FacetType.SeasonOfSeries, parentKey);
            foreach (var seasonId in seasonIds.Take(maxSeasons))
            {
                ids.Add(seasonId.ToString("N", CultureInfo.InvariantCulture));
            }
        }
    }
}
