namespace Jellyfin.Plugin.Dlna.Configuration;

/// <summary>
/// Image source for episode list browse folders.
/// </summary>
public enum EpisodeListImageSource
{
    /// <summary>
    /// Use images owned by the episode item, falling back to the parent series when missing.
    /// </summary>
    Episode = 0,

    /// <summary>
    /// Use images owned by the parent series, falling back to the episode when missing.
    /// </summary>
    Series = 1
}
