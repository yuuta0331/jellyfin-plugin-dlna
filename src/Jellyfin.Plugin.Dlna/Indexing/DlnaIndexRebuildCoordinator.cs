using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Debounces library changes and runs batched index rebuilds when the server is idle.
/// </summary>
public sealed class DlnaIndexRebuildCoordinator : IDlnaIndexRebuildCoordinator, IHostedService, IDisposable
{
    private readonly ILibraryManager _libraryManager;
    private readonly IDlnaVirtualIndexService _indexService;
    private readonly IDlnaBrowsePrewarmService _prewarmService;
    private readonly DlnaServerLoadGuard _loadGuard;
    private readonly IBrowseResponseCache _browseResponseCache;
    private readonly IBrowseNodeCache _browseNodeCache;
    private readonly ChildCountCache _childCountCache;
    private readonly IBrowseMetrics _browseMetrics;
    private readonly ILogger<DlnaIndexRebuildCoordinator> _logger;
    private readonly ConcurrentDictionary<Guid, long> _dirtyLibraries = new();
    private readonly ConcurrentDictionary<Guid, long> _cacheOnlyLibraries = new();
    private readonly Lock _timerLock = new();
    private CancellationTokenSource? _debounceCts;
    private int _rebuildRunning;
    private long _workVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaIndexRebuildCoordinator"/> class.
    /// </summary>
    public DlnaIndexRebuildCoordinator(
        ILibraryManager libraryManager,
        IDlnaVirtualIndexService indexService,
        IDlnaBrowsePrewarmService prewarmService,
        DlnaServerLoadGuard loadGuard,
        IBrowseResponseCache browseResponseCache,
        IBrowseNodeCache browseNodeCache,
        ChildCountCache childCountCache,
        IBrowseMetrics browseMetrics,
        ILogger<DlnaIndexRebuildCoordinator> logger)
    {
        _libraryManager = libraryManager;
        _indexService = indexService;
        _prewarmService = prewarmService;
        _loadGuard = loadGuard;
        _browseResponseCache = browseResponseCache;
        _browseNodeCache = browseNodeCache;
        _childCountCache = childCountCache;
        _browseMetrics = browseMetrics;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Guid> DirtyLibraries => _dirtyLibraries.Keys.ToArray();

    /// <inheritdoc />
    public IReadOnlyCollection<Guid> PendingLibraries
        => _dirtyLibraries.Keys.Concat(_cacheOnlyLibraries.Keys).Distinct().ToArray();

    /// <inheritdoc />
    public void MarkLibraryDirty(Guid libraryId, string reason)
    {
        if (!DlnaConfigurationAccessor.Current.ShouldAutomaticallyRebuildIndex())
        {
            MarkLibraryForCacheInvalidation(libraryId, reason);
            return;
        }

        _cacheOnlyLibraries.TryRemove(libraryId, out _);
        _dirtyLibraries[libraryId] = NextWorkVersion();
        if (DlnaConfigurationAccessor.Current.LogIndexDetails)
        {
            _logger.LogInformation("DLNA library marked dirty LibraryId={LibraryId} Reason={Reason}", libraryId, reason);
        }

        ResetDebounceTimer();
    }

    /// <inheritdoc />
    public void MarkAllLibrariesDirty(string reason)
    {
        if (!DlnaConfigurationAccessor.Current.ShouldAutomaticallyRebuildIndex())
        {
            MarkAllLibrariesForCacheInvalidation(reason);
            return;
        }

        _cacheOnlyLibraries.Clear();
        foreach (var library in GetDlnaLibraries())
        {
            _dirtyLibraries[library.Id] = NextWorkVersion();
        }

        if (DlnaConfigurationAccessor.Current.LogIndexDetails)
        {
            _logger.LogInformation("DLNA all libraries marked dirty Reason={Reason}", reason);
        }

        ResetDebounceTimer();
    }

    /// <inheritdoc />
    public void MarkLibraryForCacheInvalidation(Guid libraryId, string reason)
    {
        _dirtyLibraries.TryRemove(libraryId, out _);
        _cacheOnlyLibraries[libraryId] = NextWorkVersion();
        if (DlnaConfigurationAccessor.Current.LogIndexDetails)
        {
            _logger.LogInformation("DLNA library scheduled for cache invalidation LibraryId={LibraryId} Reason={Reason}", libraryId, reason);
        }

        ResetDebounceTimer();
    }

    /// <inheritdoc />
    public void MarkAllLibrariesForCacheInvalidation(string reason)
    {
        _dirtyLibraries.Clear();
        _dirtyLibraries.Clear();
        _cacheOnlyLibraries.Clear();
        foreach (var library in GetDlnaLibraries())
        {
            _cacheOnlyLibraries[library.Id] = NextWorkVersion();
        }

        if (DlnaConfigurationAccessor.Current.LogIndexDetails)
        {
            _logger.LogInformation("DLNA all libraries scheduled for cache invalidation Reason={Reason}", reason);
        }

        ResetDebounceTimer();
    }

    /// <inheritdoc />
    public void ClearPendingLibrary(Guid libraryId)
    {
        _dirtyLibraries.TryRemove(libraryId, out _);
        _cacheOnlyLibraries.TryRemove(libraryId, out _);
    }

    /// <inheritdoc />
    public void ClearPendingWork()
    {
        _dirtyLibraries.Clear();
        _cacheOnlyLibraries.Clear();
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        CancellationTokenSource? debounceCts;
        lock (_timerLock)
        {
            debounceCts = _debounceCts;
            _debounceCts = null;
        }

        if (debounceCts is not null)
        {
            await debounceCts.CancelAsync().ConfigureAwait(false);
            debounceCts.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CancellationTokenSource? debounceCts;
        lock (_timerLock)
        {
            debounceCts = _debounceCts;
            _debounceCts = null;
        }

        debounceCts?.Cancel();
        debounceCts?.Dispose();
    }

    private void ResetDebounceTimer()
    {
        var config = DlnaConfigurationAccessor.Current;
        if (!config.DebounceLibraryChangeInvalidation)
        {
            _ = Task.Run(ProcessPendingWorkAsync);
            return;
        }

        var debounceSeconds = Math.Max(5, config.LibraryChangeDebounceSeconds);
        lock (_timerLock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(debounceSeconds), token).ConfigureAwait(false);
                    await ProcessPendingWorkAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // debounce reset
                }
            }, token);
        }
    }

    private async Task ProcessPendingWorkAsync()
    {
        if (Interlocked.CompareExchange(ref _rebuildRunning, 1, 0) != 0)
        {
            return;
        }

        try
        {
            await ProcessPendingWorkCoreAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref _rebuildRunning, 0);
        }
    }

    private async Task ProcessPendingWorkCoreAsync(CancellationToken cancellationToken)
    {
        var cacheOnly = _cacheOnlyLibraries.ToArray();
        foreach (var pending in cacheOnly)
        {
            InvalidateLibraryCaches(pending.Key);
            _cacheOnlyLibraries.TryRemove(pending);
        }

        if (_dirtyLibraries.IsEmpty)
        {
            return;
        }

        if (!_loadGuard.CanRunIndexWork())
        {
            _logger.LogInformation("DLNA index rebuild deferred: server busy or minimum interval not elapsed");
            ScheduleRetry();
            return;
        }

        var config = DlnaConfigurationAccessor.Current;
        var eligibleLibraries = new List<KeyValuePair<Guid, long>>();
        foreach (var pending in _dirtyLibraries.ToArray())
        {
            if (config.SkipIndexWhenLibraryPathUnavailable && !_loadGuard.IsLibraryPathAvailable(pending.Key))
            {
                _logger.LogInformation("DLNA index rebuild deferred for library {LibraryId}: path unavailable", pending.Key);
                continue;
            }

            InvalidateLibraryCaches(pending.Key);
            eligibleLibraries.Add(pending);
        }

        if (eligibleLibraries.Count == 0)
        {
            ScheduleRetry();
            return;
        }

        var rebuiltLibraries = await _indexService.TryRebuildLibrariesAsync(
            eligibleLibraries.Select(pending => pending.Key).ToArray(),
            cancellationToken).ConfigureAwait(false);
        if (rebuiltLibraries.Count == 0)
        {
            _logger.LogInformation("DLNA index rebuild skipped: another rebuild is already running");
            ScheduleRetry();
            return;
        }

        foreach (var libraryId in rebuiltLibraries)
        {
            var completedWork = eligibleLibraries.First(pending => pending.Key == libraryId);
            _dirtyLibraries.TryRemove(completedWork);
            if (config.LogIndexDetails)
            {
                _logger.LogInformation("DLNA index rebuilt for library {LibraryId}", libraryId);
            }
        }

        _loadGuard.RecordIndexWorkCompleted();

        if (config.PrewarmAfterLibraryRebuild
            && config.PrewarmBrowseResponses
            && _loadGuard.CanRunPrewarm())
        {
            await _prewarmService.PrewarmAsync(null, cancellationToken).ConfigureAwait(false);
        }

        if (!_dirtyLibraries.IsEmpty)
        {
            ScheduleRetry();
        }
    }

    private void InvalidateLibraryCaches(Guid libraryId)
    {
        _browseMetrics.RecordInvalidation(full: false);
        _browseResponseCache.InvalidateLibrary(libraryId);
        _browseNodeCache.InvalidateLibrary(libraryId);
        _childCountCache.InvalidateAll();
    }

    private void ScheduleRetry()
    {
        var retryMinutes = Math.Max(1, DlnaConfigurationAccessor.Current.IndexRebuildRetryMinutes);
        lock (_timerLock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(retryMinutes), token).ConfigureAwait(false);
                    await ProcessPendingWorkAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // superseded
                }
            }, token);
        }
    }

    private long NextWorkVersion() => Interlocked.Increment(ref _workVersion);

    private IReadOnlyList<BaseItem> GetDlnaLibraries()
        => _libraryManager.GetUserRootFolder().Children
            .Where(LibraryBrowseQueryHelper.IsDlnaLibraryView)
            .ToList();
}
