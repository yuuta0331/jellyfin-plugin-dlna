namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Facet index categories.
/// </summary>
public enum FacetType
{
    /// <summary>
    /// Studio facet.
    /// </summary>
    Studio,

    /// <summary>
    /// Tag facet.
    /// </summary>
    Tag,

    /// <summary>
    /// Official rating facet.
    /// </summary>
    Rating,

    /// <summary>
    /// Extra videos facet.
    /// </summary>
    Extra,

    /// <summary>
    /// Genre facet.
    /// </summary>
    Genre,

    /// <summary>
    /// Production year facet.
    /// </summary>
    Year,

    /// <summary>
    /// Seasons under a series (facet key is parent series id).
    /// </summary>
    SeasonOfSeries,

    /// <summary>
    /// Episodes under a season (facet key is parent season id).
    /// </summary>
    EpisodeOfSeason,

    /// <summary>
    /// Music genre facet (album ids per genre name).
    /// </summary>
    MusicGenre,

    /// <summary>
    /// Person facet (series/movie ids per person name).
    /// </summary>
    Person
}
