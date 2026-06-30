using System;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Normalizes DLNA stream container values from route paths and query parameters.
/// </summary>
public static class DlnaStreamContainerNormalizer
{
    /// <summary>
    /// Normalizes a route or path container value.
    /// DLNA clients often probe with comma-separated extensions such as <c>mov,mp4,m4a,3gp,3g2,mj2</c>.
    /// </summary>
    public static string? NormalizeRouteContainer(string? container)
    {
        if (string.IsNullOrWhiteSpace(container))
        {
            return container;
        }

        var normalized = container.Trim().TrimStart('.');
        var commaIndex = normalized.IndexOf(',', StringComparison.Ordinal);
        if (commaIndex >= 0)
        {
            normalized = normalized[..commaIndex];
        }

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized.ToLowerInvariant();
    }

    /// <summary>
    /// Returns true when the container value is a DLNA capability probe listing multiple formats.
    /// </summary>
    public static bool IsDlnaCapabilityProbe(string? container)
        => !string.IsNullOrWhiteSpace(container) && container.Contains(',', StringComparison.Ordinal);
}
