using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Jellyfin.Plugin.Dlna.Didl;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Resolves browse results from virtual indexes with Jellyfin fallback.
/// </summary>
internal static class IndexBrowseHelper
{
    internal static bool CanUseIndex(DlnaPluginConfiguration config, IDlnaVirtualIndexService indexService, Guid libraryId)
        => config.EnableVirtualFolderIndex && indexService.IsReady(libraryId);

    internal static BrowsableQueryResult? TryGetVirtualList(
        IVirtualIndexStore store,
        ILibraryManager libraryManager,
        IDlnaVirtualIndexService indexService,
        DlnaPluginConfiguration config,
        BaseItem parent,
        InternalItemsQuery query,
        VirtualListType listType,
        BrowseTimingScope? timing)
    {
        if (!IsVirtualListBrowseEnabled(config, listType) || !CanUseIndex(config, indexService, parent.Id))
        {
            return null;
        }

        var sw = Stopwatch.StartNew();
        var ids = store.GetVirtualList(parent.Id, listType);
        sw.Stop();
        timing?.AddIndexMs(sw.ElapsedMilliseconds);

        return LoadBrowsableItems(parent.Id, store, libraryManager, config, query, ids, timing);
    }

    internal static BrowsableQueryResult? TryGetTitleBrowseGroup(
        IVirtualIndexStore store,
        ILibraryManager libraryManager,
        IDlnaVirtualIndexService indexService,
        DlnaPluginConfiguration config,
        BaseItem parent,
        InternalItemsQuery query,
        BaseItemKind itemType,
        string groupId,
        BrowseTimingScope? timing)
    {
        if (!config.EnableIndexKana || !CanUseIndex(config, indexService, parent.Id))
        {
            return null;
        }

        var sw = Stopwatch.StartNew();
        var ids = store.GetTitleBrowseGroup(parent.Id, itemType, groupId);
        sw.Stop();
        timing?.AddIndexMs(sw.ElapsedMilliseconds);

        return LoadBrowsableItems(parent.Id, store, libraryManager, config, query, ids, timing);
    }

