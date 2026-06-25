using System;
using System.Collections.Concurrent;
using System.Globalization;
using Jellyfin.Plugin.Dlna.Configuration;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// In-memory cache for browse child nodes between virtual index and DIDL XML cache.
/// </summary>
public sealed class BrowseNodeCache : IBrowseNodeCache
{
    private readonly ConcurrentDictionary<string, CachedBrowseNodes> _entries = new();
    private readonly Func<DlnaPluginConfiguration> _getConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrowseNodeCache"/> class.
    /// </summary>
    public BrowseNodeCache()
        : this(() => DlnaPlugin.Instance.Configuration)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BrowseNodeCache"/> class.
    /// </summary>
    /// <param name="getConfiguration">Provides the current plugin configuration.</param>
    public BrowseNodeCache(Func<DlnaPluginConfiguration> getConfiguration)
    {
        _getConfiguration = getConfiguration;
    }

    /// <inheritdoc />
    public bool TryGet(BrowseCacheKey key, out BrowseNodeCacheEntry entry)
    {
        entry = default;
        if (!_entries.TryGetValue(NormalizeKey(key).ToString(), out var cached))
        {
            return false;
        }

        var config = _getConfiguration();
        if (!config.EnableBrowseNodeCache)
        {
            return false;
        }

        var ttlSeconds = config.BrowseNodeCacheTtlSeconds;
        if (ttlSeconds > 0 && DateTime.UtcNow - cached.CreatedAtUtc > TimeSpan.FromSeconds(ttlSeconds))
        {
            _entries.TryRemove(NormalizeKey(key).ToString(), out _);
            return false;
        }

        entry = cached.Entry;
        return true;
    }

    /// <inheritdoc />
    public void Set(BrowseCacheKey key, BrowseNodeCacheEntry entry)
    {
        var config = _getConfiguration();
        if (!config.EnableBrowseNodeCache)
        {
            return;
        }

        var normalizedKey = NormalizeKey(key);
        if (BrowseCacheKeyNormalizer.IsLoopbackServerBase(normalizedKey.ServerBase))
        {
            return;
        }

        _entries[normalizedKey.ToString()] = new CachedBrowseNodes(entry, DateTime.UtcNow);
    }

    /// <inheritdoc />
    public void InvalidateAll()
    {
        _entries.Clear();
    }

    /// <inheritdoc />
    public void InvalidateLibrary(Guid libraryId)
    {
        var prefix = libraryId.ToString("N", CultureInfo.InvariantCulture);
        foreach (var key in _entries.Keys)
        {
            if (key.Contains(prefix, StringComparison.OrdinalIgnoreCase))
            {
                _entries.TryRemove(key, out _);
            }
        }
    }

    /// <inheritdoc />
    public BrowseCacheStatistics GetStatistics()
    {
        long estimatedBytes = 0;
        foreach (var pair in _entries)
        {
            estimatedBytes += pair.Key.Length * sizeof(char);
            foreach (var node in pair.Value.Entry.Nodes)
            {
                estimatedBytes += (node.ClientId.Length + node.Title.Length + node.UpnpClass.Length) * sizeof(char);
                estimatedBytes += (node.ParentId?.Length ?? 0) * sizeof(char);
                estimatedBytes += (node.AlbumArtUri?.Length ?? 0) * sizeof(char);
                estimatedBytes += (node.IconUri?.Length ?? 0) * sizeof(char);
            }
        }

        return new BrowseCacheStatistics(_entries.Count, estimatedBytes);
    }

    private static BrowseCacheKey NormalizeKey(BrowseCacheKey key)
        => key with
        {
            SortCriteria = BrowseCacheKeyNormalizer.NormalizeSortCriteria(key.SortCriteria),
            Filter = BrowseCacheKeyNormalizer.NormalizeFilter(key.Filter)
        };

    private readonly record struct CachedBrowseNodes(BrowseNodeCacheEntry Entry, DateTime CreatedAtUtc);
}
