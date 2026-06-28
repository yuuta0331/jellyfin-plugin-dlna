namespace Jellyfin.Plugin.Dlna.Configuration;

/// <summary>
/// A single virtual folder within a title browse preset.
/// </summary>
public class TitleBrowseGroup
{
    /// <summary>
    /// Gets or sets the stable group identifier (e.g. "a", "row-0", "other").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Japanese display label.
    /// </summary>
    public string LabelJa { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the English display label.
    /// </summary>
    public string LabelEn { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how titles are matched into this group.
    /// </summary>
    public TitleBrowseMatchMode MatchMode { get; set; }

    /// <summary>
    /// Gets or sets prefix strings for <see cref="TitleBrowseMatchMode.Prefix"/> matching.
    /// </summary>
#pragma warning disable CA1819
    public string[] Prefixes { get; set; } = [];
#pragma warning restore CA1819

    /// <summary>
    /// Gets or sets the regex pattern for <see cref="TitleBrowseMatchMode.Regex"/> matching.
    /// </summary>
    public string? RegexPattern { get; set; }

    /// <summary>
    /// Gets or sets the kana row index (0-11) for <see cref="TitleBrowseMatchMode.KanaRow"/> matching.
    /// </summary>
    public int? KanaRowIndex { get; set; }
}
