using System;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna;

/// <summary>
/// Applies plugin debug logging rules to the logging pipeline.
/// </summary>
public static class DlnaLoggingFilter
{
    /// <summary>
    /// Determines whether a log entry should be emitted.
    /// </summary>
    /// <param name="category">The logger category.</param>
    /// <param name="level">The log level.</param>
    /// <param name="debugState">The debug logging state.</param>
    /// <returns>True when the entry should be logged.</returns>
    public static bool ShouldLog(string? category, LogLevel level, DlnaDebugLoggingState debugState)
    {
        if (level > LogLevel.Debug)
        {
            return true;
        }

        if (!DlnaLoggingCategories.IsVerboseCategory(category))
        {
            return true;
        }

        return debugState.IsEnabled;
    }
}
