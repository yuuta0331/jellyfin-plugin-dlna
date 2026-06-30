using System;
using System.Collections.Generic;
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
    private readonly IDlnaIndexRebuildCoordinator _rebuildCoordinator;
    private readonly IBrowseMetrics _browseMetrics;
    private readonly ILogger<ContentInvalidationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentInvalidationService"/> class.
    /// </summary>
    public ContentInvalidationService(
        IBrowseResponseCache browseResponseCache,
        IBrowseNodeCache browseNodeCache,
        ChildCountCache childCountCache,
        IDlnaVirtualIndexService indexService,
        IDlnaIndexRebuildCoordinator rebuildCoordinator,
        IBrowseMetrics browseMetrics,
        ILogger<ContentInvalidationService> logger)
    {
        _browseResponseCache = browseResponseCache;
        _browseNodeCache = browseNodeCache;
        _childCountCache = childCountCache;
        _indexService = indexService;
        _rebuildCoordinator = rebuildCoordinator;
        _browseMetrics = browseMetrics;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Guid> PendingLibraries => _rebuildCoordinator.PendingLibraries;

    /// <inheritdoc />
    public void InvalidateAll(string reason)
    {
        _browseMetrics.RecordInvalidation(full: true);
        _browseResponseCache.InvalidateAll();
        _browseNodeCache.InvalidateAll();
        _childCountCache.InvalidateAll();
        _indexService.InvalidateAll();
        _rebuildCoordinator.ClearPendingWork();
        _logger.LogInformation("DLNA cache/index full invalidation: {Reason}", reason);
    }

    /// <inheritdoc />
    public void InvalidateCachesAndScheduleRebuild(string reason)
    {
        _browseMetrics.RecordInvalidation(full: true);
        _browseResponseCache.InvalidateAll();
        _browseNodeCache.InvalidateAll();
        _childCountCache.InvalidateAll();
        _rebuildCoordinator.MarkAllLibrariesDirty(reason);
        _logger.LogInformation("DLNA caches invalidated and rebuild scheduled: {Reason}", reason);
    }

    /// <inheritdoc />
    public void InvalidateLibrary(Guid libraryId, string reason)
    {
        _browseMetrics.RecordInvalidation(full: false);
        _browseResponseCache.InvalidateLibrary(libraryId);
        _browseNodeCache.InvalidateLibrary(libraryId);
        _childCountCache.InvalidateAll();
        _indexService.InvalidateLibrary(libraryId);
        _rebuildCoordinator.ClearPendingLibrary(libraryId);
        _logger.LogInformation("DLNA cache/index library invalidation LibraryId={LibraryId} Reason={Reason}", libraryId, reason);
    }

    /// <inheritdoc />
    public void ScheduleLibraryInvalidation(Guid libraryId, string reason)
    {
        _rebuildCoordinator.MarkLibraryDirty(libraryId, reason);
    }

    /// <inheritdoc />
    public void ScheduleAllLibrariesInvalidation(string reason)
        => _rebuildCoordinator.MarkAllLibrariesDirty(reason);

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
