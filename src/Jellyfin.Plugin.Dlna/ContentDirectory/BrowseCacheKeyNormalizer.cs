using System;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Normalizes browse cache key components so equivalent DLNA requests share a cache entry.
/// </summary>
public static class BrowseCacheKeyNormalizer
{
    /// <summary>
    /// Normalizes a sort criteria string for cache key use.
    /// </summary>
    /// <param name="sortCriteria">The raw sort criteria from the request.</param>
    /// <returns>The normalized sort criteria.</returns>
    public static string NormalizeSortCriteria(string? sortCriteria)
    {
        if (string.IsNullOrWhiteSpace(sortCriteria))
        {
            return string.Empty;
        }

        return sortCriteria.Trim();
    }

    /// <summary>
    /// Normalizes a filter string for cache key use.
    /// </summary>
    /// <param name="filter">The raw filter from the request.</param>
    /// <returns>The normalized filter.</returns>
    public static string NormalizeFilter(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return "*";
        }

        return filter.Trim();
    }

    /// <summary>
    /// Normalizes the server base URL used in generated DIDL image links.
    /// </summary>
    /// <param name="serverAddress">The request server address.</param>
    /// <returns>The normalized server base.</returns>
    public static string NormalizeServerBase(string? serverAddress)
    {
        if (string.IsNullOrWhiteSpace(serverAddress))
        {
            return string.Empty;
        }

        return serverAddress.Trim().TrimEnd('/').ToLowerInvariant();
    }

    /// <summary>
    /// Returns whether a normalized server base points to loopback.
    /// </summary>
    /// <param name="serverBase">The normalized server base.</param>
    /// <returns>True when the address is loopback.</returns>
    public static bool IsLoopbackServerBase(string serverBase)
    {
        if (string.IsNullOrEmpty(serverBase))
        {
            return false;
        }

        return serverBase.Contains("127.0.0.1", StringComparison.Ordinal)
            || serverBase.Contains("localhost", StringComparison.Ordinal)
            || serverBase.StartsWith("[::1]", StringComparison.Ordinal);
    }
}
