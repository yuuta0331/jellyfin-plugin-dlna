using System;
using MediaBrowser.Controller.Entities;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Indexing;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Defines the <see cref="ServerItem" />.
/// </summary>
internal sealed class ServerItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerItem"/> class from a summary record.
    /// </summary>
    /// <param name="summary">The item summary.</param>
    public ServerItem(ItemSummaryRecord summary)
    {
        Summary = summary;
        Item = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerItem"/> class.
    /// </summary>
    /// <param name="item">The <see cref="BaseItem"/>.</param>
    /// <param name="stubType">The stub type.</param>
    /// <param name="libraryScopeId">Optional library folder id when the item is scoped to a library (e.g. genres).</param>
    /// <param name="kanaRowIndex">Optional legacy kana row index for fifty-sound browse.</param>
    /// <param name="titleBrowseGroupId">Optional title browse group id.</param>
    /// <param name="productionYear">Optional production year for year browse.</param>
    /// <param name="facetKey">Optional facet key for studio/tag/rating browse.</param>
    /// <param name="rangeStart">Optional inclusive range start index for series range folders.</param>
    /// <param name="rangeEnd">Optional exclusive range end index for series range folders.</param>
    public ServerItem(
        BaseItem item,
        StubType? stubType,
        Guid? libraryScopeId = null,
        int? kanaRowIndex = null,
        string? titleBrowseGroupId = null,
        int? productionYear = null,
        string? facetKey = null,
        int? rangeStart = null,
        int? rangeEnd = null)
    {
        Item = item;
        LibraryScopeId = libraryScopeId;
        KanaRowIndex = kanaRowIndex;
        TitleBrowseGroupId = titleBrowseGroupId ?? (kanaRowIndex is int legacyRow
            ? TitleBrowsePresetDefaults.LegacyRowIndexToGroupId(legacyRow)
            : null);
        ProductionYear = productionYear;
        FacetKey = facetKey;
        RangeStart = rangeStart;
        RangeEnd = rangeEnd;

        if (stubType.HasValue)
        {
            StubType = stubType;
        }
        else if (item is IItemByName and not Folder)
        {
            StubType = ContentDirectory.StubType.Folder;
        }
    }

    /// <summary>
    /// Gets the underlying base item when loaded from Jellyfin.
    /// </summary>
    public BaseItem Item { get; }

    /// <summary>
    /// Gets the indexed summary when resolved from item_summary without a full DTO load.
    /// </summary>
    public ItemSummaryRecord? Summary { get; }

    /// <summary>
    /// Gets a value indicating whether this item is summary-backed.
    /// </summary>
    public bool IsSummaryBacked => Summary is not null;

    /// <summary>
    /// Gets the DLNA item type.
    /// </summary>
    public StubType? StubType { get; }

    /// <summary>
    /// Gets the library folder id when this item is scoped to a specific library (e.g. genre under a TV library).
    /// </summary>
    public Guid? LibraryScopeId { get; }

    /// <summary>
    /// Gets the kana row index (0 = あ行, 9 = わ行) for fifty-sound browse folders.
    /// </summary>
    public int? KanaRowIndex { get; }

    /// <summary>
    /// Gets the title browse group id for title browse folders.
    /// </summary>
    public string? TitleBrowseGroupId { get; }

    /// <summary>
    /// Gets the production year for year browse folders.
    /// </summary>
    public int? ProductionYear { get; }

    /// <summary>
    /// Gets the facet key for studio/tag/rating browse folders.
    /// </summary>
    public string? FacetKey { get; }

    /// <summary>
    /// Gets the inclusive start index for series range folders.
    /// </summary>
    public int? RangeStart { get; }

    /// <summary>
    /// Gets the exclusive end index for series range folders.
    /// </summary>
    public int? RangeEnd { get; }
}
