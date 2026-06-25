using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using Jellyfin.Plugin.Dlna.Configuration;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// In-memory cache for generated DLNA Browse responses.
/// </summary>
public sealed class BrowseResponseCache : IBrowseResponseCache
{
    private readonly ConcurrentDictionary<string, CachedBrowseResponse> _entries = new();
    private readonly Func<DlnaPluginConfiguration> _getConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrowseResponseCache"/> class.
    /// </summary>
    public BrowseResponseCache()
        : this(() => DlnaPlugin.Instance.Configuration)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BrowseResponseCache"/> class.
    /// </summary>
    /// <param name="getConfiguration">Provides the current plugin configuration.</param>
    public BrowseResponseCache(Func<DlnaPluginConfiguration> getConfiguration)
    {
        _getConfiguration = getConfiguration;
    }

    /// <inheritdoc />
    public bool TryGet(BrowseCacheKey key, out BrowseCacheEntry entry)
    {
        entry = default;

        if (!_entries.TryGetValue(NormalizeKey(key).ToString(), out var cached))
        {
            return false;
        }

        var config = _getConfiguration();
        if (!IsCacheEnabled(config))
        {
            return false;
        }

        var ttlSeconds = config.BrowseResponseCacheTtlSeconds;
        if (ttlSeconds > 0 && DateTime.UtcNow - cached.CreatedAtUtc > TimeSpan.FromSeconds(ttlSeconds))
        {
            _entries.TryRemove(NormalizeKey(key).ToString(), out _);
            return false;
        }

        entry = cached.Entry;
        return true;
    }

    /// <inheritdoc />
    public void Set(BrowseCacheKey key, BrowseCacheEntry entry)
    {
        var config = _getConfiguration();
        if (!IsCacheEnabled(config))
        {
            return;
        }

        var normalizedKey = NormalizeKey(key);
        if (BrowseCacheKeyNormalizer.IsLoopbackServerBase(normalizedKey.ServerBase))
        {
            return;
        }

        _entries[normalizedKey.ToString()] = new CachedBrowseResponse(entry, DateTime.UtcNow);
    }

    /// <inheritdoc />
    public void InvalidateAll()
    {
        _entries.Clear();
    }

    /// <inheritdoc />
    public void InvalidateLibrary(Guid libraryId)
    {
        var libraryToken = libraryId.ToString("N", CultureInfo.InvariantCulture);
        var keysToRemove = _entries.Keys
            .Where(key => key.Contains(libraryToken, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _entries.TryRemove(key, out _);
        }
    }

    /// <inheritdoc />
    public void InvalidatePattern(string objectIdPrefix)
    {
        if (string.IsNullOrEmpty(objectIdPrefix))
        {
            return;
        }

        var keysToRemove = _entries.Keys
            .Where(key => MatchesObjectIdPrefix(key, objectIdPrefix))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _entries.TryRemove(key, out _);
        }
    }

    /// <inheritdoc />
    public BrowseCacheStatistics GetStatistics()
    {
        long estimatedBytes = 0;
        foreach (var pair in _entries)
        {
            estimatedBytes += pair.Key.Length * sizeof(char);
            estimatedBytes += pair.Value.Entry.DidlXml.Length * sizeof(char);
        }

        return new BrowseCacheStatistics(_entries.Count, estimatedBytes);
    }

    private static bool MatchesObjectIdPrefix(string cacheKey, string objectIdPrefix)
    {
        var separatorIndex = cacheKey.IndexOf('|', StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return false;
        }

        var secondSeparatorIndex = cacheKey.IndexOf('|', separatorIndex + 1);
        if (secondSeparatorIndex < 0)
        {
            return false;
        }

        var objectId = cacheKey[(separatorIndex + 1)..secondSeparatorIndex];
        return objectId.StartsWith(objectIdPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCacheEnabled(DlnaPluginConfiguration config)
        => config.EnableBrowseResponseCache || config.EnableQuestCompatibilityMode;

    private static BrowseCacheKey NormalizeKey(BrowseCacheKey key)
        => key with
        {
            SortCriteria = BrowseCacheKeyNormalizer.NormalizeSortCriteria(key.SortCriteria),
            Filter = BrowseCacheKeyNormalizer.NormalizeFilter(key.Filter)
        };

    private sealed record CachedBrowseResponse(BrowseCacheEntry Entry, DateTime CreatedAtUtc);
}
