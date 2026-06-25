namespace Jellyfin.Plugin.Dlna.Configuration;

/// <summary>
/// Controls how folder childCount attributes are calculated for DLNA Browse responses.
/// </summary>
public enum ChildCountMode
{
    /// <summary>
    /// Omit the childCount attribute entirely.
    /// </summary>
    Disabled,

    /// <summary>
    /// Use cached or placeholder values without querying child contents.
    /// </summary>
    Estimate,

    /// <summary>
    /// Query child item counts accurately (slow for large folders).
    /// </summary>
    Accurate
}
