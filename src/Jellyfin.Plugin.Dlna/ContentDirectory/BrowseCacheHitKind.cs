namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Indicates which browse cache layer served a response, if any.
/// </summary>
public enum BrowseCacheHitKind
{
    /// <summary>
    /// No browse cache hit; response was generated.
    /// </summary>
    None = 0,

    /// <summary>
    /// Layer 4 response cache hit.
    /// </summary>
    Response = 1,

    /// <summary>
    /// Layer 3 node cache hit.
    /// </summary>
    Node = 2
}
