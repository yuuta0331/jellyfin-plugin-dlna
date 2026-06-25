using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using UserView = MediaBrowser.Controller.Entities.UserView;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Shared library query helpers for browse and indexing.
/// </summary>
internal static class LibraryBrowseQueryHelper
{
    internal static void ApplyLibraryQueryScope(InternalItemsQuery query, BaseItem parent)
    {
        query.Parent = parent;
        query.ParentId = parent.Id;
        query.Recursive = true;
    }

    internal static InternalItemsQuery CreateLibraryQuery(BaseItem library, BaseItemKind itemType)
    {
        var query = new InternalItemsQuery
        {
            IncludeItemTypes = [itemType],
            IsPlaceHolder = false,
            IsVirtualItem = false,
            DtoOptions = new DtoOptions(false)
        };
        ApplyLibraryQueryScope(query, library);
        return query;
    }

    internal static bool IsDlnaLibraryView(BaseItem item)
    {
        if (item is UserView)
        {
            return true;
        }

        // Mixed libraries (CollectionTypeOptions.mixed) are stored with null CollectionType on CollectionFolder.
        return item is CollectionFolder;
    }

    internal static bool ShouldRouteLibraryByCollectionType(BaseItem item, StubType? stubType)
    {
        if (item is not IHasCollectionType)
        {
            return false;
        }

        // folder_<guid> IDs set StubType.Folder; library roots must still route to virtual folders.
        return stubType != StubType.Folder || IsDlnaLibraryView(item);
    }

    internal static Guid ResolveLibraryId(ILibraryManager libraryManager, BaseItem item)
    {
        if (LibraryBrowseQueryHelper.IsDlnaLibraryView(item))
        {
            return item.Id;
        }

        var topParent = item.GetTopParent();
        if (topParent is not null && LibraryBrowseQueryHelper.IsDlnaLibraryView(topParent))
        {
            return topParent.Id;
        }

        var folder = libraryManager.GetCollectionFolders(item).FirstOrDefault();
        return folder?.Id ?? Guid.Empty;
    }

    internal static bool IsMixedLibrary(BaseItem item)
    {
        if (item is not IHasCollectionType hasType)
        {
            return false;
        }

        // API label CollectionTypeOptions.mixed has no CollectionType.mixed enum; Jellyfin stores null (sometimes unknown).
        return hasType.CollectionType is null or CollectionType.unknown;
    }

    internal static bool IsHomeVideosLibrary(BaseItem item)
        => item is IHasCollectionType { CollectionType: CollectionType.homevideos };

    internal static bool IsMusicVideosLibrary(BaseItem item)
        => item is IHasCollectionType { CollectionType: CollectionType.musicvideos };

    internal static bool SupportsTvVirtualFolders(BaseItem item)
        => item is IHasCollectionType { CollectionType: CollectionType.tvshows }
            || IsMixedLibrary(item);

    internal static bool SupportsMovieVirtualFolders(BaseItem item)
        => item is IHasCollectionType { CollectionType: CollectionType.movies }
            || IsMixedLibrary(item);

    internal static IReadOnlyList<BaseItemKind> GetGenreBrowseItemTypes(BaseItem library)
    {
        if (IsMixedLibrary(library))
        {
            return [BaseItemKind.Movie, BaseItemKind.Series];
        }

        if (library is IHasCollectionType { CollectionType: CollectionType.movies })
        {
            return [BaseItemKind.Movie];
        }

        return [BaseItemKind.Series];
    }
}
