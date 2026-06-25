using System;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Caches generated DLNA Browse responses.
/// </summary>
public interface IBrowseResponseCache
{
    /// <summary>
    /// Tries to get a cached Browse response.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="entry">The cached entry, if found.</param>
    /// <returns>True when the entry was found and is still valid.</returns>
    bool TryGet(BrowseCacheKey key, out BrowseCacheEntry entry);

    /// <summary>
    /// Stores a Browse response in the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="entry">The entry to store.</param>
    void Set(BrowseCacheKey key, BrowseCacheEntry entry);

    /// <summary>
    /// Clears all cached Browse responses.
    /// </summary>
    void InvalidateAll();

    /// <summary>
    /// Clears cached Browse responses for a library.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    void InvalidateLibrary(Guid libraryId);

    /// <summary>
    /// Clears cached Browse responses whose ObjectID starts with the given prefix.
    /// </summary>
    /// <param name="objectIdPrefix">The ObjectID prefix.</param>
    void InvalidatePattern(string objectIdPrefix);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Cache statistics.</returns>
    BrowseCacheStatistics GetStatistics();
}
