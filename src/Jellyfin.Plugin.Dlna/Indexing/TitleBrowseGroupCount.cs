namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Item count for a title browse group within a library.
/// </summary>
public sealed class TitleBrowseGroupCount
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TitleBrowseGroupCount"/> class.
    /// </summary>
    /// <param name="groupId">The group id.</param>
    /// <param name="count">The item count.</param>
    public TitleBrowseGroupCount(string groupId, int count)
    {
        GroupId = groupId;
        Count = count;
    }

    /// <summary>
    /// Gets the group id.
    /// </summary>
    public string GroupId { get; }

    /// <summary>
    /// Gets the item count.
    /// </summary>
    public int Count { get; }
}
