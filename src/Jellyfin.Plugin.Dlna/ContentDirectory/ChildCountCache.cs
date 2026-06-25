using System;
using System.Collections.Concurrent;
using System.Globalization;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Caches accurate folder childCount values per user and object.
/// </summary>
public sealed class ChildCountCache
{
    private readonly ConcurrentDictionary<string, int> _counts = new();

    /// <summary>
    /// Tries to get a cached childCount.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="objectId">The DLNA object id.</param>
    /// <param name="sortCriteria">The browse sort criteria.</param>
    /// <returns>The cached count, if present.</returns>
    public int? TryGet(Guid? userId, string objectId, string sortCriteria)
    {
        if (_counts.TryGetValue(BuildKey(userId, objectId, sortCriteria), out var count))
        {
            return count;
        }

        return null;
    }

    /// <summary>
    /// Stores a childCount in the cache.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="objectId">The DLNA object id.</param>
    /// <param name="sortCriteria">The browse sort criteria.</param>
    /// <param name="count">The count to store.</param>
    public void Set(Guid? userId, string objectId, string sortCriteria, int count)
    {
        _counts[BuildKey(userId, objectId, sortCriteria)] = count;
    }

    /// <summary>
    /// Clears all cached counts.
    /// </summary>
    public void InvalidateAll()
    {
        _counts.Clear();
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Cache statistics.</returns>
    public ChildCountCacheStatistics GetStatistics()
        => new(_counts.Count);

    private static string BuildKey(Guid? userId, string objectId, string sortCriteria)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{userId}:{objectId}:{sortCriteria}");
}
