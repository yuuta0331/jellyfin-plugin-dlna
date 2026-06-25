using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Manages DLNA virtual folder indexes.
/// </summary>
public interface IDlnaVirtualIndexService
{
    /// <summary>
    /// Gets the index generation counter.
    /// </summary>
    DlnaIndexGeneration Generation { get; }

    /// <summary>
    /// Returns whether a library index is ready.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    /// <returns>True when indexed.</returns>
    bool IsReady(Guid libraryId);

    /// <summary>
    /// Rebuilds indexes for all libraries.
    /// </summary>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task RebuildAllAsync(IProgress<double>? progress, CancellationToken cancellationToken);

    /// <summary>
    /// Rebuilds indexes for one library.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task RebuildLibraryAsync(Guid libraryId, CancellationToken cancellationToken);

    /// <summary>
    /// Invalidates all indexes.
    /// </summary>
    void InvalidateAll();

    /// <summary>
    /// Invalidates one library index.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    void InvalidateLibrary(Guid libraryId);
}
