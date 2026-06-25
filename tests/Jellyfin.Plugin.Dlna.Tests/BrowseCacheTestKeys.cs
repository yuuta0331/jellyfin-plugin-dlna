using System;
using Jellyfin.Plugin.Dlna.ContentDirectory;

namespace Jellyfin.Plugin.Dlna.Tests;

internal static class BrowseCacheTestKeys
{
    internal const string ServerBase = "http://testserver";

    internal static BrowseCacheKey Create(
        string objectId = "series_abc",
        string deviceProfileId = "profile",
        string serverBase = ServerBase,
        Guid? userId = null)
        => new(
            userId,
            objectId,
            "BrowseDirectChildren",
            string.Empty,
            "*",
            deviceProfileId,
            serverBase,
            42,
            0,
            null);
}
