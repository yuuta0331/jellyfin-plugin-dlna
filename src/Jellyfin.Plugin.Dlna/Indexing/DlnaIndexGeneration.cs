using System.Threading;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Tracks virtual index generation for cache keys.
/// </summary>
public sealed class DlnaIndexGeneration
{
    private int _generation;

    /// <summary>
    /// Gets the current generation.
    /// </summary>
    public int Value => Volatile.Read(ref _generation);

    /// <summary>
    /// Increments the generation.
    /// </summary>
    public void Increment() => Interlocked.Increment(ref _generation);

    /// <summary>
    /// Resets the generation.
    /// </summary>
    public void Reset() => Interlocked.Exchange(ref _generation, 0);
}
