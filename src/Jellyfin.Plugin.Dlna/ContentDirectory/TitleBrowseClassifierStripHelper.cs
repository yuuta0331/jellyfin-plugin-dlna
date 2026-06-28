using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Shared regex stripping helper for title classification.
/// </summary>
internal static class TitleBrowseClassifierStripHelper
{
    private const int MaxStripIterations = 5;

    /// <summary>
    /// Strips leading portions of text matching the supplied regex patterns.
    /// </summary>
    public static string StripWithRegexes(string text, IReadOnlyList<string> regexPatterns)
    {
        if (regexPatterns.Count == 0)
        {
            return text;
        }

        var compiled = CompileStripRegexes(regexPatterns);
        if (compiled.Count == 0)
        {
            return text;
        }

        var result = text;
        for (var iteration = 0; iteration < MaxStripIterations; iteration++)
        {
            var index = 0;
            while (index < result.Length && KanaTitleClassifier.IsLeadingSkippable(result[index]))
            {
                index++;
            }

            if (index > 0)
            {
                result = result[index..];
            }

            if (result.Length == 0)
            {
                break;
            }

            var stripped = false;
            foreach (var regex in compiled)
            {
                var match = regex.Match(result);
                if (match.Success && match.Index == 0 && match.Length > 0)
                {
                    result = result[match.Length..];
                    stripped = true;
                    break;
                }
            }

            if (!stripped)
            {
                break;
            }
        }

        return result;
    }

    private static List<Regex> CompileStripRegexes(IReadOnlyList<string> regexPatterns)
    {
        var compiled = new List<Regex>(regexPatterns.Count);
        foreach (var pattern in regexPatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                continue;
            }

            try
            {
                compiled.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase));
            }
            catch (ArgumentException)
            {
                // Skip invalid patterns.
            }
        }

        return compiled;
    }
}
