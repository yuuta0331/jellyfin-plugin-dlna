namespace Jellyfin.Plugin.Dlna.Configuration;

/// <summary>
/// Controls how many Browse paths are pre-generated during prewarm.
/// </summary>
public enum PrewarmScope
{
    /// <summary>
    /// Root, libraries, and first-page series/movies lists only (Quest-friendly).
    /// </summary>
    Minimal = 0,

    /// <summary>
    /// All configured virtual folders and facets.
    /// </summary>
    Full = 1
}
