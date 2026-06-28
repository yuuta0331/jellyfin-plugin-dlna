using System;
using Jellyfin.Plugin.Dlna.Configuration;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Fingerprint helpers for title browse configuration.
/// </summary>
internal static class TitleBrowseConfigurationFingerprint
{
    /// <summary>
    /// Adds preset and override content to a configuration fingerprint.
    /// </summary>
    public static int AddPresetContent(DlnaPluginConfiguration config)
    {
        var hash = new HashCode();
        TitleBrowseConfigurationHelper.EnsurePresetsSeeded(config);

        foreach (var preset in config.TitleBrowsePresets)
        {
            hash.Add(preset.Id);
            hash.Add(preset.NameJa);
            hash.Add(preset.NameEn);
            hash.Add(preset.IsBuiltIn);
            foreach (var group in preset.Groups)
            {
                hash.Add(group.Id);
                hash.Add(group.LabelJa);
                hash.Add(group.LabelEn);
                hash.Add((int)group.MatchMode);
                hash.Add(group.RegexPattern);
                hash.Add(group.KanaRowIndex);
                hash.Add(group.Prefixes?.Length ?? 0);
                if (group.Prefixes is not null)
                {
                    foreach (var prefix in group.Prefixes)
                    {
                        hash.Add(prefix);
                    }
                }
            }
        }

        if (config.LibraryTitleBrowseOverrides is not null)
        {
            foreach (var libraryOverride in config.LibraryTitleBrowseOverrides)
            {
                hash.Add(libraryOverride.LibraryId);
                hash.Add(libraryOverride.PresetId);
                hash.Add(libraryOverride.TitleStripRegexes?.Length ?? 0);
                if (libraryOverride.TitleStripRegexes is not null)
                {
                    foreach (var regex in libraryOverride.TitleStripRegexes)
                    {
                        hash.Add(regex);
                    }
                }
            }
        }

        return hash.ToHashCode();
    }
}
