using System;
using System.Globalization;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Identifies a cached DLNA Browse response.
/// </summary>
/// <param name="UserId">The browsing user id.</param>
/// <param name="ObjectId">The browsed object id.</param>
/// <param name="BrowseFlag">The browse flag.</param>
/// <param name="SortCriteria">The sort criteria string.</param>
/// <param name="Filter">The filter string.</param>
/// <param name="DeviceProfileId">The device profile id.</param>
/// <param name="ServerBase">The normalized server base URL embedded in DIDL links.</param>
/// <param name="ConfigFingerprint">The browse-related configuration fingerprint.</param>
/// <param name="StartIndex">The applied start index.</param>
/// <param name="Limit">The applied item limit, if any.</param>
public readonly record struct BrowseCacheKey(
    Guid? UserId,
    string ObjectId,
    string BrowseFlag,
    string SortCriteria,
    string Filter,
    string DeviceProfileId,
    string ServerBase,
    int ConfigFingerprint,
    int StartIndex,
    int? Limit)
{
    /// <inheritdoc />
    public override string ToString()
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{UserId}|{ObjectId}|{BrowseFlag}|{SortCriteria}|{Filter}|{DeviceProfileId}|{ServerBase}|{ConfigFingerprint}|{StartIndex}|{Limit}");
}

/// <summary>
/// A cached DLNA Browse response payload.
/// </summary>
/// <param name="DidlXml">The generated DIDL-Lite XML fragment.</param>
/// <param name="NumberReturned">The NumberReturned value.</param>
/// <param name="TotalMatches">The TotalMatches value.</param>
/// <param name="UpdateId">The UpdateID value.</param>
public readonly record struct BrowseCacheEntry(
    string DidlXml,
    int NumberReturned,
    int TotalMatches,
    int UpdateId);
