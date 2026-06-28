using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.Dlna.Configuration;

/// <summary>
/// Resolves title browse presets and per-library overrides from plugin configuration.
/// </summary>
public static class TitleBrowseConfigurationHelper
{
    /// <summary>
    /// Ensures built-in presets are present in the configuration.
    /// </summary>
    public static void EnsurePresetsSeeded(DlnaPluginConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (configuration.TitleBrowsePresets is null || configuration.TitleBrowsePresets.Length == 0)
        {
            configuration.TitleBrowsePresets = TitleBrowsePresetDefaults.CreateBuiltInPresets();
        }
        else
        {
            MergeBuiltInPresets(configuration);
        }

        if (string.IsNullOrWhiteSpace(configuration.ActiveTitleBrowsePresetId))
        {
            configuration.ActiveTitleBrowsePresetId = TitleBrowsePresetDefaults.AlphabetPresetId;
        }
    }

    /// <summary>
    /// Resolves the effective preset for a library.
    /// </summary>
    public static TitleBrowsePreset ResolvePreset(DlnaPluginConfiguration configuration, Guid libraryId)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        EnsurePresetsSeeded(configuration);

        var libraryOverride = GetLibraryOverride(configuration, libraryId);
        if (libraryOverride?.CustomGroups is { Length: > 0 })
        {
            return new TitleBrowsePreset
            {
                Id = libraryOverride.PresetId ?? configuration.ActiveTitleBrowsePresetId,
                NameJa = "カスタム",
                NameEn = "Custom",
                IsBuiltIn = false,
                Groups = libraryOverride.CustomGroups
            };
        }

        var presetId = libraryOverride?.PresetId ?? configuration.ActiveTitleBrowsePresetId;
        return FindPreset(configuration, presetId)
            ?? FindPreset(configuration, TitleBrowsePresetDefaults.AlphabetPresetId)
            ?? TitleBrowsePresetDefaults.CreateAlphabetPreset();
    }

    /// <summary>
    /// Gets title strip regex patterns for a library.
    /// </summary>
    public static IReadOnlyList<string> GetTitleStripRegexes(DlnaPluginConfiguration configuration, Guid libraryId)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var libraryOverride = GetLibraryOverride(configuration, libraryId);
        return libraryOverride?.TitleStripRegexes?
            .Where(static r => !string.IsNullOrWhiteSpace(r))
            .Select(static r => r.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray()
            ?? Array.Empty<string>();
    }

    /// <summary>
    /// Finds a preset by id.
    /// </summary>
    public static TitleBrowsePreset? FindPreset(DlnaPluginConfiguration configuration, string presetId)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        EnsurePresetsSeeded(configuration);
        return configuration.TitleBrowsePresets.FirstOrDefault(p => string.Equals(p.Id, presetId, StringComparison.Ordinal));
    }

    /// <summary>
    /// Gets the other group id from a preset, falling back to the default.
    /// </summary>
    public static string GetOtherGroupId(TitleBrowsePreset preset)
    {
        var other = preset.Groups.LastOrDefault(g => g.MatchMode == TitleBrowseMatchMode.Other);
        return other?.Id ?? TitleBrowsePresetDefaults.OtherGroupId;
    }

    private static LibraryTitleBrowseOverride? GetLibraryOverride(DlnaPluginConfiguration configuration, Guid libraryId)
    {
        if (libraryId == Guid.Empty || configuration.LibraryTitleBrowseOverrides is null)
        {
            return null;
        }

        return configuration.LibraryTitleBrowseOverrides
            .FirstOrDefault(o => o.LibraryId == libraryId);
    }

    private static void MergeBuiltInPresets(DlnaPluginConfiguration configuration)
    {
        var presets = configuration.TitleBrowsePresets.ToList();
        foreach (var builtIn in TitleBrowsePresetDefaults.CreateBuiltInPresets())
        {
            var existingIndex = presets.FindIndex(p => string.Equals(p.Id, builtIn.Id, StringComparison.Ordinal));
            if (existingIndex >= 0)
            {
                presets[existingIndex].IsBuiltIn = true;
                if (presets[existingIndex].Groups is null || presets[existingIndex].Groups.Length == 0)
                {
                    presets[existingIndex].Groups = builtIn.Groups;
                }
            }
            else
            {
                presets.Add(builtIn);
            }
        }

        configuration.TitleBrowsePresets = presets.ToArray();
    }
}
