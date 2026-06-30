using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Coordinates debounced, batched DLNA index rebuilds.
/// </summary>
public interface IDlnaIndexRebuildCoordinator
{
    /// <summary>
    /// Gets libraries waiting for a debounced rebuild.
    /// </summary>
    IReadOnlyCollection<Guid> DirtyLibraries { get; }

    /// <summary>
    /// Gets all libraries waiting for cache invalidation or index rebuild work.
    /// </summary>
    IReadOnlyCollection<Guid> PendingLibraries { get; }

    /// <summary>
    /// Marks a library dirty and schedules a debounced rebuild.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    /// <param name="reason">The change reason.</param>
    void MarkLibraryDirty(Guid libraryId, string reason);

    /// <summary>
    /// Marks all DLNA libraries dirty and schedules a debounced rebuild.
    /// </summary>
    /// <param name="reason">The change reason.</param>
    void MarkAllLibrariesDirty(string reason);

    /// <summary>
    /// Schedules a debounced cache-only invalidation for dirty libraries when automatic rebuild is disabled.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    /// <param name="reason">The change reason.</param>
    void MarkLibraryForCacheInvalidation(Guid libraryId, string reason);

    /// <summary>
    /// Schedules debounced cache-only invalidation for all libraries.
    /// </summary>
    /// <param name="reason">The change reason.</param>
    void MarkAllLibrariesForCacheInvalidation(string reason);

    /// <summary>
    /// Removes queued work for one library.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    void ClearPendingLibrary(Guid libraryId);

    /// <summary>
    /// Removes all queued work.
    /// </summary>
    void ClearPendingWork();
}
