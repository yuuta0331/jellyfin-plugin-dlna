namespace Jellyfin.Plugin.Dlna.Configuration;

/// <summary>
/// A preset defining title browse virtual folder groups.
/// </summary>
public class TitleBrowsePreset
{
    /// <summary>
    /// Gets or sets the preset identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Japanese preset name.
    /// </summary>
    public string NameJa { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the English preset name.
    /// </summary>
    public string NameEn { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this preset is built-in and cannot be deleted.
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// Gets or sets the virtual folder groups in evaluation order.
    /// </summary>
#pragma warning disable CA1819
    public TitleBrowseGroup[] Groups { get; set; } = [];
#pragma warning restore CA1819
}
