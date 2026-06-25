namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Precomputed virtual folder list types.
/// </summary>
public enum VirtualListType
{
    /// <summary>
    /// Recently added episodes list.
    /// </summary>
    RecentlyAddedEpisodes,

    /// <summary>
    /// Recently added series list.
    /// </summary>
    RecentlyAddedSeries,

    /// <summary>
    /// Recently added movies list.
    /// </summary>
    RecentlyAddedMovies,

    /// <summary>
    /// Recently updated series list.
    /// </summary>
    RecentlyUpdatedSeries,

    /// <summary>
    /// All series sorted by name.
    /// </summary>
    SeriesAll,

    /// <summary>
    /// All movies sorted by name.
    /// </summary>
    MoviesAll,

    /// <summary>
    /// Recently released episodes list.
    /// </summary>
    RecentlyReleasedEpisodes,

    /// <summary>
    /// Recently released movies list.
    /// </summary>
    RecentlyReleasedMovies,

    /// <summary>
    /// Recently released series list.
    /// </summary>
    RecentlyReleasedSeries,

    /// <summary>
    /// Recently modified episodes list.
    /// </summary>
    RecentlyModifiedEpisodes,

    /// <summary>
    /// Recently modified movies list.
    /// </summary>
    RecentlyModifiedMovies,

    /// <summary>
    /// Recently modified series list.
    /// </summary>
    RecentlyModifiedSeries,
}
