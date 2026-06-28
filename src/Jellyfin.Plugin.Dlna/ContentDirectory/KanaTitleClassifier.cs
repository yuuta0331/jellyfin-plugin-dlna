using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Classifies media titles into gojuon browse rows.
/// </summary>
internal static class KanaTitleClassifier
{
    private static readonly Dictionary<char, int> RowByBaseKana = BuildRowMap();

    /// <summary>
    /// Legacy row index for alphanumeric titles.
    /// </summary>
    public const int AlphanumericRowIndex = 10;

    /// <summary>
    /// Legacy row index for unclassified titles.
    /// </summary>
    public const int OtherRowIndex = 11;

    /// <summary>
    /// Classifies a normalized title string into a kana row index from the first classifiable character.
    /// </summary>
    public static int ClassifyNormalizedFirstCharacter(string normalized, out bool startsWithKanji)
        => ClassifyFirstCharacter(normalized, out startsWithKanji);

    /// <summary>
    /// Classifies a title into a kana browse row index using title browse options.
    /// </summary>
    public static int? ClassifyKanaRowIndex(string? source, TitleBrowseOptions options, out bool startsWithKanji)
    {
        startsWithKanji = false;
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var normalized = NormalizeString(source);
        normalized = TitleBrowseClassifierStripHelper.StripWithRegexes(normalized, options.TitleStripRegexes);
        return ClassifyFirstCharacter(normalized, out startsWithKanji);
    }

    /// <summary>
    /// Chooses the best kana row from SortName and Name evaluation results.
    /// </summary>
    public static int? ChooseBestRow(
        int? sortNameRow,
        int? nameRow,
        bool sortNameStartsWithKanji,
        bool nameStartsWithKanji)
    {
        if (sortNameRow is >= 0 and <= 9)
        {
            return sortNameRow.Value;
        }

        if (nameRow is >= 0 and <= 9)
        {
            return nameRow.Value;
        }

        if (sortNameRow == AlphanumericRowIndex && !nameStartsWithKanji)
        {
            return AlphanumericRowIndex;
        }

        if (nameRow == AlphanumericRowIndex)
        {
            return AlphanumericRowIndex;
        }

        if (nameStartsWithKanji)
        {
            return nameRow ?? OtherRowIndex;
        }

        return sortNameRow ?? nameRow ?? OtherRowIndex;
    }

    /// <summary>
    /// Normalizes a title string for classification.
    /// </summary>
    public static string NormalizeString(string input)
    {
        var nfkc = input.Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(nfkc.Length);
        foreach (var c in nfkc)
        {
            builder.Append(NormalizeKanaChar(c));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Returns true when the character should be skipped at the start of a title.
    /// </summary>
    public static bool IsLeadingSkippable(char c)
    {
        if (char.IsWhiteSpace(c))
        {
            return true;
        }

        return CharUnicodeInfo.GetUnicodeCategory(c) switch
        {
            UnicodeCategory.OpenPunctuation => true,
            UnicodeCategory.ClosePunctuation => true,
            UnicodeCategory.InitialQuotePunctuation => true,
            UnicodeCategory.FinalQuotePunctuation => true,
            UnicodeCategory.OtherPunctuation => true,
            UnicodeCategory.DashPunctuation => true,
            UnicodeCategory.MathSymbol => true,
            UnicodeCategory.CurrencySymbol => true,
            UnicodeCategory.ModifierSymbol => true,
            UnicodeCategory.OtherSymbol => true,
            _ => false
        };
    }

    private static int ClassifyFirstCharacter(string normalized, out bool startsWithKanji)
    {
        startsWithKanji = false;
        for (var i = 0; i < normalized.Length; i++)
        {
            var c = NormalizeKanaChar(normalized[i]);
            if (IsLeadingSkippable(c))
            {
                continue;
            }

            if (IsKanji(c))
            {
                startsWithKanji = true;
                return OtherRowIndex;
            }

            if (IsAlphanumeric(c))
            {
                return AlphanumericRowIndex;
            }

            if (IsHiragana(c))
            {
                var baseKana = ToBaseKana(c);
                if (RowByBaseKana.TryGetValue(baseKana, out var rowIndex))
                {
                    return rowIndex;
                }
            }

            return OtherRowIndex;
        }

        return OtherRowIndex;
    }

    private static char NormalizeKanaChar(char c)
    {
        return c switch
        {
            'ゑ' or 'ヱ' => 'え',
            'ゐ' or 'ヰ' => 'い',
            '\u30F4' or '\u3094' => 'う',
            >= '\u30A1' and <= '\u30F6' => (char)(c - 0x60),
            _ => c
        };
    }

    private static char ToBaseKana(char c)
    {
        c = c switch
        {
            'ぁ' => 'あ',
            'ぃ' => 'い',
            'ぅ' => 'う',
            'ぇ' => 'え',
            'ぉ' => 'お',
            'っ' => 'つ',
            'ゃ' => 'や',
            'ゅ' => 'ゆ',
            'ょ' => 'よ',
            'ゎ' => 'わ',
            'が' or 'ぎ' or 'ぐ' or 'げ' or 'ご' => (char)(c - 1),
            'ざ' or 'じ' or 'ず' or 'ぜ' or 'ぞ' => (char)(c - 1),
            'だ' or 'ぢ' or 'づ' or 'で' or 'ど' => (char)(c - 1),
            'ば' or 'び' or 'ぶ' or 'べ' or 'ぼ' => (char)(c - 1),
            'ぱ' or 'ぴ' or 'ぷ' or 'ぺ' or 'ぽ' => (char)(c - 2),
            _ => c
        };

        return c;
    }

    private static bool IsKanji(char c)
        => c is >= '\u4E00' and <= '\u9FFF';

    private static bool IsAlphanumeric(char c)
        => (c >= 'A' && c <= 'Z')
           || (c >= 'a' && c <= 'z')
           || (c >= '0' && c <= '9');

    private static bool IsHiragana(char c)
        => c is >= '\u3041' and <= '\u309F';

    private static Dictionary<char, int> BuildRowMap()
    {
        var map = new Dictionary<char, int>();
        AddRow(map, 0, "あいうえお");
        AddRow(map, 1, "かきくけこ");
        AddRow(map, 2, "さしすせそ");
        AddRow(map, 3, "たちつてと");
        AddRow(map, 4, "なにぬねの");
        AddRow(map, 5, "はひふへほ");
        AddRow(map, 6, "まみむめも");
        AddRow(map, 7, "やゆよ");
        AddRow(map, 8, "らりるれろ");
        AddRow(map, 9, "わをん");
        return map;
    }

    private static void AddRow(Dictionary<char, int> map, int rowIndex, string chars)
    {
        foreach (var c in chars)
        {
            map[c] = rowIndex;
        }
    }
}
