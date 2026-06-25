using System;
using System.Collections.Generic;
using System.Globalization;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// A lightweight cached browse child node.
/// </summary>
/// <param name="ClientId">The DLNA object id.</param>
/// <param name="Title">The display title.</param>
/// <param name="UpnpClass">The UPnP class string.</param>
/// <param name="IsFolder">Whether the node is a folder/container.</param>
/// <param name="ChildCount">Optional child count for folders.</param>
/// <param name="ParentId">Optional parent object id.</param>
/// <param name="AlbumArtUri">Optional album art URI.</param>
/// <param name="IconUri">Optional icon URI.</param>
public readonly record struct BrowseNodeRecord(
    string ClientId,
    string Title,
    string UpnpClass,
    bool IsFolder,
    int? ChildCount,
    string? ParentId,
    string? AlbumArtUri = null,
    string? IconUri = null);

/// <summary>
/// Cached browse child nodes for a parent folder.
/// </summary>
/// <param name="Nodes">The child nodes.</param>
/// <param name="TotalMatches">The total matches count.</param>
public readonly record struct BrowseNodeCacheEntry(
    IReadOnlyList<BrowseNodeRecord> Nodes,
    int TotalMatches);

/// <summary>
/// In-memory cache for browse child nodes (layer 3).
/// </summary>
public interface IBrowseNodeCache
{
    /// <summary>
    /// Tries to get a cached entry.
    /// </summary>
    bool TryGet(BrowseCacheKey key, out BrowseNodeCacheEntry entry);

    /// <summary>
    /// Stores an entry.
    /// </summary>
    void Set(BrowseCacheKey key, BrowseNodeCacheEntry entry);

    /// <summary>
    /// Clears all entries.
    /// </summary>
    void InvalidateAll();

    /// <summary>
    /// Clears entries for a library.
    /// </summary>
    void InvalidateLibrary(Guid libraryId);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Cache statistics.</returns>
    BrowseCacheStatistics GetStatistics();
}
