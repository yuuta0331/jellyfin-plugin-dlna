using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Jellyfin.Plugin.Dlna.Didl;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Builds virtual folder indexes from Jellyfin library data.
/// </summary>
internal sealed class DlnaIndexBuilder
{
    private readonly ILibraryManager _libraryManager;
    private readonly IVirtualIndexStore _store;
    private readonly IImageProcessor _imageProcessor;
    private readonly ILogger<DlnaIndexBuilder> _logger;

    public DlnaIndexBuilder(
        ILibraryManager libraryManager,
        IVirtualIndexStore store,
        IImageProcessor imageProcessor,
        ILogger<DlnaIndexBuilder> logger)
    {
        _libraryManager = libraryManager;
        _store = store;
        _imageProcessor = imageProcessor;
        _logger = logger;
    }

    public Task BuildLibraryAsync(BaseItem library, DlnaPluginConfiguration config, CancellationToken cancellationToken)
    {
        return Task.Run(() => BuildLibrary(library, config, cancellationToken), cancellationToken);
    }

    private void BuildLibrary(BaseItem library, DlnaPluginConfiguration config, CancellationToken cancellationToken)
    {
        var libraryId = library.Id;
        _store.ClearLibrary(libraryId);

        var summaries = new List<ItemSummaryRecord>();
        var summaryItems = new List<BaseItem>();
        var seriesItems = QueryItems(library, BaseItemKind.Series, cancellationToken);
        var episodeItems = QueryItems(library, BaseItemKind.Episode, cancellationToken);
        var movieItems = QueryItems(library, BaseItemKind.Movie, cancellationToken);
        summaryItems.AddRange(seriesItems);
        summaryItems.AddRange(episodeItems);
        summaryItems.AddRange(movieItems);

        var isMusic = library is IHasCollectionType { CollectionType: CollectionType.music };
        var isMixed = LibraryBrowseQueryHelper.IsMixedLibrary(library);
        var isTv = library is IHasCollectionType { CollectionType: CollectionType.tvshows } || isMixed;
        var isMovie = library is IHasCollectionType { CollectionType: CollectionType.movies } || isMixed;

        IReadOnlyList<BaseItem> seasonItems = [];
        if (config.EnableIndexSeasonList && isTv)
        {
            seasonItems = QueryItems(library, BaseItemKind.Season, cancellationToken);
            summaryItems.AddRange(seasonItems);
        }

        foreach (var item in summaryItems)
        {
            var summary = ToSummary(item);
            DlnaImageResolver.PopulateSummaryImages(item, summary, _imageProcessor, _libraryManager, _logger);
            summaries.Add(summary);
        }

        _store.ReplaceItemSummaries(libraryId, summaries);

        if (config.EnableIndexRecentlyAddedEpisodes)
        {
            _store.ReplaceVirtualList(
                libraryId,
                VirtualListType.RecentlyAddedEpisodes,
                OrderByDate(episodeItems, i => i.DateCreated));
        }

        if (config.EnableIndexRecentlyAddedSeries)
        {
            _store.ReplaceVirtualList(
                libraryId,
                VirtualListType.RecentlyAddedSeries,
                OrderByDate(seriesItems, i => i.DateCreated));
        }

        if (config.EnableIndexRecentlyAddedMovies)
        {
            _store.ReplaceVirtualList(
                libraryId,
                VirtualListType.RecentlyAddedMovies,
                OrderByDate(movieItems, i => i.DateCreated));
        }

        if (config.EnableIndexSeriesList)
        {
            _store.ReplaceVirtualList(
                libraryId,
                VirtualListType.SeriesAll,
                seriesItems.OrderBy(i => i.SortName, StringComparer.OrdinalIgnoreCase).Select(i => i.Id).ToList());
        }

        if (config.EnableIndexMoviesList)
        {
            _store.ReplaceVirtualList(
                libraryId,
                VirtualListType.MoviesAll,
                movieItems.OrderBy(i => i.SortName, StringComparer.OrdinalIgnoreCase).Select(i => i.Id).ToList());
        }

        if (config.EnableIndexRecentlyUpdatedSeries)
        {
            _store.ReplaceVirtualList(
                libraryId,
                VirtualListType.RecentlyUpdatedSeries,
                BuildRecentlyUpdatedSeries(episodeItems, seriesItems));
        }

        if (config.EnableIndexKana)
        {
            BuildKanaIndexes(libraryId, seriesItems, movieItems, config);
        }

        if (config.EnableIndexStudio || config.EnableIndexTag || config.EnableIndexRating)
        {
            BuildFacetIndexes(libraryId, seriesItems, movieItems, config);
        }

        var mediaItems = seriesItems.Concat(movieItems).ToList();
        if (config.EnableIndexGenre)
        {
            _store.ReplaceFacets(libraryId, FacetType.Genre, BuildStringFacet(mediaItems, i => i.Genres));
        }

        if (config.EnableIndexYear)
        {
            _store.ReplaceFacets(libraryId, FacetType.Year, BuildYearFacet(mediaItems));
        }

        if (config.EnableIndexRecentlyReleasedEpisodes)
        {
            _store.ReplaceVirtualList(
                libraryId,
                VirtualListType.RecentlyReleasedEpisodes,
                OrderByDate(episodeItems, GetReleaseDate));
        }

        if (config.EnableIndexRecentlyReleasedMovies)
        {
            _store.ReplaceVirtualList(
                libraryId,
                VirtualListType.RecentlyReleasedMovies,
                OrderByDate(movieItems, GetReleaseDate));
        }

        if (config.EnableIndexRecentlyReleasedSeries)
        {
            _store.ReplaceVirtualList(
                libraryId,
                VirtualListType.RecentlyReleasedSeries,
                BuildRecentlyReleasedSeries(episodeItems, seriesItems));
        }

        if (config.EnableIndexExtras)
        {
            BuildExtrasIndex(libraryId, library, cancellationToken);
        }

        if (config.EnableIndexSeasonList || config.EnableIndexEpisodeList)
        {
            BuildHierarchyIndexes(libraryId, seasonItems, episodeItems, config);
        }

        if (config.EnableIndexRecentlyModifiedEpisodes)
        {
            _store.ReplaceVirtualList(
                libraryId,
                VirtualListType.RecentlyModifiedEpisodes,
                OrderByDate(episodeItems, GetModifiedDate));
        }

        if (config.EnableIndexRecentlyModifiedMovies)
        {
            _store.ReplaceVirtualList(
                libraryId,
                VirtualListType.RecentlyModifiedMovies,
                OrderByDate(movieItems, GetModifiedDate));
        }

        if (config.EnableIndexRecentlyModifiedSeries)
        {
            _store.ReplaceVirtualList(
                libraryId,
                VirtualListType.RecentlyModifiedSeries,
                OrderByDate(seriesItems, GetModifiedDate));
        }

        if (config.EnableIndexPerson && (isTv || isMovie))
        {
            BuildPersonIndex(libraryId, seriesItems, movieItems, cancellationToken);
        }

        if (config.EnableIndexMusicGenre && isMusic)
        {
            var albumItems = QueryItems(library, BaseItemKind.MusicAlbum, cancellationToken);
            _store.ReplaceFacets(libraryId, FacetType.MusicGenre, BuildMusicGenreFacet(albumItems));
        }

        _store.MarkLibraryIndexed(libraryId);
        _logger.LogInformation("DLNA index built for library {LibraryName} ({LibraryId})", library.Name, libraryId);
    }

