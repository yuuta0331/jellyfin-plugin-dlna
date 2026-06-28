using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.Dlna.Configuration;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Classifies media titles into configurable title browse groups.
/// </summary>
internal static class TitleBrowseClassifier
{
    /// <summary>
    /// Classifies a title into a browse group id.
    /// </summary>
    public static string Classify(string? sortName, string? name, TitleBrowseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.UsesKanaRowMatching)
        {
            return ClassifyWithKanaPriority(sortName, name, options);
        }

        var sortNameGroup = ClassifySingle(sortName, options);
        if (!string.IsNullOrEmpty(sortNameGroup))
        {
            return sortNameGroup;
        }

        var nameGroup = ClassifySingle(name, options);
        return nameGroup ?? options.OtherGroupId;
    }

    /// <summary>
    /// Returns true when the title belongs to the given group.
    /// </summary>
    public static bool MatchesGroup(string? sortName, string? name, string groupId, TitleBrowseOptions options)
        => string.Equals(Classify(sortName, name, options), groupId, StringComparison.Ordinal);

    private static string ClassifyWithKanaPriority(string? sortName, string? name, TitleBrowseOptions options)
    {
        var sortNameRow = KanaTitleClassifier.ClassifyKanaRowIndex(sortName, options, out var sortNameStartsWithKanji);
        var nameRow = KanaTitleClassifier.ClassifyKanaRowIndex(name, options, out var nameStartsWithKanji);
        var rowIndex = KanaTitleClassifier.ChooseBestRow(sortNameRow, nameRow, sortNameStartsWithKanji, nameStartsWithKanji);

        if (rowIndex is >= 0 and <= TitleBrowsePresetDefaults.LegacyOtherRowIndex)
        {
            var legacyGroupId = TitleBrowsePresetDefaults.LegacyRowGroupId(rowIndex.Value);
            if (options.Groups.Any(g => string.Equals(g.Id, legacyGroupId, StringComparison.Ordinal)))
            {
                return legacyGroupId;
            }
        }

        return options.OtherGroupId;
    }

    private static string? ClassifySingle(string? source, TitleBrowseOptions options)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var normalized = KanaTitleClassifier.NormalizeString(source);
        normalized = TitleBrowseClassifierStripHelper.StripWithRegexes(normalized, options.TitleStripRegexes);

        foreach (var group in options.Groups)
        {
            if (group.MatchMode == TitleBrowseMatchMode.Other)
            {
                continue;
            }

            if (MatchesGroupDefinition(normalized, group))
            {
                return group.Id;
            }
        }

        return null;
    }

    private static bool MatchesGroupDefinition(string normalized, TitleBrowseGroup group)
    {
        return group.MatchMode switch
        {
            TitleBrowseMatchMode.Prefix => MatchesPrefix(normalized, group.Prefixes),
            TitleBrowseMatchMode.Regex => MatchesRegex(normalized, group.RegexPattern),
            TitleBrowseMatchMode.KanaRow => MatchesKanaRowGroup(normalized, group.KanaRowIndex),
            TitleBrowseMatchMode.Other => true,
            _ => false
        };
    }

    private static bool MatchesPrefix(string normalized, IReadOnlyList<string> prefixes)
    {
        if (prefixes.Count == 0)
        {
            return false;
        }

        var index = SkipLeading(normalized);
        if (index >= normalized.Length)
        {
            return false;
        }

        var remainder = normalized[index..];
        foreach (var prefix in prefixes.OrderByDescending(static p => p.Length))
        {
            if (string.IsNullOrEmpty(prefix))
            {
                continue;
            }

            if (remainder.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }

            if (ContainsAsciiLetter(prefix)
                && remainder.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesRegex(string normalized, string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(normalized, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool MatchesKanaRowGroup(string normalized, int? kanaRowIndex)
    {
        if (kanaRowIndex is null)
        {
            return false;
        }

        var row = KanaTitleClassifier.ClassifyNormalizedFirstCharacter(normalized, out _);
        return row == kanaRowIndex.Value;
    }

    private static int SkipLeading(string text)
    {
        var index = 0;
        while (index < text.Length && KanaTitleClassifier.IsLeadingSkippable(text[index]))
        {
            index++;
        }

        return index;
    }

    private static bool ContainsAsciiLetter(string value)
    {
        foreach (var c in value)
        {
            if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
            {
                return true;
            }
        }

        return false;
    }
}
