using System;

namespace Jellyfin.Plugin.Dlna;

/// <summary>
/// Identifies logger categories controlled by <see cref="DlnaDebugLoggingState"/>.
/// </summary>
public static class DlnaLoggingCategories
{
    private static readonly string[] VerboseCategoryPrefixes =
    [
        "Jellyfin.Plugin.Dlna",
        "Rssdp",
        "System.Net.Http.HttpClient.Dlna"
    ];

    /// <summary>
    /// Returns whether the category is subject to plugin debug logging control.
    /// </summary>
    /// <param name="category">The logger category name.</param>
    /// <returns>True when debug/trace output is gated.</returns>
    public static bool IsVerboseCategory(string? category)
    {
        if (string.IsNullOrEmpty(category))
        {
            return false;
        }

        foreach (var prefix in VerboseCategoryPrefixes)
        {
            if (category.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
