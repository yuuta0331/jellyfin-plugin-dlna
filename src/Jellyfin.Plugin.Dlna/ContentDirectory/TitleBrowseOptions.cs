using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.Dlna.Configuration;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Runtime options for title browse classification.
/// </summary>
internal sealed class TitleBrowseOptions
{
    /// <summary>
    /// Gets the active preset groups in evaluation order.
    /// </summary>
    public IReadOnlyList<TitleBrowseGroup> Groups { get; init; } = Array.Empty<TitleBrowseGroup>();

    /// <summary>
    /// Gets regex patterns to strip from titles before classification.
    /// </summary>
    public IReadOnlyList<string> TitleStripRegexes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the catch-all group id.
    /// </summary>
    public string OtherGroupId { get; init; } = TitleBrowsePresetDefaults.OtherGroupId;

    /// <summary>
    /// Gets a value indicating whether the preset uses kana row matching.
    /// </summary>
    public bool UsesKanaRowMatching { get; init; }

    /// <summary>
    /// Creates options from plugin configuration for a library.
    /// </summary>
    public static TitleBrowseOptions FromConfiguration(DlnaPluginConfiguration configuration, Guid libraryId)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        TitleBrowseConfigurationHelper.EnsurePresetsSeeded(configuration);

        var preset = TitleBrowseConfigurationHelper.ResolvePreset(configuration, libraryId);
        var groups = preset.Groups?.Where(static g => !string.IsNullOrWhiteSpace(g.Id)).ToArray()
            ?? Array.Empty<TitleBrowseGroup>();

        if (groups.Length == 0)
        {
            preset = TitleBrowsePresetDefaults.CreateAlphabetPreset();
            groups = preset.Groups;
        }

        return new TitleBrowseOptions
        {
            Groups = groups,
            TitleStripRegexes = TitleBrowseConfigurationHelper.GetTitleStripRegexes(configuration, libraryId),
            OtherGroupId = TitleBrowseConfigurationHelper.GetOtherGroupId(preset),
            UsesKanaRowMatching = groups.Any(static g => g.MatchMode == TitleBrowseMatchMode.KanaRow)
        };
    }
}