    private IReadOnlyList<BaseItem> QueryItems(BaseItem library, BaseItemKind itemType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = LibraryBrowseQueryHelper.CreateLibraryQuery(library, itemType);
        return _libraryManager.GetItemsResult(query).Items;
    }

    private static ItemSummaryRecord ToSummary(BaseItem item)
        => new()
        {
            ItemId = item.Id,
            ItemType = item.GetBaseItemKind(),
            Name = item.Name ?? string.Empty,
            SortName = item.SortName ?? item.Name ?? string.Empty,
            ParentId = item.ParentId,
            ProductionYear = item.ProductionYear,
            DateCreatedTicks = item.DateCreated.Ticks,
            PremiereDateTicks = item.PremiereDate?.Ticks,
            IndexNumber = item.IndexNumber,
            IsFolder = item.IsFolder || item.IsDisplayedAsFolder,
            DateModifiedTicks = item.DateModified.Ticks
        };

    private static DateTime GetModifiedDate(BaseItem item) => item.DateModified;

    private static Dictionary<string, IReadOnlyList<Guid>> BuildMusicGenreFacet(IReadOnlyList<BaseItem> albumItems)
        => BuildStringFacet(albumItems, i => i.Genres);

    private static IReadOnlyList<Guid> OrderByDate(IEnumerable<BaseItem> items, Func<BaseItem, DateTime> selector)
        => items
            .OrderByDescending(selector)
            .ThenBy(i => i.SortName, StringComparer.OrdinalIgnoreCase)
            .Select(i => i.Id)
            .ToList();

