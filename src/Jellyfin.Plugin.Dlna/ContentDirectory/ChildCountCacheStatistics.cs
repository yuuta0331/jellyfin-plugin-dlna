namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Statistics for the child count cache.
/// </summary>
/// <param name="EntryCount">The number of cached entries.</param>
public readonly record struct ChildCountCacheStatistics(int EntryCount);