    internal static BrowsableQueryResult? TryGetMixedTitleBrowseGroup(
        IVirtualIndexStore store,
        ILibraryManager libraryManager,
        IDlnaVirtualIndexService indexService,
        DlnaPluginConfiguration config,
        BaseItem parent,
        InternalItemsQuery query,
        string groupId,
        BrowseTimingScope? timing)
    {
        if (!config.EnableIndexKana || !CanUseIndex(config, indexService, parent.Id))
        {
            return null;
        }

        var sw = Stopwatch.StartNew();
        var seriesIds = store.GetTitleBrowseGroup(parent.Id, BaseItemKind.Series, groupId);
        var movieIds = store.GetTitleBrowseGroup(parent.Id, BaseItemKind.Movie, groupId);
        var ids = seriesIds.Concat(movieIds).Distinct().ToList();

        if (ids.Count > 0 && config.EnableItemSummaryBrowse)
        {
            var summaries = store.GetItemSummaries(parent.Id, ids);
            ids = ids
                .OrderBy(
                    id => summaries.TryGetValue(id, out var summary) ? summary.SortName : id.ToString(),
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        sw.Stop();
        timing?.AddIndexMs(sw.ElapsedMilliseconds);

        return LoadBrowsableItems(parent.Id, store, libraryManager, config, query, ids, timing);
    }

    internal static BrowsableQueryResult? TryGetKanaRow(
        IVirtualIndexStore store,
        ILibraryManager libraryManager,
        IDlnaVirtualIndexService indexService,
        DlnaPluginConfiguration config,
        BaseItem parent,
        InternalItemsQuery query,
        BaseItemKind itemType,
        int rowIndex,
        BrowseTimingScope? timing)
        => TryGetTitleBrowseGroup(
            store,
            libraryManager,
            indexService,
            config,
            parent,
            query,
            itemType,
            TitleBrowsePresetDefaults.LegacyRowIndexToGroupId(rowIndex),
            timing);

    internal static BrowsableQueryResult? TryGetMixedKanaRow(
        IVirtualIndexStore store,
        ILibraryManager libraryManager,
        IDlnaVirtualIndexService indexService,
        DlnaPluginConfiguration config,
        BaseItem parent,
        InternalItemsQuery query,
        int rowIndex,
        BrowseTimingScope? timing)
        => TryGetMixedTitleBrowseGroup(
            store,
            libraryManager,
            indexService,
            config,
            parent,
            query,
            TitleBrowsePresetDefaults.LegacyRowIndexToGroupId(rowIndex),
            timing);

    internal static BrowsableQueryResult? TryGetFacetItems(
        IVirtualIndexStore store,
        ILibraryManager libraryManager,
        IDlnaVirtualIndexService indexService,
        DlnaPluginConfiguration config,
        BaseItem parent,
        InternalItemsQuery query,
        FacetType facetType,
        string facetKey,
        BrowseTimingScope? timing)
    {
        if (!IsFacetIndexEnabled(config, facetType) || !CanUseIndex(config, indexService, parent.Id))
        {
            return null;
        }

        var sw = Stopwatch.StartNew();
        var ids = store.GetFacetItems(parent.Id, facetType, facetKey);
        sw.Stop();
        timing?.AddIndexMs(sw.ElapsedMilliseconds);

        return LoadBrowsableItems(parent.Id, store, libraryManager, config, query, ids, timing);
    }

    internal static BrowsableQueryResult? TryGetParentChildren(
        IVirtualIndexStore store,
        ILibraryManager libraryManager,
        IDlnaVirtualIndexService indexService,
        DlnaPluginConfiguration config,
        BaseItem parentItem,
        InternalItemsQuery query,
        FacetType facetType,
        BrowseTimingScope? timing)
    {
        var libraryId = LibraryBrowseQueryHelper.ResolveLibraryId(libraryManager, parentItem);
        if (libraryId == Guid.Empty || !IsFacetIndexEnabled(config, facetType) || !CanUseIndex(config, indexService, libraryId))
        {
            return null;
        }

        var parentKey = parentItem.Id.ToString("N", CultureInfo.InvariantCulture);
        var sw = Stopwatch.StartNew();
        var ids = store.GetFacetItems(libraryId, facetType, parentKey);
        sw.Stop();
        timing?.AddIndexMs(sw.ElapsedMilliseconds);

        return LoadBrowsableItems(libraryId, store, libraryManager, config, query, ids, timing);
    }

    internal static BrowsableQueryResult? TryGetSeriesRange(
        IVirtualIndexStore store,
        ILibraryManager libraryManager,
        IDlnaVirtualIndexService indexService,
        DlnaPluginConfiguration config,
        BaseItem parent,
        InternalItemsQuery query,
        int rangeStart,
        int rangeEnd,
        BrowseTimingScope? timing)
    {
        if (!config.EnableIndexSeriesList || !CanUseIndex(config, indexService, parent.Id))
        {
            return null;
        }

        var sw = Stopwatch.StartNew();
        var allIds = store.GetVirtualList(parent.Id, VirtualListType.SeriesAll);
        var slice = allIds.Skip(rangeStart).Take(rangeEnd - rangeStart).ToList();
        sw.Stop();
        timing?.AddIndexMs(sw.ElapsedMilliseconds);

        return LoadBrowsableItems(parent.Id, store, libraryManager, config, query, slice, timing);
    }

    internal static BrowsableQueryResult? TryGetExtras(
        IVirtualIndexStore store,
        ILibraryManager libraryManager,
        IDlnaVirtualIndexService indexService,
        DlnaPluginConfiguration config,
        BaseItem parentItem,
        InternalItemsQuery query,
        BrowseTimingScope? timing)
    {
        var libraryId = LibraryBrowseQueryHelper.ResolveLibraryId(libraryManager, parentItem);
        if (libraryId == Guid.Empty || !config.EnableIndexExtras || !CanUseIndex(config, indexService, libraryId))
        {
            return null;
        }

        var parentKey = parentItem.Id.ToString("N", CultureInfo.InvariantCulture);
        var sw = Stopwatch.StartNew();
        var ids = store.GetFacetItems(libraryId, FacetType.Extra, parentKey);
        sw.Stop();
        timing?.AddIndexMs(sw.ElapsedMilliseconds);

        if (ids.Count == 0)
        {
            return null;
        }

        return LoadBrowsableItems(libraryId, store, libraryManager, config, query, ids, timing);
    }

    internal static IReadOnlyList<ServerItem> GetFacetFolders(
        IVirtualIndexStore store,
        IDlnaVirtualIndexService indexService,
        DlnaPluginConfiguration config,
        BaseItem library,
        FacetType facetType,
        StubType folderStubType)
    {
        if (!IsFacetIndexEnabled(config, facetType) || !CanUseIndex(config, indexService, library.Id))
        {
            return [];
        }

        return store.GetFacetKeys(library.Id, facetType)
            .OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase)
            .Select(k => new ServerItem(library, folderStubType, library.Id, facetKey: k.Key))
            .ToList();
    }

    internal static IReadOnlyList<ServerItem>? TryGetMusicGenreFolders(
        ILibraryManager libraryManager,
        IVirtualIndexStore store,
        IDlnaVirtualIndexService indexService,
        DlnaPluginConfiguration config,
        BaseItem library,
        ILogger logger,
        BrowseTimingScope? timing)
    {
        if (!config.EnableIndexMusicGenre || !CanUseIndex(config, indexService, library.Id))
        {
            return null;
        }

        var sw = Stopwatch.StartNew();
        var keys = store.GetFacetKeys(library.Id, FacetType.MusicGenre);
        sw.Stop();
        timing?.AddIndexMs(sw.ElapsedMilliseconds);

        var genreItems = new List<ServerItem>();
        foreach (var key in keys.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var genre = libraryManager.GetMusicGenre(key.Key);
                if (genre is not null)
                {
                    genreItems.Add(new ServerItem(genre, null, library.Id));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting music genre {GenreName}", key.Key);
            }
        }

        return genreItems;
    }

    internal static IReadOnlyList<ServerItem> GetSeriesRangeFolders(
        IVirtualIndexStore store,
        IDlnaVirtualIndexService indexService,
        DlnaPluginConfiguration config,
        BaseItem library)
    {
        if (!CanUseIndex(config, indexService, library.Id))
        {
            return [];
        }

        if (!config.EnableIndexSeriesList)
        {
            return [];
        }

        var count = store.GetSeriesCount(library.Id);
        if (count <= config.LargeFolderRangeSplitThreshold)
        {
            return [];
        }

        var size = Math.Max(1, config.RangeFolderSize);
        var folders = new List<ServerItem>();
        for (var start = 0; start < count; start += size)
        {
            var end = Math.Min(start + size, count);
            folders.Add(new ServerItem(library, StubType.SeriesRange, library.Id, rangeStart: start, rangeEnd: end));
        }

        return folders;
    }

    internal static IReadOnlyList<ServerItem>? TryGetGenreFolders(
        ILibraryManager libraryManager,
        IVirtualIndexStore store,
        IDlnaVirtualIndexService indexService,
        DlnaPluginConfiguration config,
        BaseItem library,
        ILogger logger,
        BrowseTimingScope? timing)
    {
        if (!config.EnableIndexGenre || !CanUseIndex(config, indexService, library.Id))
        {
            return null;
        }

        var sw = Stopwatch.StartNew();
        var keys = store.GetFacetKeys(library.Id, FacetType.Genre);
        sw.Stop();
        timing?.AddIndexMs(sw.ElapsedMilliseconds);

        var genreItems = new List<ServerItem>();
        foreach (var key in keys.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var genre = libraryManager.GetGenre(key.Key);
                if (genre is not null)
                {
                    genreItems.Add(new ServerItem(genre, null, library.Id));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting genre {GenreName}", key.Key);
            }
        }

        return genreItems;
    }

    internal static IReadOnlyList<ServerItem>? TryGetYearFolders(
        IVirtualIndexStore store,
        IDlnaVirtualIndexService indexService,
        DlnaPluginConfiguration config,
        BaseItem library,
        BrowseTimingScope? timing)
    {
        if (!config.EnableIndexYear || !CanUseIndex(config, indexService, library.Id))
        {
            return null;
        }

        var sw = Stopwatch.StartNew();
        var keys = store.GetFacetKeys(library.Id, FacetType.Year);
        sw.Stop();
        timing?.AddIndexMs(sw.ElapsedMilliseconds);

        return keys
            .Select(k => k.Key)
            .Where(y => int.TryParse(y, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            .Select(y => int.Parse(y, CultureInfo.InvariantCulture))
            .OrderByDescending(y => y)
            .Select(y => new ServerItem(library, StubType.BrowseByYearItem, library.Id, productionYear: y))
            .ToList();
    }

    internal static bool IsFacetIndexEnabled(DlnaPluginConfiguration config, FacetType facetType)
        => facetType switch
        {
            FacetType.Studio => config.EnableIndexStudio,
            FacetType.Tag => config.EnableIndexTag,
            FacetType.Rating => config.EnableIndexRating,
            FacetType.Extra => config.EnableIndexExtras,
            FacetType.Genre => config.EnableIndexGenre,
            FacetType.Year => config.EnableIndexYear,
            FacetType.SeasonOfSeries => config.EnableIndexSeasonList,
            FacetType.EpisodeOfSeason => config.EnableIndexEpisodeList,
            FacetType.MusicGenre => config.EnableIndexMusicGenre,
            FacetType.Person => config.EnableIndexPerson,
            _ => true
        };

    internal static bool IsVirtualListBrowseEnabled(DlnaPluginConfiguration config, VirtualListType listType)
        => listType switch
        {
            VirtualListType.SeriesAll => config.EnableIndexSeriesList,
            VirtualListType.MoviesAll => config.EnableIndexMoviesList,
            VirtualListType.RecentlyModifiedEpisodes => config.EnableIndexRecentlyModifiedEpisodes,
            VirtualListType.RecentlyModifiedMovies => config.EnableIndexRecentlyModifiedMovies,
            VirtualListType.RecentlyModifiedSeries => config.EnableIndexRecentlyModifiedSeries,
            _ => true
        };

    private static BrowsableQueryResult LoadBrowsableItems(
        Guid libraryId,
        IVirtualIndexStore store,
        ILibraryManager libraryManager,
        DlnaPluginConfiguration config,
        InternalItemsQuery sourceQuery,
        IReadOnlyList<Guid> ids,
        BrowseTimingScope? timing)
    {
        if (ids.Count == 0)
        {
            return new BrowsableQueryResult([], 0, false);
        }

        if (config.EnableItemSummaryBrowse && libraryId != Guid.Empty)
        {
            var summarySw = Stopwatch.StartNew();
            var summaries = store.GetItemSummaries(libraryId, ids);
            summarySw.Stop();
            timing?.AddSummaryMs(summarySw.ElapsedMilliseconds);

            if (summaries.Count == ids.Count)
            {
                var summaryItems = ids.Select(id => new ServerItem(summaries[id])).ToList();
                return new BrowsableQueryResult(summaryItems, ids.Count, true);
            }

            if (summaries.Count > 0)
            {
                var missingIds = ids.Where(id => !summaries.ContainsKey(id)).ToList();
                var dtoSw = Stopwatch.StartNew();
                var baseItems = LoadBaseItemsByIds(libraryManager, sourceQuery, missingIds);
                dtoSw.Stop();
                timing?.AddDtoMs(dtoSw.ElapsedMilliseconds);

                var dtoById = baseItems.Items.ToDictionary(item => item.Id);
                var hybridItems = new List<ServerItem>(ids.Count);
                foreach (var id in ids)
                {
                    if (summaries.TryGetValue(id, out var summary))
                    {
                        hybridItems.Add(new ServerItem(summary));
                    }
                    else if (dtoById.TryGetValue(id, out var baseItem))
                    {
                        hybridItems.Add(new ServerItem(baseItem, null));
                    }
                }

                return new BrowsableQueryResult(hybridItems, ids.Count, summaries.Count > 0);
            }
        }

        var fullDtoSw = Stopwatch.StartNew();
        var allBaseItems = LoadBaseItemsByIds(libraryManager, sourceQuery, ids);
        fullDtoSw.Stop();
        timing?.AddDtoMs(fullDtoSw.ElapsedMilliseconds);

        var allServerItems = allBaseItems.Items.Select(item => new ServerItem(item, null)).ToList();
        return new BrowsableQueryResult(allServerItems, allBaseItems.TotalRecordCount, false);
    }

    private static QueryResult<BaseItem> LoadBaseItemsByIds(
        ILibraryManager libraryManager,
        InternalItemsQuery sourceQuery,
        IReadOnlyList<Guid> ids)
    {
        var query = new InternalItemsQuery(sourceQuery.User)
        {
            ItemIds = ids.ToArray(),
            DtoOptions = sourceQuery.DtoOptions ?? new DtoOptions(false),
            OrderBy = sourceQuery.OrderBy
        };

        if (sourceQuery.User is not null)
        {
            query.SetUser(sourceQuery.User);
        }

        var result = libraryManager.GetItemsResult(query);
        var orderMap = ids.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        var ordered = result.Items
            .OrderBy(item => orderMap.GetValueOrDefault(item.Id, int.MaxValue))
            .ToList();

        return new QueryResult<BaseItem>(null, ids.Count, ordered);
    }
}
