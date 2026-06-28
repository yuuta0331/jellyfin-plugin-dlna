using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Jellyfin.Plugin.Dlna.Configuration;

/// <summary>
/// Built-in title browse preset definitions.
/// </summary>
public static class TitleBrowsePresetDefaults
{
    /// <summary>
    /// Built-in alphabet preset id.
    /// </summary>
    public const string AlphabetPresetId = "alphabet";

    /// <summary>
    /// Built-in Japanese kana preset id.
    /// </summary>
    public const string JapaneseKanaPresetId = "japanese-kana";

    /// <summary>
    /// Legacy kana row index for alphanumeric titles.
    /// </summary>
    public const int LegacyAlphanumericRowIndex = 10;

    /// <summary>
    /// Legacy kana row index for unclassified titles.
    /// </summary>
    public const int LegacyOtherRowIndex = 11;

    /// <summary>
    /// Group id for the catch-all bucket.
    /// </summary>
    public const string OtherGroupId = "other";

    /// <summary>
    /// Group id for numeric titles in the alphabet preset.
    /// </summary>
    public const string DigitsGroupId = "0-9";

    /// <summary>
    /// Prefix for legacy kana row group ids.
    /// </summary>
    public const string LegacyRowGroupPrefix = "row-";

    /// <summary>
    /// Gets the built-in presets.
    /// </summary>
    public static TitleBrowsePreset[] CreateBuiltInPresets()
        => [CreateAlphabetPreset(), CreateJapaneseKanaPreset()];

    /// <summary>
    /// Creates the default alphabet preset (A-Z + 0-9 + Other).
    /// </summary>
    public static TitleBrowsePreset CreateAlphabetPreset()
    {
        var groups = new List<TitleBrowseGroup>();
        for (var i = 0; i < 26; i++)
        {
            var letter = ((char)('A' + i)).ToString(CultureInfo.InvariantCulture);
            var lower = ((char)('a' + i)).ToString(CultureInfo.InvariantCulture);
            groups.Add(new TitleBrowseGroup
            {
                Id = lower,
                LabelJa = letter,
                LabelEn = letter,
                MatchMode = TitleBrowseMatchMode.Prefix,
                Prefixes = [letter, lower]
            });
        }

        groups.Add(new TitleBrowseGroup
        {
            Id = DigitsGroupId,
            LabelJa = "0-9",
            LabelEn = "0-9",
            MatchMode = TitleBrowseMatchMode.Prefix,
            Prefixes = Enumerable.Range(0, 10).Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray()
        });

        groups.Add(CreateOtherGroup());

        return new TitleBrowsePreset
        {
            Id = AlphabetPresetId,
            NameJa = "アルファベット",
            NameEn = "Alphabet",
            IsBuiltIn = true,
            Groups = groups.ToArray()
        };
    }

    /// <summary>
    /// Creates the built-in Japanese kana preset matching the legacy 12-row layout.
    /// </summary>
    public static TitleBrowsePreset CreateJapaneseKanaPreset()
    {
        var rowLabelsJa = new[]
        {
            "あ行", "か行", "さ行", "た行", "な行", "は行", "ま行", "や行", "ら行", "わ行", "英数字", "その他"
        };
        var rowLabelsEn = new[]
        {
            "A row", "Ka row", "Sa row", "Ta row", "Na row", "Ha row", "Ma row", "Ya row", "Ra row", "Wa row", "Alphanumeric", "Other"
        };

        var groups = new TitleBrowseGroup[rowLabelsJa.Length];
        for (var i = 0; i < rowLabelsJa.Length; i++)
        {
            groups[i] = new TitleBrowseGroup
            {
                Id = LegacyRowGroupId(i),
                LabelJa = rowLabelsJa[i],
                LabelEn = rowLabelsEn[i],
                MatchMode = i == LegacyOtherRowIndex ? TitleBrowseMatchMode.Other : TitleBrowseMatchMode.KanaRow,
                KanaRowIndex = i
            };
        }

        return new TitleBrowsePreset
        {
            Id = JapaneseKanaPresetId,
            NameJa = "日本語五十音",
            NameEn = "Japanese Kana",
            IsBuiltIn = true,
            Groups = groups
        };
    }

    /// <summary>
    /// Builds a legacy row group id.
    /// </summary>
    public static string LegacyRowGroupId(int rowIndex)
        => LegacyRowGroupPrefix + rowIndex.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Maps a legacy numeric kana row index to a group id.
    /// </summary>
    public static string LegacyRowIndexToGroupId(int rowIndex)
        => LegacyRowGroupId(rowIndex);

    /// <summary>
    /// Tries to parse a legacy row group id.
    /// </summary>
    public static bool TryParseLegacyRowGroupId(string groupId, out int rowIndex)
    {
        rowIndex = -1;
        if (!groupId.StartsWith(LegacyRowGroupPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        return int.TryParse(groupId[LegacyRowGroupPrefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out rowIndex);
    }

    /// <summary>
    /// Gets the other group definition.
    /// </summary>
    public static TitleBrowseGroup CreateOtherGroup()
        => new()
        {
            Id = OtherGroupId,
            LabelJa = "その他",
            LabelEn = "Other",
            MatchMode = TitleBrowseMatchMode.Other
        };
}
