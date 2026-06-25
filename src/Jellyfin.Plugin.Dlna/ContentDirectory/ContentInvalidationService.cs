using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Indexing;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Debounces library change invalidation and applies selective cache clearing.
/// </summary>
public sealed class ContentInvalidationService : IContentInvalidationService, IDisposable
{
    private readonly IBrowseResponseCache _browseResponseCache;
    private readonly IBrowseNodeCache _browseNodeCache;
    private readonly ChildCountCache _childCountCache;
    private readonly IDlnaVirtualIndexService _indexService;
    private readonly IDlnaBrowsePrewarmService _prewarmService;
    private readonly IBrowseMetrics _browseMetrics;
    private readonly ILogger<ContentInvalidationService> _logger;
    private readonly ConcurrentDictionary<Guid, byte> _pendingLibraries = new();
    private readonly Lock _timerLock = new();
    private Timer? _debounceTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentInvalidationService"/> class.
    /// </summary>
    public ContentInvalidationService(
        IBrowseResponseCache browseResponseCache,
        IBrowseNodeCache browseNodeCache,
        ChildCountCache childCountCache,
        IDlnaVirtualIndexService indexService,
        IDlnaBrowsePrewarmService prewarmService,
        IBrowseMetrics browseMetrics,
        ILogger<ContentInvalidationService> logger)
    {
        _browseResponseCache = browseResponseCache;
        _browseNodeCache = browseNodeCache;
        _childCountCache = childCountCache;
        _indexService = indexService;
        _prewarmService = prewarmService;
        _browseMetrics = browseMetrics;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Guid> PendingLibraries => _pendingLibraries.Keys.ToArray();

    /// <inheritdoc />
    public void InvalidateAll(string reason)
    {
        _browseMetrics.RecordInvalidation(full: true);
        _browseResponseCache.InvalidateAll();
        _browseNodeCache.InvalidateAll();
        _childCountCache.InvalidateAll();
        _indexService.InvalidateAll();
        _pendingLibraries.Clear();
        _logger.LogInformation("DLNA cache/index full invalidation: {Reason}", reason);
        ScheduleIndexRebuild(null);
    }

    /// <inheritdoc />
    public void InvalidateLibrary(Guid libraryId, string reason)
    {
        _browseMetrics.RecordInvalidation(full: false);
        _browseResponseCache.InvalidateLibrary(libraryId);
        _browseNodeCache.InvalidateLibrary(libraryId);
        _childCountCache.InvalidateAll();
        _indexService.InvalidateLibrary(libraryId);
        _pendingLibraries.TryRemove(libraryId, out _);
        _logger.LogInformation("DLNA cache/index library invalidation LibraryId={LibraryId} Reason={Reason}", libraryId, reason);
        ScheduleIndexRebuild(libraryId);
    }

    /// <inheritdoc />
    public void ScheduleLibraryInvalidation(Guid libraryId, string reason)
    {
        var config = DlnaPlugin.Instance.Configuration;
        if (!config.DebounceLibraryChangeInvalidation)
        {
            InvalidateLibrary(libraryId, reason);
            return;
        }

        _pendingLibraries[libraryId] = 0;
        var debounceSeconds = Math.Max(5, config.LibraryChangeDebounceSeconds);
        lock (_timerLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(
                _ => FlushPending(reason),
                null,
                TimeSpan.FromSeconds(debounceSeconds),
                Timeout.InfiniteTimeSpan);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_timerLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }
    }

    private void FlushPending(string reason)
    {
        foreach (var libraryId in _pendingLibraries.Keys.ToList())
        {
            InvalidateLibrary(libraryId, reason);
        }
    }

    private void ScheduleIndexRebuild(Guid? libraryId)
    {
        if (!DlnaPlugin.Instance.Configuration.RebuildIndexAfterLibraryScan)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                if (libraryId is Guid id)
                {
                    await _indexService.RebuildLibraryAsync(id, CancellationToken.None).ConfigureAwait(false);
                    await _prewarmService.PrewarmAsync(id, CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    await _indexService.RebuildAllAsync(null, CancellationToken.None).ConfigureAwait(false);
                    await _prewarmService.PrewarmAsync(null, CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rebuild DLNA index after invalidation");
            }
        });
    }
}
