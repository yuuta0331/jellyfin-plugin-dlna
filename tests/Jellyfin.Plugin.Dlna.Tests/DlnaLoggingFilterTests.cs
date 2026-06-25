using System;
using Jellyfin.Plugin.Dlna.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for DLNA debug logging filters and state.
/// </summary>
public class DlnaLoggingFilterTests
{
    [Theory]
    [InlineData("Jellyfin.Plugin.Dlna.ContentDirectory.ContentDirectoryService", true)]
    [InlineData("Jellyfin.Plugin.Dlna.Playback.DlnaPlaybackController", true)]
    [InlineData("Rssdp.SsdpCommunicationsServer", true)]
    [InlineData("System.Net.Http.HttpClient.Dlna.LogicalHandler", true)]
    [InlineData("Microsoft.AspNetCore.Hosting.Diagnostics", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsVerboseCategory_MatchesExpectedPrefixes(string? category, bool expected)
    {
        Assert.Equal(expected, DlnaLoggingCategories.IsVerboseCategory(category));
    }

    [Fact]
    public void DebugLoggingState_SyncFrom_UpdatesIsEnabled()
    {
        var state = new DlnaDebugLoggingState();
        var configuration = new DlnaPluginConfiguration
        {
            EnableDebugLogging = true
        };

        state.SyncFrom(configuration);
        Assert.True(state.IsEnabled);

        configuration.EnableDebugLogging = false;
        state.SyncFrom(configuration);
        Assert.False(state.IsEnabled);
    }

    [Theory]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Trace)]
    public void LoggingFilter_BlocksVerboseCategoriesWhenDisabled(LogLevel level)
    {
        var state = new DlnaDebugLoggingState();
        state.SyncFrom(new DlnaPluginConfiguration { EnableDebugLogging = false });

        Assert.False(DlnaLoggingFilter.ShouldLog(
            "Jellyfin.Plugin.Dlna.ContentDirectory.ContentDirectoryService",
            level,
            state));
    }

    [Fact]
    public void LoggingFilter_AllowsInformationForVerboseCategoriesWhenDisabled()
    {
        var state = new DlnaDebugLoggingState();
        state.SyncFrom(new DlnaPluginConfiguration { EnableDebugLogging = false });

        Assert.True(DlnaLoggingFilter.ShouldLog(
            "Jellyfin.Plugin.Dlna.Indexing.DlnaIndexBuilder",
            LogLevel.Information,
            state));
    }

    [Fact]
    public void VerboseDependencyLogger_ReturnsNullLoggerWhenDisabled()
    {
        var inner = new CountingLogger { DebugEnabled = true };
        var state = new DlnaDebugLoggingState();
        state.SyncFrom(new DlnaPluginConfiguration { EnableDebugLogging = false });

        var dependencyLogger = DlnaPluginLog.VerboseDependencyLogger(inner);
        dependencyLogger.LogDebug("suppressed");

        Assert.Equal(0, inner.LogCallCount);
        Assert.False(dependencyLogger.IsEnabled(LogLevel.Debug));
    }

    [Fact]
    public void VerboseDependencyLogger_ReturnsInnerLoggerWhenEnabled()
    {
        var inner = new CountingLogger { DebugEnabled = true };
        var state = new DlnaDebugLoggingState();
        state.SyncFrom(new DlnaPluginConfiguration { EnableDebugLogging = true });

        var dependencyLogger = DlnaPluginLog.VerboseDependencyLogger(inner);
        Assert.Same(inner, dependencyLogger);
    }

    [Fact]
    public void GatedLogger_DoesNotLogDebugWhenStateDisabled()
    {
        var inner = new CountingLogger { DebugEnabled = true };
        var state = new DlnaDebugLoggingState();
        state.SyncFrom(new DlnaPluginConfiguration { EnableDebugLogging = false });
        var gated = new DlnaGatedLogger(inner, state);

        Assert.False(gated.IsEnabled(LogLevel.Debug));
        gated.LogDebug("suppressed");

        Assert.Equal(0, inner.LogCallCount);
    }

    [Fact]
    public void GatedLogger_LogsDebugWhenStateEnabled()
    {
        var inner = new CountingLogger { DebugEnabled = true };
        var state = new DlnaDebugLoggingState();
        state.SyncFrom(new DlnaPluginConfiguration { EnableDebugLogging = true });
        var gated = new DlnaGatedLogger(inner, state);

        Assert.True(gated.IsEnabled(LogLevel.Debug));
        gated.LogDebug("allowed");

        Assert.Equal(1, inner.LogCallCount);
    }

    private sealed class CountingLogger : ILogger
    {
        public bool DebugEnabled { get; set; }

        public int LogCallCount { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Debug && DebugEnabled;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            LogCallCount++;
        }
    }
}

internal static class TestLoggerExtensions
{
    public static void LogDebug(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Debug, 0, message, null, static (s, _) => s);
    }
}
