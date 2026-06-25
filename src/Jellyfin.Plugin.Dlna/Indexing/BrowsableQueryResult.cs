using System.Collections.Generic;
using Jellyfin.Plugin.Dlna.ContentDirectory;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Browse query result that may be backed by item summaries instead of full BaseItem DTOs.
/// </summary>
internal sealed class BrowsableQueryResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BrowsableQueryResult"/> class.
    /// </summary>
    /// <param name="items">The server items.</param>
    /// <param name="totalRecordCount">The total record count.</param>
    /// <param name="summaryHit">Whether all items were resolved from item_summary.</param>
    public BrowsableQueryResult(IReadOnlyList<ServerItem> items, int totalRecordCount, bool summaryHit)
    {
        Items = items;
        TotalRecordCount = totalRecordCount;
        SummaryHit = summaryHit;
    }

    /// <summary>
    /// Gets the server items.
    /// </summary>
    public IReadOnlyList<ServerItem> Items { get; }

    /// <summary>
    /// Gets the total record count.
    /// </summary>
    public int TotalRecordCount { get; }

    /// <summary>
    /// Gets a value indicating whether item summaries were used for all items.
    /// </summary>
    public bool SummaryHit { get; }
}
