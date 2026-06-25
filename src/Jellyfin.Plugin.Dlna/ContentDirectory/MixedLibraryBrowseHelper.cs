using System.Collections.Generic;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Indexing;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Virtual folder helpers for mixed (TV + movies) libraries.
/// </summary>
internal static class MixedLibraryBrowseHelper
{
    internal static bool IsTvExclusiveStub(StubType stub)
        => stub switch
        {
            StubType.NextUp => true,
            StubType.Series => true,
            StubType.RecentlyAddedEpisodes => true,
            StubType.RecentlyAddedSeries => true,
            StubType.RecentlyReleasedEpisodes => true,
            StubType.RecentlyReleasedSeries => true,
            StubType.RecentlyUpdatedSeries => true,
            StubType.CurrentlyAiring => true,
            StubType.FavoriteSeries => true,
            StubType.FavoriteEpisodes => true,
            StubType.RecentlyModifiedSeries => true,
            StubType.RecentlyModifiedEpisodes => true,
            StubType.SeriesRange => true,
            StubType.SeriesRangeItem => true,
            _ => false
        };

    internal static bool IsMovieExclusiveStub(StubType stub)
        => stub switch
        {
            StubType.ContinueWatching => true,
            StubType.Movies => true,
            StubType.RecentlyAddedMovies => true,
            StubType.RecentlyReleasedMovies => true,
            StubType.Collections => true,
            StubType.Favorites => true,
            StubType.ThreeDMovies => true,
            StubType.FourKMovies => true,
            StubType.EightKMovies => true,
            StubType.VrMovies => true,
            StubType.EightKVrMovies => true,
            StubType.RecentlyModifiedMovies => true,
            StubType.Extras => true,
            _ => false
        };

    internal static bool IsSharedBrowseStub(StubType stub)
        => stub switch
        {
            StubType.Genres => true,
            StubType.BrowseByKana => true,
            StubType.BrowseByKanaRow => true,
            StubType.BrowseByYear => true,
            StubType.BrowseByYearItem => true,
            StubType.BrowseByStudio => true,
            StubType.StudioItem => true,
            StubType.BrowseByTag => true,
            StubType.TagItem => true,
            StubType.BrowseByRating => true,
            StubType.RatingItem => true,
            StubType.BrowseByPerson => true,
            StubType.PersonItem => true,
            _ => false
        };

    internal static ServerItem[] BuildMixedRootFolderList(
        DlnaPluginConfiguration config,
        BaseItem item,
        IVirtualIndexStore store,
        IDlnaVirtualIndexService indexService)
    {
        var serverItemsList = new List<ServerItem>
        {
            new(item, StubType.ContinueWatching),
            new(item, StubType.NextUp)
        };

        if (config.EnableRecentlyAddedEpisodes)
        {
            serverItemsList.Add(new(item, StubType.RecentlyAddedEpisodes));
        }

        if (config.EnableRecentlyAddedSeries)
        {
            serverItemsList.Add(new(item, StubType.RecentlyAddedSeries));
        }

        if (config.EnableRecentlyAddedMovies)
        {
            serverItemsList.Add(new(item, StubType.RecentlyAddedMovies));
        }

        serverItemsList.Add(new(item, StubType.Series));
        serverItemsList.Add(new(item, StubType.Movies));

        if (config.EnableRecentlyUpdatedSeries)
        {
            serverItemsList.Add(new(item, StubType.RecentlyUpdatedSeries));
        }

        if (config.EnableRecentlyReleasedEpisodes)
        {
            serverItemsList.Add(new(item, StubType.RecentlyReleasedEpisodes));
        }

        if (config.EnableRecentlyReleasedSeries)
        {
            serverItemsList.Add(new(item, StubType.RecentlyReleasedSeries));
        }

        if (config.EnableRecentlyReleasedMovies)
        {
            serverItemsList.Add(new(item, StubType.RecentlyReleasedMovies));
        }

        if (config.EnableCurrentlyAiring)
        {
            serverItemsList.Add(new(item, StubType.CurrentlyAiring));
        }

        if (config.EnableThreeDMovies)
        {
            serverItemsList.Add(new(item, StubType.ThreeDMovies));
        }

        if (config.EnableFourKMovies)
        {
            serverItemsList.Add(new(item, StubType.FourKMovies));
        }

        if (config.EnableEightKMovies)
        {
            serverItemsList.Add(new(item, StubType.EightKMovies));
        }

        if (config.EnableVrMovies)
        {
            serverItemsList.Add(new(item, StubType.VrMovies));
        }

        if (config.EnableEightKVrMovies)
        {
            serverItemsList.Add(new(item, StubType.EightKVrMovies));
        }

        serverItemsList.Add(new(item, StubType.Collections));
        serverItemsList.Add(new(item, StubType.Favorites));
        serverItemsList.Add(new(item, StubType.FavoriteSeries));
        serverItemsList.Add(new(item, StubType.FavoriteEpisodes));

        if (config.EnableBrowseByKana)
        {
            serverItemsList.Add(new(item, StubType.BrowseByKana));
        }

        if (config.EnableBrowseByStudio)
        {
            serverItemsList.Add(new(item, StubType.BrowseByStudio));
        }

        if (config.EnableBrowseByTag)
        {
            serverItemsList.Add(new(item, StubType.BrowseByTag));
        }

        if (config.EnableBrowseByRating)
        {
            serverItemsList.Add(new(item, StubType.BrowseByRating));
        }

        if (config.EnableBrowseByPerson)
        {
            serverItemsList.Add(new(item, StubType.BrowseByPerson));
        }

        if (config.EnableRecentlyModifiedSeries)
        {
            serverItemsList.Add(new(item, StubType.RecentlyModifiedSeries));
        }

        if (config.EnableRecentlyModifiedMovies)
        {
            serverItemsList.Add(new(item, StubType.RecentlyModifiedMovies));
        }

        if (config.EnableRecentlyModifiedEpisodes)
        {
            serverItemsList.Add(new(item, StubType.RecentlyModifiedEpisodes));
        }

        if (config.EnableBrowseByYear)
        {
            serverItemsList.Add(new(item, StubType.BrowseByYear));
        }

        serverItemsList.Add(new(item, StubType.Genres));
        serverItemsList.AddRange(IndexBrowseHelper.GetSeriesRangeFolders(store, indexService, config, item));

        return serverItemsList.ToArray();
    }
}
