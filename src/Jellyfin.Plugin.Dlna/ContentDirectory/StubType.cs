namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// DLNA item types.
/// </summary>
public enum StubType
{
    /// <summary>
    /// Folder stub.
    /// </summary>
    Folder = 0,

    /// <summary>
    /// Latest stub.
    /// </summary>
    Latest = 2,

    /// <summary>
    /// Playlists stub.
    /// </summary>
    Playlists = 3,

    /// <summary>
    /// Albums stub.
    /// </summary>
    Albums = 4,

    /// <summary>
    /// AlbumArtists stub.
    /// </summary>
    AlbumArtists = 5,

    /// <summary>
    /// Artists stub.
    /// </summary>
    Artists = 6,

    /// <summary>
    /// Songs stub.
    /// </summary>
    Songs = 7,

    /// <summary>
    /// Genres stub.
    /// </summary>
    Genres = 8,

    /// <summary>
    /// FavoriteSongs stub.
    /// </summary>
    FavoriteSongs = 9,

    /// <summary>
    /// FavoriteArtists stub.
    /// </summary>
    FavoriteArtists = 10,

    /// <summary>
    /// FavoriteAlbums stub.
    /// </summary>
    FavoriteAlbums = 11,

    /// <summary>
    /// ContinueWatching stub.
    /// </summary>
    ContinueWatching = 12,

    /// <summary>
    /// Movies stub.
    /// </summary>
    Movies = 13,

    /// <summary>
    /// Collections stub.
    /// </summary>
    Collections = 14,

    /// <summary>
    /// Favorites stub.
    /// </summary>
    Favorites = 15,

    /// <summary>
    /// NextUp stub.
    /// </summary>
    NextUp = 16,

    /// <summary>
    /// Series stub.
    /// </summary>
    Series = 17,

    /// <summary>
    /// FavoriteSeries stub.
    /// </summary>
    FavoriteSeries = 18,

    /// <summary>
    /// FavoriteEpisodes stub.
    /// </summary>
    FavoriteEpisodes = 19,

    /// <summary>
    /// RecentlyAddedEpisodes stub.
    /// </summary>
    RecentlyAddedEpisodes = 20,

    /// <summary>
    /// RecentlyAddedSeries stub.
    /// </summary>
    RecentlyAddedSeries = 21,

    /// <summary>
    /// RecentlyReleasedEpisodes stub.
    /// </summary>
    RecentlyReleasedEpisodes = 22,

    /// <summary>
    /// Extras stub.
    /// </summary>
    Extras = 23,

    /// <summary>
    /// RecentlyAddedMovies stub.
    /// </summary>
    RecentlyAddedMovies = 24,

    /// <summary>
    /// RecentlyReleasedMovies stub.
    /// </summary>
    RecentlyReleasedMovies = 25,

    /// <summary>
    /// ThreeDMovies stub.
    /// </summary>
    ThreeDMovies = 26,

    /// <summary>
    /// FourKMovies stub.
    /// </summary>
    FourKMovies = 27,

    /// <summary>
    /// EightKMovies stub.
    /// </summary>
    EightKMovies = 28,

    /// <summary>
    /// VrMovies stub.
    /// </summary>
    VrMovies = 29,

    /// <summary>
    /// EightKVrMovies stub.
    /// </summary>
    EightKVrMovies = 30,

    /// <summary>
    /// RecentlyReleasedSeries stub.
    /// </summary>
    RecentlyReleasedSeries = 31,

    /// <summary>
    /// CurrentlyAiring stub.
    /// </summary>
    CurrentlyAiring = 32,

    /// <summary>
    /// BrowseByKana stub (parent folder listing kana rows).
    /// </summary>
    BrowseByKana = 33,

    /// <summary>
    /// BrowseByKanaRow stub (items within a kana row).
    /// </summary>
    BrowseByKanaRow = 34,

    /// <summary>
    /// BrowseByYear stub (parent folder listing years).
    /// </summary>
    BrowseByYear = 35,

    /// <summary>
    /// BrowseByYearItem stub (items within a production year).
    /// </summary>
    BrowseByYearItem = 36,

    /// <summary>
    /// Recently updated series stub (sorted by latest episode DateCreated).
    /// </summary>
    RecentlyUpdatedSeries = 37,

    /// <summary>
    /// Browse by studio parent folder.
    /// </summary>
    BrowseByStudio = 38,

    /// <summary>
    /// Browse by tag parent folder.
    /// </summary>
    BrowseByTag = 39,

    /// <summary>
    /// Browse by rating parent folder.
    /// </summary>
    BrowseByRating = 40,

    /// <summary>
    /// Studio facet folder.
    /// </summary>
    StudioItem = 41,

    /// <summary>
    /// Tag facet folder.
    /// </summary>
    TagItem = 42,

    /// <summary>
    /// Rating facet folder.
    /// </summary>
    RatingItem = 43,

    /// <summary>
    /// Series range folder (e.g. 0001-0500).
    /// </summary>
    SeriesRange = 44,

    /// <summary>
    /// Series within a range folder.
    /// </summary>
    SeriesRangeItem = 45,

    /// <summary>
    /// Browse by person parent folder.
    /// </summary>
    BrowseByPerson = 46,

    /// <summary>
    /// Person facet folder.
    /// </summary>
    PersonItem = 47,

    /// <summary>
    /// Recently modified series (metadata/image updates).
    /// </summary>
    RecentlyModifiedSeries = 48,

    /// <summary>
    /// Recently modified movies (metadata/image updates).
    /// </summary>
    RecentlyModifiedMovies = 49,

    /// <summary>
    /// Recently modified episodes (metadata/image updates).
    /// </summary>
    RecentlyModifiedEpisodes = 50,

    /// <summary>
    /// Home videos stub (all videos in a home videos library).
    /// </summary>
    Videos = 51,

    /// <summary>
    /// Photos stub (home videos and photos library).
    /// </summary>
    Photos = 52,

    /// <summary>
    /// Music videos stub (all music videos in a music videos library).
    /// </summary>
    MusicVideos = 53
}
