using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Indexing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Determines whether virtual folders should be shown when empty folders are hidden.
/// </summary>
internal static class VirtualFolderEmptyChecker
{
    internal static void AddIfVisible(
        ICollection<ServerItem> items,
        BaseItem library,
        User? user,
        StubType stubType,
        DlnaPluginConfiguration config,
        IVirtualIndexStore store,
        IDlnaVirtualIndexService indexService,
        ILibraryManager? libraryManager,
        bool enabled = true)
    {
        if (!enabled)
        {
            return;
        }

        if (ShouldShow(stubType, library, user, config, store, indexService, libraryManager))
        {
            items.Add(new ServerItem(library, stubType));
        }
    }

    internal static bool ShouldShow(
        StubType stubType,
        BaseItem library,
        User? user,
        DlnaPluginConfiguration config,
        IVirtualIndexStore store,
        IDlnaVirtualIndexService indexService,
        ILibraryManager? libraryManager)
    {
        if (!config.HideEmptyVirtualFolders)
        {
            return true;
        }

        if (libraryManager is null)
        {
            return true;
        }

        if (library.Id == Guid.Empty)
        {
            return true;
        }

        if (TryGetVirtualListType(stubType, out var listType))
        {
            return HasVirtualListItems(library.Id, listType, config, store, indexService, libraryManager, user, stubType);
        }

        return stubType switch
        {
            StubType.BrowseByKana => HasAnyTitleBrowseItems(library.Id, config, store, indexService),
            StubType.Genres => HasFacetItems(library.Id, FacetType.Genre, config, store, indexService),
            StubType.BrowseByStudio => HasFacetItems(library.Id, FacetType.Studio, config, store, indexService),
            StubType.BrowseByTag => HasFacetItems(library.Id, FacetType.Tag, config, store, indexService),
            StubType.BrowseByRating => HasFacetItems(library.Id, FacetType.Rating, config, store, indexService),
            StubType.BrowseByPerson => HasFacetItems(library.Id, FacetType.Person, config, store, indexService),
            StubType.BrowseByYear => HasFacetItems(library.Id, FacetType.Year, config, store, indexService),
            StubType.Collections => HasCollectionItems(user, libraryManager),
            StubType.ContinueWatching => HasContinueWatchingItems(library, user, libraryManager),
            StubType.NextUp => HasNextUpItems(library, user, libraryManager),
            StubType.Favorites or StubType.FavoriteSeries or StubType.FavoriteEpisodes
                or StubType.FavoriteAlbums or StubType.FavoriteArtists or StubType.FavoriteSongs
                => HasFavoriteItems(library, user, libraryManager, stubType),
            StubType.ThreeDMovies => HasTaggedMovies(library, user, libraryManager, "3D"),
            StubType.FourKMovies => HasTaggedMovies(library, user, libraryManager, "4K"),
            StubType.EightKMovies => HasTaggedMovies(library, user, libraryManager, "8K"),
            StubType.VrMovies => HasTaggedMovies(library, user, libraryManager, "VR180", "VR360"),
            StubType.EightKVrMovies => HasTaggedMovies(library, user, libraryManager, "8K VR180", "8K VR360"),
            StubType.CurrentlyAiring => HasCurrentlyAiringItems(library, user, libraryManager),
            _ => true
        };
    }

    private static bool TryGetVirtualListType(StubType stubType, out VirtualListType listType)
    {
        switch (stubType)
        {
            case StubType.RecentlyAddedEpisodes:
                listType = VirtualListType.RecentlyAddedEpisodes;
                return true;
            case StubType.RecentlyAddedSeries:
                listType = VirtualListType.RecentlyAddedSeries;
                return true;
            case StubType.RecentlyAddedMovies:
                listType = VirtualListType.RecentlyAddedMovies;
                return true;
            case StubType.RecentlyReleasedEpisodes:
                listType = VirtualListType.RecentlyReleasedEpisodes;
                return true;
            case StubType.RecentlyReleasedMovies:
                listType = VirtualListType.RecentlyReleasedMovies;
                return true;
            case StubType.RecentlyReleasedSeries:
                listType = VirtualListType.RecentlyReleasedSeries;
                return true;
            case StubType.RecentlyUpdatedSeries:
                listType = VirtualListType.RecentlyUpdatedSeries;
                return true;
            case StubType.RecentlyModifiedEpisodes:
                listType = VirtualListType.RecentlyModifiedEpisodes;
                return true;
            case StubType.RecentlyModifiedMovies:
                listType = VirtualListType.RecentlyModifiedMovies;
                return true;
            case StubType.RecentlyModifiedSeries:
                listType = VirtualListType.RecentlyModifiedSeries;
                return true;
            case StubType.Series:
                listType = VirtualListType.SeriesAll;
                return true;
            case StubType.Movies:
                listType = VirtualListType.MoviesAll;
                return true;
            default:
                listType = default;
                return false;
        }
    }

    private static bool HasVirtualListItems(
        Guid libraryId,
        VirtualListType listType,
        DlnaPluginConfiguration config,
        IVirtualIndexStore store,
        IDlnaVirtualIndexService indexService,
        ILibraryManager? libraryManager,
        User? user,
        StubType stubType)
    {
        if (config.EnableVirtualFolderIndex
            && IndexBrowseHelper.CanUseIndex(config, indexService, libraryId)
            && IndexBrowseHelper.IsVirtualListBrowseEnabled(config, listType))
        {
            return store.GetVirtualListCount(libraryId, listType) > 0;
        }

        return HasFallbackItems(libraryId, user, libraryManager, stubType);
    }

