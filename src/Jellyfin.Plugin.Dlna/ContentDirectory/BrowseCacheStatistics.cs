namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Statistics for the browse response cache.
/// </summary>
/// <param name="EntryCount">The number of cached entries.</param>
/// <param name="EstimatedBytes">The estimated memory usage in bytes.</param>
public readonly record struct BrowseCacheStatistics(int EntryCount, long EstimatedBytes);
