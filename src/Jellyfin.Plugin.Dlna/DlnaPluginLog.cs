using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Plugin.Dlna;

/// <summary>
/// Gates verbose DLNA plugin logging behind <see cref="Configuration.DlnaPluginConfiguration.EnableDebugLogging"/>.
/// </summary>
public static class DlnaPluginLog
{
    /// <summary>
    /// Gets a value indicating whether debug logging is enabled.
    /// </summary>
    public static bool IsDebugEnabled => DlnaDebugLoggingState.Current?.IsEnabled == true;

    /// <summary>
    /// Gets a logger for Jellyfin core helpers (e.g. <c>StreamBuilder</c>) that emit verbose debug output.
    /// Returns <see cref="NullLogger.Instance"/> when debug logging is disabled.
    /// </summary>
    /// <param name="logger">The caller's logger.</param>
    /// <returns>A logger suitable for verbose third-party dependencies.</returns>
    public static ILogger VerboseDependencyLogger(ILogger logger)
        => IsDebugEnabled ? logger : NullLogger.Instance;

    /// <summary>
    /// Writes a debug log entry when debug logging is enabled.
    /// </summary>
    public static void Debug(ILogger logger, string message, params object?[] args)
    {
        if (IsDebugEnabled)
        {
#pragma warning disable CA2254
            logger.LogDebug(message, args);
#pragma warning restore CA2254
        }
    }

    /// <summary>
    /// Writes a debug log entry with an exception when debug logging is enabled.
    /// </summary>
    public static void Debug(ILogger logger, Exception exception, string message, params object?[] args)
    {
        if (IsDebugEnabled)
        {
#pragma warning disable CA2254
            logger.LogDebug(exception, message, args);
#pragma warning restore CA2254
        }
    }

    /// <summary>
    /// Writes a per-browse performance summary when debug logging is enabled.
    /// </summary>
    public static void BrowsePerformance(ILogger logger, string summary)
    {
        if (IsDebugEnabled)
        {
            logger.LogInformation("[DLNA Browse] {Summary}", summary);
        }
    }
}
