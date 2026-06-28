using System;

namespace Jellyfin.Plugin.Dlna.Configuration;

/// <summary>
/// Per-library overrides for title browse classification.
/// </summary>
public class LibraryTitleBrowseOverride
{
    /// <summary>
    /// Gets or sets the Jellyfin library folder id.
    /// </summary>
    public Guid LibraryId { get; set; }

    /// <summary>
    /// Gets or sets an optional preset id override for this library.
    /// </summary>
    public string? PresetId { get; set; }

    /// <summary>
    /// Gets or sets regex patterns applied to titles before classification (one per line).
    /// Each pattern is matched at the start and removed when it matches.
    /// </summary>
#pragma warning disable CA1819
    public string[] TitleStripRegexes { get; set; } = [];
#pragma warning restore CA1819

    /// <summary>
    /// Gets or sets optional custom groups that replace the preset groups for this library.
    /// </summary>
#pragma warning disable CA1819
    public TitleBrowseGroup[]? CustomGroups { get; set; }
#pragma warning restore CA1819
}