    private static IReadOnlyList<Guid> BuildRecentlyUpdatedSeries(
        IReadOnlyList<BaseItem> episodes,
        IReadOnlyList<BaseItem> seriesItems)
    {
        var latestBySeries = new Dictionary<Guid, DateTime>();
        foreach (var episode in episodes.OfType<Episode>())
        {
            if (episode.SeriesId == Guid.Empty)
            {
                continue;
            }

            if (!latestBySeries.TryGetValue(episode.SeriesId, out var current) || episode.DateCreated > current)
            {
                latestBySeries[episode.SeriesId] = episode.DateCreated;
            }
        }

        return seriesItems
            .Where(s => latestBySeries.ContainsKey(s.Id))
            .OrderByDescending(s => latestBySeries[s.Id])
            .ThenBy(s => s.SortName, StringComparer.OrdinalIgnoreCase)
            .Select(s => s.Id)
            .ToList();
    }

    private static DateTime GetReleaseDate(BaseItem item)
        => item.PremiereDate ?? item.DateCreated;

    private static IReadOnlyList<Guid> BuildRecentlyReleasedSeries(
        IReadOnlyList<BaseItem> episodes,
        IReadOnlyList<BaseItem> seriesItems)
    {
        var seenSeries = new HashSet<Guid>();
        var seriesList = new List<Guid>();
        var seriesById = seriesItems.ToDictionary(s => s.Id);

        foreach (var item in episodes
                     .OrderByDescending(i => i.PremiereDate ?? i.DateCreated)
                     .ThenBy(i => i.SortName, StringComparer.OrdinalIgnoreCase))
        {
            if (item is not Episode episode)
            {
                continue;
            }

            var seriesId = episode.SeriesId;
            if (seriesId == Guid.Empty || !seenSeries.Add(seriesId))
            {
                continue;
            }

            if (seriesById.ContainsKey(seriesId))
            {
                seriesList.Add(seriesId);
            }
        }

        return seriesList;
    }

    private static Dictionary<string, IReadOnlyList<Guid>> BuildYearFacet(IReadOnlyList<BaseItem> items)
    {
        var map = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (!item.ProductionYear.HasValue)
            {
                continue;
            }

            var key = item.ProductionYear.Value.ToString(CultureInfo.InvariantCulture);
            if (!map.TryGetValue(key, out var list))
            {
                list = [];
                map[key] = list;
            }

            list.Add(item.Id);
        }

