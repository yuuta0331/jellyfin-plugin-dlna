namespace Jellyfin.Plugin.Dlna.Configuration;

/// <summary>
/// How a title browse group matches titles.
/// </summary>
public enum TitleBrowseMatchMode
{
    /// <summary>
    /// Matches when the normalized title starts with one of the configured prefixes.
    /// </summary>
    Prefix = 0,

    /// <summary>
    /// Matches when a regex matches at the start of the normalized title.
    /// </summary>
    Regex = 1,

    /// <summary>
    /// Matches using gojuon row classification (KanaRowIndex 0-11).
    /// </summary>
    KanaRow = 2,

    /// <summary>
    /// Catch-all bucket for unmatched titles. Must be the last group in a preset.
    /// </summary>
    Other = 3,
}
