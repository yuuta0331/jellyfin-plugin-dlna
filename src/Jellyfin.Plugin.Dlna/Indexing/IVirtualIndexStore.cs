using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Persistent virtual folder index storage.
/// </summary>
public interface IVirtualIndexStore
{
    /// <summary>
    /// Clears all index data.
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Gets index storage statistics.
    /// </summary>
    /// <returns>Index storage statistics.</returns>
    IndexStoreStatistics GetStatistics();

    /// <summary>
    /// Gets the database file path.
    /// </summary>
    string DatabasePath { get; }

    /// <summary>
    /// Clears index data for a library.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    void ClearLibrary(Guid libraryId);

    /// <summary>
    /// Returns whether a library index exists.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    /// <returns>True when indexed.</returns>
    bool IsLibraryIndexed(Guid libraryId);

    /// <summary>
    /// Marks a library as indexed.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    void MarkLibraryIndexed(Guid libraryId);

    /// <summary>
    /// Replaces item summaries for a library.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    /// <param name="summaries">The summaries.</param>
    void ReplaceItemSummaries(Guid libraryId, IReadOnlyList<ItemSummaryRecord> summaries);

    /// <summary>
    /// Gets item summaries for the supplied item ids.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    /// <param name="itemIds">The item ids.</param>
    /// <returns>Summaries keyed by item id.</returns>
    IReadOnlyDictionary<Guid, ItemSummaryRecord> GetItemSummaries(Guid libraryId, IReadOnlyList<Guid> itemIds);

    /// <summary>
    /// Replaces a virtual list.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    /// <param name="listType">The list type.</param>
    /// <param name="itemIds">Ordered item ids.</param>
    void ReplaceVirtualList(Guid libraryId, VirtualListType listType, IReadOnlyList<Guid> itemIds);

    /// <summary>
    /// Gets a virtual list.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    /// <param name="listType">The list type.</param>
    /// <returns>Ordered item ids.</returns>
    IReadOnlyList<Guid> GetVirtualList(Guid libraryId, VirtualListType listType);

    /// <summary>
    /// Replaces kana row entries for a library and item type.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    /// <param name="itemType">The item type.</param>
    /// <param name="rowIndex">The row index.</param>
    /// <param name="itemIds">Ordered item ids.</param>
    void ReplaceKanaRow(Guid libraryId, BaseItemKind itemType, int rowIndex, IReadOnlyList<Guid> itemIds);

    /// <summary>
    /// Gets kana row item ids.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    /// <param name="itemType">The item type.</param>
    /// <param name="rowIndex">The row index.</param>
    /// <returns>Ordered item ids.</returns>
    IReadOnlyList<Guid> GetKanaRow(Guid libraryId, BaseItemKind itemType, int rowIndex);

    /// <summary>
    /// Replaces facet entries for a library.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    /// <param name="facetType">The facet type.</param>
    /// <param name="entries">Facet key to item ids.</param>
    void ReplaceFacets(Guid libraryId, FacetType facetType, IReadOnlyDictionary<string, IReadOnlyList<Guid>> entries);

    /// <summary>
    /// Gets facet keys for a library.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    /// <param name="facetType">The facet type.</param>
    /// <returns>Facet keys with counts.</returns>
    IReadOnlyList<FacetKeyCount> GetFacetKeys(Guid libraryId, FacetType facetType);

    /// <summary>
    /// Gets item ids for a facet key.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    /// <param name="facetType">The facet type.</param>
    /// <param name="facetKey">The facet key.</param>
    /// <returns>Ordered item ids.</returns>
    IReadOnlyList<Guid> GetFacetItems(Guid libraryId, FacetType facetType, string facetKey);

    /// <summary>
    /// Gets the series count for range splitting.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    /// <returns>The count.</returns>
    int GetSeriesCount(Guid libraryId);
}