        return map.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<Guid>)kvp.Value
                .OrderBy(id => items.First(i => i.Id == id).SortName, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            StringComparer.OrdinalIgnoreCase);
    }

    private void BuildKanaIndexes(
        Guid libraryId,
        IReadOnlyList<BaseItem> seriesItems,
        IReadOnlyList<BaseItem> movieItems,
        DlnaPluginConfiguration config)
    {
        var options = TitleBrowseOptions.FromConfiguration(config, libraryId);
        BuildTitleBrowseForType(libraryId, BaseItemKind.Series, seriesItems, options);
        BuildTitleBrowseForType(libraryId, BaseItemKind.Movie, movieItems, options);
    }

    private void BuildTitleBrowseForType(
        Guid libraryId,
        BaseItemKind itemType,
        IReadOnlyList<BaseItem> items,
        TitleBrowseOptions options)
    {
        var sortNames = items.ToDictionary(i => i.Id, i => i.SortName ?? i.Name ?? string.Empty);
        var buckets = options.Groups.ToDictionary(
            g => g.Id,
            _ => new List<Guid>(),
            StringComparer.Ordinal);

        foreach (var item in items)
        {
            var groupId = TitleBrowseHelper.Classify(item.SortName, item.Name, options);
            if (!buckets.TryGetValue(groupId, out var bucket))
            {
                bucket = [];
                buckets[groupId] = bucket;
            }

            bucket.Add(item.Id);
        }

        foreach (var group in options.Groups)
        {
            if (!buckets.TryGetValue(group.Id, out var ids))
            {
                ids = [];
            }

            var ordered = ids
                .OrderBy(id => sortNames[id], StringComparer.OrdinalIgnoreCase)
                .ToList();
            _store.ReplaceTitleBrowseGroup(libraryId, itemType, group.Id, ordered);
        }
    }

    private void BuildFacetIndexes(
        Guid libraryId,
        IReadOnlyList<BaseItem> seriesItems,
        IReadOnlyList<BaseItem> movieItems,
        DlnaPluginConfiguration config)
    {
        var all = seriesItems.Concat(movieItems).ToList();
        if (config.EnableIndexStudio)
        {
            _store.ReplaceFacets(libraryId, FacetType.Studio, BuildStringFacet(all, i => i.Studios));
        }

        if (config.EnableIndexTag)
        {
            _store.ReplaceFacets(libraryId, FacetType.Tag, BuildStringFacet(all, i => i.Tags));
        }

        if (config.EnableIndexRating)
        {
            _store.ReplaceFacets(
                libraryId,
                FacetType.Rating,
                BuildStringFacet(all, i => string.IsNullOrWhiteSpace(i.OfficialRating) ? [] : [i.OfficialRating]));
        }
    }

    private static Dictionary<string, IReadOnlyList<Guid>> BuildStringFacet(
        IReadOnlyList<BaseItem> items,
        Func<BaseItem, IEnumerable<string>> selector)
    {
        var map = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            foreach (var key in selector(item).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!map.TryGetValue(key, out var list))
                {
                    list = [];
                    map[key] = list;
                }

                list.Add(item.Id);
            }
        }

        return map.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<Guid>)kvp.Value
                .OrderBy(id => items.First(i => i.Id == id).SortName, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            StringComparer.OrdinalIgnoreCase);
    }

    private void BuildHierarchyIndexes(
        Guid libraryId,
        IReadOnlyList<BaseItem> seasonItems,
        IReadOnlyList<BaseItem> episodeItems,
        DlnaPluginConfiguration config)
    {
        if (config.EnableIndexSeasonList)
        {
            _store.ReplaceFacets(
                libraryId,
                FacetType.SeasonOfSeries,
                BuildParentChildrenFacet(seasonItems, s => s.ParentId, SortByIndexNumberThenName));
        }

        if (config.EnableIndexEpisodeList)
        {
            _store.ReplaceFacets(
                libraryId,
                FacetType.EpisodeOfSeason,
                BuildParentChildrenFacet(episodeItems, e => e.ParentId, SortByIndexNumberThenName));
        }
    }

    private void BuildPersonIndex(
        Guid libraryId,
        IReadOnlyList<BaseItem> seriesItems,
        IReadOnlyList<BaseItem> movieItems,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var map = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in seriesItems.Concat(movieItems))
        {
            var people = _libraryManager.GetPeople(new InternalPeopleQuery { ItemId = item.Id });
            foreach (var person in people)
            {
                if (string.IsNullOrWhiteSpace(person.Name))
                {
                    continue;
                }

                if (person.Type is not PersonKind.Actor and not PersonKind.Unknown)
                {
                    continue;
                }

                if (!map.TryGetValue(person.Name, out var list))
                {
                    list = [];
                    map[person.Name] = list;
                }

                if (!list.Contains(item.Id))
                {
                    list.Add(item.Id);
                }
            }
        }

        _store.ReplaceFacets(
            libraryId,
            FacetType.Person,
            map.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<Guid>)kvp.Value,
                StringComparer.OrdinalIgnoreCase));
    }

    private static int SortByIndexNumberThenName(BaseItem a, BaseItem b)
    {
        var indexCompare = (a.IndexNumber ?? int.MaxValue).CompareTo(b.IndexNumber ?? int.MaxValue);
        return indexCompare != 0
            ? indexCompare
            : string.Compare(a.SortName, b.SortName, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, IReadOnlyList<Guid>> BuildParentChildrenFacet(
        IReadOnlyList<BaseItem> items,
        Func<BaseItem, Guid> parentSelector,
        Func<BaseItem, BaseItem, int> compare)
    {
        var itemById = items.ToDictionary(i => i.Id);
        var map = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var parentId = parentSelector(item);
            if (parentId == Guid.Empty)
            {
                continue;
            }

            var key = parentId.ToString("N", CultureInfo.InvariantCulture);
            if (!map.TryGetValue(key, out var list))
            {
                list = [];
                map[key] = list;
            }

            list.Add(item.Id);
        }

        return map.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<Guid>)kvp.Value
                .OrderBy(id => itemById[id], Comparer<BaseItem>.Create((a, b) => compare(a, b)))
                .ToList(),
            StringComparer.OrdinalIgnoreCase);
    }

    private void BuildExtrasIndex(Guid libraryId, BaseItem library, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = LibraryBrowseQueryHelper.CreateLibraryQuery(library, BaseItemKind.Video);
        var videos = _libraryManager.GetItemsResult(query).Items;
        var extras = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
        foreach (var video in videos)
        {
            if (!video.ExtraType.HasValue || video.ParentId == Guid.Empty)
            {
                continue;
            }

            var parentKey = video.ParentId.ToString("N", CultureInfo.InvariantCulture);
            if (!extras.TryGetValue(parentKey, out var list))
            {
                list = [];
                extras[parentKey] = list;
            }

            list.Add(video.Id);
        }

        _store.ReplaceFacets(
            libraryId,
            FacetType.Extra,
            extras.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<Guid>)kvp.Value,
                StringComparer.OrdinalIgnoreCase));
    }
}
