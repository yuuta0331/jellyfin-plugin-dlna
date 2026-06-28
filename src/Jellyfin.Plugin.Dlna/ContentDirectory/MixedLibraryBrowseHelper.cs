using System.Collections.Generic;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Indexing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

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
        IDlnaVirtualIndexService indexService,
        ILibraryManager? libraryManager,
        User? user)
    {
        var serverItemsList = new List<ServerItem>();
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.ContinueWatching, config, store, indexService, libraryManager);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.NextUp, config, store, indexService, libraryManager);

        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.RecentlyAddedEpisodes, config, store, indexService, libraryManager, config.EnableRecentlyAddedEpisodes);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.RecentlyAddedSeries, config, store, indexService, libraryManager, config.EnableRecentlyAddedSeries);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.RecentlyAddedMovies, config, store, indexService, libraryManager, config.EnableRecentlyAddedMovies);

        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.Series, config, store, indexService, libraryManager);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.Movies, config, store, indexService, libraryManager);

        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.RecentlyUpdatedSeries, config, store, indexService, libraryManager, config.EnableRecentlyUpdatedSeries);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.RecentlyReleasedEpisodes, config, store, indexService, libraryManager, config.EnableRecentlyReleasedEpisodes);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.RecentlyReleasedSeries, config, store, indexService, libraryManager, config.EnableRecentlyReleasedSeries);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.RecentlyReleasedMovies, config, store, indexService, libraryManager, config.EnableRecentlyReleasedMovies);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.CurrentlyAiring, config, store, indexService, libraryManager, config.EnableCurrentlyAiring);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.ThreeDMovies, config, store, indexService, libraryManager, config.EnableThreeDMovies);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.FourKMovies, config, store, indexService, libraryManager, config.EnableFourKMovies);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.EightKMovies, config, store, indexService, libraryManager, config.EnableEightKMovies);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.VrMovies, config, store, indexService, libraryManager, config.EnableVrMovies);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.EightKVrMovies, config, store, indexService, libraryManager, config.EnableEightKVrMovies);

        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.Collections, config, store, indexService, libraryManager);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.Favorites, config, store, indexService, libraryManager);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.FavoriteSeries, config, store, indexService, libraryManager);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.FavoriteEpisodes, config, store, indexService, libraryManager);

        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.BrowseByKana, config, store, indexService, libraryManager, config.EnableBrowseByKana);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.BrowseByStudio, config, store, indexService, libraryManager, config.EnableBrowseByStudio);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.BrowseByTag, config, store, indexService, libraryManager, config.EnableBrowseByTag);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.BrowseByRating, config, store, indexService, libraryManager, config.EnableBrowseByRating);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.BrowseByPerson, config, store, indexService, libraryManager, config.EnableBrowseByPerson);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.RecentlyModifiedSeries, config, store, indexService, libraryManager, config.EnableRecentlyModifiedSeries);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.RecentlyModifiedMovies, config, store, indexService, libraryManager, config.EnableRecentlyModifiedMovies);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.RecentlyModifiedEpisodes, config, store, indexService, libraryManager, config.EnableRecentlyModifiedEpisodes);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.BrowseByYear, config, store, indexService, libraryManager, config.EnableBrowseByYear);
        VirtualFolderEmptyChecker.AddIfVisible(serverItemsList, item, user, StubType.Genres, config, store, indexService, libraryManager);
        serverItemsList.AddRange(IndexBrowseHelper.GetSeriesRangeFolders(store, indexService, config, item));

        return serverItemsList.ToArray();
    }
}
