using System;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna;

/// <summary>
/// Wraps an <see cref="ILogger"/> and suppresses debug/trace output unless plugin debug logging is enabled.
/// </summary>
public sealed class DlnaGatedLogger : ILogger
{
    private readonly ILogger _inner;
    private readonly DlnaDebugLoggingState _debugState;

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaGatedLogger"/> class.
    /// </summary>
    /// <param name="inner">The inner logger.</param>
    /// <param name="debugState">The debug logging state.</param>
    public DlnaGatedLogger(ILogger inner, DlnaDebugLoggingState debugState)
    {
        _inner = inner;
        _debugState = debugState;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => _inner.BeginScope(state);

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        if (IsGatedLevel(logLevel) && !_debugState.IsEnabled)
        {
            return false;
        }

        return _inner.IsEnabled(logLevel);
    }

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (IsGatedLevel(logLevel) && !_debugState.IsEnabled)
        {
            return;
        }

        _inner.Log(logLevel, eventId, state, exception, formatter);
    }

    private static bool IsGatedLevel(LogLevel logLevel)
        => logLevel == LogLevel.Debug || logLevel == LogLevel.Trace;
}
