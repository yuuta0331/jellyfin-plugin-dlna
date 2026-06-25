namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Resolved browse paging parameters.
/// </summary>
public sealed class BrowsePagingContext
{
    /// <summary>
    /// Gets the start index.
    /// </summary>
    public int StartIndex { get; init; }

    /// <summary>
    /// Gets the item limit, or null for unlimited.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Gets a value indicating whether TotalMatches should remain strict.
    /// </summary>
    public bool StrictTotalMatches { get; init; }
}
