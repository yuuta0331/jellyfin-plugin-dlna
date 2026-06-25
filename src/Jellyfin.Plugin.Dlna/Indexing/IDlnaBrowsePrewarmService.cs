using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Pre-generates DLNA Browse responses to warm the response cache.
/// </summary>
public interface IDlnaBrowsePrewarmService
{
    /// <summary>
    /// Prewarms Browse responses for indexed libraries.
    /// </summary>
    /// <param name="libraryId">Optional library id; when null, all DLNA libraries are prewarmed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task PrewarmAsync(Guid? libraryId, CancellationToken cancellationToken);
}
