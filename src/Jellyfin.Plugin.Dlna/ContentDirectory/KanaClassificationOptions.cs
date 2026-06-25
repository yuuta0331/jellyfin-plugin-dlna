using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Jellyfin.Plugin.Dlna.Configuration;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Options for kana row title classification.
/// </summary>
internal sealed class KanaClassificationOptions
{
    /// <summary>
    /// Gets a value indicating whether known title prefixes should be stripped before classification.
    /// </summary>
    public bool EnablePrefixStripping { get; init; }

    /// <summary>
    /// Gets the title prefixes to strip, ordered longest first.
    /// </summary>
    public IReadOnlyList<string> Prefixes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creates options from plugin configuration.
    /// </summary>
    public static KanaClassificationOptions FromConfiguration(DlnaPluginConfiguration configuration)
    {
        var prefixes = configuration.KanaTitlePrefixes?
            .Where(static p => !string.IsNullOrWhiteSpace(p))
            .Select(static p => p.Trim().Normalize(NormalizationForm.FormKC))
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(static p => p.Length)
            .ToArray() ?? Array.Empty<string>();

        return new KanaClassificationOptions
        {
            EnablePrefixStripping = configuration.EnableKanaPrefixStripping,
            Prefixes = prefixes
        };
    }

    /// <summary>
    /// Default classification options.
    /// </summary>
    public static KanaClassificationOptions Default { get; } = FromConfiguration(new DlnaPluginConfiguration());
}
