using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Coordinates selective cache and index invalidation.
/// </summary>
public interface IContentInvalidationService
{
    /// <summary>
    /// Invalidates all caches and indexes.
    /// </summary>
    void InvalidateAll(string reason);

    /// <summary>
    /// Invalidates caches for a specific library.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    /// <param name="reason">The invalidation reason.</param>
    void InvalidateLibrary(Guid libraryId, string reason);

    /// <summary>
    /// Schedules a debounced library invalidation.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    /// <param name="reason">The invalidation reason.</param>
    void ScheduleLibraryInvalidation(Guid libraryId, string reason);

    /// <summary>
    /// Gets libraries pending debounced invalidation.
    /// </summary>
    IReadOnlyCollection<Guid> PendingLibraries { get; }
}