    private static bool HasAnyTitleBrowseItems(
        Guid libraryId,
        DlnaPluginConfiguration config,
        IVirtualIndexStore store,
        IDlnaVirtualIndexService indexService)
    {
        if (config.EnableVirtualFolderIndex && IndexBrowseHelper.CanUseIndex(config, indexService, libraryId))
        {
            return store.GetTitleBrowseGroupCounts(libraryId, BaseItemKind.Series).Any(c => c.Count > 0)
                || store.GetTitleBrowseGroupCounts(libraryId, BaseItemKind.Movie).Any(c => c.Count > 0);
        }

        return true;
    }

    private static bool HasFacetItems(
        Guid libraryId,
        FacetType facetType,
        DlnaPluginConfiguration config,
        IVirtualIndexStore store,
        IDlnaVirtualIndexService indexService)
    {
        if (config.EnableVirtualFolderIndex && IndexBrowseHelper.CanUseIndex(config, indexService, libraryId))
        {
            return store.GetFacetKeys(libraryId, facetType).Count > 0;
        }

        return true;
    }

    private static bool HasFallbackItems(Guid libraryId, User? user, ILibraryManager? libraryManager, StubType stubType)
    {
        if (user is null)
        {
            return true;
        }

        if (libraryManager is null)
        {
            return true;
        }

        var library = libraryManager.GetItemById(libraryId);
        if (library is null)
        {
            return false;
        }

        var query = new InternalItemsQuery(user)
        {
            ParentId = libraryId,
            Limit = 1,
            EnableTotalRecordCount = true,
            IsPlaceHolder = false
        };

        var itemTypes = stubType switch
        {
            StubType.RecentlyAddedEpisodes or StubType.RecentlyReleasedEpisodes or StubType.RecentlyModifiedEpisodes
                => (BaseItemKind[])[BaseItemKind.Episode],
            StubType.RecentlyAddedSeries or StubType.RecentlyReleasedSeries or StubType.RecentlyUpdatedSeries
                or StubType.RecentlyModifiedSeries or StubType.Series
                => [BaseItemKind.Series],
            StubType.RecentlyAddedMovies or StubType.RecentlyReleasedMovies or StubType.RecentlyModifiedMovies
                or StubType.Movies
                => [BaseItemKind.Movie],
            _ => null
        };

        if (itemTypes is null)
        {
            return true;
        }

        query.IncludeItemTypes = itemTypes;

        return libraryManager.GetItemsResult(query).TotalRecordCount > 0;
    }

    private static bool HasCollectionItems(User? user, ILibraryManager libraryManager)
    {
        if (user is null)
        {
            return true;
        }

        var query = new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.BoxSet],
            Limit = 1,
            EnableTotalRecordCount = true,
            IsPlaceHolder = false
        };
        return libraryManager.GetItemsResult(query).TotalRecordCount > 0;
    }

    private static bool HasContinueWatchingItems(BaseItem library, User? user, ILibraryManager libraryManager)
    {
        if (user is null)
        {
            return true;
        }

        var query = new InternalItemsQuery(user)
        {
            ParentId = library.Id,
            Limit = 1,
            EnableTotalRecordCount = true,
            IsPlaceHolder = false,
            IsResumable = true
        };
        return libraryManager.GetItemsResult(query).TotalRecordCount > 0;
    }

    private static bool HasNextUpItems(BaseItem library, User? user, ILibraryManager libraryManager)
    {
        if (user is null)
        {
            return true;
        }

        var query = new InternalItemsQuery(user)
        {
            ParentId = library.Id,
            IncludeItemTypes = [BaseItemKind.Episode],
            Limit = 1,
            EnableTotalRecordCount = true,
            IsPlaceHolder = false
        };
        return libraryManager.GetItemsResult(query).TotalRecordCount > 0;
    }

    private static bool HasFavoriteItems(BaseItem library, User? user, ILibraryManager libraryManager, StubType stubType)
    {
        if (user is null)
        {
            return true;
        }

        var query = new InternalItemsQuery(user)
        {
            ParentId = library.Id,
            Limit = 1,
            EnableTotalRecordCount = true,
            IsPlaceHolder = false,
            IsFavorite = true
        };

        query.IncludeItemTypes = stubType switch
        {
            StubType.FavoriteSeries => [BaseItemKind.Series],
            StubType.FavoriteEpisodes => [BaseItemKind.Episode],
            StubType.FavoriteAlbums => [BaseItemKind.MusicAlbum],
            StubType.FavoriteArtists => [BaseItemKind.MusicArtist],
            StubType.FavoriteSongs => [BaseItemKind.Audio],
            _ => [BaseItemKind.Movie, BaseItemKind.Series, BaseItemKind.Episode]
        };

        return libraryManager.GetItemsResult(query).TotalRecordCount > 0;
    }

    private static bool HasTaggedMovies(BaseItem library, User? user, ILibraryManager libraryManager, params string[] tags)
    {
        if (user is null)
        {
            return true;
        }

        var query = new InternalItemsQuery(user)
        {
            ParentId = library.Id,
            IncludeItemTypes = [BaseItemKind.Movie],
            Limit = 1,
            EnableTotalRecordCount = true,
            IsPlaceHolder = false,
            Tags = tags
        };
        return libraryManager.GetItemsResult(query).TotalRecordCount > 0;
    }

    private static bool HasCurrentlyAiringItems(BaseItem library, User? user, ILibraryManager libraryManager)
    {
        if (user is null)
        {
            return true;
        }

        var query = new InternalItemsQuery(user)
        {
            ParentId = library.Id,
            IncludeItemTypes = [BaseItemKind.Series],
            Limit = 1,
            EnableTotalRecordCount = true,
            IsPlaceHolder = false,
            IsAiring = true
        };
        return libraryManager.GetItemsResult(query).TotalRecordCount > 0;
    }
}
