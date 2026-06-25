using Jellyfin.Plugin.Dlna.ContentDirectory;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.Dlna.Didl;

/// <summary>
/// Browse context used to select poster vs thumbnail presentation.
/// </summary>
public enum DlnaImageBrowseContext
{
    /// <summary>
    /// Default browse context.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Virtual list folders (movies, series, facets, etc.).
    /// </summary>
    VirtualList = 1,

    /// <summary>
    /// DLNA search results.
    /// </summary>
    Search = 2,

    /// <summary>
    /// Episode-oriented lists.
    /// </summary>
    EpisodeList = 3,

    /// <summary>
    /// Season lists under a series.
    /// </summary>
    SeasonList = 4,

    /// <summary>
    /// Music library browse lists.
    /// </summary>
    MusicList = 5
}

/// <summary>
/// Maps parent browse nodes to image browse contexts.
/// </summary>
public static class DlnaImageBrowseContextMapper
{
    /// <summary>
    /// Resolves the image browse context from the parent folder.
    /// </summary>
    public static DlnaImageBrowseContext FromParent(StubType? parentStub, BaseItem? parentItem)
    {
        if (parentStub.HasValue)
        {
            return FromStub(parentStub.Value);
        }

        if (parentItem is Season)
        {
            return DlnaImageBrowseContext.EpisodeList;
        }

        if (parentItem is Series)
        {
            return DlnaImageBrowseContext.SeasonList;
        }

        if (parentItem is MusicArtist)
        {
            return DlnaImageBrowseContext.MusicList;
        }

        return DlnaImageBrowseContext.Default;
    }

    private static DlnaImageBrowseContext FromStub(StubType stub)
        => stub switch
        {
            StubType.Movies
                or StubType.Series
                or StubType.RecentlyAddedSeries
                or StubType.RecentlyAddedMovies
                or StubType.RecentlyReleasedSeries
                or StubType.RecentlyReleasedMovies
                or StubType.RecentlyModifiedSeries
                or StubType.RecentlyModifiedMovies
                or StubType.RecentlyUpdatedSeries
                or StubType.Collections
                or StubType.ThreeDMovies
                or StubType.FourKMovies
                or StubType.EightKMovies
                or StubType.VrMovies
                or StubType.EightKVrMovies
                or StubType.SeriesRange
                or StubType.SeriesRangeItem
                or StubType.BrowseByYearItem
                or StubType.BrowseByKanaRow
                or StubType.StudioItem
                or StubType.TagItem
                or StubType.RatingItem
                or StubType.PersonItem
                or StubType.FavoriteSeries
                or StubType.CurrentlyAiring
                => DlnaImageBrowseContext.VirtualList,

            StubType.RecentlyAddedEpisodes
                or StubType.RecentlyReleasedEpisodes
                or StubType.RecentlyModifiedEpisodes
                or StubType.ContinueWatching
                or StubType.NextUp
                or StubType.FavoriteEpisodes
                => DlnaImageBrowseContext.EpisodeList,

            StubType.Albums
                or StubType.AlbumArtists
                or StubType.Artists
                or StubType.Songs
                or StubType.FavoriteAlbums
                or StubType.FavoriteArtists
                or             StubType.FavoriteSongs
                or StubType.Playlists
                => DlnaImageBrowseContext.MusicList,

            _ => DlnaImageBrowseContext.Default
        };
}
